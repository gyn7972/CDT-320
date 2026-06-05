using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    /// <summary>
    /// CDT320Simulator(WPF 3D 뷰어)와 단방향 상태 통보용 TCP 클라이언트.
    /// <para>
    /// 정책(B안): QMC.CDT-320가 마스터이고 시뮬레이터는 순수 뷰어.
    /// 이 브릿지는 CDT320_Machine 트리의 축/DO/DI 이벤트를 구독하여
    /// 시뮬레이터의 기존 TCP 명령(AXIS_MOVE / DO_SET / DI_GET)으로 상태를 통보한다.
    /// </para>
    /// <para>
    /// 축 위치 쓰로틀: 50ms 간격 또는 0.1mm 변화 중 선도달 조건.
    /// 이동 완료(MoveCompleted) 시에는 쓰로틀 무시하고 최종 위치를 즉시 통보한다.
    /// </para>
    /// </summary>
    public class SimulatorBridge : IDisposable
    {
        // ──────────────────────────────────────────────
        //  상수
        // ──────────────────────────────────────────────

        private const int    PosThrottleMs        = 20;     // 50fps (was 50ms = 20fps)
        private const double PosChangeThresholdMm = 0.02;   // 0.02mm (was 0.1mm)
        private const int    ReconnectIntervalMs  = 3000;   // auto-reconnect 시도 간격
        private const int    HeartbeatIntervalMs  = 3000;   // PING 송신 주기
        private const int    HeartbeatTimeoutMs   = 8000;   // RX 무응답 시간 임계값 (이 시간 동안 RX 없으면 끊긴 것으로 판정)

        // ──────────────────────────────────────────────
        //  필드
        // ──────────────────────────────────────────────

        private readonly CDT320_Machine _machine;

        private TcpClient     _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly object _writeLock = new object();

        // 쓰로틀링 상태 (축 번호 기준)
        private readonly Dictionary<int, double>   _lastSentPos   = new Dictionary<int, double>();
        private readonly Dictionary<int, DateTime> _lastSentTime  = new Dictionary<int, DateTime>();

        // 매핑 테이블
        private readonly Dictionary<BaseAxis, int>               _axisMap = new Dictionary<BaseAxis, int>();
        private readonly Dictionary<BaseDigitalOutput, string>   _doMap   = new Dictionary<BaseDigitalOutput, string>();
        private readonly Dictionary<BaseDigitalInput,  string>   _diMap   = new Dictionary<BaseDigitalInput,  string>();

        // Auto-reconnect 상태
        private bool _autoReconnect;
        private CancellationTokenSource _reconnectCts;

        // Heartbeat 상태 — TCP socket 이 ungraceful close 됐을 때 빠르게 감지
        private System.Threading.Timer _heartbeatTimer;
        private DateTime _lastRxTime = DateTime.UtcNow;

        // ──────────────────────────────────────────────
        //  공개 상태
        // ──────────────────────────────────────────────

        public event Action<string> Log;
        public event Action<bool>   ConnectionChanged;

        public bool   IsConnected => _client != null && _client.Connected;
        public string Host { get; private set; }
        public int    Port { get; private set; }

        public int AxisMapCount => _axisMap.Count;
        public int DoMapCount   => _doMap.Count;
        public int DiMapCount   => _diMap.Count;

        // ──────────────────────────────────────────────
        //  생성자
        // ──────────────────────────────────────────────

        public SimulatorBridge(CDT320_Machine machine)
        {
            if (machine == null) throw new ArgumentNullException("machine");
            _machine = machine;
            BuildMaps();
            Instance = this;
        }

        /// <summary>프로세스 내 단일 SimulatorBridge 인스턴스 (Form1.Load 시 set).</summary>
        public static SimulatorBridge Instance { get; private set; }

        /// <summary>
        /// 시뮬레이터에 카메라 Expose 반짝임 효과를 송신한다.<br/>
        /// camId: "BOTTOM" | "SIDE1" | "SIDE2" (대소문자 무관)
        /// </summary>
        public void CameraExposeFlash(string camId, int durationMs = 100)
        {
            if (!IsConnected || string.IsNullOrEmpty(camId)) return;
            string esc = camId.Replace("\"", "");
            SendJson("{\"cmd\":\"CAMERA_FLASH\",\"cam\":\"" + esc + "\",\"ms\":" + durationMs + "}");
        }

        /// <summary>Stage 61 — 시뮬레이터 axis 번호 (0~37) 에 매핑된 QMC BaseAxis 를 reverse-lookup.</summary>
        public BaseAxis LookupAxisByNumber(int axisNo)
        {
            foreach (var kv in _axisMap)
            {
                if (kv.Value == axisNo) return kv.Key;
            }
            return null;
        }

        // ──────────────────────────────────────────────
        //  매핑 테이블 빌드
        //  사양서 축 번호표(0~36) ↔ QMC 컴포넌트
        //  LeftArm = FRONT PICKER (axis 9~18)
        //  RightArm = REAR PICKER (axis 21~30)
        // ──────────────────────────────────────────────

        private void BuildMaps()
        {
            var m = _machine;

            // Input side (0~7)
            _axisMap[m.InputCassette.InputLifterZ] = 0;   // InputLifterZ
            _axisMap[m.InputFeeder.FeederY]        = 1;   // InputFeederY
            _axisMap[m.InputStage.StageY]        = 2;   // WaferStageY
            _axisMap[m.InputStage.StageT]        = 3;   // WaferStageT
            _axisMap[m.InputStage.ExpanderZ]     = 4;   // WaferExpandingZ
            _axisMap[m.InputStage.CameraX]       = 5;   // InputVisionX
            _axisMap[m.InputStage.NeedleBlockX]  = 6;   // NeedleX
            _axisMap[m.InputStage.NeedleZ]       = 7;   // NeedleZ
            // Stage 44 — EjectPinZ 매핑 추가 (이전 미대응)
            _axisMap[m.InputStage.EjectPinZ]     = 8;   // EjectPinZ

            // FRONT PICKER (LeftArm): 9, 10, 11~18
            var left = m.PickerFront;
            _axisMap[left.ArmX] = 9;
            _axisMap[left.ArmY] = 10;
            for (int i = 0; i < 4; i++)
            {
                _axisMap[left.Pickers[i].PickerT] = 11 + i * 2;   // T0=11, T1=13, T2=15, T3=17
                _axisMap[left.Pickers[i].PickerZ] = 12 + i * 2;   // Z0=12, Z1=14, Z2=16, Z3=18
            }

            // REAR PICKER (RightArm): 21, 22, 23~30
            var right = m.PickerRear;
            _axisMap[right.ArmX] = 21;
            _axisMap[right.ArmY] = 22;
            for (int i = 0; i < 4; i++)
            {
                _axisMap[right.Pickers[i].PickerT] = 23 + i * 2;
                _axisMap[right.Pickers[i].PickerZ] = 24 + i * 2;
            }

            // Stage 44 — Side Vision Y 매핑 (이전 미대응)
            _axisMap[left.SideVisionY]  = 19;   // FrontSideVisionY0
            _axisMap[right.SideVisionY] = 20;   // REAR  SIDE VISION_Y0

            // Output side (31~36)
            _axisMap[m.OutputStage.GoodStage.StageY] = 31;   // OutputGoodStageY
            _axisMap[m.OutputStage.GoodStage.StageZ] = 32;   // OutputGoodStageZ
            _axisMap[m.OutputStage.NgStage.StageY]   = 33;   // OutputNGStageY
            _axisMap[m.OutputStage.BinCameraX]       = 34;   // OutputVisionX
            _axisMap[m.OutputFeeder.FeederY]         = 35;   // OutputFeederY
            _axisMap[m.OutputCassette.OutputLifterZ]    = 36;   // OutputLifterZ

            // ─── DO 매핑 ──────────────────────────────
            _doMap[m.InputStage.NeedleVacuum] = "Y046";

            for (int i = 0; i < 4; i++)
            {
                _doMap[left.Vacuums[i]]  = "Y" + (48 + i).ToString("D3"); // Y048~Y051
                _doMap[left.Blows[i]]    = "Y" + (56 + i).ToString("D3"); // Y056~Y059
                _doMap[right.Vacuums[i]] = "Y" + (64 + i).ToString("D3"); // Y064~Y067
                _doMap[right.Blows[i]]   = "Y" + (72 + i).ToString("D3"); // Y072~Y075
            }

            // ─── DI 매핑 (Stage 26) ──────────────────
            // 카세트/피더 관련 DI 를 시뮬레이터로 통보. 시뮬레이터에는 DI_SET 명령이
            // 없어 LOG 형태로만 송신되나, 추후 시뮬레이터 측 DI 시각화 추가 시
            // 본 매핑이 그대로 활용된다.
            _diMap[m.InputCassette.CassetteExistSensor] = "X060";
            _diMap[m.InputCassette.ProtrusionSensor]    = "X061";
            _diMap[m.InputCassette.WaferDetectSensor]   = "X062";
            _diMap[m.InputFeeder.WaferClampedSensor]    = "X063";
        }

        // ──────────────────────────────────────────────
        //  연결/해제
        // ──────────────────────────────────────────────

        public async Task ConnectAsync(string host, int port)
        {
            // Host/Port 기억 → auto-reconnect 가 같은 endpoint 로 시도
            Host = host;
            Port = port;
            _autoReconnect = true;

            if (IsConnected) return;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _cts    = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                LogMessage("[SimBridge] Connect 실패: " + ex.Message + " — auto-reconnect 시도");
                _client = null; _stream = null;
                EnsureReconnectLoop();
                return;
            }

            Subscribe();

            LogMessage($"SimulatorBridge connected to {host}:{port} " +
                       $"(axes={_axisMap.Count}, DO={_doMap.Count}, DI={_diMap.Count})");

            RaiseConnectionChanged(true);

            // 마스터 클레임: 시뮬레이터가 로컬 RUN CYCLE을 중지하도록 알림
            SendJson("{\"cmd\":\"HELLO\",\"role\":\"master\",\"from\":\"QMC.CDT-320\"}");

            // 초기 스냅샷: 현재 축 위치 + DO 상태 전송
            SendInitialSnapshot();

            _lastRxTime = DateTime.UtcNow;
            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            StartHeartbeat();
        }

        /// <summary>
        /// Heartbeat 타이머 시작 — 주기적으로 PING 송신하고 RX timeout 감지.
        /// _lastRxTime 이 HeartbeatTimeoutMs 이상 지났으면 connection dead 로 판정하고 Disconnect 트리거.
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatTimer = new System.Threading.Timer(_ =>
            {
                if (!IsConnected) return;
                try
                {
                    // PING 송신 (응답 카운트 안 함 — RX 자체가 keepalive)
                    SendJson("{\"cmd\":\"PING\"}");

                    // RX timeout 체크
                    var sinceRx = (DateTime.UtcNow - _lastRxTime).TotalMilliseconds;
                    if (sinceRx > HeartbeatTimeoutMs)
                    {
                        LogMessage("[SimBridge] heartbeat RX timeout (" + (int)sinceRx + "ms) — disconnecting + auto-reconnect");
                        Disconnect();   // _autoReconnect 가 true 면 EnsureReconnectLoop 트리거됨
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("[SimBridge] heartbeat ex: " + ex.Message);
                    Disconnect();
                }
            }, null, HeartbeatIntervalMs, HeartbeatIntervalMs);
        }

        private void StopHeartbeat()
        {
            try { _heartbeatTimer?.Dispose(); } catch { }
            _heartbeatTimer = null;
        }

        /// <summary>
        /// Sim 이 죽었거나 재시작된 경우 자동 재연결 백그라운드 루프.
        /// 한 번 ConnectAsync 호출 후 _autoReconnect=true 이면 연결 끊김 감지 시 자동 재연결.
        /// </summary>
        private void EnsureReconnectLoop()
        {
            if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested) return;
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _autoReconnect && !IsConnected)
                {
                    try
                    {
                        await Task.Delay(ReconnectIntervalMs, token);
                        if (IsConnected) break;
                        if (string.IsNullOrEmpty(Host) || Port <= 0) break;

                        LogMessage($"[SimBridge] auto-reconnect 시도 {Host}:{Port}");
                        var tc = new TcpClient();
                        await tc.ConnectAsync(Host, Port);
                        _client = tc;
                        _stream = tc.GetStream();
                        _cts    = new CancellationTokenSource();

                        Subscribe();
                        LogMessage($"[SimBridge] reconnected {Host}:{Port}");
                        RaiseConnectionChanged(true);

                        SendJson("{\"cmd\":\"HELLO\",\"role\":\"master\",\"from\":\"QMC.CDT-320\"}");
                        SendInitialSnapshot();
                        _lastRxTime = DateTime.UtcNow;
                        _ = Task.Run(() => ReceiveLoop(_cts.Token));
                        StartHeartbeat();
                        break;   // 성공 — 루프 종료
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        LogMessage("[SimBridge] reconnect 실패: " + ex.Message);
                        // 다음 interval 후 재시도
                    }
                }
            }, token);
        }

        public void Disconnect()
        {
            if (_client == null) return;

            StopHeartbeat();
            Unsubscribe();

            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            _stream = null;
            _client = null;
            _cts    = null;

            _lastSentPos.Clear();
            _lastSentTime.Clear();

            LogMessage("SimulatorBridge disconnected.");
            RaiseConnectionChanged(false);

            // _autoReconnect == true 이면 백그라운드에서 재연결 시도
            if (_autoReconnect) EnsureReconnectLoop();
        }

        /// <summary>완전 종료 (auto-reconnect 끄고 disconnect).</summary>
        public void DisconnectAndStop()
        {
            _autoReconnect = false;
            try { _reconnectCts?.Cancel(); } catch { }
            _reconnectCts = null;
            Disconnect();
        }

        public void Dispose() => DisconnectAndStop();

        // ──────────────────────────────────────────────
        //  구독 / 해제
        // ──────────────────────────────────────────────

        private void Subscribe()
        {
            foreach (var a in _axisMap.Keys)
            {
                a.ActualPositionChanged += OnAxisPositionChanged;
                a.MoveCompleted         += OnAxisMoveCompleted;
            }
            foreach (var d in _doMap.Keys)
                d.StateChanged += OnDoStateChanged;
            foreach (var d in _diMap.Keys)
                d.StateChanged += OnDiStateChanged;
        }

        private void Unsubscribe()
        {
            foreach (var a in _axisMap.Keys)
            {
                a.ActualPositionChanged -= OnAxisPositionChanged;
                a.MoveCompleted         -= OnAxisMoveCompleted;
            }
            foreach (var d in _doMap.Keys)
                d.StateChanged -= OnDoStateChanged;
            foreach (var d in _diMap.Keys)
                d.StateChanged -= OnDiStateChanged;
        }

        // ──────────────────────────────────────────────
        //  이벤트 핸들러 — 쓰로틀링 적용
        // ──────────────────────────────────────────────

        private void OnAxisPositionChanged(BaseAxis axis, double pos)
        {
            if (!IsConnected) return;
            if (!_axisMap.TryGetValue(axis, out int axisNo)) return;

            DateTime now = DateTime.UtcNow;

            // 쓰로틀: 50ms 간격 또는 0.1mm 변화 중 선도달 조건
            if (_lastSentTime.TryGetValue(axisNo, out DateTime lastT) &&
                _lastSentPos.TryGetValue(axisNo, out double lastP))
            {
                double dPos   = Math.Abs(pos - lastP);
                int    dMs    = (int)(now - lastT).TotalMilliseconds;
                if (dPos < PosChangeThresholdMm && dMs < PosThrottleMs) return;
            }

            _lastSentTime[axisNo] = now;
            _lastSentPos[axisNo]  = pos;

            SendJson(BuildAxisMove(axisNo, pos, vel: 0));
        }

        private void OnAxisMoveCompleted(BaseAxis axis)
        {
            if (!IsConnected) return;
            if (!_axisMap.TryGetValue(axis, out int axisNo)) return;

            // 완료 시점: 최종 위치 확실히 통보 + 쓰로틀 상태 갱신
            _lastSentTime[axisNo] = DateTime.UtcNow;
            _lastSentPos[axisNo]  = axis.ActualPosition;

            SendJson(BuildAxisMove(axisNo, axis.ActualPosition, vel: 0));
        }

        private void OnDoStateChanged(BaseDigitalOutput port, bool state)
        {
            if (!IsConnected) return;
            if (!_doMap.TryGetValue(port, out string sym)) return;
            SendJson(BuildDoSet(sym, state));
        }

        private void OnDiStateChanged(BaseDigitalInput port, bool state)
        {
            if (!IsConnected) return;
            if (!_diMap.TryGetValue(port, out string sym)) return;
            // Stage 26 — DI_SET 명령 송신. 시뮬레이터가 미구현 명령은 무시.
            SendJson("{\"cmd\":\"DI_SET\",\"port\":\"" + sym + "\",\"val\":" + (state ? "1" : "0") + "}");
            LogMessage($"[DI] {sym} = {state}");
        }

        // ──────────────────────────────────────────────
        //  초기 스냅샷
        // ──────────────────────────────────────────────

        private void SendInitialSnapshot()
        {
            // 축 위치
            foreach (var kv in _axisMap)
            {
                double pos = kv.Key.ActualPosition;
                _lastSentPos[kv.Value]  = pos;
                _lastSentTime[kv.Value] = DateTime.UtcNow;
                SendJson(BuildAxisMove(kv.Value, pos, vel: 1000.0)); // 빠르게 이동시켜 순간 반영
            }
            // DO 상태
            foreach (var kv in _doMap)
                SendJson(BuildDoSet(kv.Value, kv.Key.IsOn));
        }

        // ──────────────────────────────────────────────
        //  JSON 빌더 (zero-dependency)
        // ──────────────────────────────────────────────

        private static string BuildAxisMove(int axis, double pos, double vel)
        {
            var inv = CultureInfo.InvariantCulture;
            return "{\"cmd\":\"AXIS_MOVE\",\"axis\":" + axis +
                   ",\"pos\":" + pos.ToString("F3", inv) +
                   ",\"vel\":" + vel.ToString("F1", inv) + "}";
        }

        private static string BuildDoSet(string port, bool state)
        {
            return "{\"cmd\":\"DO_SET\",\"port\":\"" + port + "\",\"val\":" + (state ? "1" : "0") + "}";
        }

        // ──────────────────────────────────────────────
        //  TCP 송수신
        // ──────────────────────────────────────────────

        private void SendJson(string json)
        {
            if (_stream == null) return;
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            try
            {
                lock (_writeLock)
                {
                    _stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                LogMessage("[TX error] " + ex.Message);
                Disconnect();
            }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            byte[] buf = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && _client != null && _client.Connected)
                {
                    int n = await _stream.ReadAsync(buf, 0, buf.Length, ct);
                    if (n == 0) break;
                    // RX 발생 — heartbeat watchdog 카운터 갱신
                    _lastRxTime = DateTime.UtcNow;
                    // 시뮬레이터가 보낸 ACK/이벤트 수신 — 로그는 verbose 라 생략 (heartbeat 만 활용)
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogMessage("[RX error] " + ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        // ──────────────────────────────────────────────
        //  헬퍼
        // ──────────────────────────────────────────────

        private void LogMessage(string msg)
        {
            var h = Log;
            if (h != null) try { h(msg); } catch { }
        }

        private void RaiseConnectionChanged(bool connected)
        {
            var h = ConnectionChanged;
            if (h != null) try { h(connected); } catch { }
        }
    }
}
