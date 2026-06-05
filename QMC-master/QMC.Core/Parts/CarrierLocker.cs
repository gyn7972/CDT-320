using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.IO;
using MechaSys.SoftBricks;

namespace QMC.Parts
{
    #region CarrierLocker
    /// <summary>
    /// 디지털 접점들로 구성되는 ILocker 파트를 정의합니다.
    /// </summary>
    public class CarrierLocker : Part, ILocker
    {
        #region Define
        [Serializable]
        public new enum AlarmKeys
        {
            EnableLockInputDoesNotOn,
            CannotInitialize,
        }
        #endregion

        #region Field
        private LockerState m_InitialState;
        private Sensor m_EnableLock;
        private Actuator m_LockOutput;
        private Actuator m_UnlockOutput;
        #endregion

        #region Constructor
        /// <summary>
        /// CarrierLocker 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="nameable">INameable 개체입니다.</param>
        public CarrierLocker(Nameable nameable)
            : base(nameable)
        {
            this.InitialState = LockerState.Undefined;

            this.EnableLock = new Sensor(new Nameable(nameof(this.EnableLock)));
            this.LockOutput = new Actuator(new Nameable(nameof(this.LockOutput)));
            this.UnlockOutput = new Actuator(new Nameable(nameof(this.UnlockOutput)));
        }
        /// <summary>
        /// CarrierLocker 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public CarrierLocker() : this(new Nameable()) { }
        #endregion

        #region Property
        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public Actuator LockOutput
        {
            get { return this.m_LockOutput; }
            protected set
            {
                this.Parts.SetNotNull(this.m_LockOutput, value);
                this.m_LockOutput = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public Sensor EnableLock
        {
            get { return this.m_EnableLock; }
            protected set
            {
                this.Parts.SetNotNull(this.m_EnableLock, value);
                this.m_EnableLock = value;
            }
        }

        [Composite]
        [Multiplicity(MultiplicityAttribute.AllowableNumbers.OneOnly)]
        public Actuator UnlockOutput
        {
            get { return this.m_UnlockOutput; }
            protected set
            {
                this.Parts.SetNotNull(this.m_UnlockOutput, value);
                this.m_UnlockOutput = value;
            }
        }

        /// <summary>
        /// Lock 이후 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        public TimeSpanX DelayAfterLock
        {
            get { return this.ConstructConfiguration.DelayAfterLock; }
            set { this.ConstructConfiguration.DelayAfterLock = value; }
        }

        /// <summary>
        /// Unlock 이후 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        public TimeSpanX DelayAfterUnlock
        {
            get { return this.ConstructConfiguration.DelayAfterUnlock; }
            set { this.ConstructConfiguration.DelayAfterUnlock = value; }
        }

        /// <summary>
        /// Lock 이전 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        public TimeSpanX DelayBeforeLock
        {
            get { return this.ConstructConfiguration.DelayBeforeLock; }
            set { this.ConstructConfiguration.DelayBeforeLock = value; }
        }

        /// <summary>
        /// Unlock 이전 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        public TimeSpanX DelayBeforeUnlock
        {
            get { return this.ConstructConfiguration.DelayBeforeUnlock; }
            set { this.ConstructConfiguration.DelayBeforeUnlock = value; }
        }
        #endregion

        #region Method
        /// <summary>
        /// AfterLock 이벤트를 발생시킨다.
        /// </summary>
        /// <param name="e">이벤트 데이터가 들어 있는 EquipmentEventArgs입니다</param>
        protected virtual void OnAfterLock(EquipmentEventArgs e)
        {
            if(this.AfterLock != null)
                this.AfterLock(this, e);
        }

        /// <summary>
        /// AfterUnlock 이벤트를 발생시킨다.
        /// </summary>
        /// <param name="e">이벤트 데이터가 들어 있는 EquipmentEventArgs입니다</param>
        protected virtual void OnAfterUnlock(EquipmentEventArgs e)
        {
            if(this.AfterUnlock != null)
                this.AfterUnlock(this, e);
        }

        /// <summary>
        /// BeforeLock 이벤트를 발생시킨다.
        /// </summary>
        /// <param name="e">이벤트 데이터가 들어 있는 EquipmentEventArgs입니다</param>
        protected virtual void OnBeforeLock(EquipmentEventArgs e)
        {
            if(this.BeforeLock != null)
                this.BeforeLock(this, e);
        }

        /// <summary>
        /// BeforeUnlock 이벤트를 발생시킨다.
        /// </summary>
        /// <param name="e">이벤트 데이터가 들어 있는 EquipmentEventArgs입니다</param>
        protected virtual void OnBeforeUnlock(EquipmentEventArgs e)
        {
            if(this.BeforeUnlock != null)
                this.BeforeUnlock(this, e);
        }
        #endregion

        #region ICarrierLocker
        /// <summary>
        /// Lock 이후에 발생합니다.
        /// </summary>
        public event MechaSys.SoftBricks.EquipmentEventHandler AfterLock;
        /// <summary>
        /// Unlock 이후에 발생합니다.
        /// </summary>
        public event MechaSys.SoftBricks.EquipmentEventHandler AfterUnlock;
        /// <summary>
        /// Lock 이전에 발생합니다.
        /// </summary>
        public event MechaSys.SoftBricks.EquipmentEventHandler BeforeLock;
        /// <summary>
        /// Unlock 이전에 발생합니다.
        /// </summary>
        public event MechaSys.SoftBricks.EquipmentEventHandler BeforeUnlock;

        /// <summary>
        /// 초기 상태를 가져오거나 설정합니다.
        /// </summary>
        public LockerState InitialState
        {
            get { return this.m_InitialState; }
            set { this.m_InitialState = value; }
        }

        #region Check()
        /// <summary>
        /// CarrierLocker의 상태를 반환합니다.
        /// </summary>
        /// <param name="state">현재 상태입니다.</param>
        /// <returns>작업에 대한 결과를 반환합니다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public virtual int Check(ref LockerState state)
        {
            int ret = 0;
            DioValue lockOutput = DioValue.Off;
            DioValue unlockOutput = DioValue.Off;
            int value = (int)state;

            if((ret = this.LockOutput.Check(ref lockOutput)) != 0) return ret;
            if((ret = this.UnlockOutput.Check(ref unlockOutput)) != 0) return ret;

            if(lockOutput == DioValue.On && unlockOutput == DioValue.Off) state = LockerState.Lock;
            else if(lockOutput == DioValue.Off && unlockOutput == DioValue.On) state = LockerState.Unlock;
            else state = LockerState.Undefined;

            return ret;
        }
        #endregion

        #region Lock()
        /// <summary>
        /// Lock 비동기 작업을 수행합니다.
        /// </summary>
        /// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
        /// <param name="value">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
        /// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
        public MethodCallerAsyncResult BeginLock(MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new IntAsyncDelegate(this.LockProcedure), callback, null);
        }
        /// <summary>
        /// Lock 비동기 작업을 수행합니다.
        /// </summary>
        /// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
        public MethodCallerAsyncResult BeginLock()
        {
            return this.BeginLock(null, null);
        }
        /// <summary>
        /// Lock 비동기 작업을 완료합니다.
        /// </summary>
        /// <param name="ar">BeginLock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public int EndLock(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        /// <summary>
        /// Lock 작업을 수행합니다.
        /// </summary>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public int Lock()
        {
            this.Operational.Stop();
            return this.LockProcedure();
        }

        private int LockProcedure()
        {
            int ret = 0;
            EquipmentEventArgs e = new EquipmentEventArgs();
            LockerState state = LockerState.Undefined;

            // raise BeforeLock event.
            this.OnBeforeLock(e);
            if(e.Result != 0) return e.Result;

            // delay
            SafeThread.Delay(this.DelayBeforeLock);

            // control output and check input
            if((ret = this.OnLock()) != 0) return ret;

            // delay
            SafeThread.Delay(this.DelayAfterLock);

            // rasie AfterLock event.
            this.OnAfterLock(e);
            if(e.Result != 0) return e.Result;

            return ret;
        }

        /// <summary>
        /// Lock 작업을 수행합니다.
        /// </summary>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        protected virtual int OnLock()
        {
            int ret = 0;
            DioValue lockInput = DioValue.On;

            Retry:
            if((ret = this.EnableLock.Check(ref lockInput)) != 0) return ret;
            if(lockInput != DioValue.On)
            {
                Alarm alarm = null;

                alarm = this.Alarms[AlarmKeys.EnableLockInputDoesNotOn];

                if((ret = alarm.Post(this)) != 0)
                {
                    if(ret == (int)AlarmRecovery.Retry) goto Retry;
                    return ret;
                }
            }
            if((ret = this.LockOutput.On()) != 0) return ret;
            if((ret = this.UnlockOutput.Off()) != 0) return ret;

            return ret;
        }
        #endregion

        #region Unlock()
        /// <summary>
        /// Unlock 비동기 작업을 수행합니다.
        /// </summary>
        /// <param name="callback">MethodCallerAsyncCallback 비동기 작업의 완료 알림을 받는 대리자입니다</param>
        /// <param name="value">비동기 작업과 관련된 상태 정보를 포함하는 응용프로그램에서 지정된 객체입니다.</param>
        /// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
        public MethodCallerAsyncResult BeginUnlock(MethodCallerAsyncCallback callback, object value)
        {
            return this.Operational.BeginInvoke(new IntAsyncDelegate(this.UnlockProcedure), callback, value);
        }
        /// <summary>
        /// Unlock 비동기 작업을 수행합니다.
        /// </summary>
        /// <returns>MethodCallerAsyncResult는 비동기 작업을 참조합니다.</returns>
        public MethodCallerAsyncResult BeginUnlock()
        {
            return this.BeginUnlock(null, null);
        }
        /// <summary>
        /// Unlock 비동기 작업을 완료합니다.
        /// </summary>
        /// <param name="ar">BeginUnlock 메서드를 호출하여 반환된 MethodCallerAsyncResult입니다.</param>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public int EndUnlock(MethodCallerAsyncResult ar)
        {
            return (int)this.Operational.EndInvoke(ar);
        }
        /// <summary>
        /// Unlock 작업을 수행합니다.
        /// </summary>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        public int Unlock()
        {
            this.Operational.Stop();
            return this.UnlockProcedure();
        }

        private int UnlockProcedure()
        {
            int ret = 0;
            EquipmentEventArgs e = new EquipmentEventArgs();
            LockerState state = LockerState.Undefined;

            // raise BeforeUnlock event.
            this.OnBeforeUnlock(e);
            if(e.Result != 0) return e.Result;

            // delay
            SafeThread.Delay(this.DelayBeforeUnlock);

            if((ret = this.OnUnlock()) != 0) return ret;

            // delay
            SafeThread.Delay(this.DelayAfterUnlock);

            // raise AfterUnlock event.
            this.OnAfterUnlock(e);
            if(e.Result != 0) return e.Result;

            return ret;
        }

        /// <summary>
        /// Unlock 작업을 수행합니다.
        /// </summary>
        /// <returns>작업에 대한 결과를 반환한다. 0이면 성공 그렇지 않으면 실패를 나타냅니다</returns>
        protected virtual int OnUnlock()
        {
            int ret = 0;

            if((ret = this.UnlockOutput.On()) != 0) return ret;
            if((ret = this.LockOutput.Off()) != 0) return ret;

            return ret;
        }
        #endregion

        public virtual int CheckLock()
        {
            int ret = 0;
            LockerState state = LockerState.Lock;

            if((ret = this.Check(ref state)) != 0) return ret;

            if(state != LockerState.Lock)
            {
                DioValue dioValue = DioValue.On;

                if((ret = this.EnableLock.Check(ref dioValue)) != 0) return ret;

                if(dioValue == DioValue.On)
                {
                    if((ret = this.LockProcedure()) != 0) return ret;
                }
                else
                {
                    if((ret = this.Alarms[AlarmKeys.EnableLockInputDoesNotOn].Post(this)) != 0) return ret;
                }
            }

            return ret;
        }
        #endregion

        #region Part
        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <returns>성공하면 0이고, 그렇지 않으면 0이 아닌 값입니다.</returns>
        protected override int OnInitialize()
        {
            int ret = 0;

            if((ret = this.CheckLock()) != 0) return ret;

            return ret;
        }

        protected override int OnAbort()
        {
            int ret = 0;
            return ret;
        }

        protected override int OnStop()
        {
            int ret = 0;
            return ret;
        }
        #endregion

        #region Element
        /// <summary>
        /// 재정의 되었습니다. CarrierLockerConstructConfiguration 형식으로 반환합니다.
        /// </summary>
        protected new CarrierLockerConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as CarrierLockerConstructConfiguration; }
        }

        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <returns>CarrierLockerConstructConfiguration 형식의 새 인스턴스입니다.</returns>
        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new CarrierLockerConstructConfiguration();
        }

        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <param name="configuration">적용할 생성 구성 정보입니다.</param>
        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            CarrierLockerConstructConfiguration value = configuration as CarrierLockerConstructConfiguration;

            base.OnSetConstructConfiguration(configuration);

            if(value == null) return;

            this.InitialState = this.ConstructConfiguration.InitialState;
        }

        protected override void OnMakeAlarms()
        {
            Alarm alarm = null;

            base.OnMakeAlarms();

            alarm = new Alarm(AlarmKeys.EnableLockInputDoesNotOn.ToString());
            alarm.AlarmGrade = AlarmGrade.Waiting;
            alarm.Cause = "The locker lock output was activated, but the lock confirmation input was not detected";
            alarm.Remedy = "Please check the locker mechanism and the lock confirmation sensor, then retry";
            this.Alarms.Add(AlarmKeys.EnableLockInputDoesNotOn, alarm);

            alarm = new Alarm(AlarmKeys.CannotInitialize.ToString(), "Cannot initialize because it is not locked");
            alarm.AlarmGrade = AlarmGrade.Fail;
            this.Alarms.Add(AlarmKeys.CannotInitialize, alarm);
        }
        #endregion
    }
    #endregion

    #region CarrierLockerConstructConfiguration
    /// <summary>
    /// CarrierLocker에 대한 생성 구성 정보입니다.
    /// </summary>
    [Serializable]
    public class CarrierLockerConstructConfiguration : DioPartConstructConfiguration
    {
        #region Field
        private TimeSpanX m_DelayAfterLock;
        private TimeSpanX m_DelayAfterUnlock;

        private TimeSpanX m_DelayBeforeLock;
        private TimeSpanX m_DelayBeforeUnlock;

        private LockerState m_InitialState;
        #endregion

        #region Constructor
        /// <summary>
        /// CarrierLockerConstructConfiguration 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="constructMethod">생성 방법입니다.</param>
        public CarrierLockerConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        /// <summary>
        /// CarrierLockerConstructConfiguration 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public CarrierLockerConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        /// <summary>
        /// Lock 이후 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        [Category("IO")]
        public TimeSpanX DelayAfterLock
        {
            get { return this.m_DelayAfterLock; }
            set { this.m_DelayAfterLock = value; }
        }

        /// <summary>
        /// Unlock 이후 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        [Category("IO")]
        public TimeSpanX DelayAfterUnlock
        {
            get { return this.m_DelayAfterUnlock; }
            set { this.m_DelayAfterUnlock = value; }
        }

        /// <summary>
        /// Lock 이전 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        [Category("IO")]
        public TimeSpanX DelayBeforeLock
        {
            get { return this.m_DelayBeforeLock; }
            set { this.m_DelayBeforeLock = value; }
        }

        /// <summary>
        /// Unlock 이전 지연 시간을 가져오거나 설정합니다.
        /// </summary>
        [Category("IO")]
        public TimeSpanX DelayBeforeUnlock
        {
            get { return this.m_DelayBeforeUnlock; }
            set { this.m_DelayBeforeUnlock = value; }
        }

        /// <summary>
        /// 초기 상태를 가져오거나 설정합니다.
        /// </summary>
        [Category("IO")]
        [DefaultValue(LockerState.Undefined)]
        public LockerState InitialState
        {
            get { return this.m_InitialState; }
            set { this.m_InitialState = value; }
        }
        #endregion

        #region ConstructConfiguration
        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.DelayAfterLock = TimeSpanX.Zero;
            this.DelayAfterUnlock = TimeSpanX.Zero;

            this.DelayBeforeLock = TimeSpanX.Zero;
            this.DelayBeforeUnlock = TimeSpanX.Zero;

            this.InitialState = LockerState.Undefined;
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            base.OnDeserialized(context);
        }
        #endregion
    }
    #endregion
}
