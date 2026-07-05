using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using Matrox.MatroxImagingLibrary;
using QMC.Common;
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
        private MIL_DIG_HOOK_FUNCTION_PTR _exposureEndHook;   // 노출 종료(ExposureEnd) 훅 델리게이트(GC 방지로 필드 보관)
        private MIL_DIG_HOOK_FUNCTION_PTR _frameStartHook;    // 프레임 전송 시작 훅 델리게이트(ExposureEnd 폴백용, GC 방지로 필드 보관)
        private volatile bool _expEndHwFired;                  // HW ExposureEnd 훅이 한 번이라도 발화했는지(폴백 억제)
        private long _expEndCount;                             // 발화 횟수(진단 로그용)
        private long _frameStartCount;                         // FRAME_START 횟수(진단 로그용)
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

            // ── ExposureEnd 훅 등록 ──────────────────────────────
            // ① 카메라 GenICam 이벤트 알림 켜기 시도(Hik 과 동일 개념) — 미지원 카메라는 TryFeature 가 조용히 무시.
            //    CXP 에서 노출을 카메라(Timed)가 제어하면 그래버 훅이 이 알림에 의존할 수 있다.
            TryFeatureS("EventSelector", "ExposureEnd");
            TryFeatureS("EventNotification", "On");

            // ② 그래버 M_GRAB_EXPOSURE_END 훅 — 그래버가 노출 신호를 제어/수신하는 구성에서 발화.
            _expEndHwFired = false;
            _expEndCount = 0;
            _frameStartCount = 0;
            _exposureEndHook = ExposureEndHook;
            try { MIL.MdigHookFunction(_dig, MIL.M_GRAB_EXPOSURE_END, _exposureEndHook, IntPtr.Zero); }
            catch (Exception ex) { _exposureEndHook = null; LiveLog("ExposureEnd 훅 등록 실패(미지원 가능): " + ex.Message); }

            // ③ 폴백: M_GRAB_FRAME_START(그래버가 프레임 수신 시작 = 노출 종료 직후) —
            //    HW ExposureEnd 훅이 한 번도 발화하지 않는 구성에서 이 시점으로 ExposureEnded 를 대체 발화한다.
            //    (글로벌 셔터 기준 노출 종료 후 readout/전송이 시작되므로 '기구 동작 앞당김' 계약을 만족)
            _frameStartHook = FrameStartHook;
            try { MIL.MdigHookFunction(_dig, MIL.M_GRAB_FRAME_START, _frameStartHook, IntPtr.Zero); }
            catch (Exception ex) { _frameStartHook = null; LiveLog("FRAME_START 훅 등록 실패: " + ex.Message); }

            // 열릴 때마다 현재(레시피/UI) 설정을 카메라에 재적용 — startup·Connect·재오픈 모두 동일 상태 보장.
            ApplyCurrentSettings();
            LiveLog("Open 상태: " + DescribeAcqState());
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
            try { if (_exposureEndHook != null) MIL.MdigHookFunction(_dig, MIL.M_GRAB_EXPOSURE_END + MIL.M_UNHOOK, _exposureEndHook, IntPtr.Zero); } catch { }
            _exposureEndHook = null;
            try { if (_frameStartHook != null) MIL.MdigHookFunction(_dig, MIL.M_GRAB_FRAME_START + MIL.M_UNHOOK, _frameStartHook, IntPtr.Zero); } catch { }
            _frameStartHook = null;
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
                // 라이브 중이 아니면 직전 단발 획득을 확실히 정지 → 다음 그랩이 깨끗이 재-arm.
                if (!_continuousOn)
                    try { MIL.MdigHalt(_dig); } catch { }
                EnsureSingleFrameGrabMode();

                // 단발 촬상 = AcquisitionMode SingleFrame + AcquisitionStart(=MdigGrab) → 1프레임.
                //   (노출 Timed, 스트로브는 DCF). 동기라 완료까지 블록한다.
                MIL.MdigGrab(_dig, _buf);

                var bmp = BufferToBitmap();
                if (bmp == null) return GrabResult.Fail("buffer→bitmap 실패", Info.Id);
                return new GrabResult(bmp, 0, Info.Id);
            }
            catch (Exception ex) { return GrabResult.Fail("MdigGrab: " + ex.Message, Info.Id); }
            finally { System.Threading.Interlocked.Exchange(ref _grabBusy, 0); }
        }

        private volatile bool _continuousOn;
        private readonly object _liveCtl = new object();   // StartLive/StopLive 직렬화 — Stop(MdigHalt) 진행 중 Start 겹침 방지
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

        /// <summary>라이브(연속) 모드 보장 — AcquisitionMode Continuous + TriggerMode Off. 변경 시에만 적용.
        /// 모드 전환 시 카메라가 AcquisitionFrameRate 를 재계산해 낮춰둘 수 있으므로 캐시 값을 재적용한다.</summary>
        private void EnsureContinuousLiveMode()
        {
            bool changed = false;
            if (_acqModeApplied != "Continuous")
            {
                TryFeatureS("AcquisitionMode", "Continuous");
                _acqModeApplied = "Continuous";
                changed = true;
            }
            if (!_trigOffApplied)
            {
                TryFeatureS("TriggerMode", "Off");
                _trigOffApplied = true;
                changed = true;
            }
            if (changed && AcquisitionFrameRate > 0)
                TryFeatureD("AcquisitionFrameRate", ClampFeatureRangeD("AcquisitionFrameRate", AcquisitionFrameRate));
        }

        public override void StartLive()
        {
            if (!IsOpen || IsGrabbing) return;
            // 직전 StopLive(MdigHalt, 수 초 블록 가능)가 다른 스레드에서 진행 중이면 완료까지 대기 후 시작.
            //   (UI 는 Stop/Live 를 각각 워커로 던지므로 겹칠 수 있다 — 겹치면 MIL 획득 상태가 꼬인다.)
            lock (_liveCtl)
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
                LiveLog("StartLive: continuousOn=" + _continuousOn + ", acqMode=" + _acqModeApplied + " | " + DescribeAcqState());
            }
        }

        /// <summary>주요 획득 상태 진단 문자열 — 트리거/프레임레이트/노출의 카메라 실제값(readback) + DCF 프레임레이트.
        /// 라이브 저속(예: DCF 트리거 1Hz 잔존) 원인 추적용.</summary>
        private string DescribeAcqState()
        {
            return "TriggerMode=" + InquireFeatureAsString("TriggerMode")
                 + " TriggerSource=" + InquireFeatureAsString("TriggerSource")
                 + " AcqFrameRate=" + InquireFeatureAsString("AcquisitionFrameRate")
                 + " FrameRateEnable=" + InquireFeatureAsString("AcquisitionFrameRateEnable")
                 + " ExposureMode=" + InquireFeatureAsString("ExposureMode")
                 + " ExposureTime=" + InquireFeatureAsString("ExposureTime")
                 + " DCF_fps=" + InquireDcfFrameRate();
        }

        /// <summary>GenICam feature 현재값을 문자열로 readback. 미지원/실패 시 "?".</summary>
        private string InquireFeatureAsString(string feature)
        {
            if (IsNull(_dig)) return "?";
            try
            {
                var sb = new System.Text.StringBuilder(256);
                MIL.MdigInquireFeature(_dig, MIL.M_FEATURE_VALUE_AS_STRING, feature, MIL.M_TYPE_STRING, sb);
                return sb.ToString();
            }
            catch { return "?"; }
        }

        /// <summary>DCF(디지타이저 구성)의 공칭 프레임레이트 readback. 실패 시 "?".</summary>
        private string InquireDcfFrameRate()
        {
            try { double fr = 0; MIL.MdigInquire(_dig, MIL.M_SELECTED_FRAME_RATE, ref fr); return fr.ToString("0.###"); }
            catch { return "?"; }
        }

        private void LiveLog(string msg)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "MilLive", Info.Id + " " + msg); } catch { }
        }

        public override void StopLive()
        {
            // IsGrabbing=false 는 락 밖에서 먼저 — 진행 중인 훅이 즉시 빠지게(RaiseFrame/UI 마샬링 차단).
            //   StartLive 가 락을 쥐고 있어도 새 프레임 발행부터 멈추게 한 뒤 직렬화 구간에 진입한다.
            IsGrabbing = false;
            lock (_liveCtl)
            {
                bool wasLive = _continuousOn;
                LiveLog("StopLive enter: continuousOn=" + _continuousOn + ", frames=" + _liveFrameCount);

                // 순서 중요 — MdigHalt 보다 먼저: ① IsGrabbing=false 로 진행 중인 훅이 즉시 빠지게,
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

        /// <summary>노출 종료 훅(M_GRAB_EXPOSURE_END) — MIL 내부 스레드에서 호출된다.
        /// 전송 완료(M_GRAB_FRAME_END)보다 앞서 도착하므로 즉시 ExposureEnded 를 발화해
        /// 다음 기구 동작을 앞당길 수 있다(HikGigECamera 와 동일 계약).
        /// <para>주의: 그래버가 노출 신호를 모르는 구성(카메라 Timed 노출 등)에서는 발화되지 않는다 →
        /// 그 경우 <see cref="FrameStartHook"/> 가 대체 발화한다.</para></summary>
        private MIL_INT ExposureEndHook(MIL_INT hookType, MIL_ID eventId, IntPtr userPtr)
        {
            try
            {
                _expEndHwFired = true;   // HW 훅 동작 확인 → FRAME_START 폴백 영구 억제
                long n = System.Threading.Interlocked.Increment(ref _expEndCount);
                if (n == 1) LiveLog("ExposureEnd HW 훅 첫 발화 확인");
                if (IsOpen) RaiseExposureEnded();
            }
            catch (Exception ex) { LiveLog("ExposureEnd 발화 예외: " + ex.Message); }
            return 0;
        }

        /// <summary>프레임 전송 시작 훅(M_GRAB_FRAME_START) — ExposureEnd 폴백.
        /// 그래버가 프레임 수신을 시작했다는 것은 센서 노출이 이미 끝났다는 뜻이므로,
        /// HW ExposureEnd 훅이 동작하지 않는 구성에서 이 시점에 ExposureEnded 를 발화한다.</summary>
        private MIL_INT FrameStartHook(MIL_INT hookType, MIL_ID eventId, IntPtr userPtr)
        {
            try
            {
                long n = System.Threading.Interlocked.Increment(ref _frameStartCount);
                if (n == 1) LiveLog("FRAME_START 훅 첫 발화 (HW ExposureEnd " + (_expEndHwFired ? "지원" : "미발화 → 폴백 사용") + ")");
                if (_expEndHwFired) return 0;   // HW 훅이 살아있으면 중복 발화 방지
                if (IsOpen) RaiseExposureEnded();
            }
            catch (Exception ex) { LiveLog("FRAME_START 폴백 발화 예외: " + ex.Message); }
            return 0;
        }

        public override void TriggerSoftware()
        {
            if (!IsOpen) return;
            try { MIL.MdigControl(_dig, MIL.M_GRAB_TRIGGER, (MIL_INT)MIL.M_ACTIVATE); } catch { }
        }

        // ── 파라미터 (GenICam feature; CameraLink/미지원 카메라면 무시) ──
        // DCF 는 링크/스트로브/포맷 baseline, 가변 파라미터는 GenICam feature 로 런타임 적용(일반적 구성).
        // 값은 카메라의 유효 범위(M_FEATURE_MIN/MAX)로 클램프 후 적용 — 범위 밖 값을 그대로 쓰면 MIL 이
        // 쓰기를 거부해 카메라에 남아있던 이전 값으로 동작한다(예: FrameRate 30 요청 → 거부 → 1fps 잔존).
        protected override void OnExposureChanged (double us)     => TryFeatureD("ExposureTime", ClampFeatureRangeD("ExposureTime", us));
        protected override void OnGainChanged     (double gainDb) => TryFeatureD("Gain", ClampFeatureRangeD("Gain", gainDb));
        protected override void OnFrameRateChanged(double fps)    => TryFeatureD("AcquisitionFrameRate", ClampFeatureRangeD("AcquisitionFrameRate", fps));

        /// <summary>GenICam float feature 값을 카메라 유효 범위(M_FEATURE_MIN/MAX)로 클램프.
        /// 범위 조회 실패(미지원 feature 등) 시 원값 그대로 반환. 클램프 발생 시 진단 로그를 남긴다.</summary>
        private double ClampFeatureRangeD(string feature, double val)
        {
            if (IsNull(_dig)) return val;
            try
            {
                double min = double.NaN, max = double.NaN;
                MIL.MdigInquireFeature(_dig, MIL.M_FEATURE_MIN, feature, MIL.M_TYPE_DOUBLE, ref min);
                MIL.MdigInquireFeature(_dig, MIL.M_FEATURE_MAX, feature, MIL.M_TYPE_DOUBLE, ref max);
                double clamped = val;
                if (!double.IsNaN(max) && max > 0 && clamped > max) clamped = max;
                if (!double.IsNaN(min) && !double.IsNaN(max) && clamped < min) clamped = min;
                if (clamped != val)
                    LiveLog(feature + " " + val + " → 유효범위(" + min + "~" + max + ") 클램프 " + clamped);
                return clamped;
            }
            catch { return val; }
        }

        /// <summary>Trigger Mode(On/Off) + Trigger Source 를 분리 적용.
        /// <para>MIL 경로의 단발/라이브는 MdigGrab/MdigGrabContinuous(AcquisitionStart)가 촬상을 구동하므로
        /// Software 모드는 카메라 TriggerMode=On 으로 올리지 않고 Off 로 둔다 — VNP 는 트리거 On 전환 시
        /// AcquisitionFrameRate 를 1로 재계산(무효화)하는 부작용이 있어 라이브/연속그랩이 1fps 로 떨어진다.</para></summary>
        protected override void OnTriggerModeChanged(CameraTriggerMode mode)
        {
            _trigOffApplied = false;   // 트리거 모드가 외부에서 바뀌면 캐시 무효화 → 다음 Grab/Live 가 TriggerMode 재적용
            switch (mode)
            {
                case CameraTriggerMode.Continuous:
                case CameraTriggerMode.Software:
                    TryFeatureS("TriggerMode", "Off");
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
            // 트리거 모드 전환은 카메라가 종속 feature(AcquisitionFrameRate 등)를 재계산/초기화할 수 있으므로
            // 캐시된 프레임레이트를 재적용해 원복한다(값은 유효 범위로 클램프).
            if (AcquisitionFrameRate > 0)
                TryFeatureD("AcquisitionFrameRate", ClampFeatureRangeD("AcquisitionFrameRate", AcquisitionFrameRate));
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

        /// <summary>MVS 카탈로그 노드 → MIL GenICam feature 적용(MVS의 SetParameterTyped 와 동일 역할).
        /// MIL/카메라가 해당 feature 를 지원하지 않으면 조용히 무시된다(TryFeature* 가 catch).</summary>
        public override void SetParameterTyped(string node, CameraParamKind kind, string value)
        {
            if (!IsOpen || IsNull(_dig) || string.IsNullOrEmpty(node)) return;
            try
            {
                switch (kind)
                {
                    case CameraParamKind.Float:
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var f)) TryFeatureD(node, f);
                        break;
                    case CameraParamKind.Int:
                        if (long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) TryFeatureI(node, i);
                        break;
                    case CameraParamKind.Bool:
                        TryFeatureS(node, (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase)) ? "True" : "False");
                        break;
                    case CameraParamKind.Enum:
                        if (!string.IsNullOrEmpty(value)) TryFeatureS(node, value);
                        break;
                    case CameraParamKind.Command:
                        try { MIL.MdigControlFeature(_dig, MIL.M_FEATURE_EXECUTE, node, MIL.M_DEFAULT, MIL.M_NULL); } catch { }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MilCamera] SetParameterTyped({node},{kind}) 실패: {ex.Message}");
            }
        }

        // UserSet(카메라 플래시 저장)은 카메라 GenICam 기능이며, MIL/프레임그래버 계열 카메라(VNP 등)는
        // 대개 지원하지 않는다. 따라서 MIL 은 UserSet 저장을 구현하지 않고(CameraBase 기본 = 미지원),
        // 설정 영속은 핸들러 레시피(Config/Recipe, 시작 시 재적용)로 처리한다.

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
        /// MIL 미가용(미설치/보드 없음)이면 **가상 항목(Mil/0..)** 을 노출한다 — 개인 PC(Sim 환경)에서도
        /// 실기와 동일하게 "Mil/n" 을 선택/설정할 수 있게(Sim=Real). 생성은 CameraFactory 가 SimCamera 로 대체.
        /// </summary>
        public static List<CameraInfo> Enumerate()
        {
            var list = new List<CameraInfo>();
            try
            {
                MilSystem.EnsureInit();
                if (!MilSystem.IsAvailable)
                {
                    for (int i = 0; i < VirtualSlotCount; i++)
                        list.Add(new CameraInfo
                        {
                            Id        = "Mil/" + i,
                            Model     = "Matrox MIL (가상 — 미설치, Sim 동작)",
                            Vendor    = "Matrox",
                            Transport = CameraTransport.CoaXPress
                        });
                    return list;
                }

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
        /// <summary>MIL 미가용 시 목록에 노출할 가상 디지타이저 수 — 실기 없는 개발 PC 용.</summary>
        private const int VirtualSlotCount = 2;

        /// <summary>MIL SDK/보드 가용 여부(최초 1회 초기화). CameraFactory 가 SimCamera 대체 판단에 사용.</summary>
        public static bool IsMilAvailable
        {
            get { MilSystem.EnsureInit(); return MilSystem.IsAvailable; }
        }

        // Dispose 는 CameraBase.Dispose() (→ Close()) 가 처리. 별도 override 불필요.
    }
}
