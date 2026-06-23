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
        private MIL_DIG_HOOK_FUNCTION_PTR _liveHook;   // 라이브 프레임 콜백 델리게이트(GC 방지로 필드 보관)
        private readonly System.Diagnostics.Stopwatch _liveSw = System.Diagnostics.Stopwatch.StartNew();
        private long _lastLiveTickMs;
        private readonly string _tmpPath;
        private byte[] _hostBuf;   // Mono 프레임 호스트 복사 버퍼(재사용 — GC 압박 감소)
        private int    _diskFallbackStreak;   // 디스크 폴백 연속 횟수 — 라이브에서 디스크 I/O 폭주 방지

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
            // Open 직후 — 카메라 실제 상태를 알 수 없으므로 모드 캐시를 무효화하고 기준 모드를 새로 적용한다.
            _acqModeApplied = null;
            _trigOffApplied = false;

            try { OnExposureChanged(ExposureUs); }              catch { }
            try { OnGainChanged(Gain); }                        catch { }
            try { OnFrameRateChanged(AcquisitionFrameRate); }   catch { }
            try { OnPixelFormatChanged(PixelFormat); }          catch { }
            try { if (Roi.Width > 0 && Roi.Height > 0) OnRoiChanged(Roi); } catch { }

            // 기준 = 단발 그랩 모드(SingleFrame + TriggerMode Off). 라이브 진입 시에만 Continuous 로 전환.
            //   (이 카메라는 AcquisitionStart 로 촬상하므로 TriggerMode 는 항상 Off 유지.)
            try { EnsureSingleFrameGrabMode(); } catch { }
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
            // 재진입 가드 — 이전 그랩이 아직 진행 중이면 겹치지 않게 즉시 반환(연속 클릭/다중 경로 겹침 멈춤 방지).
            if (System.Threading.Interlocked.Exchange(ref _grabBusy, 1) == 1)
                return GrabResult.Fail("grab busy", Info.Id);
            try
            {
                try { MIL.MdigControl(_dig, MIL.M_GRAB_TIMEOUT, (double)timeoutMs); } catch { }
                // 동기 그랩 — MdigGrab 이 프레임 완료까지 블록한다.
                try { MIL.MdigControl(_dig, MIL.M_GRAB_MODE, (double)MIL.M_SYNCHRONOUS); } catch { }
                // 라이브 중이 아니면 직전 단발 획득을 확실히 정지 → 다음 AcquisitionStart 가 깨끗이 재-arm
                //   (직전 SingleFrame 획득이 정리되기 전에 연속으로 누르면 다음 MdigGrab 이 멈추는 문제 방지).
                if (!_continuousOn)
                    try { MIL.MdigHalt(_dig); } catch { }

                // 단발 촬상 = AcquisitionMode SingleFrame + AcquisitionStart(=MdigGrab) → 1프레임.
                //   (Intellicam 의 Single Frame + Acquisition Start 1회와 동일. 노출 Timed, 스트로브는 DCF.)
                EnsureSingleFrameGrabMode();
                MIL.MdigGrab(_dig, _buf);            // AcquisitionStart → 1프레임(+ 스트로브). 동기라 완료까지 블록.

                var bmp = BufferToBitmap();
                if (bmp == null) return GrabResult.Fail("buffer→bitmap 실패", Info.Id);
                return new GrabResult(bmp, 0, Info.Id);
            }
            catch (Exception ex) { return GrabResult.Fail("MdigGrab: " + ex.Message, Info.Id); }
            finally { System.Threading.Interlocked.Exchange(ref _grabBusy, 0); }
        }

        private volatile bool _continuousOn;
        private int _grabBusy;   // 0/1 — 단발 그랩 재진입 가드(연속 클릭/다중 경로 겹침 방지)
        private long _liveFrameCount;   // 진단용 — 라이브 훅 호출 횟수

        // 획득 모드/트리거 캐시 — 매 Grab/Live 마다 GenICam feature 를 쓰면 카메라 왕복으로 버벅임/멈춤이
        // 생기므로, 실제 상태가 바뀔 때만 적용한다.
        private string _acqModeApplied;     // 마지막 적용 AcquisitionMode ("SingleFrame"/"Continuous")
        private bool   _trigOffApplied;     // TriggerMode=Off 적용됨

        /// <summary>단발 그랩 모드 보장 — AcquisitionMode SingleFrame(+FrameCount 1) + TriggerMode Off.
        /// 이미 적용돼 있으면 카메라에 쓰지 않는다(연속 그랩 시 재설정/멈춤 방지).</summary>
        private void EnsureSingleFrameGrabMode()
        {
            if (_acqModeApplied != "SingleFrame")
            {
                TryFeatureS("AcquisitionMode", "SingleFrame");
                TryFeatureI("AcquisitionFrameCount", 1);
                _acqModeApplied = "SingleFrame";
            }
            if (!_trigOffApplied)
            {
                TryFeatureS("TriggerMode", "Off");
                _trigOffApplied = true;
            }
        }

        /// <summary>라이브(연속) 모드 보장 — AcquisitionMode Continuous + TriggerMode Off. 변경 시에만 적용.</summary>
        private void EnsureContinuousLiveMode()
        {
            if (_acqModeApplied != "Continuous")
            {
                TryFeatureS("AcquisitionMode", "Continuous");
                _acqModeApplied = "Continuous";
            }
            if (!_trigOffApplied)
            {
                TryFeatureS("TriggerMode", "Off");
                _trigOffApplied = true;
            }
        }

        public override void StartLive()
        {
            if (!IsOpen || IsGrabbing) return;
            IsGrabbing = true;

            // 라이브 = Continuous(free-run): 트리거 없이 연속 수신 → 화면이 갱신된다. (모드는 변경 시에만 적용)
            //   (SingleFrame 복원은 StopLive 에서.)
            EnsureContinuousLiveMode();

            // 프레임마다 MIL 내부 스레드가 호출하는 훅 — 우리 폴링 스레드를 만들지 않는다.
            //   델리게이트는 필드로 보관해야 콜백 중 GC 되지 않는다.
            _lastLiveTickMs = 0;
            _liveFrameCount = 0;
            _liveHook = LiveGrabHook;
            try { MIL.MdigHookFunction(_dig, MIL.M_GRAB_FRAME_END, _liveHook, IntPtr.Zero); } catch { }
            try { MIL.MdigGrabContinuous(_dig, _buf); _continuousOn = true; }
            catch (Exception ex) { _continuousOn = false; LiveLog("MdigGrabContinuous 실패: " + ex.Message); }
            LiveLog("StartLive: continuousOn=" + _continuousOn + ", acqMode=" + _acqModeApplied);
        }

        private void LiveLog(string msg)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "MilLive", Info.Id + " " + msg); } catch { }
        }

        public override void StopLive()
        {
            bool wasLive = _continuousOn || IsGrabbing;
            LiveLog("StopLive enter: continuousOn=" + _continuousOn + ", frames=" + _liveFrameCount);

            // 순서 중요 — MdigHalt 보다 먼저: ① IsGrabbing=false 로 진행 중인 훅이 즉시 빠지게(RaiseFrame/UI 마샬링 차단),
            //   ② 훅 해제로 새 콜백 차단. 이렇게 해야 MdigHalt 가 무거운 콜백(144M 변환)·UI 마샬링과 데드락/멈춤하지 않는다.
            IsGrabbing = false;
            try { if (_liveHook != null) MIL.MdigHookFunction(_dig, MIL.M_GRAB_FRAME_END + MIL.M_UNHOOK, _liveHook, IntPtr.Zero); } catch { }
            _liveHook = null;
            LiveLog("unhooked");

            if (_continuousOn)
            {
                LiveLog("before MdigHalt");
                try { MIL.MdigHalt(_dig); } catch (Exception ex) { LiveLog("MdigHalt 예외: " + ex.Message); }
                LiveLog("after MdigHalt");
                _continuousOn = false;
            }

            // 스탑 = SingleFrame(1프레임)으로 복원 → 다음 Grab 은 AcquisitionStart 1장(+스트로브). (변경 시에만 적용)
            if (wasLive)
                EnsureSingleFrameGrabMode();
            LiveLog("StopLive done");
        }

        /// <summary>라이브 미리보기 최대 FPS(고해상도 센서 변환·표시 부하 완화). 0 이하면 무제한.</summary>
        public int LivePreviewFps { get; set; } = 15;

        /// <summary>MIL 연속 그랩의 프레임 완료 콜백(M_GRAB_FRAME_END) — MIL 내부 스레드에서 호출된다.
        /// _buf 를 비트맵으로 변환해 FrameReceived 로 발행. 별도 폴링 스레드를 쓰지 않는다.</summary>
        private MIL_INT LiveGrabHook(MIL_INT hookType, MIL_ID eventId, IntPtr userPtr)
        {
            try
            {
                if (!IsGrabbing || !IsOpen) return 0;

                long n = System.Threading.Interlocked.Increment(ref _liveFrameCount);
                if (n == 1 || n % 30 == 0) LiveLog("hook frame #" + n);

                // 미리보기 FPS 캡 — 고MP 변환 부하/적체 완화(초과 프레임은 건너뜀).
                int fps = LivePreviewFps;
                if (fps > 0)
                {
                    long now = _liveSw.ElapsedMilliseconds;
                    if (now - _lastLiveTickMs < (1000 / fps)) return 0;
                    _lastLiveTickMs = now;
                }

                var bmp = BufferToBitmap();
                if (bmp != null) RaiseFrame(new GrabResult(bmp, 0, Info.Id));
            }
            catch { }
            return 0;
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
            _trigOffApplied = false;   // 트리거 모드가 외부에서 바뀌면 캐시 무효화 → 다음 Grab/Live 가 TriggerMode 재적용
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
                    _diskFallbackStreak = 0;   // 고속 경로 정상 → 폴백 연속 카운터 리셋
                    return bmp;
                }
                catch { /* 고속 경로 실패 → 아래 디스크 폴백으로 */ }
            }

            // 2) 폴백 — MbufExport(디스크). 느리지만 모든 포맷 안전.
            // 단, Mono 고속 경로가 연속 실패하면 매 프레임 디스크 쓰기가 폭주하므로 N회 이후 폴백 중단(표시 생략).
            if (_bands < 3 && _diskFallbackStreak >= 5) return null;
            try
            {
                MIL.MbufExport(_tmpPath, MIL.M_BMP, _buf);
                _diskFallbackStreak++;
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
