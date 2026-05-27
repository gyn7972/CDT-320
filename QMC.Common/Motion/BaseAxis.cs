using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Motion
{
    /// <summary>
    /// 占쏙옙占?占쏙옙占?占쏙옙占쏙옙 占쌩삼옙 占쏙옙占싱쏙옙 클占쏙옙占쏙옙.<br/>
    /// <list type="bullet">
    ///   <item><description>占시뮬뤄옙占싱쇽옙 占쏙옙占쏙옙 占쏙옙占쏙옙 ? 占쏙옙占쏙옙 占싹듸옙占쏙옙占?占쏙옙占싱듸옙 UI/占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙.</description></item>
    ///   <item><description>占쏙옙占?占쏙옙占쏘·占싱듸옙 占쌨쇽옙占쏙옙占?<c>virtual</c> ? 占실븝옙占쏙옙 클占쏙옙占쏙옙占쏙옙 override占싹울옙 API 占쏙옙占쏙옙.</description></item>
    ///   <item><description><see cref="BaseComponent{TSetup,TConfig,TRecipe}"/> 占쏙옙占?? 占쏙옙占쏙옙 트占쏙옙占쏙옙 Leaf 占쏙옙占쏙옙 占쏙옙占쏙옙.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseAxis
        : BaseComponent<AxisSetup, AxisConfig, AxisRecipe>
    {
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙占쏙옙 占쏙옙占쏙옙 占십듸옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>占쏙옙溜占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트 占승쏙옙크 占쏙옙占?占쏙옙큰 占쌀쏙옙.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>占시뮬뤄옙占싱쇽옙 占싱듸옙 占쏙옙표 占쏙옙치 (CommandPosition 占쏙옙占썹본).</summary>
        private double _simTargetPosition;

        /// <summary>Override占쏙옙 占쏙옙표 占쏙옙치. NaN占싱몌옙 占쏙옙占쏙옙.</summary>
        private double _overrideTargetPosition = double.NaN;

        /// <summary>Override占쏙옙 占쌈듸옙. NaN占싱몌옙 占쏙옙占쏙옙.</summary>
        private double _overrideVelocity = double.NaN;

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙티 (protected set ? 占쏙옙占쏙옙 클占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙)
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>占실듸옙占?占쏙옙占쌘댐옙) 占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙치.</summary>
        public double ActualPosition    { get; protected set; }

        /// <summary>占싱듸옙 占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙표 占쏙옙치.</summary>
        public double CommandPosition   { get; protected set; }

        /// <summary>占쏙옙占쏙옙 占싱듸옙 占쌈듸옙.</summary>
        public double CurrentVelocity   { get; protected set; }

        /// <summary>占쏙옙占쏙옙 ON 占쏙옙占쏙옙 占쏙옙占쏙옙.</summary>
        public bool IsServoOn           { get; protected set; }

        /// <summary>占싱듸옙 占쏙옙 占쏙옙占쏙옙.</summary>
        public bool IsMoving            { get; protected set; }

        /// <summary>In-Position(INP) 占쏙옙호 ? 占쏙옙표 占쏙옙치 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙화 占싹뤄옙.</summary>
        public bool IsInPosition        { get; protected set; }

        /// <summary>占싯띰옙 占쌩삼옙 占쏙옙占쏙옙.</summary>
        public bool IsAlarm             { get; protected set; }

        /// <summary>占쏙옙占쏙옙 占쏙옙占쏙옙 占싹뤄옙 占쏙옙占쏙옙.</summary>
        public bool IsHomeDone          { get; protected set; }

        /// <summary>占싯띰옙 占쌘듸옙.</summary>
        public uint AlarmCode           { get; protected set; }

        /// <summary>占시뤄옙占쏙옙 占쏙옙占쏙옙 占싹듸옙占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙호.</summary>
        public bool Sensor_PEL          { get; protected set; }

        /// <summary>占쏙옙占싱너쏙옙 占쏙옙占쏙옙 占싹듸옙占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙호.</summary>
        public bool Sensor_MEL          { get; protected set; }

        /// <summary>占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙호.</summary>
        public bool Sensor_ORG          { get; protected set; }

        /// <summary>占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占?</summary>
        protected MotionMode _currentMode;

        /// <summary>Jog 占싱듸옙 占쏙옙占쏙옙 (+1 占실댐옙 -1, 0占싱몌옙 占쏙옙占쏙옙).</summary>
        protected int _jogDirection;

        // ??????????????????????????????????????????????
        //  ?곹깭 蹂寃??대깽??(?몃? 愿李곗옄?? UI / Simulator Bridge ??
        // ??????????????????????????????????????????????

        /// <summary>ActualPosition ??蹂寃쎈맆 ?뚮쭏??諛쒗뻾.</summary>
        public event System.Action<BaseAxis, double> ActualPositionChanged;

        /// <summary>?대룞 ?쒖옉 ?쒖젏??1??諛쒗뻾 (IsMoving false?뭪rue).</summary>
        public event System.Action<BaseAxis> MoveStarted;

        /// <summary>?대룞 ?꾨즺 ?쒖젏??1??諛쒗뻾 (IsMoving true?뭚alse, ?뺤긽 ?꾨즺).</summary>
        public event System.Action<BaseAxis> MoveCompleted;

        /// <summary>_lastBroadcastPosition: ActualPositionChanged ?대깽??以묐났 諛⑹???罹먯떆.</summary>
        private double _lastBroadcastPosition = double.NaN;

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙占쏙옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// <see cref="BaseAxis"/>占쏙옙 占십깍옙화占싹곤옙 占쏙옙溜占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트 占승쏙옙크占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        /// <param name="name">占쏙옙 占싱몌옙 (占쏙옙: "Z_Axis_Motor")</param>
        protected BaseAxis(string name) : base(name)
        {
            StartStatusUpdateTask();
        }

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙1. 占썩본 占쏙옙占쏙옙 占쌨쇽옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>占쏙옙占쏙옙占쏙옙 활占쏙옙화(ON)占싼댐옙.</summary>
        public virtual void ServoOn()
        {
            if (IsAlarm) return;
            IsServoOn = true;
        }

        /// <summary>占쏙옙占쏙옙占쏙옙 占쏙옙활占쏙옙화(OFF)占싼댐옙.</summary>
        public virtual void ServoOff()
        {
            Stop();
            IsServoOn = false;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占싱듸옙占쏙옙 占쏙옙占쏙옙構占?IsMoving占쏙옙 false占쏙옙 占쏙옙占쏙옙占싼댐옙.
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
        /// 占쏙옙占?占쏙옙占쏙옙(Emergency Stop)占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// 占쏙옙占?占쏙옙占쏙옙占싹몌옙 AlarmCode 1占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        public virtual void EStop()
        {
            Stop();
            IsAlarm   = true;
            AlarmCode = 1;
        }

        /// <summary>占싯띰옙占쏙옙 占쏙옙占쏙옙占싼댐옙. 占싹듸옙占쏙옙占?占싯띰옙 占쏙옙占쏙옙占쏙옙 占쏙옙占신듸옙 占쏙옙 호占쏙옙占쌔억옙 占싼댐옙.</summary>
        public virtual void ResetAlarm()
        {
            IsAlarm   = false;
            AlarmCode = 0;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙치(ActualPosition, CommandPosition)占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// 占쌍뤄옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙표 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙磯占?
        /// </summary>
        /// <param name="newPosition">占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙치 占쏙옙</param>
        public virtual void SetPosition(double newPosition)
        {
            ActualPosition  = newPosition;
            CommandPosition = newPosition;
            _simTargetPosition = newPosition;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 Recipe占쏙옙 占쌈듸옙占쏙옙占쏙옙占쏙옙占쏙옙 占식띰옙占쏙옙拷占?占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        /// <param name="velocity">占싱듸옙 占쌈듸옙 [占쏙옙占쏙옙/s]</param>
        /// <param name="acc">占쏙옙占쌈듸옙 [占쏙옙占쏙옙/s占쏙옙]</param>
        /// <param name="dec">占쏙옙占쌈듸옙 [占쏙옙占쏙옙/s占쏙옙]</param>
        public virtual void SetMotionProfile(double velocity, double acc, double dec)
        {
            Recipe.DefaultVelocity = velocity;
            Recipe.Acceleration    = acc;
            Recipe.Deceleration    = dec;
        }

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙2. 占싱듸옙 占쌨쇽옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙표占쏙옙 占쏟동깍옙 占싱듸옙占싼댐옙.
        /// 占싱듸옙 占싹뤄옙(InPosition) 占실댐옙 占싯띰옙 占쌩삼옙 占시깍옙占쏙옙 占쏙옙占쏙옙磯占?
        /// </summary>
        /// <param name="targetPos">占쏙옙표 占쏙옙占쏙옙 占쏙옙치</param>
        /// <param name="velocity">占싱듸옙 占쌈듸옙 (0 占쏙옙占쏙옙占싱몌옙 Recipe.DefaultVelocity 占쏙옙占?</param>
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

        // ??????????????????????????????????????????????
        //  ?대깽??諛쒗뻾 ?ы띁
        // ??????????????????????????????????????????????

        protected void RaisePositionChanged()
        {
            // ?대깽??援щ룆?먭? ?녾굅???꾩튂媛 ?숈씪?섎㈃ ?ㅽ궢
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
        /// 占쏙옙占쏙옙 占쏙옙치占쏙옙占쏙옙 占쏙옙占?占신몌옙占쏙옙큼 占쏟동깍옙 占싱듸옙占싼댐옙.
        /// </summary>
        /// <param name="distance">占싱듸옙 占신몌옙 (占쏙옙占쏙옙占싱몌옙 占쏙옙占싱너쏙옙 占쏙옙占쏙옙)</param>
        /// <param name="velocity">占싱듸옙 占쌈듸옙 (0 占쏙옙占쏙옙占싱몌옙 Recipe.DefaultVelocity 占쏙옙占?</param>
        public virtual async Task MoveRelativeAsync(double distance, double velocity = 0)
        {
            double targetPos = ActualPosition + distance;
            await MoveAbsoluteAsync(targetPos, velocity);
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占쏟동깍옙占?占쏙옙占쏙옙占싼댐옙.<br/>
        /// 占시뮬뤄옙占싱쇽옙: 占쏙옙占싱너쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占싱듸옙 占쏙옙 HomeOffset占쏙옙 占쏙옙占쏙옙占싹울옙 占쏙옙표占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        public virtual async Task HomeSearchAsync()
        {
            if (!IsServoOn || IsAlarm) return;

            _currentMode = MotionMode.Homing;
            IsHomeDone   = false;

            // 占쏙옙占쏙옙 占시뮬뤄옙占싱쇽옙: SoftLimitMinus 占쏙옙치占쏙옙 占싱듸옙占싹울옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占?占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
            double homeTarget  = Setup.SoftLimitMinus + 1.0; // 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙
            CommandPosition    = homeTarget;
            _simTargetPosition = homeTarget;
            CurrentVelocity    = Recipe.HomeVelocity;
            IsMoving           = true;
            IsInPosition       = false;

            await WaitUntilMoveDone(_cts.Token);

            if (IsAlarm) return;

            // 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 (占쏙옙표 占썹보占쏙옙) 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
            SetPosition(Setup.HomeOffset);
            Sensor_ORG   = true;
            IsHomeDone   = true;
            _currentMode = MotionMode.None;
        }

        /// <summary>
        /// IsMoving占쏙옙 false占쏙옙 占실곤옙 IsInPosition占쏙옙 true占쏙옙 占쏙옙 占쏙옙占쏙옙占쏙옙,
        /// 占실댐옙 IsAlarm占쏙옙 占쌩삼옙占쏙옙 占쏙옙占쏙옙占쏙옙 10ms 占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙磯占?
        /// </summary>
        /// <param name="ct">占쏙옙占?占쏙옙큰</param>
        protected async Task WaitUntilMoveDone(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (IsAlarm)               break;
                if (!IsMoving && IsInPosition) break;

                await Task.Delay(10, ct).ContinueWith(_ => { }); // 占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙
            }
        }

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙3. Jog 占쏙옙 Override 占쏙옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// JogSpeedType占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쌈듸옙 占쏙옙占쏙옙 占쏙옙환占싹댐옙 占쏙옙占쏙옙 占쏙옙틸占쏙옙티.
        /// </summary>
        /// <param name="speedType">占쌈듸옙 占쌤곤옙</param>
        /// <param name="customVel">Custom 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙占?占쌈듸옙 占쏙옙</param>
        /// <returns>占쏙옙占쏙옙占쏙옙 Jog 占쌈듸옙</returns>
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
        /// Jog 占쏙옙튼占쏙옙 占쏙옙占쏙옙占쏙옙 占쌍댐옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占싱듸옙占싼댐옙.
        /// 占시뮬뤄옙占싱쇽옙 占쏙옙占쏙옙占쏙옙 _jogDirection占쏙옙 CurrentVelocity占쏙옙 占쏙옙占쏙옙占쏙옙占?占쏙옙 틱 占쏙옙치占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        /// <param name="direction">+1 (占시뤄옙占쏙옙 占쏙옙占쏙옙) 占실댐옙 -1 (占쏙옙占싱너쏙옙 占쏙옙占쏙옙)</param>
        /// <param name="speedType">占쌈듸옙 占쌤곤옙</param>
        /// <param name="customVel">Custom 占쏙옙占쏙옙 占쏙옙 占쌈듸옙 占쏙옙 (占썩본占쏙옙: 0)</param>
        public virtual void MoveJogContinuous(int direction, JogSpeedType speedType,
                                              double customVel = 0)
        {
            if (!IsServoOn || IsAlarm) return;

            _jogDirection   = direction;
            CurrentVelocity = GetJogVelocity(speedType, customVel);
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Jog;

            // CommandPosition占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 ? 占시뮬뤄옙占쏙옙占싶곤옙 占쏙옙 틱 占쏙옙占쏙옙
            CommandPosition    = direction > 0 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
            _simTargetPosition = CommandPosition;
        }

        /// <summary>
        /// 클占쏙옙 1회占쏙옙 stepDistance占쏙옙큼 占쏙옙占?占싱듸옙占싹댐옙 Step Jog占쏙옙 占쏟동깍옙占?占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        /// <param name="direction">+1 (占시뤄옙占쏙옙 占쏙옙占쏙옙) 占실댐옙 -1 (占쏙옙占싱너쏙옙 占쏙옙占쏙옙)</param>
        /// <param name="speedType">占쌈듸옙 占쌤곤옙</param>
        /// <param name="stepDistance">1회 占싱듸옙 占신몌옙 (占쏙옙占?占쏙옙占쎈값)</param>
        /// <param name="customVel">Custom 占쏙옙占쏙옙 占쏙옙 占쌈듸옙 占쏙옙 (占썩본占쏙옙: 0)</param>
        public virtual async Task MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                   double stepDistance, double customVel = 0)
        {
            double vel = GetJogVelocity(speedType, customVel);
            await MoveRelativeAsync(direction * Math.Abs(stepDistance), vel);
        }

        /// <summary>
        /// Jog 占싱듸옙占쏙옙 占쏙옙占쏙옙占싼댐옙. <see cref="Stop"/>占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占실미몌옙 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 표占쏙옙占싼댐옙.
        /// </summary>
        public virtual void StopJog()
        {
            Stop();
        }

        /// <summary>
        /// 占싱듸옙 占쏙옙 占쏙옙표 占쌈듸옙占쏙옙 占실시곤옙占쏙옙占쏙옙 占쏙옙占쏙옙(Override)占싼댐옙.
        /// </summary>
        /// <param name="newVelocity">占쏙옙占쏙옙占쏙옙 占쏙옙 占쌈듸옙</param>
        public virtual void OverrideVelocity(double newVelocity)
        {
            if (newVelocity <= 0) return;
            _overrideVelocity = newVelocity;
            CurrentVelocity   = newVelocity;
        }

        /// <summary>
        /// 占싱듸옙 占쏙옙 占쏙옙표 占쏙옙치占쏙옙 占실시곤옙占쏙옙占쏙옙 占쏙옙占쏙옙(Override)占싼댐옙.
        /// </summary>
        /// <param name="newTargetPosition">占쏙옙占쏙옙占쏙옙 占쏙옙 占쏙옙표 占쏙옙치</param>
        public virtual void OverridePosition(double newTargetPosition)
        {
            _overrideTargetPosition = newTargetPosition;
            CommandPosition         = newTargetPosition;
            _simTargetPosition      = newTargetPosition;
        }

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙4. 占쏙옙溜占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트 占쏙옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// 10ms 占쌍깍옙占?<see cref="UpdateStatus"/>占쏙옙 호占쏙옙占싹댐옙 占쏙옙溜占쏙옙占?占승쏙옙크占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// 占승쏙옙크占쏙옙 占쏙옙체 占쌀몌옙 占쏙옙 <see cref="Dispose"/>占쏙옙 占쏙옙占쏙옙 占쏙옙撚홱占?
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
                        // 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트 占쏙옙 占쏙옙占쌤댐옙 占쏙옙占쏙옙占쏙옙 占쌩단쏙옙키占쏙옙 占십는댐옙.
                        await Task.Delay(10, token).ContinueWith(_ => { });
                    }
                }
            }, token);
        }

        /// <summary>
        /// 10ms 占쌍깍옙占?호占쏙옙풔占?占쏙옙占쏙옙 占쏙옙占쏙옙 占쌨쇽옙占쏙옙.<br/>
        /// <list type="bullet">
        ///   <item><description>占시뮬뤄옙占싱쇽옙 占쏙옙占?<see cref="AxisConfig.IsSimulationMode"/> = true):
        ///     <see cref="SimulateMotion"/>占쏙옙 호占쏙옙占싹울옙 占쏙옙占쏙옙 占쏙옙치占쏙옙 占쏙옙占쏙옙磯占?</description></item>
        ///   <item><description>占실븝옙占쏙옙 占쏙옙占? override占싹울옙 占쏙옙占쏙옙 API占쏙옙 占쏙옙占승몌옙 占싻는댐옙.</description></item>
        /// </list>
        /// </summary>
        public virtual void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                SimulateMotion();
            }
            // else: 占실븝옙占쏙옙 占쏙옙占?? 占쏙옙占쏙옙 클占쏙옙占쏙옙占쏙옙占쏙옙 override占싹울옙 占쏙옙占쏙옙
        }

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙5. 占시뮬뤄옙占싱쇽옙 占쏙옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// 10ms 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占?占시뮬뤄옙占싱쇽옙 占쏙옙占쏙옙.<br/>
        /// <list type="bullet">
        ///   <item><description>CurrentVelocity 占쏙옙占쏙옙占쏙옙占?占쏙옙 틱 占싱듸옙 占신몌옙占쏙옙 占쏙옙占쏙옙磯占?</description></item>
        ///   <item><description>占쏙옙표 占쏙옙치 占쏙옙占쏙옙 占쏙옙 占싱듸옙 占싹뤄옙 처占쏙옙占쏙옙 占싼댐옙.</description></item>
        ///   <item><description>占쏙옙占쏙옙트 占쏙옙占쏙옙 占십곤옙 占쏙옙 占싯띰옙占쏙옙 占쌩삼옙占쏙옙키占쏙옙 占쏙옙占쏙옙占싼댐옙.</description></item>
        /// </list>
        /// </summary>
        protected virtual void SimulateMotion()
        {
            if (!IsMoving) return;

            // Override 占쏙옙 占쌥울옙
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

            // 10ms 占쏙옙占쏙옙 占싱듸옙 占신몌옙 占쏙옙占?(占쏙옙占쏙옙: [占쏙옙占쏙옙/s] * 0.01s)
            const double tickSeconds = 0.01;
            double step = CurrentVelocity * tickSeconds;

            double remaining = _simTargetPosition - ActualPosition;

            if (_currentMode == MotionMode.Jog)
            {
                // Jog: 占쏙옙占썩에 占쏙옙占쏙옙 step占쏙옙 占싱듸옙, 占쏙옙占쌉울옙 占쏙옙占쏙옙占싹몌옙 占쏙옙占쏙옙
                ActualPosition += _jogDirection * step;
                RaisePositionChanged();
            }
            else
            {
                // 占쏙옙占쎈·占쏙옙占?占싱듸옙: 占쏙옙표占쏙옙 占쏙옙占쏙옙 step占쏙옙 占쏙옙占쏙옙
                if (Math.Abs(remaining) <= step)
                {
                    // 占쏙옙표 占쏙옙占쏙옙
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

            // 占쏙옙占쏙옙 占쏙옙占쏙옙트 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
            if (Setup.SoftLimitEnabled && ActualPosition >= Setup.SoftLimitPlus)
            {
                ActualPosition = Setup.SoftLimitPlus;
                TriggerSoftLimitAlarm(alarmCode: 10);
                return;
            }

            if (Setup.SoftLimitEnabled && ActualPosition <= Setup.SoftLimitMinus)
            {
                ActualPosition = Setup.SoftLimitMinus;
                TriggerSoftLimitAlarm(alarmCode: 11);
                return;
            }
        }

        /// <summary>
        /// 占쏙옙占쏙옙트 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙 占싯띰옙占쏙옙 占쌩삼옙占쏙옙키占쏙옙 占쏙옙占?占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        /// <param name="alarmCode">占쏙옙占쏙옙占쏙옙 占싯띰옙 占쌘듸옙 (10: PEL, 11: MEL)</param>
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

        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙
        //  占쏙옙6. IDisposable ? 占쏙옙溜占쏙옙占?占승쏙옙크 占쏙옙占쏙옙
        // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙

        /// <summary>
        /// 占쏙옙溜占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트 占승쏙옙크占쏙옙 占쏙옙占쏙옙構占?占쏙옙占쌀쏙옙占쏙옙 占쏙옙占쏙옙占싼댐옙.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
