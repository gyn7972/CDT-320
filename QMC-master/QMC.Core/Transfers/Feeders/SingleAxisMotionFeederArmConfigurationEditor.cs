using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Motions.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Transfers.Feeders
{
    public partial class SingleAxisMotionFeederArmConfigurationEditor : MotionFeederArmConfigurationEditor
    {
        #region Field
        private Dictionary<string, XyztPositionDataEditor> m_PositionDataEditors;
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmConfigurationEditor(IElementConfigurable element) : base(element)
        {
            InitializeComponent();

            this.m_PositionDataEditors = new Dictionary<string, XyztPositionDataEditor>();
        }
        public SingleAxisMotionFeederArmConfigurationEditor() : this(null) { }
        #endregion

        #region Event Handlers
        private void PositionRepository_SelectedChange(object sender, int e)
        {
            PositionRepository repository = sender as PositionRepository;

            if(this.m_PositionDataEditors.ContainsKey(repository.Locator.ToString()) == true)
            {
                this.m_PositionDataEditors[repository.Locator.ToString()].SetContents(repository.SelectedSize, repository.Configuration.Body.Positions);
            }
        }

        private void PositionRepository_AppliedConfiguration(object sender, MechaSys.SoftBricks.EquipmentEventArgs e)
        {
            PositionRepository repository = sender as PositionRepository;

            if(this.m_PositionDataEditors.ContainsKey(repository.Locator.ToString()) == true)
            {
                this.m_PositionDataEditors[repository.Locator.ToString()].SetContents(repository.SelectedSize, repository.Configuration.Body.Positions);
            }
        }
        #endregion

        #region ElementConfigurationEditorX Members
        public new SingleAxisMotionFeederArm Owner
        {
            get { return base.Owner as SingleAxisMotionFeederArm; }
        }
        protected override int OnSetContents(ElementConfiguration configuration)
        {
            int ret = 0;
            SingleAxisMotionFeederArmConfiguration specialized = null;

            if((ret = base.OnSetContents(configuration)) != 0) return ret;

            specialized = configuration as SingleAxisMotionFeederArmConfiguration;
            if(specialized == null)
                throw new ArgumentNullException("Configuration");

            foreach(KeyValuePair<string, XyztPositionDataEditor> pair in this.m_PositionDataEditors)
            {
                PositionRepository repository = ElementList.GetByLocator(pair.Key) as PositionRepository;

                pair.Value.SetContents(repository.SelectedSize, repository.Configuration.Body.Positions);
            }

            this.checkBoxEnableAlign.Checked = specialized.Body.EnableAlign;

            return ret;
        }
        protected override int OnGetContents(ref ElementConfiguration configuration)
        {
            int ret = 0;
            Dictionary<string, IXyztPositionData> datas = null;
            SingleAxisMotionFeederArmConfiguration specialized = null;

            if((ret = base.OnGetContents(ref configuration)) != 0) return ret;

            specialized = configuration as SingleAxisMotionFeederArmConfiguration;
            if(specialized == null)
                throw new ArgumentNullException("Configuration");

            foreach(KeyValuePair<string, XyztPositionDataEditor> pair in this.m_PositionDataEditors)
            {
                if(pair.Value.Tag is PositionRepository positionRepository)
                {
                    datas = positionRepository.Configuration.Body.Positions;
                    pair.Value.GetContents(ref datas);
                    positionRepository.Configuration.Body.Positions = datas;
                    ElementConfigurator.Save(positionRepository.Configuration);
                    positionRepository.ApplyConfiguration(positionRepository.Configuration);
                }
            }

            specialized.Body.EnableAlign = this.checkBoxEnableAlign.Checked;

            return ret;
        }
        #endregion

        #region UserControlX Members
        protected override void OnPrepare()
        {
            SingleAxisMotionFeederArm.Positions[] actions = null;

            base.OnPrepare();

            if(this.DesignMode == true) return;

            actions = (SingleAxisMotionFeederArm.Positions[])Enum.GetValues(typeof(SingleAxisMotionFeederArm.Positions));
            this.transferXyztPositionDataEditor1.Actions = Array.ConvertAll(actions, x => x.ToString());
            this.transferXyztPositionDataEditor1.Tag = this.Owner.PositionRepository;
            this.transferXyztPositionDataEditor1.Title = $"{Sys.Translate(this.Owner.Alias, Sys.LanguageDomains.Name)} {Sys.Translate("position")}";

            this.m_PositionDataEditors.Add(this.Owner.PositionRepository.Locator.ToString(), this.transferXyztPositionDataEditor1);

            this.Owner.PositionRepository.SelectedChange += PositionRepository_SelectedChange;
            this.Owner.PositionRepository.AppliedConfiguration += PositionRepository_AppliedConfiguration;

            this.groupBoxX1.Text = Sys.Translate(this.groupBoxX1.Text);
            this.checkBoxEnableAlign.Text = Sys.Translate(this.checkBoxEnableAlign.Text);
        }
        #endregion
    }
}
