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
            RaiseConnectionChanged(CameraConnectionEvent.Opened);
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
        protected override void OnExposureChanged (double us)     => TryFeatureD("ExposureTime", us);
        protected override void OnGainChanged     (double gainDb) => TryFeatureD("Gain", gainDb);
        protected override void OnFrameRateChanged(double fps)    => TryFeatureD("AcquisitionFrameRate", fps);

        private void TryFeatureD(string feature, double val)
        {
            if (IsNull(_dig)) return;
            try { MIL.MdigControlFeature(_dig, MIL.M_FEATURE_VALUE, feature, MIL.M_TYPE_DOUBLE, ref val); } catch { }
        }

        // ── Buffer → Bitmap (MbufExport 경유 — mono/color 모든 포맷 안전) ──
        private Bitmap BufferToBitmap()
        {
            try
            {
                MIL.MbufExport(_tmpPath, MIL.M_BMP, _buf);
                using (var fs = new FileStream(_tmpPath, FileMode.Open, FileAccess.Read))
                using (var tmp = new Bitmap(fs))
                    return new Bitmap(tmp);   // 파일 핸들과 분리된 사본
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
