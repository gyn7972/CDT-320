using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Motion
{
    /// <summary>
    /// ��� ��� ���� �߻� ���̽� Ŭ����.<br/>
    /// <list type="bullet">
    ///   <item><description>�ùķ��̼� ���� ���� ? ���� �ϵ���� ���̵� UI/������ ���� ����.</description></item>
    ///   <item><description>��� ����̵� �޼���� <c>virtual</c> ? �Ǻ��� Ŭ������ override�Ͽ� API ����.</description></item>
    ///   <item><description><see cref="BaseComponent{TSetup,TConfig,TRecipe}"/> ��� ? ���� Ʈ���� Leaf ���� ����.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseAxis
        : BaseComponent<AxisSetup, AxisConfig, AxisRecipe>
    {
        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ���� ���� �ʵ�
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>��׶��� ���� ������Ʈ �½�ũ ��� ��ū �ҽ�.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>�ùķ��̼� �̵� ��ǥ ��ġ (CommandPosition ���纻).</summary>
        private double _simTargetPosition;

        /// <summary>Override�� ��ǥ ��ġ. NaN�̸� ����.</summary>
        private double _overrideTargetPosition = double.NaN;

        /// <summary>Override�� �ӵ�. NaN�̸� ����.</summary>
        private double _overrideVelocity = double.NaN;

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ���� ������Ƽ (protected set ? ���� Ŭ�������� ���� ���� ����)
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>�ǵ��(���ڴ�) ��� ���� ���� ��ġ.</summary>
        public double ActualPosition    { get; protected set; }

        /// <summary>�̵� �������� ������ ��ǥ ��ġ.</summary>
        public double CommandPosition   { get; protected set; }

        /// <summary>���� �̵� �ӵ�.</summary>
        public double CurrentVelocity   { get; protected set; }

        /// <summary>���� ON ���� ����.</summary>
        public bool IsServoOn           { get; protected set; }

        /// <summary>�̵� �� ����.</summary>
        public bool IsMoving            { get; protected set; }

        /// <summary>In-Position(INP) ��ȣ ? ��ǥ ��ġ ���� �� ����ȭ �Ϸ�.</summary>
        public bool IsInPosition        { get; protected set; }

        /// <summary>�˶� �߻� ����.</summary>
        public bool IsAlarm             { get; protected set; }

        /// <summary>���� ���� �Ϸ� ����.</summary>
        public bool IsHomeDone          { get; protected set; }

        /// <summary>�˶� �ڵ�.</summary>
        public uint AlarmCode           { get; protected set; }

        /// <summary>�÷��� ���� �ϵ���� ���� ���� ��ȣ.</summary>
        public bool Sensor_PEL          { get; protected set; }

        /// <summary>���̳ʽ� ���� �ϵ���� ���� ���� ��ȣ.</summary>
        public bool Sensor_MEL          { get; protected set; }

        /// <summary>���� ���� ��ȣ.</summary>
        public bool Sensor_ORG          { get; protected set; }

        /// <summary>���� ���� ���.</summary>
        protected MotionMode _currentMode;

        /// <summary>Jog �̵� ���� (+1 �Ǵ� -1, 0�̸� ����).</summary>
        protected int _jogDirection;

        // ──────────────────────────────────────────────
        //  상태 변경 이벤트 (외부 관찰자용: UI / Simulator Bridge 등)
        // ──────────────────────────────────────────────

        /// <summary>ActualPosition 이 변경될 때마다 발행.</summary>
        public event System.Action<BaseAxis, double> ActualPositionChanged;

        /// <summary>이동 시작 시점에 1회 발행 (IsMoving false→true).</summary>
        public event System.Action<BaseAxis> MoveStarted;

        /// <summary>이동 완료 시점에 1회 발행 (IsMoving true→false, 정상 완료).</summary>
        public event System.Action<BaseAxis> MoveCompleted;

        /// <summary>_lastBroadcastPosition: ActualPositionChanged 이벤트 중복 방지용 캐시.</summary>
        private double _lastBroadcastPosition = double.NaN;

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ������
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// <see cref="BaseAxis"/>�� �ʱ�ȭ�ϰ� ��׶��� ���� ������Ʈ �½�ũ�� �����Ѵ�.
        /// </summary>
        /// <param name="name">�� �̸� (��: "Z_Axis_Motor")</param>
        protected BaseAxis(string name) : base(name)
        {
            StartStatusUpdateTask();
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��1. �⺻ ���� �޼���
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>������ Ȱ��ȭ(ON)�Ѵ�.</summary>
        public virtual void ServoOn()
        {
            if (IsAlarm) return;
            IsServoOn = true;
        }

        /// <summary>������ ��Ȱ��ȭ(OFF)�Ѵ�.</summary>
        public virtual void ServoOff()
        {
            Stop();
            IsServoOn = false;
        }

        /// <summary>
        /// ���� ������ �����Ѵ�.
        /// ���� ���� ���� �̵��� ����ϰ� IsMoving�� false�� �����Ѵ�.
        /// </summary>
        public virtual void Stop()
        {
            IsMoving        = false;
            IsInPosition    = false;
            CurrentVelocity = 0.0;
            _currentMode    = MotionMode.None;
            _jogDirection   = 0;
        }

        /// <summary>
        /// ��� ����(Emergency Stop)�� �����Ѵ�.
        /// ��� �����ϸ� AlarmCode 1�� �����Ѵ�.
        /// </summary>
        public virtual void EStop()
        {
            Stop();
            IsAlarm   = true;
            AlarmCode = 1;
        }

        /// <summary>�˶��� �����Ѵ�. �ϵ���� �˶� ������ ���ŵ� �� ȣ���ؾ� �Ѵ�.</summary>
        public virtual void ResetAlarm()
        {
            IsAlarm   = false;
            AlarmCode = 0;
        }

        /// <summary>
        /// ���� ��ġ(ActualPosition, CommandPosition)�� ������ ������ ������ �����Ѵ�.
        /// �ַ� ���� ���� �� ��ǥ ������ ����Ѵ�.
        /// </summary>
        /// <param name="newPosition">���� ������ ��ġ ��</param>
        public virtual void SetPosition(double newPosition)
        {
            ActualPosition  = newPosition;
            CommandPosition = newPosition;
            _simTargetPosition = newPosition;
        }

        /// <summary>
        /// ���� Recipe�� �ӵ��������� �Ķ���͸� �����Ѵ�.
        /// </summary>
        /// <param name="velocity">�̵� �ӵ� [����/s]</param>
        /// <param name="acc">���ӵ� [����/s��]</param>
        /// <param name="dec">���ӵ� [����/s��]</param>
        public virtual void SetMotionProfile(double velocity, double acc, double dec)
        {
            Recipe.DefaultVelocity = velocity;
            Recipe.Acceleration    = acc;
            Recipe.Deceleration    = dec;
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��2. �̵� �޼���
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// ���� ��ǥ�� �񵿱� �̵��Ѵ�.
        /// �̵� �Ϸ�(InPosition) �Ǵ� �˶� �߻� �ñ��� ����Ѵ�.
        /// </summary>
        /// <param name="targetPos">��ǥ ���� ��ġ</param>
        /// <param name="velocity">�̵� �ӵ� (0 �����̸� Recipe.DefaultVelocity ���)</param>
        public virtual async Task MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            if (!IsServoOn || IsAlarm) return;

            double vel         = velocity > 0 ? velocity : Recipe.DefaultVelocity;
            CommandPosition    = targetPos;
            _simTargetPosition = targetPos;
            CurrentVelocity    = vel;
            IsMoving           = true;
            IsInPosition       = false;
            _currentMode       = MotionMode.Absolute;

            RaiseMoveStarted();

            await WaitUntilMoveDone(_cts.Token);
        }

        // ──────────────────────────────────────────────
        //  이벤트 발행 헬퍼
        // ──────────────────────────────────────────────

        protected void RaisePositionChanged()
        {
            // 이벤트 구독자가 없거나 위치가 동일하면 스킵
            var h = ActualPositionChanged;
            if (h == null) return;
            if (!double.IsNaN(_lastBroadcastPosition) && _lastBroadcastPosition == ActualPosition) return;
            _lastBroadcastPosition = ActualPosition;
            try { h(this, ActualPosition); } catch { }
        }

        protected void RaiseMoveStarted()
        {
            var h = MoveStarted;
            if (h == null) return;
            try { h(this); } catch { }
        }

        protected void RaiseMoveCompleted()
        {
            var h = MoveCompleted;
            if (h == null) return;
            try { h(this); } catch { }
        }

        /// <summary>
        /// ���� ��ġ���� ��� �Ÿ���ŭ �񵿱� �̵��Ѵ�.
        /// </summary>
        /// <param name="distance">�̵� �Ÿ� (�����̸� ���̳ʽ� ����)</param>
        /// <param name="velocity">�̵� �ӵ� (0 �����̸� Recipe.DefaultVelocity ���)</param>
        public virtual async Task MoveRelativeAsync(double distance, double velocity = 0)
        {
            double targetPos = ActualPosition + distance;
            await MoveAbsoluteAsync(targetPos, velocity);
        }

        /// <summary>
        /// ���� ���� �������� �񵿱�� �����Ѵ�.<br/>
        /// �ùķ��̼�: ���̳ʽ� ���� ������ �̵� �� HomeOffset�� �����Ͽ� ��ǥ�� �����Ѵ�.
        /// </summary>
        public virtual async Task HomeSearchAsync()
        {
            if (!IsServoOn || IsAlarm) return;

            _currentMode = MotionMode.Homing;
            IsHomeDone   = false;

            // ���� �ùķ��̼�: SoftLimitMinus ��ġ�� �̵��Ͽ� ���� ���� ��� ����������
            double homeTarget  = Setup.SoftLimitMinus + 1.0; // ���� ���� ����
            CommandPosition    = homeTarget;
            _simTargetPosition = homeTarget;
            CurrentVelocity    = Recipe.HomeVelocity;
            IsMoving           = true;
            IsInPosition       = false;

            await WaitUntilMoveDone(_cts.Token);

            if (IsAlarm) return;

            // ���� ���� ������ ���� (��ǥ �纸��) ������������������������������������������������������������
            SetPosition(Setup.HomeOffset);
            Sensor_ORG   = true;
            IsHomeDone   = true;
            _currentMode = MotionMode.None;
        }

        /// <summary>
        /// IsMoving�� false�� �ǰ� IsInPosition�� true�� �� ������,
        /// �Ǵ� IsAlarm�� �߻��� ������ 10ms �������� ����Ѵ�.
        /// </summary>
        /// <param name="ct">��� ��ū</param>
        protected async Task WaitUntilMoveDone(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (IsAlarm)               break;
                if (!IsMoving && IsInPosition) break;

                await Task.Delay(10, ct).ContinueWith(_ => { }); // ��� ���� ����
            }
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��3. Jog �� Override ����
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// JogSpeedType�� ���� ������ �ӵ� ���� ��ȯ�ϴ� ���� ��ƿ��Ƽ.
        /// </summary>
        /// <param name="speedType">�ӵ� �ܰ�</param>
        /// <param name="customVel">Custom ���� �� ����� �ӵ� ��</param>
        /// <returns>������ Jog �ӵ�</returns>
        protected double GetJogVelocity(JogSpeedType speedType, double customVel)
        {
            switch (speedType)
            {
                case JogSpeedType.Coarse:
                    return Recipe.JogCoarseVelocity;
                case JogSpeedType.Fine:
                    return Recipe.JogFineVelocity;
                case JogSpeedType.Custom:
                    return customVel > 0 ? customVel : Recipe.JogFineVelocity;
                default:
                    return Recipe.JogFineVelocity;
            }
        }

        /// <summary>
        /// Jog ��ư�� ������ �ִ� ���� �������� �̵��Ѵ�.
        /// �ùķ��̼� ������ _jogDirection�� CurrentVelocity�� ������� �� ƽ ��ġ�� �����Ѵ�.
        /// </summary>
        /// <param name="direction">+1 (�÷��� ����) �Ǵ� -1 (���̳ʽ� ����)</param>
        /// <param name="speedType">�ӵ� �ܰ�</param>
        /// <param name="customVel">Custom ���� �� �ӵ� �� (�⺻��: 0)</param>
        public virtual void MoveJogContinuous(int direction, JogSpeedType speedType,
                                              double customVel = 0)
        {
            if (!IsServoOn || IsAlarm) return;

            _jogDirection   = direction;
            CurrentVelocity = GetJogVelocity(speedType, customVel);
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Jog;

            // CommandPosition�� ���� ���� ������ ���� ? �ùķ����Ͱ� �� ƽ ����
            CommandPosition    = direction > 0 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
            _simTargetPosition = CommandPosition;
        }

        /// <summary>
        /// Ŭ�� 1ȸ�� stepDistance��ŭ ��� �̵��ϴ� Step Jog�� �񵿱�� �����Ѵ�.
        /// </summary>
        /// <param name="direction">+1 (�÷��� ����) �Ǵ� -1 (���̳ʽ� ����)</param>
        /// <param name="speedType">�ӵ� �ܰ�</param>
        /// <param name="stepDistance">1ȸ �̵� �Ÿ� (��� ���밪)</param>
        /// <param name="customVel">Custom ���� �� �ӵ� �� (�⺻��: 0)</param>
        public virtual async Task MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                   double stepDistance, double customVel = 0)
        {
            double vel = GetJogVelocity(speedType, customVel);
            await MoveRelativeAsync(direction * Math.Abs(stepDistance), vel);
        }

        /// <summary>
        /// Jog �̵��� �����Ѵ�. <see cref="Stop"/>�� ���������� �ǹ̸� ���������� ǥ���Ѵ�.
        /// </summary>
        public virtual void StopJog()
        {
            Stop();
        }

        /// <summary>
        /// �̵� �� ��ǥ �ӵ��� �ǽð����� ����(Override)�Ѵ�.
        /// </summary>
        /// <param name="newVelocity">������ �� �ӵ�</param>
        public virtual void OverrideVelocity(double newVelocity)
        {
            if (newVelocity <= 0) return;
            _overrideVelocity = newVelocity;
            CurrentVelocity   = newVelocity;
        }

        /// <summary>
        /// �̵� �� ��ǥ ��ġ�� �ǽð����� ����(Override)�Ѵ�.
        /// </summary>
        /// <param name="newTargetPosition">������ �� ��ǥ ��ġ</param>
        public virtual void OverridePosition(double newTargetPosition)
        {
            _overrideTargetPosition = newTargetPosition;
            CommandPosition         = newTargetPosition;
            _simTargetPosition      = newTargetPosition;
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��4. ��׶��� ���� ������Ʈ ����
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// 10ms �ֱ�� <see cref="UpdateStatus"/>�� ȣ���ϴ� ��׶��� �½�ũ�� �����Ѵ�.
        /// �½�ũ�� ��ü �Ҹ� �� <see cref="Dispose"/>�� ���� ��ҵȴ�.
        /// </summary>
        private void StartStatusUpdateTask()
        {
            CancellationToken token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        UpdateStatus();
                        await Task.Delay(10, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // ���� ������Ʈ �� ���ܴ� ������ �ߴܽ�Ű�� �ʴ´�.
                        await Task.Delay(10, token).ContinueWith(_ => { });
                    }
                }
            }, token);
        }

        /// <summary>
        /// 10ms �ֱ�� ȣ��Ǵ� ���� ���� �޼���.<br/>
        /// <list type="bullet">
        ///   <item><description>�ùķ��̼� ���(<see cref="AxisConfig.IsSimulationMode"/> = true):
        ///     <see cref="SimulateMotion"/>�� ȣ���Ͽ� ���� ��ġ�� ����Ѵ�.</description></item>
        ///   <item><description>�Ǻ��� ���: override�Ͽ� ���� API�� ���¸� �д´�.</description></item>
        /// </list>
        /// </summary>
        public virtual void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                SimulateMotion();
            }
            // else: �Ǻ��� ��� ? ���� Ŭ�������� override�Ͽ� ����
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��5. �ùķ��̼� ����
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// 10ms ������ ���� ��� �ùķ��̼� ����.<br/>
        /// <list type="bullet">
        ///   <item><description>CurrentVelocity ������� �� ƽ �̵� �Ÿ��� ����Ѵ�.</description></item>
        ///   <item><description>��ǥ ��ġ ���� �� �̵� �Ϸ� ó���� �Ѵ�.</description></item>
        ///   <item><description>����Ʈ ���� �ʰ� �� �˶��� �߻���Ű�� �����Ѵ�.</description></item>
        /// </list>
        /// </summary>
        protected virtual void SimulateMotion()
        {
            if (!IsMoving) return;

            // Override �� �ݿ�
            if (!double.IsNaN(_overrideVelocity))
            {
                CurrentVelocity     = _overrideVelocity;
                _overrideVelocity   = double.NaN;
            }

            if (!double.IsNaN(_overrideTargetPosition))
            {
                _simTargetPosition        = _overrideTargetPosition;
                _overrideTargetPosition   = double.NaN;
            }

            // 10ms ���� �̵� �Ÿ� ��� (����: [����/s] * 0.01s)
            const double tickSeconds = 0.01;
            double step = CurrentVelocity * tickSeconds;

            double remaining = _simTargetPosition - ActualPosition;

            if (_currentMode == MotionMode.Jog)
            {
                // Jog: ���⿡ ���� step�� �̵�, ���Կ� �����ϸ� ����
                ActualPosition += _jogDirection * step;
                RaisePositionChanged();
            }
            else
            {
                // ���롤��� �̵�: ��ǥ�� ���� step�� ����
                if (Math.Abs(remaining) <= step)
                {
                    // ��ǥ ����
                    ActualPosition  = _simTargetPosition;
                    CommandPosition = _simTargetPosition;
                    IsMoving        = false;
                    IsInPosition    = true;
                    CurrentVelocity = 0.0;
                    _currentMode    = MotionMode.None;
                    RaisePositionChanged();
                    RaiseMoveCompleted();
                    return;
                }

                ActualPosition += Math.Sign(remaining) * step;
                RaisePositionChanged();
            }

            // ���� ����Ʈ ���� ���� ��������������������������������������������������������������������������������������
            if (ActualPosition >= Setup.SoftLimitPlus)
            {
                ActualPosition = Setup.SoftLimitPlus;
                TriggerSoftLimitAlarm(alarmCode: 10);
                return;
            }

            if (ActualPosition <= Setup.SoftLimitMinus)
            {
                ActualPosition = Setup.SoftLimitMinus;
                TriggerSoftLimitAlarm(alarmCode: 11);
                return;
            }
        }

        /// <summary>
        /// ����Ʈ ���� ���� �� �˶��� �߻���Ű�� ��� �����Ѵ�.
        /// </summary>
        /// <param name="alarmCode">������ �˶� �ڵ� (10: PEL, 11: MEL)</param>
        private void TriggerSoftLimitAlarm(uint alarmCode)
        {
            IsMoving        = false;
            IsInPosition    = false;
            CurrentVelocity = 0.0;
            IsAlarm         = true;
            AlarmCode       = alarmCode;
            _currentMode    = MotionMode.None;
            _jogDirection   = 0;
        }

        // ��������������������������������������������������������������������������������������������������������������������������������������������
        //  ��6. IDisposable ? ��׶��� �½�ũ ����
        // ��������������������������������������������������������������������������������������������������������������������������������������������

        /// <summary>
        /// ��׶��� ���� ������Ʈ �½�ũ�� ����ϰ� ���ҽ��� �����Ѵ�.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
