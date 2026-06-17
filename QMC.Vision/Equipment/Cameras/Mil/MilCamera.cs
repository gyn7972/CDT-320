using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using Matrox.MatroxImagingLibrary;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Cameras.Mil
{
    /// <summary>
    /// Matrox 보드(MIL) 카메라 — Camera Link / CoaXPress.
    /// 디지타이저 1채널 = 카메라 1대. 식별 ID 는 "Mil/0".."Mil/N".
    /// <para>SDK/보드/카메라 없으면 Open 에서 throw → SimCamera 로 대체.</para>
    /// 영상 형식은 DataFormat 으로 결정: 비어있으면 "M_DEFAULT"(CXP GenICam 자동),
    /// CameraLink 면 .dcf 경로(<see cref="VisionSettings.MilDcfPath"/>).
    /// </summary>
    public class MilCamera : CameraBase
    {
        private MIL_ID _dig = MIL.M_NULL;
        private MIL_ID _buf = MIL.M_NULL;
        private int    _digNum;
        private int    _bands = 1;
        private Thread _liveThread;
        private volatile bool _liveRun;
        private readonly string _tmpPath;
        private byte[] _hostBuf;   // Mono 프레임 호스트 복사 버퍼(재사용 — GC 압박 감소)

        public MilCamera(CameraInfo info) : base(info)
        {
            if (string.IsNullOrEmpty(Info.Vendor)) Info.Vendor = "Matrox";
            if (Info.Transport == CameraTransport.Sim) Info.Transport = CameraTransport.CoaXPress;
            _digNum  = ParseDigNum(Info.Id);
            _tmpPath = Path.Combine(Path.GetTempPath(), "qmc_mil_dig" + _digNum + ".bmp");
        }

        private static bool IsNull(MIL_ID id) { return ((long)id) == 0; }

        private static int ParseDigNum(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                int slash = id.IndexOf('/');
                if (slash >= 0 && int.TryParse(id.Substring(slash + 1), out int n)) return n;
            }
            return 0;
        }

        // ── Open / Close ──────────────────────────────
        public override void Open()
        {
            if (IsOpen) return;
            MilSystem.EnsureInit();
            if (!MilSystem.IsAvailable) throw new InvalidOperationException(MilSystem.GetInstallHint());

            var cfg = VisionConfigStore.Current ?? new VisionSettings();
            string fmt = string.IsNullOrWhiteSpace(cfg.MilDcfPath) ? "M_DEFAULT" : cfg.MilDcfPath;

            MIL_INT dn = _digNum;
            MIL.MdigAlloc(MilSystem.SysId, dn, fmt, MIL.M_DEFAULT, ref _dig);
            if (IsNull(_dig))
                throw new InvalidOperationException(
                    "MdigAlloc 실패 (digitizer " + _digNum + ", format '" + fmt + "') — 카메라 미연결 또는 DCF/포맷 필요");

            MIL_INT sx = 0, sy = 0, band = 1;
            try { MIL.MdigInquire(_dig, MIL.M_SIZE_X,    ref sx);   } catch { }
            try { MIL.MdigInquire(_dig, MIL.M_SIZE_Y,    ref sy);   } catch { }
            try { MIL.MdigInquire(_dig, MIL.M_SIZE_BAND, ref band); } catch { band = 1; }
            int w = (int)(long)sx, h = (int)(long)sy;
            _bands = (int)(long)band; if (_bands < 1) _bands = 1;
            if (w <= 0) w = 640;
            if (h <= 0) h = 480;
            Resolution = new Size(w, h);

            long    attr = (long)MIL.M_IMAGE + MIL.M_GRAB + MIL.M_PROC;
            MIL_INT type = 8 + MIL.M_UNSIGNED;   // 8-bit unsigned
            if (_bands >= 3)
                MIL.MbufAllocColor(MilSystem.SysId, (MIL_INT)_bands, (MIL_INT)w, (MIL_INT)h, type, attr, ref _buf);
            else
                MIL.MbufAlloc2d(MilSystem.SysId, (MIL_INT)w, (MIL_INT)h, type, attr, ref _buf);

            if (IsNull(_buf))
            {
                try { MIL.MdigFree(_dig); } catch { }
                _dig = MIL.M_NULL;
                throw new InvalidOperationException("MbufAlloc 실패 (" + w + "x" + h + ", bands=" + _bands + ")");
            }

            IsOpen = true;
            // 열릴 때마다 현재(레시피/UI) 설정을 카메라에 재적용 — startup·Connect·재오픈 모두 동일 상태 보장.
            ApplyCurrentSettings();
            RaiseConnectionChanged(CameraConnectionEvent.Opened);
        }

        /// <summary>현재 CameraBase 에 캐시된 설정값을 카메라(GenICam feature)에 재적용. Open 직후 호출.</summary>
        private void ApplyCurrentSettings()
        {
            try { OnExposureChanged(ExposureUs); }              catch { }
            try { OnGainChanged(Gain); }                        catch { }
            try { OnFrameRateChanged(AcquisitionFrameRate); }   catch { }
            try { OnPixelFormatChanged(PixelFormat); }          catch { }
            try { OnTriggerModeChanged(TriggerMode); }          catch { }
            try { if (Roi.Width > 0 && Roi.Height > 0) OnRoiChanged(Roi); } catch { }
        }

        public override void Close()
        {
            StopLive();
            if (!IsOpen) return;
            try { if (!IsNull(_buf)) MIL.MbufFree(_buf); } catch { }
            try { if (!IsNull(_dig)) MIL.MdigFree(_dig); } catch { }
            _buf = MIL.M_NULL;
            _dig = MIL.M_NULL;
            IsOpen = false;
            RaiseConnectionChanged(CameraConnectionEvent.Closed);
        }

        // ── Grab ──────────────────────────────────────
        public override GrabResult Grab(int timeoutMs = 3000)
        {
            if (!IsOpen) return GrabResult.Fail("camera not open", Info.Id);
            try
            {
                try { MIL.MdigControl(_dig, MIL.M_GRAB_TIMEOUT, (double)timeoutMs); } catch { }
                MIL.MdigGrab(_dig, _buf);
                var bmp = BufferToBitmap();
                if (bmp == null) return GrabResult.Fail("buffer→bitmap 실패", Info.Id);
                return new GrabResult(bmp, 0, Info.Id);
            }
            catch (Exception ex) { return GrabResult.Fail("MdigGrab: " + ex.Message, Info.Id); }
        }

        public override void StartLive()
        {
            if (!IsOpen || IsGrabbing) return;
            IsGrabbing = true;
            _liveRun   = true;
            _liveThread = new Thread(LiveLoop) { IsBackground = true, Name = "MilLive/" + _digNum };
            _liveThread.Start();
        }

        public override void StopLive()
        {
            _liveRun = false;
            try { _liveThread?.Join(1000); } catch { }
            _liveThread = null;
            IsGrabbing = false;
        }

        private void LiveLoop()
        {
            while (_liveRun && IsOpen)
            {
                try
                {
                    MIL.MdigGrab(_dig, _buf);
                    var bmp = BufferToBitmap();
                    if (bmp != null) RaiseFrame(new GrabResult(bmp, 0, Info.Id));
                }
                catch { Thread.Sleep(5); }
            }
        }

        public override void TriggerSoftware()
        {
            if (!IsOpen) return;
            try { MIL.MdigControl(_dig, MIL.M_GRAB_TRIGGER, (MIL_INT)MIL.M_ACTIVATE); } catch { }
        }

        // ── 파라미터 (GenICam feature; CameraLink/미지원 카메라면 무시) ──
        // DCF 는 링크/스트로브/포맷 baseline, 가변 파라미터는 GenICam feature 로 런타임 적용(일반적 구성).
        protected override void OnExposureChanged (double us)     => TryFeatureD("ExposureTime", us);
        protected override void OnGainChanged     (double gainDb) => TryFeatureD("Gain", gainDb);
        protected override void OnFrameRateChanged(double fps)    => TryFeatureD("AcquisitionFrameRate", fps);

        /// <summary>Trigger Mode(On/Off) + Trigger Source 를 분리 적용 (MVS 노드와 동일).</summary>
        protected override void OnTriggerModeChanged(CameraTriggerMode mode)
        {
            switch (mode)
            {
                case CameraTriggerMode.Continuous:
                    TryFeatureS("TriggerMode", "Off");
                    break;
                case CameraTriggerMode.Software:
                    TryFeatureS("TriggerMode", "On"); TryFeatureS("TriggerSource", "Software");
                    break;
                case CameraTriggerMode.Line0:
                    TryFeatureS("TriggerMode", "On"); TryFeatureS("TriggerSource", "Line0");
                    break;
                case CameraTriggerMode.Line1:
                    TryFeatureS("TriggerMode", "On"); TryFeatureS("TriggerSource", "Line1");
                    break;
                case CameraTriggerMode.Line2:
                    TryFeatureS("TriggerMode", "On"); TryFeatureS("TriggerSource", "Line2");
                    break;
            }
        }

        protected override void OnPixelFormatChanged(CameraPixelFormat fmt)
            => TryFeatureS("PixelFormat", fmt.ToString());

        /// <summary>ROI 적용 — grab 정지 상태에서만 안전. 0 크기면 센서 풀(=DCF 기본) 유지.</summary>
        protected override void OnRoiChanged(Rectangle roi)
        {
            if (IsNull(_dig) || roi.Width <= 0 || roi.Height <= 0) return;
            // Offset 을 먼저 0 으로 내려 Width/Height 증가 시 범위 초과 방지.
            TryFeatureI("OffsetX", 0);
            TryFeatureI("OffsetY", 0);
            TryFeatureI("Width",  roi.Width);
            TryFeatureI("Height", roi.Height);
            TryFeatureI("OffsetX", roi.X);
            TryFeatureI("OffsetY", roi.Y);
        }

        private void TryFeatureD(string feature, double val)
        {
            if (IsNull(_dig)) return;
            try { MIL.MdigControlFeature(_dig, MIL.M_FEATURE_VALUE, feature, MIL.M_TYPE_DOUBLE, ref val); } catch { }
        }

        private void TryFeatureS(string feature, string val)
        {
            if (IsNull(_dig)) return;
            try { MIL.MdigControlFeature(_dig, MIL.M_FEATURE_VALUE, feature, MIL.M_TYPE_STRING, val); } catch { }
        }

        private void TryFeatureI(string feature, long val)
        {
            if (IsNull(_dig)) return;
            try { MIL_INT v = (MIL_INT)val; MIL.MdigControlFeature(_dig, MIL.M_FEATURE_VALUE, feature, MIL.M_TYPE_MIL_INT, ref v); } catch { }
        }

        // ── Buffer → Bitmap (메모리 직접 변환 — 디스크 미경유) ──
        // 144MP Mono 를 매 프레임 디스크 BMP 로 export/read 하던 병목 제거(약 1fps → 대폭 향상).
        private Bitmap BufferToBitmap()
        {
            int w = Resolution.Width, h = Resolution.Height;
            if (w <= 0 || h <= 0 || IsNull(_buf)) return null;

            // 1) 고속 경로 — Mono 8-bit 메모리 직접 변환(디스크 미경유). 실패 시 디스크 폴백.
            if (_bands < 3)
            {
                try
                {
                    int need = w * h;
                    if (_hostBuf == null || _hostBuf.Length < need) _hostBuf = new byte[need];
                    MIL.MbufGet(_buf, _hostBuf);

                    var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    var pal = bmp.Palette;
                    for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
                    bmp.Palette = pal;

                    var bd = bmp.LockBits(new Rectangle(0, 0, w, h),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    try
                    {
                        if (bd.Stride == w)
                            System.Runtime.InteropServices.Marshal.Copy(_hostBuf, 0, bd.Scan0, need);
                        else
                            for (int y = 0; y < h; y++)
                                System.Runtime.InteropServices.Marshal.Copy(
                                    _hostBuf, y * w, System.IntPtr.Add(bd.Scan0, y * bd.Stride), w);
                    }
                    finally { bmp.UnlockBits(bd); }
                    return bmp;
                }
                catch { /* 고속 경로 실패 → 아래 디스크 폴백으로 */ }
            }

            // 2) 폴백 — MbufExport(디스크). 느리지만 모든 포맷 안전(표시는 항상 보장).
            try
            {
                MIL.MbufExport(_tmpPath, MIL.M_BMP, _buf);
                using (var fs = new FileStream(_tmpPath, FileMode.Open, FileAccess.Read))
                using (var tmp = new Bitmap(fs))
                    return new Bitmap(tmp);
            }
            catch { return null; }
        }

        // ── Enumerate ─────────────────────────────────
        /// <summary>
        /// 각 디지타이저 슬롯에 MdigAlloc 을 시도해 **실제로 카메라가 잡히는 채널만** 목록에 넣는다.
        /// <para>듀얼/쿼드 링크(멀티링크) 카메라는 MIL 이 링크를 묶어 1개 디지타이저로 잡으므로
        /// 자동으로 1개 항목으로 합쳐지고, 소비된/빈 채널은 목록에서 제외된다.</para>
        /// MIL 미가용이거나 연결된 카메라가 없으면 빈 목록.
        /// </summary>
        public static List<CameraInfo> Enumerate()
        {
            var list = new List<CameraInfo>();
            try
            {
                MilSystem.EnsureInit();
                if (!MilSystem.IsAvailable) return list;

                int slots = MilSystem.DigitizerCount;
                if (slots <= 0) slots = 1;

                var cfg = VisionConfigStore.Current ?? new VisionSettings();
                string fmt = string.IsNullOrWhiteSpace(cfg.MilDcfPath) ? "M_DEFAULT" : cfg.MilDcfPath;

                for (int i = 0; i < slots; i++)
                {
                    MIL_ID dig = MIL.M_NULL;
                    try { MIL.MdigAlloc(MilSystem.SysId, (MIL_INT)i, fmt, MIL.M_DEFAULT, ref dig); }
                    catch { dig = MIL.M_NULL; }
                    if (IsNull(dig)) continue;   // 카메라 없음/링크 소비됨 → 제외

                    MIL_INT sx = 0, sy = 0;
                    try { MIL.MdigInquire(dig, MIL.M_SIZE_X, ref sx); } catch { }
                    try { MIL.MdigInquire(dig, MIL.M_SIZE_Y, ref sy); } catch { }
                    list.Add(new CameraInfo
                    {
                        Id        = "Mil/" + i,
                        Model     = "Matrox MIL " + ((long)sx) + "x" + ((long)sy),
                        Vendor    = "Matrox",
                        Transport = CameraTransport.CoaXPress
                    });
                    try { MIL.MdigFree(dig); } catch { }
                }
            }
            catch { }
            return list;
        }
        // Dispose 는 CameraBase.Dispose() (→ Close()) 가 처리. 별도 override 불필요.
    }
}
