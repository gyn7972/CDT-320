using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using QMC.Vision.Core;

namespace QMC.Vision.Cameras.Hik
{
    /// <summary>
    /// HIKVISION GigE 카메라 구현.
    /// <see cref="HikMvsDll"/> 로 SDK 를 동적 로드하고, 리플렉션으로 MyCamera API 호출.
    /// SDK 미설치 시 Open() 에서 즉시 false 상태가 되고, <see cref="SimCamera"/> 로 대체하면 됨.
    /// <para>
    /// 주요 MVS API:
    ///   MV_CC_CreateHandle / MV_CC_OpenDevice / MV_CC_CloseDevice / MV_CC_DestroyHandle
    ///   MV_CC_StartGrabbing / MV_CC_StopGrabbing / MV_CC_GetOneFrameTimeout
    ///   MV_CC_SetIntValue / MV_CC_SetFloatValue / MV_CC_SetEnumValue
    ///   MV_CC_RegisterImageCallBackEx (콜백)
    /// </para>
    /// </summary>
    public class HikGigECamera : CameraBase
    {
        private object  _camera;           // MyCamera 인스턴스
        //private IntPtr  _handle;
        private Thread  _liveThread;
        private volatile bool _liveRun;
        private byte[]  _frameBuf;
        private int     _frameBufSize;

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

            // MyCamera camera = new MyCamera();
            _camera = Activator.CreateInstance(HikMvsDll.MyCameraType);

            // deviceInfo 조립: Info.IpAddress 로 MV_GIGE_DEVICE_INFO 를 만들거나,
            // 편의상 전체 enum 후 IP 일치하는 것 선택.
            var devInfo = FindDeviceInfoByIp(Info.IpAddress);
            if (devInfo == null)
                throw new InvalidOperationException("Device not found: " + Info.IpAddress);

            // ret = camera.MV_CC_CreateHandle(ref devInfo);
            int r = (int)Invoke("MV_CC_CreateHandle", new[] { devInfo });
            EnsureSuccess(r, "MV_CC_CreateHandle");

            // ret = camera.MV_CC_OpenDevice(MV_ACCESS_Exclusive=1, 0);
            r = (int)Invoke("MV_CC_OpenDevice", new object[] { (uint)1, (ushort)0 });
            EnsureSuccess(r, "MV_CC_OpenDevice");

            // 기본 파라미터 적용
            ApplyInitialParameters();

            IsOpen = true;
            RaiseConnectionChanged(CameraConnectionEvent.Opened);
        }

        public override void Close()
        {
            StopLive();
            if (!IsOpen) return;
            try { Invoke("MV_CC_CloseDevice", null); } catch { }
            try { Invoke("MV_CC_DestroyHandle", null); } catch { }
            IsOpen = false;
            _camera = null;
            RaiseConnectionChanged(CameraConnectionEvent.Closed);
        }

        // ──────────────────────────────────────────
        //  Grab
        // ──────────────────────────────────────────

        public override GrabResult Grab(int timeoutMs = 3000)
        {
            if (!IsOpen) return GrabResult.Fail("camera not open", Info.Id);

            // 1장 Start-Grab → GetOneFrameTimeout → Stop 간결 구현
            EnsureFrameBuffer();

            int r = (int)Invoke("MV_CC_StartGrabbing", null);
            if (r != 0) return GrabResult.Fail("MV_CC_StartGrabbing 0x" + r.ToString("X"), Info.Id);

            // MV_FRAME_OUT_INFO_EX
            var frameInfoType = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_FRAME_OUT_INFO_EX");
            var frameInfoInst = frameInfoType != null ? Activator.CreateInstance(frameInfoType) : null;
            var pBuf = Marshal.UnsafeAddrOfPinnedArrayElement(_frameBuf, 0);
            var args = new object[] { pBuf, (uint)_frameBuf.Length, frameInfoInst, timeoutMs };

            r = (int)Invoke("MV_CC_GetOneFrameTimeout", args);
            try { Invoke("MV_CC_StopGrabbing", null); } catch { }

            if (r != 0) return GrabResult.Fail("MV_CC_GetOneFrameTimeout 0x" + r.ToString("X"), Info.Id);

            var bmp = BufferToBitmap(_frameBuf, args[2]);
            return new GrabResult(bmp, 0, Info.Id);
        }

        public override void StartLive()
        {
            if (!IsOpen || IsGrabbing) return;
            EnsureFrameBuffer();
            int r = (int)Invoke("MV_CC_StartGrabbing", null);
            if (r != 0) throw new Exception("MV_CC_StartGrabbing fail 0x" + r.ToString("X"));

            IsGrabbing = true;
            _liveRun = true;
            _liveThread = new Thread(LiveLoop) { IsBackground = true };
            _liveThread.Start();
        }

        public override void StopLive()
        {
            _liveRun = false;
            try { _liveThread?.Join(1000); } catch { }
            _liveThread = null;
            try { Invoke("MV_CC_StopGrabbing", null); } catch { }
            IsGrabbing = false;
        }

        public override void TriggerSoftware()
        {
            if (!IsOpen) return;
            Invoke("MV_CC_SetCommandValue", new object[] { "TriggerSoftware" });
        }

        // ──────────────────────────────────────────
        //  Parameter hooks (MyCamera.MV_CC_Set* 호출)
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
            // GenICam 노드값 조회 — 간단히 Float/Int/Enum 순차 시도
            try
            {
                var floatT = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MVCC_FLOATVALUE");
                var inst   = Activator.CreateInstance(floatT);
                int r      = (int)Invoke("MV_CC_GetFloatValue", new object[] { key, inst });
                if (r == 0)
                {
                    var fv = floatT.GetField("fCurValue").GetValue(inst);
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

        // ──────────────────────────────────────────
        //  Enum (디바이스 검색)
        // ──────────────────────────────────────────

        /// <summary>GigE 카메라 전체 목록 획득. SDK 미로드 시 빈 배열.</summary>
        public static List<CameraInfo> Enumerate()
        {
            var result = new List<CameraInfo>();
            if (!HikMvsDll.IsLoaded) return result;
            try
            {
                var camType = HikMvsDll.MyCameraType;
                var listT   = HikMvsDll.DeviceInfoListType;
                var listObj = Activator.CreateInstance(listT);

                // int ret = MyCamera.MV_CC_EnumDevices(MV_GIGE_DEVICE, ref listObj);
                var mi = camType.GetMethod("MV_CC_EnumDevices",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(uint), listT.MakeByRefType() }, null);
                if (mi == null)
                {
                    // 일부 버전: EnumDevicesEx2
                    mi = camType.GetMethod("MV_CC_EnumDevicesEx2",
                        BindingFlags.Public | BindingFlags.Static);
                }
                var args = new object[] { (uint)0x00000001, listObj }; // MV_GIGE_DEVICE=1
                mi.Invoke(null, args);
                listObj = args[1];

                int n = (int)listT.GetField("nDeviceNum").GetValue(listObj);
                var devs = (Array)listT.GetField("pDeviceInfo").GetValue(listObj);
                for (int i = 0; i < n; i++)
                {
                    var dev = devs.GetValue(i);
                    if (dev == null) continue;
                    var info = ExtractCameraInfo(dev);
                    if (info != null) result.Add(info);
                }
            }
            catch { }
            return result;
        }

        private static CameraInfo ExtractCameraInfo(object devInfoObj)
        {
            // MV_CC_DEVICE_INFO.SpecialInfo.stGigEInfo (chModelName, chSerialNumber, nCurrentIp...)
            try
            {
                var specialInfo = devInfoObj.GetType().GetField("SpecialInfo").GetValue(devInfoObj);
                var gigeInfo    = specialInfo.GetType().GetField("stGigEInfo").GetValue(specialInfo);
                string model    = GetStrField(gigeInfo, "chModelName");
                string serial   = GetStrField(gigeInfo, "chSerialNumber");
                uint   ip       = (uint)gigeInfo.GetType().GetField("nCurrentIp").GetValue(gigeInfo);
                string ipStr    = $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
                return new CameraInfo
                {
                    Id = ipStr, Model = model, Vendor = "HIKVISION",
                    SerialNumber = serial, IpAddress = ipStr,
                    Transport = CameraTransport.GigE
                };
            }
            catch { return null; }
        }

        // ──────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────

        private object FindDeviceInfoByIp(string ipAddress)
        {
            if (!HikMvsDll.IsLoaded) return null;
            try
            {
                var camType = HikMvsDll.MyCameraType;
                var listT   = HikMvsDll.DeviceInfoListType;
                var listObj = Activator.CreateInstance(listT);

                var mi = camType.GetMethod("MV_CC_EnumDevices",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(uint), listT.MakeByRefType() }, null);
                var args = new object[] { (uint)0x00000001, listObj };
                mi.Invoke(null, args);
                listObj = args[1];

                int n = (int)listT.GetField("nDeviceNum").GetValue(listObj);
                var devs = (Array)listT.GetField("pDeviceInfo").GetValue(listObj);
                for (int i = 0; i < n; i++)
                {
                    var dev = devs.GetValue(i);
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

        private void LiveLoop()
        {
            EnsureFrameBuffer();
            var frameInfoType = HikMvsDll.Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_FRAME_OUT_INFO_EX");
            var frameInfoInst = frameInfoType != null ? Activator.CreateInstance(frameInfoType) : null;

            while (_liveRun && IsOpen)
            {
                try
                {
                    var pBuf = Marshal.UnsafeAddrOfPinnedArrayElement(_frameBuf, 0);
                    var args = new object[] { pBuf, (uint)_frameBuf.Length, frameInfoInst, 500 };
                    int r = (int)Invoke("MV_CC_GetOneFrameTimeout", args);
                    if (r == 0)
                    {
                        var bmp = BufferToBitmap(_frameBuf, args[2]);
                        RaiseFrame(new GrabResult(bmp, 0, Info.Id));
                    }
                }
                catch { /* 타임아웃 무시 */ }
            }
        }

        private Bitmap BufferToBitmap(byte[] buf, object frameInfoObj)
        {
            try
            {
                int w = (ushort)frameInfoObj.GetType().GetField("nWidth").GetValue(frameInfoObj);
                int h = (ushort)frameInfoObj.GetType().GetField("nHeight").GetValue(frameInfoObj);
                if (w <= 0 || h <= 0) return null;

                var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                // 그레이스케일 팔레트
                var pal = bmp.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = pal;

                var rect = new Rectangle(0, 0, w, h);
                var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                try
                {
                    int copyLen = Math.Min(buf.Length, data.Stride * h);
                    Marshal.Copy(buf, 0, data.Scan0, copyLen);
                }
                finally { bmp.UnlockBits(data); }
                return bmp;
            }
            catch { return null; }
        }

        private void EnsureFrameBuffer()
        {
            int need = 5120 * 5120 * 3; // 최대 가정
            if (_frameBuf != null && _frameBufSize >= need) return;
            _frameBuf = new byte[need];
            _frameBufSize = need;
        }

        private object Invoke(string method, object[] args)
        {
            if (_camera == null) throw new InvalidOperationException("camera not initialized");
            var mi = HikMvsDll.MyCameraType.GetMethod(method);
            if (mi == null) throw new MissingMethodException(HikMvsDll.MyCameraType.Name, method);
            return mi.Invoke(_camera, args ?? Array.Empty<object>());
        }

        private void TrySetFloat   (string key, float v)   { try { Invoke("MV_CC_SetFloatValue", new object[] { key, v }); } catch { } }
        private void TrySetInt     (string key, int v)      { try { Invoke("MV_CC_SetIntValue",   new object[] { key, (long)v }); } catch { } }
        private void TrySetEnum    (string key, uint v)     { try { Invoke("MV_CC_SetEnumValue",  new object[] { key, v }); } catch { } }
        private void TrySetEnumByName(string key, string v) { try { Invoke("MV_CC_SetEnumValueByString", new object[] { key, v }); } catch { } }

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
                if (v is byte[] b) return System.Text.Encoding.ASCII.GetString(b).TrimEnd('\0');
                return v?.ToString();
            }
            catch { return ""; }
        }
    }
}
