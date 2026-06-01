using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.IO.Parts;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;
using QMC.Transfers.Feeders;

namespace QMC.Jobs
{
    #region MotionFeederArmRetractJob
    /// <summary>
    /// Die가 붙어 있는 TapeFrame에 대해서 loading 되어야 할 die들에 대해 scan을 수행하는 job
    /// </summary>
    public class MotionFeederArmRetractJob : Job
    {
        #region Field
        private Feeder m_TransferModule;
        #endregion

        #region Constructor
        /// <summary>
        /// MotionFeederArmRetractJob 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="name">이름을 지정합니다.</param>
        /// <param name="jobCalculator">우선 순위 계산기를 지정합니다.</param>
        /// <param name="jobAction">작업 실행기를 지정합니다.</param>
        public MotionFeederArmRetractJob(string name, IJobCalculator jobCalculator, IJobAction jobAction)
            : base(name, jobCalculator, jobAction)
        {
        }
        /// <summary>
        /// MotionFeederArmRetractJob 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="name">이름을 지정합니다.</param>
        public MotionFeederArmRetractJob(string name) : this(name, null, null) { }
        /// <summary>
        /// MotionFeederArmRetractJob 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public MotionFeederArmRetractJob() : this("") { }
        #endregion

        #region Property
        public Feeder TransferModule
        {
            get { return this.m_TransferModule; }
            set { this.m_TransferModule = value; }
        }
        #endregion

        #region Job
        /// <summary>
        /// 재정의 되었습니다. MotionFeederArmRetractJobConstructConfiguration 형식으로 반환합니다.
        /// </summary>
        protected new MotionFeederArmRetractJobConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as MotionFeederArmRetractJobConstructConfiguration; }
        }

        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <returns>MotionFeederArmRetractJobConstructConfiguration 형식의 새 인스턴스입니다.</returns>
        protected override JobConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new MotionFeederArmRetractJobConstructConfiguration();
        }

        // Revision 2
        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <param name="configuration">적용할 생성 구성 정보입니다.</param>
        protected override void OnSetConstructConfiguration(JobConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;

            this.TransferModule = Sys.Equipment.Modules.GetByUid(this.ConstructConfiguration.ModuleUid) as Feeder;
        }

        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        /// <param name="target">대상 생성 구성 정보입니다.</param>
        protected override void OnMakeConstructConfiguration(ref JobConstructConfiguration target)
        {
            MotionFeederArmRetractJobConstructConfiguration specialized = target as MotionFeederArmRetractJobConstructConfiguration;

            base.OnMakeConstructConfiguration(ref target);

            if(specialized == null) return;

            specialized.ModuleUid = this.TransferModule.Uid;
        }
        #endregion
    }
    #endregion

    #region MotionFeederArmRetractJobConstructConfiguration
    [Serializable]
    public class MotionFeederArmRetractJobConstructConfiguration : JobConstructConfiguration
    {
        #region Field
        private string m_ModuleUid;
        #endregion

        #region Constructor
        /// <summary>
        /// MotionFeederArmRetractJobConstructConfiguration 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="name">이름을 지정합니다.</param>
        public MotionFeederArmRetractJobConstructConfiguration(string name)
            : base(name)
        {
        }
        /// <summary>
        /// MotionFeederArmRetractJobConstructConfiguration 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public MotionFeederArmRetractJobConstructConfiguration() : this(string.Empty) { }
        #endregion

        #region Property
        /// <summary>
        /// 모듈의 UID를 가져오거나 설정합니다.
        /// </summary>
        public string ModuleUid
        {
            get { return this.m_ModuleUid; }
            set { this.m_ModuleUid = value; }
        }
        #endregion

        #region ConstructConfiguration Members
        /// <summary>
        /// 재정의 되었습니다.
        /// </summary>
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.ModuleUid = string.Empty;
        }
        #endregion
    }
    #endregion

    #region MotionFeederArmRetractJobAction
    /// <summary>
    /// MotionFeederArmRetractJobAction에 대한 작업 실행기를 정의합니다.
    /// </summary>
    public class MotionFeederArmRetractJobAction : JobAction
    {
        #region Constructor
        /// <summary>
        /// MotionFeederArmRetractJobAction 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="owner">개체를 소유한 작업입니다.</param>
        public MotionFeederArmRetractJobAction(MotionFeederArmRetractJob owner)
            : base(owner)
        {
        }
        /// <summary>
        /// MotionFeederArmRetractJobAction 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public MotionFeederArmRetractJobAction() : this(null) { }
        #endregion

        #region Method
        /// <summary>
        /// 주 모듈의 서비스 상태를 EquipmentSelected로 변경합니다.
        /// </summary>
        /// <returns>성공하면 0이고, 그렇지 않으면 0이 아닌 값입니다.</returns>
        protected virtual int OnChangeServiceToEquipmentSelected()
        {
            int ret = 0;
            if((ret = this.Owner.TransferModule.ServiceStateManager.ChangeService(this.Owner, PartServiceStateMachine.StateEnum.EquipmentSelected)) != 0) return ret;
            return ret;
        }

        /// <summary>
        /// 주 모듈의 서비스 상태를 InService로 변경합니다.
        /// </summary>
        /// <returns>성공하면 0이고, 그렇지 않으면 0이 아닌 값입니다.</returns>
        protected virtual int OnChangeServiceToInService()
        {
            int ret = 0;

            if(Object.Equals(this.Owner.TransferModule.ServiceStateManager.Requester, this.Owner) &&
                this.Owner.TransferModule.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.EquipmentSelected)
            {
                if((ret = this.Owner.TransferModule.ServiceStateManager.ChangeService(this.Owner, PartServiceStateMachine.StateEnum.InService)) != 0) return ret;
            }
            return ret;
        }
        
        protected virtual int OnRetract()
        {
            int ret = 0;
            foreach(FeederArm arm in this.Owner.TransferModule.Arms)
            {
                if(arm is MotionFeederArm motionFeederArm)
                {
                    if(arm is SingleAxisMotionFeederArm feederArm)
                    {
                        if((ret = feederArm.TravelMotion.MoveToHomeSync()) != 0) return ret;
                    }

                    if(motionFeederArm.EvasiveVerticalCylinder != null)
                    {
                        if((ret = motionFeederArm.EvasiveVerticalCylinder.Up()) != 0) return ret;
                    }
                }
            }
            return ret;
        }
        #endregion

        #region JobAction Members
        /// <summary>
        /// 재정의 되었습니다. MotionFeederArmRetractJobOwner 형식으로 가져오거나 설정합니다.
        /// </summary>
        public new MotionFeederArmRetractJob Owner
        {
            get { return (MotionFeederArmRetractJob)base.Owner; }
            set { base.Owner = value; }
        }

        /// <summary>
        /// 작업을 실행합니다.
        /// </summary>
        /// <returns>성공하면 0이고, 그렇지 않으면 0이 아닌 값입니다.</returns>
        public override int Procedure()
        {
            int ret = 0;
            MaterialCollection materials = new MaterialCollection();

            try
            {
                if((ret = this.OnChangeServiceToEquipmentSelected()) != 0) return ret;
                if((ret = this.OnRetract()) != 0) return ret;
            }
            finally
            {
                this.OnChangeServiceToInService();
            }

            return ret;
        }
        #endregion
    }
    #endregion

    #region MotionFeederArmRetractJobCalculator
    /// <summary>
    /// MotionFeederArmRetractJobCalculator 대한 우선 순위 계산기를 정의합니다.
    /// </summary>
    public class MotionFeederArmRetractJobCalculator : JobCalculator
    {
        #region Constructor
        /// <summary>
        /// MotionFeederArmRetractJobCalculator 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="owner">개체를 소유한 작업입니다.</param>
        public MotionFeederArmRetractJobCalculator(MotionFeederArmRetractJob owner)
            : base(owner)
        {
        }
        /// <summary>
        /// MotionFeederArmRetractJobCalculator 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public MotionFeederArmRetractJobCalculator() : this(null) { }
        #endregion

        #region Method
        /// <summary>
        /// 프로세스 모듈의 상태를 체크합니다.
        /// </summary>
        /// <returns>프로세스 모듈의 서비스 상태가 InService이고 행위 상태가 Idle이고 레시피 실행기의 상태가 None이면 true이고, 그렇지 않으면 false입니다.</returns>
        protected virtual bool CheckModuleState()
        {
            PartServiceStateMachine.StateEnum serviceState = this.Owner.TransferModule.ServiceState.CurrentStateValue;
            PartBehaviorStateMachine.StateEnum behaviorState = this.Owner.TransferModule.BehaviorState.CurrentStateValue;

            if(serviceState != PartServiceStateMachine.StateEnum.InService)
            {
                this.DisableReason = string.Format("The service state is not InService. [State = {0}]", serviceState);
                return false;
            }
            if(behaviorState != PartBehaviorStateMachine.StateEnum.Idle)
            {
                this.DisableReason = string.Format("The behavior state is not Idle. [State = {0}]", behaviorState);
                return false;
            }

            return true;
        }

        protected virtual bool CheckFeederArmState()
        {
            foreach(FeederArm arm in this.Owner.TransferModule.Arms)
            {
                if(arm is MotionFeederArm motionfeederArm && motionfeederArm.EvasiveVerticalCylinder != null)
                {
                    VerticalCylinderState state = VerticalCylinderState.Up;
                    motionfeederArm.EvasiveVerticalCylinder.Check(ref state);
                    if(state == VerticalCylinderState.Down) return true;
                }
            }

            this.DisableReason = string.Format("Feeder arm state is not verify");
            return false;
        }
        #endregion

        #region JobCalculator Members
        /// <summary>
        /// 재정의 되었습니다. MotionFeederArmRetractJob 형식으로 가져오거나 설정합니다.
        /// </summary>
        public new MotionFeederArmRetractJob Owner
        {
            get { return (MotionFeederArmRetractJob)base.Owner; }
            set { base.Owner = value; }
        }

        /// <summary>
        /// 우선 순위를 산출합니다.
        /// </summary>
        /// <returns>우선 순위입니다.</returns>
        public override JobPriority GetPriority()
        {
            JobPriority priority = this.DefaultPriority;

            if(this.CheckModuleState() == false) return JobPriority.DisablePriority;
            if(this.CheckFeederArmState() == false) return JobPriority.DisablePriority;

            priority.Major = 1;
            this.DisableReason = "";

            return priority;
        }
        #endregion
    }
    #endregion
}
