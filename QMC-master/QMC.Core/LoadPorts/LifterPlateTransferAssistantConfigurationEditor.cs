using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Motions.Controls;
using MechaSys.SoftBricks.Parts;

namespace QMC.LoadPorts
{
    public partial class LifterPlateTransferAssistantConfigurationEditor : ElementConfigurationEditorX
    {
        #region Field
        private Dictionary<string, XyztPositionDataEditor> m_PositionDataEditors;
        #endregion

        #region Constructor
        public LifterPlateTransferAssistantConfigurationEditor(IElementConfigurable element) : base(element)
        {
            InitializeComponent();

            this.m_PositionDataEditors = new Dictionary<string, XyztPositionDataEditor>();

        }
        public LifterPlateTransferAssistantConfigurationEditor() : this(null) { }
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

        private void PositionRepository_AppliedConfiguration(object sender, EquipmentEventArgs e)
        {
            PositionRepository repository = sender as PositionRepository;

            if(this.m_PositionDataEditors.ContainsKey(repository.Locator.ToString()) == true)
            {
                this.m_PositionDataEditors[repository.Locator.ToString()].SetContents(repository.SelectedSize, repository.Configuration.Body.Positions);
            }
        }

        #endregion

        #region Method
        private RadioButtonX MakeRadioButton(int size)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.AutoSize = false;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = $"{size.ToString()} [inch]";
            button.Tag = size;
            button.Size = new Size(100, 40);
            return button;
        }
        #endregion

        #region ElementConfigurationEditorX Members
        public new LifterPlateTransferAssistant Owner
        {
            get { return base.Owner as LifterPlateTransferAssistant; }
        }

        protected override int OnSetContents(ElementConfiguration configuration)
        {
            int ret = 0;

            if((ret = base.OnSetContents(configuration)) != 0) return ret;

            foreach(KeyValuePair<string, XyztPositionDataEditor> pair in this.m_PositionDataEditors)
            {
                PositionRepository repository = ElementList.GetByLocator(pair.Key) as PositionRepository;

                pair.Value.SetContents(repository.SelectedSize, repository.Configuration.Body.Positions);
            }

            if(this.Owner.Owner.MaterialSizeAssistant != null)
            {
                for(int i = 0; i < this.flowLayoutPanelXMaterialSize.Controls.Count; i++)
                {
                    if(this.flowLayoutPanelXMaterialSize.Controls[i] is RadioButtonX radioButton)
                    {
                        if(radioButton.Tag is int size && this.Owner.Owner.MaterialSizeAssistant.Configuration.Body.SelectedSize == size)
                        {
                            radioButton.Checked = true;
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        protected override int OnGetContents(ref ElementConfiguration configuration)
        {
            int ret = 0;
            Dictionary<string, IXyztPositionData> datas = null;

            if((ret = base.OnGetContents(ref configuration)) != 0) return ret;

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

            if(this.Owner.Owner.MaterialSizeAssistant != null)
            {
                for(int i = 0; i < this.flowLayoutPanelXMaterialSize.Controls.Count; i++)
                {
                    if(this.flowLayoutPanelXMaterialSize.Controls[i] is RadioButtonX radioButton)
                    {
                        if(radioButton.Checked == false) continue;

                        if(radioButton.Tag is int size)
                        {
                            this.Owner.Owner.MaterialSizeAssistant.Configuration.Body.SelectedSize = size;
                            ElementConfigurator.Save(this.Owner.Owner.MaterialSizeAssistant.Configuration);
                            this.Owner.Owner.MaterialSizeAssistant.ApplyConfiguration(this.Owner.Owner.MaterialSizeAssistant.Configuration);
                            break;
                        }
                    }
                }
            }

            return ret;
        }
        #endregion

        #region UserControlX Members
        protected override void OnPrepare()
        {
            LifterPlateTransferAssistant.Positions[] actions = null;
            RadioButtonX button = null;

            base.OnPrepare();

            if(this.DesignMode == true) return;

            actions = (LifterPlateTransferAssistant.Positions[])Enum.GetValues(typeof(LifterPlateTransferAssistant.Positions));
            this.targetXyztPositionDataEditor1.Actions = Array.ConvertAll(actions, x => x.ToString());
            this.targetXyztPositionDataEditor1.Tag = this.Owner.PositionRepository;
            this.targetXyztPositionDataEditor1.Title = $"{Sys.Translate(this.Owner.Owner.Alias, Sys.LanguageDomains.Name)} {Sys.Translate("position")}";

            this.m_PositionDataEditors.Add(this.Owner.PositionRepository.Locator.ToString(), this.targetXyztPositionDataEditor1);

            this.Owner.PositionRepository.SelectedChange += PositionRepository_SelectedChange;
            this.Owner.PositionRepository.AppliedConfiguration += PositionRepository_AppliedConfiguration;

            if(this.Owner.Owner.MaterialSizeAssistant != null)
            {
                foreach(MaterialSizeSpecification specification in this.Owner.Owner.MaterialSizeAssistant.MaterialSizeSpecifications)
                {
                    button = this.MakeRadioButton(specification.Size);

                    this.flowLayoutPanelXMaterialSize.Controls.Add(button);
                }
            }
            else
                this.groupBoxXMaterialSize.Visible = false;

            this.groupBoxXMaterialSize.Text = Sys.Translate(this.groupBoxXMaterialSize.Text);
        }
        #endregion
    }
}
