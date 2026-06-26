using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MvCamCtrl.NET;
using QMC.Vision.Core;

namespace QMC.Vision.Cameras.Hik
{
    /// <summary>
    /// HIKVISION GigE 카메라 구현.
    /// <see cref="HikMvsDll"/> 로 SDK 를 동적 로드하고, 리플렉션으로 MyCamera API 호출.
    /// SDK 미설치 시 Open() 에서 즉시 false 상태가 되고, SimCamera 로 대체하면 됨.
    /// <para>
    /// MVS 4.x SDK 는 메서드명에 _NET 접미사, nested 타입은 MyCamera+ 안에 위치한다.
    /// 구 버전 호환을 위해 후보 이름들을 fallback 시퀀스로 시도한다.
    /// </para>
    /// </summary>
    public class HikGigECamera : CameraBase
    {
        private object  _camera;           // MyCamera 인스턴스
        private Thread  _liveThread;
        private volatile bool _liveRun;
        private byte[]  _frameBuf;
        private int     _frameBufSize;
        private object  _frameCallbackDelegate;       // 콜백 보유용 GC 방지
        private MyCamera.cbEventdelegateEx _eventCb;  // ExposureEnd 등 HW 이벤트 콜백 (GC 방지)

        public HikGigECamera(CameraInfo info) : base(info)
        {
            if (string.IsNullOrEmpty(Info.Vendor)) Info.Vendor = "HIKVISION";
            Info.Transport = CameraTransport.GigE;
        }

        // ──────────────────────────────────────────
        //  Open / Close
        // ──────────────────────────────────────────

        public override void Open()
        {
            if (IsOpen) return;
            if (!HikMvsDll.IsLoaded)
                throw new InvalidOperationException(HikMvsDll.GetInstallHint());

            _camera = Activator.CreateInstance(HikMvsDll.MyCameraType);

            var devInfo = FindDeviceInfoByIp(Info.IpAddress);
            if (devInfo == null)
                throw new InvalidOperationException("Device not found: " + Info.IpAddress);

            // CreateDevice_NET(ref MV_CC_DEVICE_INFO) — 4.x; 구 버전: CreateHandle(ref MV_CC_DEVICE_INFO)
            int r = InvokeFirst(new[] { "MV_CC_CreateDevice_NET", "MV_CC_CreateHandle" }, new[] { devInfo });
            EnsureSuccess(r, "MV_CC_CreateDevice");

            // OpenDevice — 인자 없는 오버로드 우선, 실패 시 (Exclusive=1, Key=0) 재시도.
            int TryOpenOnce()
            {
                int rr = InvokeFirst(new[] { "MV_CC_OpenDevice_NET", "MV_CC_OpenDevice" }, Array.Empty<object>());
                if (rr != 0)
                    rr = InvokeFirst(new[] { "MV_CC_OpenDevice_NET", "MV_CC_OpenDevice" }, new object[] { (uint)1, (ushort)0 });
                return rr;
            }

            // 강제 종료(비정상 종료) 후 재시작 시, 카메라가 이전 제어 연결을 GVCP 하트비트 만료까지(보통 수 초)
            // 붙들고 있어 OpenDevice 가 한동안 거부된다 → 만료를 기다리며 자동 재시도(최대 ~7.5초).
            r = TryOpenOnce();
            for (int attempt = 1; r != 0 && attempt <= 5; attempt++)
            {
                System.Threading.Thread.Sleep(1500);
                r = TryOpenOnce();
            }
            EnsureSuccess(r, "MV_CC_OpenDevice");

            // 하트비트 타임아웃을 짧게(3초) — 다음에 비정상 종료돼도 카메라가 더 빨리 제어연결을 해제하도록.
            // (기존 리플렉션 헬퍼 사용 — 미지원 카메라/노드면 조용히 무시)
            TrySetInt("GevHeartbeatTimeout", 3000);

            // OpenDevice 성공 후 초기화 단계에서 예외가 나면 장치가 SDK 레벨에서 열린 채 남아
            // 핸들이 누수되고(다음 Open 이 0x80000203 access 오류), Close() 는 IsOpen=false 라 정리하지 못한다.
            // → 실패 시 여기서 즉시 Close+Destroy 후 재던짐.
            try
            {
                // GigE 패킷 크기 최적화
                TryOptimizePacketSize();

                ApplyInitialParameters();

                // ExposureEnd HW 이벤트 등록 (강타입 — ref MV_EVENT_OUT_INFO delegate)
                TryEnableExposureEndEvent();
            }
            catch
            {
                try { InvokeFirst(new[] { "MV_CC_CloseDevice_NET", "MV_CC_CloseDevice" }, Array.Empty<object>()); } catch { }
                try { InvokeFirst(new[] { "MV_CC_DestroyDevice_NET", "MV_CC_DestroyHandle" }, Array.Empty<object>()); } catch { }
                _camera = null;
                IsOpen = false;
                throw;
            }

            IsOpen = true;
            RaiseConnectionChanged(CameraConnectionEvent.Opened);
        }

        /// <summary>EventSelector=ExposureEnd → EventNotification=On → RegisterEventCallBackEx.
        /// 카메라/모드 미지원 시 조용히 무시 (ExposureEnded 이벤트가 발화되지 않을 뿐).</summary>
        private void TryEnableExposureEndEvent()
        {
            var cam = _camera as MyCamera;
            if (cam == null) return;
            try
            {
                cam.MV_CC_SetEnumValueByString_NET("EventSelector", "ExposureEnd");
                cam.MV_CC_SetEnumValueByString_NET("EventNotification", "On");
                _eventCb = new MyCamera.cbEventdelegateEx(OnHwEvent);
                cam.MV_CC_RegisterEventCallBackEx_NET("ExposureEnd", _eventCb, IntPtr.Zero);
            }
            catch { /* 미지원 카메라 — fallback 은 VisionModule 의 프레임완료 EPD */ }
        }

        private void OnHwEvent(ref MyCamera.MV_EVENT_OUT_INFO info, IntPtr user)
        {
            // 노출 종료 — 전송 완료(GetOneFrame) 전에 도착. 즉시 ExposureEnded 발화.
            RaiseExposureEnded();
        }

        public override void Close()
        {
            StopLive();
            // IsOpen 가 false 여도 _camera 핸들이 살아있으면(부분 오픈/누수) 반드시 SDK 정리한다.
            if (!IsOpen && _camera == null) return;
            try { InvokeFirst(new[] { "MV_CC_CloseDevice_NET", "MV_CC_CloseDevice" }, Array.Empty<object>()); } catch { }
            try { InvokeFirst(new[] { "MV_CC_DestroyDevice_NET", "MV_CC_DestroyHandle" }, Array.Empty<object>()); } catch { }
            IsOpen = false;
            _camera = null;
            _eventCb = null;
            RaiseConnectionChanged(CameraConnectionEvent.Closed);
        }

        // ──────────────────────────────────────────
        //  Grab
        // ──────────────────────────────────────────

        public override GrabResult Grab(int timeoutMs = 3000)
        {
            if (!IsOpen) return GrabResult.Fail("camera not open", Info.Id);

            EnsureFrameBuffer();
            int r = InvokeFirst(new[] { "MV_CC_StartGrabbing_NET", "MV_CC_StartGrabbing" }, Array.Empty<object>());
            if (r != 0) return GrabResult.Fail("MV_CC_StartGrabbing 0x" + r.ToString("X"), Info.Id);

            try
            {
                // Software trigger 모드면 트리거 1발 발사
                if (TriggerMode == CameraTriggerMode.Software)
                    TriggerSoftware();

                var frameInfo = CreateFrameInfoEx();
                GCHandle gch = GCHandle.Alloc(_frameBuf, GCHandleType.Pinned);
                try
                {
                    var args = new object[] { gch.AddrOfPinnedObject(), (uint)_frameBuf.Length, frameInfo, timeoutMs };
                    r = InvokeFirst(new[] { "MV_CC_GetOneFrameTimeout_NET", "MV_CC_GetOneFrameTimeout" }, args);
                    if (r != 0) return GrabResult.Fail("MV_CC_GetOneFrameTimeout 0x" + r.ToString("X"), Info.Id);

                    var bmp = BufferToBitmap(_frameBuf, args[2]);
                    return new GrabResult(bmp, 0, Info.Id);
                }
                finally { gch.Free(); }
            }
            finally
            {
                try { InvokeFirst(new[] { "MV_CC_StopGrabbing_NET", "MV_CC_StopGrabbing" }, Array.Empty<object>()); } catch { }
            }
        }

        public override void StartLive()
        {
            if (!IsOpen || IsGrabbing) return;
            EnsureFrameBuffer();

            // 콜백 등록 시도 (실패하면 polling 으로 fallback)
            bool callbackRegistered = TryRegisterFrameCallback();

            int r = InvokeFirst(new[] { "MV_CC_StartGrabbing_NET", "MV_CC_StartGrabbing" }, Array.Empty<object>());
            if (r != 0) throw new Exception("MV_CC_StartGrabbing fail 0x" + r.ToString("X"));

            IsGrabbing = true;
            if (!callbackRegistered)
            {
                _liveRun = true;
                _liveThread = new Thread(LiveLoop) { IsBackground = true };
                _liveThread.Start();
            }
        }

        public override void StopLive()
        {
            _liveRun = false;
            try { _liveThread?.Join(1000); } catch { }
            _liveThread = null;
            try { InvokeFirst(new[] { "MV_CC_StopGrabbing_NET", "MV_CC_StopGrabbing" }, Array.Empty<object>()); } catch { }
            IsGrabbing = false;
            _frameCallbackDelegate = null;
        }

        public override void TriggerSoftware()
        {
            if (!IsOpen) return;
            // 4.x: MV_CC_TriggerSoftwareExecute_NET(); 구 버전: MV_CC_SetCommandValue("TriggerSoftware")
            int r = InvokeFirst(new[] { "MV_CC_TriggerSoftwareExecute_NET" }, Array.Empty<object>());
            if (r != 0)
                InvokeFirst(new[] { "MV_CC_SetCommandValue_NET", "MV_CC_SetCommandValue" }, new object[] { "TriggerSoftware" });
        }

        // ──────────────────────────────────────────
        //  Parameter hooks
        // ──────────────────────────────────────────

        protected override void OnExposureChanged(double us)
            => TrySetFloat("ExposureTime", (float)us);

        protected override void OnGainChanged(double gainDb)
            => TrySetFloat("Gain", (float)gainDb);

        protected override void OnFrameRateChanged(double fps)
            => TrySetFloat("AcquisitionFrameRate", (float)fps);

        protected override void OnTriggerModeChanged(CameraTriggerMode mode)
        {
            switch (mode)
            {
                case CameraTriggerMode.Continuous:
                    TrySetEnum("TriggerMode", 0);
                    break;
                case CameraTriggerMode.Software:
                    TrySetEnum("TriggerMode",   1);
                    TrySetEnum("TriggerSource", 7); // MV_TRIGGER_SOURCE_SOFTWARE
                    break;
                case CameraTriggerMode.Line0: TrySetEnum("TriggerMode", 1); TrySetEnum("TriggerSource", 0); break;
                case CameraTriggerMode.Line1: TrySetEnum("TriggerMode", 1); TrySetEnum("TriggerSource", 1); break;
                case CameraTriggerMode.Line2: TrySetEnum("TriggerMode", 1); TrySetEnum("TriggerSource", 2); break;
            }
        }

        protected override void OnPixelFormatChanged(CameraPixelFormat fmt)
        {
            string name = fmt.ToString();
            TrySetEnumByName("PixelFormat", name);
        }

        protected override void OnRoiChanged(Rectangle roi)
        {
            if (roi.Width  > 0) TrySetInt("Width",   roi.Width);
            if (roi.Height > 0) TrySetInt("Height",  roi.Height);
            TrySetInt("OffsetX", roi.X);
            TrySetInt("OffsetY", roi.Y);
            Resolution = new Size(Math.Max(roi.Width, 1), Math.Max(roi.Height, 1));
        }

        public override string GetRawParameter(string key)
        {
            try
            {
                var floatT = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.MyCamera+MVCC_FLOATVALUE")
                          ?? HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MVCC_FLOATVALUE");
                if (floatT == null) return null;
                var inst = Activator.CreateInstance(floatT);
                var args = new object[] { key, inst };
                int r = InvokeFirst(new[] { "MV_CC_GetFloatValue_NET", "MV_CC_GetFloatValue" }, args);
                if (r == 0)
                {
                    var fv = floatT.GetField("fCurValue").GetValue(args[1]);
                    return fv.ToString();
                }
            }
            catch { }
            return null;
        }

        public override void SetRawParameter(string key, string value)
        {
            if (double.TryParse(value, out var dv)) TrySetFloat(key, (float)dv);
            else                                    TrySetEnumByName(key, value);
        }

        /// <summary>노드 카탈로그 기반 제네릭 노드 적용 — 타입별 SDK set 메서드로 디스패치.
        /// 미오픈/미지원 노드는 무시(실패는 Debug 로그). 값 파싱은 InvariantCulture.</summary>
        public override void SetParameterTyped(string node, CameraParamKind kind, string value)
        {
            if (!IsOpen || string.IsNullOrEmpty(node)) return;
            try
            {
                switch (kind)
                {
                    case CameraParamKind.Float:
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                            TrySetFloat(node, (float)f);
                        break;
                    case CameraParamKind.Int:
                        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                            TrySetInt(node, i);
                        break;
                    case CameraParamKind.Bool:
                        TrySetBool(node, ParseBool(value));
                        break;
                    case CameraParamKind.Enum:
                        if (!string.IsNullOrEmpty(value)) TrySetEnumByName(node, value);
                        break;
                    case CameraParamKind.Command:
                        InvokeFirst(new[] { "MV_CC_SetCommandValue_NET", "MV_CC_SetCommandValue" }, new object[] { node });
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HikGigECamera] SetParameterTyped({node},{kind}) 실패: {ex.Message}");
            }
        }

        /// <summary>MVS Feature Save 파일(.mfs)을 카메라에 일괄 적용(MV_CC_FeatureLoad). Grabbing 중에는 거부.</summary>
        public override bool LoadFeatures(string filePath, out string error)
        {
            error = null;
            try
            {
                if (!IsOpen) { error = "카메라가 열려 있지 않습니다."; return false; }
                if (IsGrabbing) { error = "Live/Grabbing 중에는 .mfs 적용 불가 — 정지 후 시도하세요."; return false; }
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                { error = "파일을 찾을 수 없습니다: " + filePath; return false; }

                int r = InvokeFirst(new[] { "MV_CC_FeatureLoad_NET", "MV_CC_FeatureLoad" }, new object[] { filePath });
                if (r != 0) { error = "MV_CC_FeatureLoad 실패 0x" + r.ToString("X8"); return false; }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                System.Diagnostics.Debug.WriteLine("[HikGigECamera] LoadFeatures 실패: " + ex.Message);
                return false;
            }
        }

        /// <summary>현재 카메라의 전체 노드값을 MVS Feature Save 파일(.mfs)로 저장(MV_CC_FeatureSave).</summary>
        public override bool SaveFeatures(string filePath, out string error)
        {
            error = null;
            try
            {
                if (!IsOpen) { error = "카메라가 열려 있지 않습니다."; return false; }
                if (string.IsNullOrWhiteSpace(filePath)) { error = "저장 경로가 비어 있습니다."; return false; }

                int r = InvokeFirst(new[] { "MV_CC_FeatureSave_NET", "MV_CC_FeatureSave" }, new object[] { filePath });
                if (r != 0) { error = "MV_CC_FeatureSave 실패 0x" + r.ToString("X8"); return false; }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                System.Diagnostics.Debug.WriteLine("[HikGigECamera] SaveFeatures 실패: " + ex.Message);
                return false;
            }
        }

        // ──────────────────────────────────────────
        //  Enum (디바이스 검색)
        // ──────────────────────────────────────────

        /// <summary>GigE 카메라 전체 목록 획득. SDK 미로드 시 빈 배열.</summary>
        public static List<CameraInfo> Enumerate()
        {
            var result = new List<CameraInfo>();
            if (!HikMvsDll.IsLoaded || HikMvsDll.DeviceInfoListType == null) return result;
            try
            {
                var camType = HikMvsDll.MyCameraType;
                var listT   = HikMvsDll.DeviceInfoListType;
                var listObj = Activator.CreateInstance(listT);

                var mi = FindStatic(camType, new[] { "MV_CC_EnumDevices_NET", "MV_CC_EnumDevices" },
                                    new[] { typeof(uint), listT.MakeByRefType() });
                if (mi == null) return result;

                var args = new object[] { (uint)0x00000001, listObj }; // MV_GIGE_DEVICE=1
                mi.Invoke(null, args);
                listObj = args[1];

                int n = (int)(uint)listT.GetField("nDeviceNum").GetValue(listObj);
                var devs = listT.GetField("pDeviceInfo").GetValue(listObj);
                for (int i = 0; i < n; i++)
                {
                    var info = ExtractCameraInfo(GetDeviceAt(devs, i));
                    if (info != null) result.Add(info);
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// pDeviceInfo[i] 에서 MV_CC_DEVICE_INFO struct 얻기.
        /// 4.x: IntPtr[] → Marshal.PtrToStructure; 구 버전: struct[] → 직접 사용.
        /// </summary>
        private static object GetDeviceAt(object devsField, int i)
        {
            if (devsField is IntPtr[] ptrs)
            {
                if (ptrs[i] == IntPtr.Zero) return null;
                return Marshal.PtrToStructure(ptrs[i], HikMvsDll.DeviceInfoType);
            }
            if (devsField is Array arr) return arr.GetValue(i);
            return null;
        }

        private static CameraInfo ExtractCameraInfo(object devInfoObj)
        {
            if (devInfoObj == null) return null;
            try
            {
                var specialInfo = devInfoObj.GetType().GetField("SpecialInfo").GetValue(devInfoObj);
                var gigeRaw     = specialInfo.GetType().GetField("stGigEInfo").GetValue(specialInfo);

                // 4.x SDK: stGigEInfo 는 byte[] (직렬화된 MV_GIGE_DEVICE_INFO)
                object gigeInfo = gigeRaw;
                if (gigeRaw is byte[] bytes)
                {
                    var gigeT = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_GIGE_DEVICE_INFO")
                             ?? HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_GIGE_DEVICE_INFO");
                    if (gigeT == null) return null;
                    int size = Marshal.SizeOf(gigeT);
                    IntPtr p = Marshal.AllocHGlobal(size);
                    try
                    {
                        Marshal.Copy(bytes, 0, p, Math.Min(bytes.Length, size));
                        gigeInfo = Marshal.PtrToStructure(p, gigeT);
                    }
                    finally { Marshal.FreeHGlobal(p); }
                }

                string model    = GetStrField(gigeInfo, "chModelName");
                string serial   = GetStrField(gigeInfo, "chSerialNumber");
                string userName = GetStrField(gigeInfo, "chUserDefinedName");
                uint   ip       = (uint)gigeInfo.GetType().GetField("nCurrentIp").GetValue(gigeInfo);
                string ipStr    = $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
                return new CameraInfo
                {
                    Id = ipStr, Model = model, Vendor = "HIKVISION",
                    SerialNumber = serial, IpAddress = ipStr,
                    UserDefinedName = userName ?? "",
                    Transport = CameraTransport.GigE
                };
            }
            catch { return null; }
        }

        // ──────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────

        /// <summary>EnumDevices 다시 호출해서 IP 매칭되는 raw device 정보를 찾음.</summary>
        private object FindDeviceInfoByIp(string ipAddress)
        {
            if (!HikMvsDll.IsLoaded || HikMvsDll.DeviceInfoListType == null) return null;
            try
            {
                var camType = HikMvsDll.MyCameraType;
                var listT   = HikMvsDll.DeviceInfoListType;
                var listObj = Activator.CreateInstance(listT);

                var mi = FindStatic(camType, new[] { "MV_CC_EnumDevices_NET", "MV_CC_EnumDevices" },
                                    new[] { typeof(uint), listT.MakeByRefType() });
                if (mi == null) return null;

                var args = new object[] { (uint)0x00000001, listObj };
                mi.Invoke(null, args);
                listObj = args[1];

                int n = (int)(uint)listT.GetField("nDeviceNum").GetValue(listObj);
                var devs = listT.GetField("pDeviceInfo").GetValue(listObj);
                for (int i = 0; i < n; i++)
                {
                    var dev = GetDeviceAt(devs, i);
                    var info = ExtractCameraInfo(dev);
                    if (info != null && info.IpAddress == ipAddress) return dev;
                }
            }
            catch { }
            return null;
        }

        private void ApplyInitialParameters()
        {
            TrySetEnum("TriggerMode", 0); // Continuous 로 시작
            OnExposureChanged(ExposureUs);
            OnGainChanged(Gain);
            OnPixelFormatChanged(PixelFormat);
        }

        private void TryOptimizePacketSize()
        {
            try
            {
                int pkt = (int)InvokeRaw(new[] { "MV_CC_GetOptimalPacketSize_NET", "MV_CC_GetOptimalPacketSize" }, Array.Empty<object>());
                if (pkt > 0)
                    InvokeFirst(new[] { "MV_GIGE_SetGevSCPSPacketSize_NET", "MV_GIGE_SetGevSCPSPacketSize" }, new object[] { (uint)pkt });
            }
            catch { }
        }

        /// <summary>4.x SDK 의 RegisterImageCallBackEx_NET 시도. 성공 시 true.</summary>
        private bool TryRegisterFrameCallback()
        {
            try
            {
                var delT = HikMvsDll.MyCameraType.GetNestedType("cbOutputExdelegate")
                        ?? HikMvsDll.Assembly.GetType("MvCamCtrl.NET.cbOutputExdelegate");
                if (delT == null) return false;

                var thisRef = this;
                MethodInfo onFrameMi = typeof(HikGigECamera).GetMethod(
                    nameof(OnFrameCallback), BindingFlags.NonPublic | BindingFlags.Instance);
                var del = Delegate.CreateDelegate(delT, thisRef, onFrameMi);
                _frameCallbackDelegate = del;

                int r = InvokeFirst(new[] { "MV_CC_RegisterImageCallBackEx_NET", "MV_CC_RegisterImageCallBackEx" },
                                    new object[] { del, IntPtr.Zero });
                return r == 0;
            }
            catch { return false; }
        }

        private void OnFrameCallback(IntPtr pData, object pFrameInfo, IntPtr pUser)
        {
            try
            {
                var w = (ushort)pFrameInfo.GetType().GetField("nWidth").GetValue(pFrameInfo);
                var h = (ushort)pFrameInfo.GetType().GetField("nHeight").GetValue(pFrameInfo);
                var lenField = pFrameInfo.GetType().GetField("nFrameLen");
                int len = lenField != null ? (int)(uint)lenField.GetValue(pFrameInfo) : (w * h);
                if (len <= 0) return;

                var buf = new byte[len];
                Marshal.Copy(pData, buf, 0, len);
                var bmp = BufferToBitmap(buf, pFrameInfo);
                if (bmp != null) RaiseFrame(new GrabResult(bmp, 0, Info.Id));
            }
            catch { }
        }

        private void LiveLoop()
        {
            EnsureFrameBuffer();
            while (_liveRun && IsOpen)
            {
                try
                {
                    var frameInfo = CreateFrameInfoEx();
                    GCHandle gch = GCHandle.Alloc(_frameBuf, GCHandleType.Pinned);
                    try
                    {
                        var args = new object[] { gch.AddrOfPinnedObject(), (uint)_frameBuf.Length, frameInfo, 500 };
                        int r = InvokeFirst(new[] { "MV_CC_GetOneFrameTimeout_NET", "MV_CC_GetOneFrameTimeout" }, args);
                        if (r == 0)
                        {
                            var bmp = BufferToBitmap(_frameBuf, args[2]);
                            if (bmp != null) RaiseFrame(new GrabResult(bmp, 0, Info.Id));
                        }
                    }
                    finally { gch.Free(); }
                }
                catch { /* timeout 무시 */ }
            }
        }

        private object CreateFrameInfoEx()
        {
            var t = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_FRAME_OUT_INFO_EX")
                 ?? HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_FRAME_OUT_INFO_EX");
            return t != null ? Activator.CreateInstance(t) : null;
        }

        private Bitmap BufferToBitmap(byte[] buf, object frameInfoObj)
        {
            try
            {
                int w = (ushort)frameInfoObj.GetType().GetField("nWidth").GetValue(frameInfoObj);
                int h = (ushort)frameInfoObj.GetType().GetField("nHeight").GetValue(frameInfoObj);
                if (w <= 0 || h <= 0) return null;

                uint pixType = 0;
                var pixField = frameInfoObj.GetType().GetField("enPixelType");
                if (pixField != null)
                {
                    var v = pixField.GetValue(frameInfoObj);
                    pixType = Convert.ToUInt32(v);
                }

                // Mono8 = 0x01080001 / RGB8_Packed = 0x02180014 / BGR8_Packed = 0x02180015
                if (pixType == 0x02180014) return BuildRgb24(buf, w, h, swapRb: false);
                if (pixType == 0x02180015) return BuildRgb24(buf, w, h, swapRb: true);
                return BuildMono8(buf, w, h);
            }
            catch { return null; }
        }

        private static Bitmap BuildMono8(byte[] buf, int w, int h)
        {
            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var pal = bmp.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = pal;

            var rect = new Rectangle(0, 0, w, h);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            try
            {
                int stride = data.Stride;
                if (stride == w)
                {
                    int copyLen = Math.Min(buf.Length, stride * h);
                    Marshal.Copy(buf, 0, data.Scan0, copyLen);
                }
                else
                {
                    for (int y = 0; y < h; y++)
                    {
                        int off = y * w;
                        if (off + w > buf.Length) break;
                        Marshal.Copy(buf, off, IntPtr.Add(data.Scan0, y * stride), w);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        private static Bitmap BuildRgb24(byte[] buf, int w, int h, bool swapRb)
        {
            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, w, h);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride;
                var row = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    int srcOff = y * w * 3;
                    if (srcOff + w * 3 > buf.Length) break;
                    for (int x = 0; x < w; x++)
                    {
                        int s = srcOff + x * 3;
                        int d = x * 3;
                        if (swapRb) { row[d + 0] = buf[s + 0]; row[d + 1] = buf[s + 1]; row[d + 2] = buf[s + 2]; }
                        else        { row[d + 0] = buf[s + 2]; row[d + 1] = buf[s + 1]; row[d + 2] = buf[s + 0]; }
                    }
                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * stride), stride);
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        private void EnsureFrameBuffer()
        {
            int need = 5120 * 5120 * 3;
            if (_frameBuf != null && _frameBufSize >= need) return;
            _frameBuf = new byte[need];
            _frameBufSize = need;
        }

        // ─── Method dispatch ───────────────────────

        /// <summary>여러 후보 이름 중 첫 번째 매치 메서드 호출. 반환 int.</summary>
        private int InvokeFirst(string[] names, object[] args)
        {
            object ret = InvokeRaw(names, args);
            return ret == null ? -1 : Convert.ToInt32(ret);
        }

        private object InvokeRaw(string[] names, object[] args)
        {
            if (_camera == null) throw new InvalidOperationException("camera not initialized");
            var argTypes = new Type[args?.Length ?? 0];
            for (int i = 0; i < argTypes.Length; i++) argTypes[i] = args[i]?.GetType() ?? typeof(object);

            foreach (var name in names)
            {
                MethodInfo mi = null;
                try { mi = HikMvsDll.MyCameraType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, argTypes, null); } catch { }
                if (mi == null) mi = HikMvsDll.MyCameraType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
                if (mi == null) continue;
                return mi.Invoke(_camera, args ?? Array.Empty<object>());
            }
            return null;
        }

        private static MethodInfo FindStatic(Type t, string[] names, Type[] paramTypes)
        {
            foreach (var n in names)
            {
                var mi = t.GetMethod(n, BindingFlags.Public | BindingFlags.Static, null, paramTypes, null);
                if (mi != null) return mi;
            }
            return null;
        }

        private void TrySetFloat   (string key, float v)   { try { InvokeFirst(new[] { "MV_CC_SetFloatValue_NET", "MV_CC_SetFloatValue" }, new object[] { key, v }); } catch { } }
        private void TrySetInt     (string key, int v)     { try { InvokeFirst(new[] { "MV_CC_SetIntValue_NET",   "MV_CC_SetIntValue"   }, new object[] { key, (uint)v }); } catch { } }
        private void TrySetEnum    (string key, uint v)    { try { InvokeFirst(new[] { "MV_CC_SetEnumValue_NET",  "MV_CC_SetEnumValue"  }, new object[] { key, v }); } catch { } }
        private void TrySetEnumByName(string key, string v){ try { InvokeFirst(new[] { "MV_CC_SetEnumValueByString_NET", "MV_CC_SetEnumValueByString" }, new object[] { key, v }); } catch { } }
        private void TrySetBool    (string key, bool v)    { try { InvokeFirst(new[] { "MV_CC_SetBoolValue_NET",  "MV_CC_SetBoolValue"  }, new object[] { key, v }); } catch { } }

        /// <summary>"True"/"False"/"1"/"0"/"on" 등을 bool 로 파싱.</summary>
        private static bool ParseBool(string v)
        {
            if (string.IsNullOrEmpty(v)) return false;
            if (bool.TryParse(v, out var b)) return b;
            return v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureSuccess(int r, string op)
        {
            if (r != 0) throw new Exception($"{op} failed 0x{r:X8}");
        }

        private static string GetStrField(object obj, string name)
        {
            try
            {
                var f = obj.GetType().GetField(name);
                var v = f?.GetValue(obj);
                if (v is byte[] b) return System.Text.Encoding.ASCII.GetString(b).TrimEnd('\0').Trim();
                if (v is string s) return s.TrimEnd('\0').Trim();
                return v?.ToString();
            }
            catch { return ""; }
        }
    }
}
