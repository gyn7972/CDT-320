using System;
using System.Windows.Forms;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Jobs
{
    #region TransferVsSelfExecutionJobDispatcherJudge
    public abstract class TransferVsSelfExecutionJobDispatcherJudge : JobDispatcherJudge
    {
        #region Constructor
        public TransferVsSelfExecutionJobDispatcherJudge() : base()
        {

        }
        #endregion

        #region JobDispatcherJudge Members
        protected new TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration; }
        }

        protected override JobDispatcherJudgeConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration();
        }

        protected override void OnMakeConstructConfiguration(ref JobDispatcherJudgeConstructConfiguration configuration)
        {
            TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration specialized = configuration as TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration;

            base.OnMakeConstructConfiguration(ref configuration);

            if (specialized == null) return;
        }

        protected override void OnSetConstructConfiguration(JobDispatcherJudgeConstructConfiguration configuration)
        {
            TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration specialized = configuration as TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration;

            base.OnSetConstructConfiguration(configuration);

            if (specialized == null) return;
        }
        #endregion
    }
    #endregion

    #region TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration
    [Serializable]
    public class TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration : JobDispatcherJudgeConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration(string name)
            : base(name)
        {
        }
        public TransferVsSelfExecutionJobDispatcherJudgeConstructConfiguration() : this(string.Empty) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}