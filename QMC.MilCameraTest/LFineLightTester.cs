using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization;
using QMC.Common.Data.Store;
using QMC.Vision.Optics.LFine;

namespace QMC.MilCameraTest
{
    /// <summary>
    /// 엘파인 LCP24-100PS/VS 조명 테스트 클라이언트 — 페이지(1~12)별 채널 밝기(ON-TIME) 설정.
    /// <para>
    /// 매뉴얼(LCP24-100PS 2021.04): RS-232C 115200bps 8N1, 페이지 "00"~"11"(12개),
    /// 채널 "00"~"15"(16개), ON-TIME 0~1500µs(10µs 단위), 무응답(fire-and-forget).
    /// UI 페이지는 1~12 로 표기하고 장비 와이어 페이지(0~11)로 변환해 송신한다.
    /// </para>
    /// 마지막 설정값(포트/선택 페이지/페이지별 채널값)은 실행 폴더 lfine_light.json 에 저장/복원한다.
    /// </summary>
    public sealed class LFineLightTester : IDisposable
    {
        // ── Const ──────────────────────────────────────
        public const int PageCount = 12;       // 매뉴얼 페이지 "00"~"11"
        public const int ChannelCount = 16;
        public const int MaxOnTimeUs = 1500;   // 10µs 단위, 매뉴얼 "000"~"150"

        // ── Fields ─────────────────────────────────────
        private SerialPort _port;
        private readonly object _txLock = new object();
        private readonly int[][] _pageOnTimesUs;   // [페이지 0~11][채널 0~15] µs
        private readonly string _configPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lfine_light.json");

        // ── Constructor ────────────────────────────────
        public LFineLightTester()
        {
            _pageOnTimesUs = new int[PageCount][];
            for (int p = 0; p < PageCount; p++) _pageOnTimesUs[p] = new int[ChannelCount];
        }

        // ── Properties ─────────────────────────────────
        public bool IsOpen => _port != null && _port.IsOpen;
        public string PortName { get; private set; } = "COM4";
        /// <summary>UI 선택 페이지(1~12). 적용/저장 대상.</summary>
        public int SelectedPage { get; set; } = 1;

        // ── Public Methods ─────────────────────────────
        /// <summary>UI 페이지(1~12)의 채널 작업값(µs) 버퍼. 반환 배열 직접 수정 가능(길이 16).</summary>
        public int[] GetPageBuffer(int uiPage)
        {
            int idx = ClampUiPage(uiPage) - 1;
            return _pageOnTimesUs[idx];
        }

        /// <summary>시리얼 연결 (115200bps 8N1 — 매뉴얼 §5).</summary>
        public bool Open(string portName, out string error)
        {
            error = null;
            try
            {
                Close();
                _port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                _port.Open();
                PortName = portName;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try { _port?.Dispose(); } catch { }
                _port = null;
                return false;
            }
        }

        public void Close()
        {
            try { if (_port != null && _port.IsOpen) _port.Close(); } catch { }
            try { _port?.Dispose(); } catch { }
            _port = null;
        }

        /// <summary>UI 페이지(1~12)에 채널 1~16 ON-TIME(µs) 일괄 적용 (SP 1프레임 — 와이어 페이지 = uiPage-1).
        /// 값은 0~1500µs 로 클램프되어 버퍼에도 반영된다(프로토콜은 ÷10 인코딩).</summary>
        public bool ApplyChannels(int uiPage, int[] onTimesUs, out string error)
        {
            error = null;
            if (onTimesUs == null || onTimesUs.Length != ChannelCount) { error = "채널 값 배열(16) 필요"; return false; }
            int page = ClampUiPage(uiPage);
            var buf = _pageOnTimesUs[page - 1];
            var clamped = new int[ChannelCount];
            for (int i = 0; i < ChannelCount; i++)
                clamped[i] = Clamp(onTimesUs[i]);
            Array.Copy(clamped, buf, ChannelCount);
            return SendFrame(LFineProtocol.PageOnTimeFrame(page - 1, clamped), out error);
        }

        /// <summary>UI 페이지(1~12)의 데이터를 장비 플래시에 저장 (WP — 전원 꺼도 유지).</summary>
        public bool SaveToDevice(int uiPage, out string error)
            => SendFrame(LFineProtocol.WritePageFrame(ClampUiPage(uiPage) - 1), out error);

        /// <summary>실행 폴더 lfine_light.json 에서 포트/선택 페이지/페이지별 채널값 복원. 파일 없으면 기본값 유지.</summary>
        public void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                using (var fs = File.OpenRead(_configPath))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(LFineLightTestConfig));
                    var cfg = (LFineLightTestConfig)ser.ReadObject(fs);
                    if (cfg == null) return;
                    if (!string.IsNullOrWhiteSpace(cfg.Port)) PortName = cfg.Port.Trim();
                    SelectedPage = ClampUiPage(cfg.SelectedPage);
                    if (cfg.PageOnTimesUs != null)
                        for (int p = 0; p < PageCount && p < cfg.PageOnTimesUs.Length; p++)
                        {
                            var src = cfg.PageOnTimesUs[p];
                            if (src == null) continue;
                            for (int c = 0; c < ChannelCount && c < src.Length; c++)
                                _pageOnTimesUs[p][c] = Clamp(src[c]);
                        }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[LFineLightTester] 설정 로드 실패: " + ex.Message); }
        }

        /// <summary>포트/선택 페이지/페이지별 채널값을 lfine_light.json 에 pretty UTF-8 로 저장.</summary>
        public void SaveConfig()
        {
            try
            {
                var pages = new int[PageCount][];
                for (int p = 0; p < PageCount; p++) pages[p] = (int[])_pageOnTimesUs[p].Clone();
                var cfg = new LFineLightTestConfig { Port = PortName, SelectedPage = SelectedPage, PageOnTimesUs = pages };
                using (var fs = File.Create(_configPath))
                    JsonPrettySerializer.WriteObject(fs, typeof(LFineLightTestConfig), cfg);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[LFineLightTester] 설정 저장 실패: " + ex.Message); }
        }

        public void Dispose()
        {
            Close();
        }

        // ── Private Methods ────────────────────────────
        private static int ClampUiPage(int uiPage)
        {
            if (uiPage < 1) return 1;
            if (uiPage > PageCount) return PageCount;
            return uiPage;
        }

        private static int Clamp(int us)
        {
            if (us < 0) return 0;
            if (us > MaxOnTimeUs) return MaxOnTimeUs;
            return us;
        }

        /// <summary>프레임 송신 — 무응답 프로토콜(fire-and-forget). 실패 사유는 error 로 반환.</summary>
        private bool SendFrame(byte[] frame, out string error)
        {
            error = null;
            try
            {
                if (_port == null || !_port.IsOpen) { error = "시리얼 미연결 — [연결] 먼저 수행"; return false; }
                lock (_txLock) _port.Write(frame, 0, frame.Length);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    /// <summary>lfine_light.json 영속 모델 — 포트 + 선택 페이지(1~12) + 페이지별 채널 ON-TIME(µs).</summary>
    [DataContract]
    public sealed class LFineLightTestConfig
    {
        [DataMember] public string Port { get; set; } = "COM4";
        [DataMember(IsRequired = false)] public int SelectedPage { get; set; } = 1;
        [DataMember(IsRequired = false)] public int[][] PageOnTimesUs { get; set; }
    }
}
