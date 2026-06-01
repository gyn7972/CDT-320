using System;
using System.Drawing;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Materials.Controls;
using MechaSys.SoftBricks.Recipes;

using QMC.Equipments;

using SentiCore.Diagnostics;

namespace QMC.Hmi.Forms
{
    public partial class UnionRecipeEquipmentOperationForm : MechaSys.SoftBricks.Hmi.Forms.Semi.DisplayForm
    {
        #region Define
        private enum InfoCols
        {
            Name,
            Value
        }

        private enum InfoRows
        {
            CarrierId,
            Partner,

            Count
        }
        #endregion

        #region Field
        private const string DefaultCaption = "Job Operation";

        private LoadPort m_SelectedLoadPort;
        private RecipeClass m_SpecifiedRecipeClass;
        private SynchronizedManagedRecipeList m_SynchronizedManagedRecipes;
        private string m_PriviousApplyIdentifier;
        #endregion

        #region Constructor
        public UnionRecipeEquipmentOperationForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Property
        protected LoadPort SelectedLoadPort
        {
            get { return this.m_SelectedLoadPort; }
            private set { this.m_SelectedLoadPort = value; }
        }

        protected RecipeClass SpecifiedRecipeClass
        {
            get { return this.m_SpecifiedRecipeClass; }
            private set { this.m_SpecifiedRecipeClass = value; }
        }

        protected SynchronizedManagedRecipeList SynchronizedManagedRecipes
        {
            get { return this.m_SynchronizedManagedRecipes; }
            private set { this.m_SynchronizedManagedRecipes = value; }
        }
        #endregion

        #region Event Handlers
        private void timer_Tick(object sender, EventArgs e)
        {
            this.Display();
        }

        private void buttonOperation_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (this.SelectedLoadPort == null) return;

            if (sender == this.buttonStart)
            {
                int ret = 0;
                if((ret = this.VerifyBeforeStart()) != 0) return;
                this.CreateControlJob();
                TraceLogger.ProcessLogging($"Processing start");
            }
            else if (sender == this.buttonPause)
            {
                this.PauseControlJob();
                TraceLogger.ProcessLogging($"Processing pause");
            }
            else if (sender == this.buttonResume)
            {
                this.ResumeControlJob();
                TraceLogger.ProcessLogging($"Processing resume");
            }
            else if (sender == this.buttonStop)
            {
                this.StopControlJob();
                TraceLogger.ProcessLogging($"Processing stop");
            }
        }

        private void buttonShowRecipe_Click(object sender, EventArgs e)
        {
            RecipeIdentifier identifier = null;

            identifier = this.titleLabelRecipeIdentifier.Tag as RecipeIdentifier;
            if (identifier == null) return;

            if (Sys.Equipment is IShowRecipeEditor)
            {
                ((IShowRecipeEditor)Sys.Equipment).ShowRecipeEditor(identifier);
            }
        }

        private void buttonSlotSelect_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonDeselect)
            {
                this.slotSelector1.DeselectSlot();
                this.slotSelector2.DeselectSlot();

                this.m_PriviousApplyIdentifier = "";
            }
            else if (sender == this.buttonSelect)
            {                
                this.slotSelector1.SelectSlot();
                this.slotSelector2.SelectSlot();

                this.m_PriviousApplyIdentifier = this.slotSelector1.SelectedText;
            }
        }

        private void radioButtonLoaders_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (button.Checked == false) return;

            this.SelectedLoadPort = button.Tag as LoadPort;
            if (this.SelectedLoadPort != null)
                this.Caption = string.Format("{0} - [{1}]", DefaultCaption, this.SelectedLoadPort.Name);
            this.SetSlotCount();
            this.MakeRecipeList();
            this.InitGridInfo();
            this.slotSelector1.DeselectSlot();
            this.slotSelector2.DeselectSlot();
            this.Display();
        }

        protected virtual void flexGridRecipe_CurrentCellChanged(object sender, EventArgs e)
        {
            string text = string.Empty;
            this.titleLabelRecipeIdentifier.Text = string.Empty;
            this.titleLabelRecipeIdentifier.Tag = null;

            if (this.flexGridRecipe.CurrentCell != null)
            {
                int index = this.flexGridRecipe.CurrentCell.RowIndex;
                RecipeIdentifier identifier = this.flexGridRecipe.Rows[index].Tag as RecipeIdentifier;
                if (identifier != null)
                {
                    this.titleLabelRecipeIdentifier.Text = text = identifier.ToNameString();
                    this.titleLabelRecipeIdentifier.Tag = identifier;
                }
            }

            this.slotSelector1.SelectedText = text;
            this.slotSelector2.SelectedText = text;
        }

        private void buttonPages_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonPageDown)
            {

            }
            else if (sender == this.buttonPageUp)
            {
            }
        }
        #endregion

        #region Method
        #region Misc
        /// <summary>
        /// Job Operation이 가능한 Load Port List를 얻는다.
        /// </summary>
        /// <returns></returns>
        protected virtual LoadPort[] GetLoadPortList()
        {
            LoadPort[] loadports = null;

            loadports = LoadPortManager.GetLoadPorts();

            return loadports;
        }

        private void CreateLoaderButtons()
        {
            LoadPort[] loadports = null;

            #region get loadports
            if (Sys.Equipment is IDualZoneEquipment)
            {
                loadports = new LoadPort[((IDualZoneEquipment)Sys.Equipment).InputLoadPorts.Count];
                ((IDualZoneEquipment)Sys.Equipment).InputLoadPorts.CopyTo(loadports, 0);
            }
            else
                loadports = this.GetLoadPortList();
            #endregion

            // make loadport list
            for (int i = 0; i < loadports.Length; i++)
            {
                if (CarrierOperationConfigurator.Supported)
                {
                    LoadPortCarrierOperationSpecification specification =
                        CarrierOperationConfigurator.GetLoadPortCarrierOperationSpecification(loadports[i]);
                    if (specification.Mode == CarrierOperationMode.None) continue;
                }

                RadioButtonX button = new RadioButtonX();

                button.Tag = loadports[i];
                button.Appearance = Appearance.Button;
                button.AutoSize = false;
                button.ImageAlign = ContentAlignment.MiddleRight;
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.TextImageRelation = TextImageRelation.TextBeforeImage;
                button.Text = loadports[i].Alias;
                button.Size = new Size(110, 40);
                button.Checked = true;
                button.Checked = false;
                button.CheckedChanged += new EventHandler(this.radioButtonLoaders_CheckedChanged);

                this.flowLayoutPanelLoaders.Controls.Add(button);
            }

            // select 1st loadport
            if (0 < loadports.Length)
            {
                ((RadioButton)this.flowLayoutPanelLoaders.Controls[0]).Checked = true;
            }

            this.flowLayoutPanelLoaders.Visible = 1 < loadports.Length;
        }

        private RecipeIdentifier GetIdentifier(string identifier)
        {
            for(int i = 0; i < this.flexGridRecipe.Rows.Count; i++)
            {
                if(this.flexGridRecipe[this.ColumnName.Name, i].Value.ToString() == identifier)
                    return this.flexGridRecipe.Rows[i].Tag as RecipeIdentifier;
            }
            return null;
        }
        protected virtual void MakeRecipeList()
        {
            RecipeIdentifier[] identifiers = null;
            RecipeIdentifier identifier = null;
            RecipeIdentifier previousIdentifier = this.titleLabelRecipeIdentifier.Tag as RecipeIdentifier;
            ManagedRecipe recipe = null;
            string tact = "No";

            try
            {
                if (this.SelectedLoadPort == null) return;
                // get recipe identifiers
                lock (this.SynchronizedManagedRecipes.SyncRoot)
                {
                    identifiers = new RecipeIdentifier[this.SynchronizedManagedRecipes.Recipes.Count];
                    this.SynchronizedManagedRecipes.Recipes.CopyTo(identifiers, 0);
                }
                this.flexGridRecipe.Rows.Clear();
                for (int i = 0; i < identifiers.Length; i++)
                {
                    identifier = identifiers[i];
                    recipe = RecipeManager.LoadRecipe(identifier);
                    if (recipe is RouteRecipe)
                        tact = ((RouteRecipe)recipe).Body.Tact.Usage ? "Yes" : "No";

                    this.flexGridRecipe.Rows.Add(identifier.No, identifier.Name, tact);
                    this.flexGridRecipe.Rows[i].Tag = identifier;
                }
                // get control job
                ControlJob control = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
                if (control == null)
                {
                    if(string.IsNullOrEmpty(this.m_PriviousApplyIdentifier) == false && this.GetIdentifier(this.m_PriviousApplyIdentifier) != null)
                    {
                        identifier = this.GetIdentifier(this.m_PriviousApplyIdentifier);
                    }
                    else if(this.SelectedLoadPort != null && string.IsNullOrEmpty(this.SelectedLoadPort.Plate.History.LatestRecipeIdentifier) == false)
                    {
                        identifier = RecipeIdentifier.Parse(this.SelectedLoadPort.Plate.History.LatestRecipeIdentifier);
                    }
                    else
                    {
                        if(previousIdentifier == null && this.flexGridRecipe.Rows.Count > 0)
                            identifier = this.flexGridRecipe.Rows[0].Tag as RecipeIdentifier;
                        else
                            identifier = previousIdentifier;
                    }
                }
                else
                {
                    ProcessingControlSpec controlSpec = control.ProcessingControlSpec[0];
                    ProcessJob process = ProcessJobManager.GetByJobId(controlSpec.PRJobID);
                    identifier = RecipeIdentifier.Parse(process.RecipeID);
                }

                if (this.SelectRecipe(identifier) != 0)
                    this.flexGridRecipe_CurrentCellChanged(this.flexGridRecipe, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MechaSys.SoftBricks.Diagnostics.Log.WriteFramework(ex.ToString());
            }
        }

        protected int SelectRecipe(RecipeIdentifier id)
        {
            if (id == null) return -1;
            for (int i = 0; i < this.flexGridRecipe.Rows.Count; i++)
            {
                RecipeIdentifier eachId = this.flexGridRecipe.Rows[i].Tag as RecipeIdentifier;
                if (id.ToString(false) == eachId.ToString(false))
                {
                    this.flexGridRecipe.CurrentCell = this.flexGridRecipe.Rows[i].Cells[1];
                    this.flexGridRecipe_CurrentCellChanged(this.flexGridRecipe, EventArgs.Empty);
                    return 0;
                }
            }
            return -1;
        }

        private void InitGridInfo()
        {
            DataGridViewTextBoxCell textBoxCell;
            DataGridViewComboBoxCell comboBoxCell;
            LoadPortCarrierOperationSpecification eachSpec;
            LoadPort eachLoadPort;
            ControlJob eachCj;

            this.flexGridInfo.Rows.Clear();
            if (this.SelectedLoadPort == null) return;

            this.flexGridInfo.Rows.Add((int)InfoRows.Count);

            #region Carrier ID
            this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.CarrierId].Value = "Carrier ID";
            textBoxCell = new DataGridViewTextBoxCell();
            this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.CarrierId] = textBoxCell;
            textBoxCell.ReadOnly = true; // Cell을 추가한 후 설정할 수 있다.
            #endregion

            #region Partner
            if (CarrierOperationConfigurator.Supported)
            {
                LoadPortCarrierOperationSpecification specification =
                    CarrierOperationConfigurator.GetLoadPortCarrierOperationSpecification(this.SelectedLoadPort);

                if (specification.Mode == CarrierOperationMode.Uni)
                {
                    #region
                    this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value = "Unloader";
                    textBoxCell = new DataGridViewTextBoxCell();
                    this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] = textBoxCell;
                    textBoxCell.ReadOnly = true;
                    textBoxCell.Value = this.SelectedLoadPort.Name;
                    this.flexGridInfo.Rows[(int)InfoRows.Partner].Visible = false;
                    #endregion
                }
                else if (specification.Mode == CarrierOperationMode.Pair)
                {
                    #region
                    this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value =
                        specification.Type == CarrierInOutType.Input ? "Unloader" : "Loader";
                    textBoxCell = new DataGridViewTextBoxCell();
                    this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] = textBoxCell;
                    textBoxCell.ReadOnly = true;
                    LoadPort partner = Sys.Equipment.Modules.GetByUid(specification.Partner) as LoadPort;
                    textBoxCell.Value = partner.Name;
                    #endregion
                }
                else if (specification.Mode == CarrierOperationMode.Manual)
                {
                    ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
                    if (cj == null)
                    {
                        #region
                        this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value = "Unloader";
                        comboBoxCell = this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] as DataGridViewComboBoxCell;
                        comboBoxCell.Items.Clear();
                        for (int i = 0; i < CarrierOperationConfigurator.Configuration.Body.LoadPorts.Count; i++)
                        {
                            eachSpec = CarrierOperationConfigurator.Configuration.Body.LoadPorts[i];
                            if (eachSpec.Mode != CarrierOperationMode.Manual) continue;
                            eachLoadPort = Sys.Equipment.Modules.GetByUid(eachSpec.Uid) as LoadPort;
                            eachCj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(eachLoadPort);
                            if (eachCj == null)
                            {
                                comboBoxCell.Items.Add(eachLoadPort.Name);
                                if (this.SelectedLoadPort.Name == eachLoadPort.Name)
                                    comboBoxCell.Value = eachLoadPort.Name;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region
                        MaterialOutSpec materialOutSpec = cj.MaterialOutSpec[0];
                        LoadPort partner = null;
                        if (this.SelectedLoadPort.Plate.CarrierIdentifier == materialOutSpec.SourceMap.CarrierID)
                        {
                            LoadPortManager.GetLoadPort(materialOutSpec.DestinationMap.CarrierID, out partner);
                            this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value = "Unloader";
                        }
                        else if (this.SelectedLoadPort.Plate.CarrierIdentifier == materialOutSpec.DestinationMap.CarrierID)
                        {
                            LoadPortManager.GetLoadPort(materialOutSpec.SourceMap.CarrierID, out partner);
                            this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value = "Loader";
                        }

                        if (partner != null)
                        {
                            textBoxCell = new DataGridViewTextBoxCell();
                            this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] = textBoxCell;
                            textBoxCell.ReadOnly = true;
                            textBoxCell.Value = partner.Name;
                        }
                        #endregion
                    }
                }
                else
                {
                    this.flexGridInfo.Rows[(int)InfoRows.Partner].Visible = false;
                }
            }
            else
            {
                #region
                this.flexGridInfo[(int)InfoCols.Name, (int)InfoRows.Partner].Value = "Unloader";
                textBoxCell = new DataGridViewTextBoxCell();
                this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] = textBoxCell;
                textBoxCell.ReadOnly = true;
                textBoxCell.Value = this.SelectedLoadPort.Name;
                this.flexGridInfo.Rows[(int)InfoRows.Partner].Visible = false;
                #endregion
            }
            #endregion
        }

        private void DisplayGridInfo()
        {
            if (this.SelectedLoadPort == null) return;

            // Carrier ID
            if (this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.CarrierId].Value as string != this.SelectedLoadPort.Plate.CarrierIdentifier)
                this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.CarrierId].Value = this.SelectedLoadPort.Plate.CarrierIdentifier;

            // Partner
            if (CarrierOperationConfigurator.Supported == true)
            {
                LoadPortCarrierOperationSpecification specification = CarrierOperationConfigurator.GetLoadPortCarrierOperationSpecification(this.SelectedLoadPort);
                if (specification.Mode == CarrierOperationMode.Manual)
                {
                    ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
                    if (cj == null && this.flexGridInfo[(int)InfoCols.Value, (int)InfoRows.Partner] is DataGridViewTextBoxCell)
                        this.InitGridInfo();
                }
            }
        }

        private void SetSlotCount()
        {
            int slotCount = (int)Carrier.DefaultCapacity;
            Carrier carrier = null;

            // 케리어가 없는 경우는 configuration에 있는 첫번째것을 사용한다.
            if (0 < BatchContainerConfigurator.BatchContainerConfigurations.Count)
                slotCount = BatchContainerConfigurator.BatchContainerConfigurations[0].Body.MechanicalSpecification.Capacity;

            if (this.SelectedLoadPort != null)
                carrier = this.SelectedLoadPort.Port.Location.GetMaterial() as Carrier;

            if (carrier != null && carrier.Presence == MaterialPresence.Exist)
                slotCount = (int)carrier.Capacity;

            if (this.slotSelector1.SlotCount != slotCount)
                this.slotSelector1.SlotCount = this.slotSelector2.SlotCount = slotCount;
            if (this.slotStateView1.SlotCount != slotCount)
                this.slotStateView1.SlotCount = this.slotStateView2.SlotCount = slotCount;
        }
        #endregion

        #region Repetitive Actions
        private void Display()
        {
            this.DisplayGridInfo();
            this.ManageOperationButtons();
            this.ManageSlotBoxes();
        }

        private void ManageOperationButtons()
        {
            MaterialPresence presence = MaterialPresence.Unknown;

            if (this.SelectedLoadPort == null)
            {
                // 선택되지 않은 경우
                this.buttonStart.Enabled = this.buttonPause.Enabled = this.buttonResume.Enabled = this.buttonStop.Enabled = false;
                return;
            }

            ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
            presence = this.SelectedLoadPort.Port.Location.GetMaterial().Presence;
            if (cj == null)
            {
                if (CarrierOperationConfigurator.Supported)
                {
                    LoadPortCarrierOperationSpecification specification =
                        CarrierOperationConfigurator.GetLoadPortCarrierOperationSpecification(this.SelectedLoadPort);
                    if (specification.Mode == CarrierOperationMode.Pair)
                    {
                        if (specification.Type == CarrierInOutType.Input)
                        {
                            this.buttonStart.Enabled = presence == MaterialPresence.Exist;
                            this.buttonPause.Enabled = this.buttonResume.Enabled = this.buttonStop.Enabled = false;
                        }
                        else
                        {
                            this.buttonStart.Enabled = this.buttonPause.Enabled = this.buttonResume.Enabled = this.buttonStop.Enabled = false;
                        }
                    }
                    else
                    {
                        this.buttonStart.Enabled = presence == MaterialPresence.Exist;
                        this.buttonPause.Enabled = this.buttonResume.Enabled = this.buttonStop.Enabled = false;
                    }
                }
                else
                {
                    this.buttonStart.Enabled = presence == MaterialPresence.Exist;
                    this.buttonPause.Enabled = this.buttonResume.Enabled = this.buttonStop.Enabled = false;
                }
            }
            else
            {
                this.buttonStart.Enabled = cj.State.CurrentStateValue == ControlJobStateMachine.StateEnum.None && presence == MaterialPresence.Exist;
                this.buttonStop.Enabled = cj.State.IsActive() == true || cj.State.CurrentStateValue == ControlJobStateMachine.StateEnum.Queued;
                this.buttonPause.Enabled = cj.State.CurrentStateValue == ControlJobStateMachine.StateEnum.Executing;
                this.buttonResume.Enabled = cj.State.CurrentStateValue == ControlJobStateMachine.StateEnum.Paused;
            }
        }

        private void ManageSlotBoxes()
        {
            bool visible = false;
            ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
            StackCarrier stackCarrier = null;

            if (this.SelectedLoadPort != null)
                stackCarrier = this.SelectedLoadPort.Plate.Port.Location.GetMaterial() as StackCarrier;

            if (cj == null && this.SelectedLoadPort != null && this.SelectedLoadPort.Port.Location.GetMaterial().Presence != MaterialPresence.Exist)
            {
                // carrier가 없는 경우는 보이지 않도록 안다.
                this.slotSelector1.Visible = this.slotStateView1.Visible = false;
                this.slotSelector2.Visible = this.slotStateView2.Visible = false;
            }
            else
            {
                visible = cj == null;
                if (this.slotSelector1.Visible != visible)
                    this.slotSelector1.Visible = visible;
                visible = visible && stackCarrier != null && stackCarrier.Above != null;
                if (this.slotSelector2.Visible != visible)
                    this.slotSelector2.Visible = visible;

                visible = cj != null;
                if (this.slotStateView1.Visible == false && visible)
                    this.slotSelector1.DeselectSlot();
                if (this.slotStateView1.Visible != visible)
                    this.slotStateView1.Visible = visible;

                visible = visible && stackCarrier != null && stackCarrier.Above != null;
                if (this.slotStateView2.Visible == false && visible)
                    this.slotSelector2.DeselectSlot();
                if (this.slotStateView2.Visible != visible)
                    this.slotStateView2.Visible = visible;
            }

            bool slotSelectionButtonEnabled = (cj == null && this.SelectedLoadPort != null && this.slotSelector1.Visible == true);
            this.buttonDeselect.Enabled = slotSelectionButtonEnabled;
            this.buttonSelect.Enabled = slotSelectionButtonEnabled;

            if (this.slotStateView1.Visible == true && this.SelectedLoadPort != null)
            {
                Carrier carrier = CarrierManager.GetByObjId(this.SelectedLoadPort.Plate.CarrierIdentifier);
                if (this.slotStateView1.Carrier != carrier)
                    this.slotStateView1.Carrier = carrier;
                this.slotStateView1.Display();
            }

            if (this.slotStateView2.Visible == true && this.SelectedLoadPort != null && stackCarrier != null)
            {
                if (this.slotStateView2.Carrier != stackCarrier.Above)
                    this.slotStateView2.Carrier = stackCarrier.Above;
                this.slotStateView2.Display();
            }

            if (this.slotSelector1.Visible == true || this.slotStateView1.Visible == true)
                this.SetSlotCount();
        }
        #endregion

        #region Control Job Handling
        protected virtual int GetControlJobActivatingSpecifications(ref ControlJobSpecificationCollection specifications)
        {
            int ret = 0;
            ControlJobSpecification specification = null;
            SlotSelector.SlotInformationCollection selectedSlots = this.slotSelector1.GetSelectedSlots();
            LoadPort outputLoadPort = null;
            string[] recipeNames;
            RecipeIdentifier recipeId;
            int[] selectedSlotIndexes;
            ProcessJobSpecification recipeSlot;
            LoadPort[] batchLoadports = null;
            StackCarrier stackCarrier = null;
            Carrier inputCarrier, outputCarrier;
            CarrierCollection inputCarriers = null, outputCarriers = null;

            batchLoadports = CarrierOperationConfigurator.GetBatchLoadPorts(this.SelectedLoadPort);
            for (int i = 0; i < batchLoadports.Length; i++)
            {
                // carrier를 로드하지 않은 것은 제외한다.
                if (batchLoadports[i].Plate.AssociationState.CurrentStateValue == LoadPortCarrierAssociationStateMachine.StateEnum.NotAssociated) continue;

                outputLoadPort = CarrierOperationConfigurator.GetPartnerLoadPort(batchLoadports[i]);
                if (batchLoadports[i] != outputLoadPort)
                {
                    if (string.IsNullOrEmpty(outputLoadPort.Plate.CarrierIdentifier) == true)
                    {
                        MsgBox.Show("Unloader carrier doesnot exist.");
                        return -1;
                    }
                }

                inputCarrier = batchLoadports[i].Plate.Port.Location.GetMaterial() as Carrier;
                outputCarrier = outputLoadPort.Plate.Port.Location.GetMaterial() as Carrier;

                if (inputCarrier is StackCarrier)
                {
                    inputCarriers = ((StackCarrier)inputCarrier).GetAll();
                    outputCarriers = ((StackCarrier)outputCarrier).GetAll();
                }
                else
                {
                    inputCarriers = new CarrierCollection();
                    inputCarriers.Add(inputCarrier);
                    outputCarriers = new CarrierCollection();
                    outputCarriers.Add(outputCarrier);
                }

                for (int j = 0; j < inputCarriers.Count; j++)
                {
                    specification = new ControlJobSpecification();

                    specification.InputCarrierId = inputCarriers[j].ObjId;
                    specification.OutputCarrierId = outputCarriers[j].ObjId;

                    if (j == 0)
                    {
                        selectedSlots = this.slotSelector1.GetSelectedSlots();
                        if(selectedSlots.Count == 0)
                        {
                            if(Sys.Equipment is GeneralSemiEquipment equipment && equipment.RecipeAssigner.AssignedUnionRecipe != null)
                                this.slotSelector1.SelectSlot(equipment.RecipeAssigner.AssignedUnionRecipe.Name);
                            else
                                this.slotSelector1.SelectSlot();
                        }
                            
                        selectedSlots = this.slotSelector1.GetSelectedSlots();
                    }
                    else
                    {
                        selectedSlots = this.slotSelector2.GetSelectedSlots();
                        if(selectedSlots.Count == 0)
                        {
                            if(Sys.Equipment is GeneralSemiEquipment equipment && equipment.RecipeAssigner.AssignedUnionRecipe != null)
                                this.slotSelector2.SelectSlot(equipment.RecipeAssigner.AssignedUnionRecipe.Name);
                            else
                                this.slotSelector2.SelectSlot();
                        }
                        selectedSlots = this.slotSelector2.GetSelectedSlots();
                    }

                    recipeNames = selectedSlots.GetUniqueSelectedText();
                    for (int k = 0; k < recipeNames.Length; k++)
                    {
                        recipeId = RecipeIdentifier.Parse(string.Concat(this.SpecifiedRecipeClass.Name, recipeNames[k]));
                        recipeSlot = new ProcessJobSpecification(recipeId);
                        selectedSlotIndexes = selectedSlots.GetSelectedIndices(recipeNames[k]);
                        foreach (int index in selectedSlotIndexes)
                            recipeSlot.SlotNos.Add(index + 1);
                        specification.ProcessJobSpecifications.Add(recipeSlot);
                    }

                    specifications.Add(specification);
                }
            }

            return ret;
        }

        protected virtual int VerifyBeforeStart()
        {
            int ret = 0;
            return ret;
        }

        protected virtual int CreateControlJob()
        {
            int ret = 0;
            ControlJobSpecificationCollection specifications = new ControlJobSpecificationCollection();

            if ((ret = this.GetControlJobActivatingSpecifications(ref specifications)) != 0) return ret;

            if ((ret = ((SemiEquipment)Sys.Equipment).ControlJobActivator.CreateInstance(specifications)) != 0) return ret;
            
            // job 생성 후 unloader partner를 표시하고 변경을 할 수 없도록 한다
            this.InitGridInfo();

            return ret;
        }

        private int PauseControlJob()
        {
            int ret = 0;
            ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
            if (cj == null) return ErrorManager.Register("The control job does not exist.");
            // 실패한 경우는 알람이 발생한다.
            if ((ret = ControlJobManager.Pause(cj.ObjID)) != 0) return ret;
            this.buttonPause.Enabled = false;
            return ret;
        }

        private int ResumeControlJob()
        {
            int ret = 0;
            ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
            if (cj == null) return ErrorManager.Register("The control job does not exist.");
            // 실패한 경우는 알람이 발생한다.
            if ((ret = ControlJobManager.Resume(cj.ObjID)) != 0) return ret;
            this.buttonResume.Enabled = false;
            return ret;
        }

        private int StopControlJob()
        {
            int ret = 0;
            ControlJob cj = CarrierOperationConfigurator.GetControlJobInBatchLoadPorts(this.SelectedLoadPort);
            if (cj == null) return ErrorManager.Register("The control job does not exist.");
            if (MsgBox.ShowOkCancel("Do you really want to stop?") != DialogResult.OK) return ret;
            // 실패한 경우는 알람이 발생한다.
            if ((ret = ControlJobManager.Stop(cj.ObjID, null)) != 0) return ret;
            this.buttonStop.Enabled = false;
            return ret;
        }
        #endregion
        #endregion

        #region ChildForm
        protected override void OnHiding(EventArgs e)
        {
            base.OnHiding(e);
            this.timer.Enabled = false;
        }

        protected override void OnShowing(EventArgs e)
        {
            base.OnShowing(e);
            this.MakeRecipeList();
            this.Display();
            this.timer.Enabled = true;
        }

        protected override void OnPrepare()
        {
            RecipeTreeItem treeItem = null;
            UnionRecipeAssigner assigner = null;
            IHaveUnionRecipeAssigner haveUnionRecipeAssigner = null;

            base.OnPrepare();

            this.buttonShowRecipe.Visible = Sys.Equipment is IShowRecipeEditor;

            this.ColumnNo.Visible = RecipeManager.Configuration.Body.ManagedByNo;

            haveUnionRecipeAssigner = Sys.Equipment as IHaveUnionRecipeAssigner;
            if (haveUnionRecipeAssigner == null)
                throw new ApplicationException("Equipment is not a IHaveUnionRecipeAssigner");
            assigner = haveUnionRecipeAssigner.RecipeAssigner;
            if (assigner == null)
                throw new ApplicationException();

            treeItem = RecipeManager.GetRecipeTreeItem(assigner.RecipeClass);
            if (treeItem == null)
                throw new ArgumentException("Unable to specify which recipe to use for operation.");

            this.SpecifiedRecipeClass = assigner.RecipeClass;

            this.SynchronizedManagedRecipes = new SynchronizedManagedRecipeList();
            this.SynchronizedManagedRecipes.Synchronize(this.SpecifiedRecipeClass);

            this.CreateLoaderButtons();

            this.buttonSelect.Text = QMCSystem.Translate(this.buttonSelect.Text);
            this.buttonDeselect.Text = QMCSystem.Translate(this.buttonDeselect.Text);
            this.buttonShowRecipe.Text = QMCSystem.Translate(this.buttonShowRecipe.Text);
            this.buttonStart.Text = QMCSystem.Translate(this.buttonStart.Text);
            this.buttonResume.Text = QMCSystem.Translate(this.buttonResume.Text);
            this.buttonPause.Text = QMCSystem.Translate(this.buttonPause.Text);
            this.buttonStop.Text = QMCSystem.Translate(this.buttonStop.Text);
            this.titleLabelRecipeIdentifier.Title = QMCSystem.Translate(this.titleLabelRecipeIdentifier.Title);
         
            this.ColumnName.HeaderText = QMCSystem.Translate(this.ColumnName.HeaderText);
            this.ColumnNo.HeaderText = QMCSystem.Translate(this.ColumnNo.HeaderText);

            this.flexGridInfoColumnName.HeaderText = QMCSystem.Translate(this.flexGridInfoColumnName.HeaderText);
            this.flexGridInfoColumnValue.HeaderText = QMCSystem.Translate(this.flexGridInfoColumnValue.HeaderText);
        }
        #endregion
    }
}