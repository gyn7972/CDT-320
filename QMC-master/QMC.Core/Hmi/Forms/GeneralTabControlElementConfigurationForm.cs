using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Transfer;

using QMC.Equipments;
using QMC.Parts;

namespace QMC.Hmi.Forms
{
    public partial class GeneralTabControlElementConfigurationForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlElementConfigurationForm
    {
        #region Define
        #endregion

        #region Field
        private Dictionary<string, AccordionListView> m_AccordionItems;
        private AccordionListViewItem m_SelectedItem;
        #endregion

        #region Constructor
        public GeneralTabControlElementConfigurationForm(IElementConfigurable target) : base(target)
        {
            InitializeComponent();

            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel1.WrapContents = false;

            this.m_AccordionItems = new Dictionary<string, AccordionListView>();

            this.OmittedElementTypes.Add(typeof(PositionRepository));

            this.labelDescription.Paint += (s, e) =>
            {
                if(s is Label lbl == false) return;
                if(lbl.Tag is string text == false || string.IsNullOrEmpty(text)) return;

                int iconSize = 14;
                int textPadding = 4;

                // 텍스트를 그릴 영역 크기(아이콘 영역을 뺀 나머지 폭)
                int availableWidth = lbl.Width - (iconSize + textPadding);
                Size proposedSize = new Size(availableWidth, int.MaxValue);

                //text = QMCSystem.Translate(text);

                // 실제 텍스트가 차지할 크기 계산 (줄바꿈 포함)
                Size textSize = TextRenderer.MeasureText(
                    text,
                    lbl.Font,
                    proposedSize,
                    TextFormatFlags.WordBreak
                );

                // Label 높이를 텍스트 + 아이콘 크기에 맞게 조정
                int neededHeight = Math.Max(iconSize, textSize.Height);
                if(lbl.Height != neededHeight)
                    lbl.Height = neededHeight;

                // 아이콘 그리기 (세로 가운데 정렬)
                int iconY = (lbl.Height - iconSize) / 2;
                using(Icon ico = new Icon(SystemIcons.Information, new Size(iconSize, iconSize)))
                {
                    e.Graphics.DrawImage(ico.ToBitmap(), new Rectangle(0, iconY, iconSize, iconSize));
                }

                // 텍스트 그리기 (아이콘 오른쪽에 줄바꿈 지원)
                var textRect = new Rectangle(iconSize + textPadding, 0, availableWidth, lbl.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    lbl.Font,
                    textRect,
                    lbl.ForeColor,
                    TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.Top
                );
            };
        }
        public GeneralTabControlElementConfigurationForm() : this(null) { }
        #endregion

        #region Event Handlers
        private void Item_Selected(object sender, EventArgs e)
        {
            if(this.m_SelectedItem != null)
                this.m_SelectedItem.Defocus();

            if(sender is AccordionListViewItem item)
                this.m_SelectedItem = item;

            this.RedrawItem();
        }

        private void buttonXAdd_Click(object sender, EventArgs e)
        {
            NumericTextBox currentBase = null;
            NumericTextBox currentOffset = null;

            if(sender == this.buttonXAddX)
            {
                currentBase = this.numericTextBoxCurrentBaseX;
                currentOffset = this.numericTextBoxCurrentOffsetX;
            }
            else if(sender == this.buttonXAddY)
            {
                currentBase = this.numericTextBoxCurrentBaseY;
                currentOffset = this.numericTextBoxCurrentOffsetY;
            }
            else if(sender == this.buttonXAddZ)
            {
                currentBase = this.numericTextBoxCurrentBaseZ;
                currentOffset = this.numericTextBoxCurrentOffsetZ;
            }
            else if(sender == this.buttonXAddT)
            {
                currentBase = this.numericTextBoxCurrentBaseT;
                currentOffset = this.numericTextBoxCurrentOffsetT;
            }

            if(currentBase != null && currentOffset != null)
            {
                double baseValue = Convert.ToDouble(currentBase.Text);
                double offsetValue = Convert.ToDouble(currentOffset.Text);

                currentBase.Text = (baseValue + offsetValue).ToString();
                currentOffset.Text = 0.ToString();
            }
        }

        private void buttonXApply_Click(object sender, EventArgs e)
        {
            if(this.m_SelectedItem != null)
            {
                Xyzt baseValue = new Xyzt()
                {
                    X = Convert.ToDouble(this.numericTextBoxCurrentBaseX.Text),
                    Y = Convert.ToDouble(this.numericTextBoxCurrentBaseY.Text),
                    T = Convert.ToDouble(this.numericTextBoxCurrentBaseT.Text),
                    Z = Convert.ToDouble(this.numericTextBoxCurrentBaseZ.Text),
                };

                Xyzt offsetValue = new Xyzt()
                {
                    X = Convert.ToDouble(this.numericTextBoxCurrentOffsetX.Text),
                    Y = Convert.ToDouble(this.numericTextBoxCurrentOffsetY.Text),
                    T = Convert.ToDouble(this.numericTextBoxCurrentOffsetT.Text),
                    Z = Convert.ToDouble(this.numericTextBoxCurrentOffsetZ.Text),
                };

                if(this.m_SelectedItem.EnablePower == true)
                {
                    int power = Convert.ToInt32(this.numericTextBoxCurrentPower.Text);
                    this.m_SelectedItem.ApplyContent(baseValue, offsetValue, power);
                }
                else
                    this.m_SelectedItem.ApplyContent(baseValue, offsetValue);
            }
        }

        private void GeneralTabControlElementConfigurationForm_AppliedConfiguration(object sender, EquipmentEventArgs e)
        {
            PositionRepository positionRepository = sender as PositionRepository;
            AccordionListView listView = null;
            Part owner = null;

            owner = ElementList.GetOwner<Part>(positionRepository);

            if(this.m_AccordionItems.ContainsKey(owner.Alias) == false) return;

            listView = this.m_AccordionItems[owner.Alias];

            string section = null;

            if(positionRepository.Owner is Part)
                section = "Common position";
            else if(positionRepository.Owner is IContainServiceExecutor serviceExecutor && string.IsNullOrEmpty(serviceExecutor.ServiceName) == false)
                section = $"{serviceExecutor.ServiceName} position";
            else
                section = $"{positionRepository.Owner.Alias} position";

            listView.SetContents(section, positionRepository.Name, positionRepository.SelectedSize, positionRepository.Configuration.Body.Positions);

            this.RedrawItem();
        }

        private void GeneralTabControlElementConfigurationForm_SelectedChange(object sender, int e)
        {
            PositionRepository positionRepository = sender as PositionRepository;
            AccordionListView listView = null;
            Part owner = null;

            owner = ElementList.GetOwner<Part>(positionRepository);

            if(this.m_AccordionItems.ContainsKey(owner.Alias) == false) return;

            listView = this.m_AccordionItems[owner.Alias];

            string section = null;

            if(positionRepository.Owner is Part)
                section = "Common position";
            else if(positionRepository.Owner is IContainServiceExecutor serviceExecutor && string.IsNullOrEmpty(serviceExecutor.ServiceName) == false)
                section = $"{serviceExecutor.ServiceName} position";
            else
                section = $"{positionRepository.Owner.Alias} position";

            listView.SetContents(section, positionRepository.Name, positionRepository.SelectedSize, positionRepository.Configuration.Body.Positions);

            this.RedrawItem();
        }

        private void numericTextBox_TextChanged(object sender, EventArgs e)
        {
            double baseValue = 0;
            double offsetValue = 0;
            NumericTextBox baseNumericTextBox = null;
            NumericTextBox offsetNumericTextBox = null;
            NumericTextBox totalNumericTextBox = null;

            if(sender == this.numericTextBoxCurrentBaseX || sender == this.numericTextBoxCurrentOffsetX)
            {
                baseNumericTextBox = this.numericTextBoxCurrentBaseX;
                offsetNumericTextBox = this.numericTextBoxCurrentOffsetX;
                totalNumericTextBox = this.numericTextBoxCurrentTotalX;
            }
            else if(sender == this.numericTextBoxCurrentBaseY || sender == this.numericTextBoxCurrentOffsetY)
            {
                baseNumericTextBox = this.numericTextBoxCurrentBaseY;
                offsetNumericTextBox = this.numericTextBoxCurrentOffsetY;
                totalNumericTextBox = this.numericTextBoxCurrentTotalY;
            }
            else if(sender == this.numericTextBoxCurrentBaseZ || sender == this.numericTextBoxCurrentOffsetZ)
            {
                baseNumericTextBox = this.numericTextBoxCurrentBaseZ;
                offsetNumericTextBox = this.numericTextBoxCurrentOffsetZ;
                totalNumericTextBox = this.numericTextBoxCurrentTotalZ;
            }
            else if(sender == this.numericTextBoxCurrentBaseT || sender == this.numericTextBoxCurrentOffsetT)
            {
                baseNumericTextBox = this.numericTextBoxCurrentBaseT;
                offsetNumericTextBox = this.numericTextBoxCurrentOffsetT;
                totalNumericTextBox = this.numericTextBoxCurrentTotalT;
            }

            baseValue = Convert.ToDouble(baseNumericTextBox.Text);
            offsetValue = Convert.ToDouble(offsetNumericTextBox.Text);

            totalNumericTextBox.Text = (baseValue + offsetValue).ToString();
        }
        #endregion

        #region Method
        private string GetPortName(string transferName, int portIndex)
        {
            ITransfer transfer = Sys.Equipment.Modules.GetByName(transferName) as ITransfer;

            if(transfer == null) return "";

            return transfer.Ports[portIndex].MaterialStorablePart.Alias;
        }

        private void RedrawItem()
        {
            if(this.m_SelectedItem != null)
            {
                if(this.m_SelectedItem.Parent.Parent is AccordionListView listView)
                {
                    this.labelPart.Text = listView.Text;
                    this.labelPart.Visible = true;
                }
                else
                    this.labelPart.Visible = false;

                this.labelSection.Visible = true;

                if(this.m_SelectedItem.Data.MaterialSize == -1)
                    this.labelSection.Text = $"{this.m_SelectedItem.Section}";
                else
                    this.labelSection.Text = $"{this.m_SelectedItem.Section} - {this.m_SelectedItem.Data.MaterialSize} inch";

                this.labelPosition.Visible = true;

                if(string.IsNullOrEmpty(this.m_SelectedItem.Group) == true)
                    this.labelPosition.Text = $"{this.m_SelectedItem.Data.Action}";
                else
                    this.labelPosition.Text = $"{this.m_SelectedItem.Data.Action} [{this.m_SelectedItem.Group}]";

                if(this.m_SelectedItem.Tag is PositionRepository positionRepository)
                {
                    string description = positionRepository.GetDescription(this.m_SelectedItem.Data.Action);

                    if(string.IsNullOrEmpty(description) == true)
                        this.labelDescription.Visible = false;
                    else
                    {
                        this.labelDescription.Text = "";
                        this.labelDescription.Visible = true;
                        this.labelDescription.Tag = description;
                        this.labelDescription.Invalidate();
                    }
                }

                this.panelXX.Visible = this.m_SelectedItem.EnableX;
                this.panelXY.Visible = this.m_SelectedItem.EnableY;
                this.panelXZ.Visible = this.m_SelectedItem.EnableZ;
                this.panelXT.Visible = this.m_SelectedItem.EnableT;
                this.panelXPower.Visible = this.m_SelectedItem.EnablePower;

                this.numericTextBoxPreviousBaseX.Text = this.m_SelectedItem.Data.Base.X.ToString();
                this.numericTextBoxPreviousOffsetX.Text = this.m_SelectedItem.Data.Offset.X.ToString();
                this.numericTextBoxPreviousTotalX.Text = this.m_SelectedItem.Data.Position.X.ToString();

                this.numericTextBoxCurrentBaseX.Text = this.m_SelectedItem.CurrentData.Base.X.ToString();
                this.numericTextBoxCurrentOffsetX.Text = this.m_SelectedItem.CurrentData.Offset.X.ToString();
                this.numericTextBoxCurrentTotalX.Text = this.m_SelectedItem.CurrentData.Position.X.ToString();

                this.numericTextBoxPreviousBaseY.Text = this.m_SelectedItem.Data.Base.Y.ToString();
                this.numericTextBoxPreviousOffsetY.Text = this.m_SelectedItem.Data.Offset.Y.ToString();
                this.numericTextBoxPreviousTotalY.Text = this.m_SelectedItem.Data.Position.Y.ToString();

                this.numericTextBoxCurrentBaseY.Text = this.m_SelectedItem.CurrentData.Base.Y.ToString();
                this.numericTextBoxCurrentOffsetY.Text = this.m_SelectedItem.CurrentData.Offset.Y.ToString();
                this.numericTextBoxCurrentTotalY.Text = this.m_SelectedItem.CurrentData.Position.Y.ToString();

                this.numericTextBoxPreviousBaseZ.Text = this.m_SelectedItem.Data.Base.Z.ToString();
                this.numericTextBoxPreviousOffsetZ.Text = this.m_SelectedItem.Data.Offset.Z.ToString();
                this.numericTextBoxPreviousTotalZ.Text = this.m_SelectedItem.Data.Position.Z.ToString();

                this.numericTextBoxCurrentBaseZ.Text = this.m_SelectedItem.CurrentData.Base.Z.ToString();
                this.numericTextBoxCurrentOffsetZ.Text = this.m_SelectedItem.CurrentData.Offset.Z.ToString();
                this.numericTextBoxCurrentTotalZ.Text = this.m_SelectedItem.CurrentData.Position.Z.ToString();

                this.numericTextBoxPreviousBaseT.Text = this.m_SelectedItem.Data.Base.T.ToString();
                this.numericTextBoxPreviousOffsetT.Text = this.m_SelectedItem.Data.Offset.T.ToString();
                this.numericTextBoxPreviousTotalT.Text = this.m_SelectedItem.Data.Position.T.ToString();

                this.numericTextBoxCurrentBaseT.Text = this.m_SelectedItem.CurrentData.Base.T.ToString();
                this.numericTextBoxCurrentOffsetT.Text = this.m_SelectedItem.CurrentData.Offset.T.ToString();
                this.numericTextBoxCurrentTotalT.Text = this.m_SelectedItem.CurrentData.Position.T.ToString();

                this.numericTextBoxPreviousPower.Text = this.m_SelectedItem.Data.Power.ToString();

                this.panelApply.Visible = true;
            }
            else
            {
                this.labelPart.Visible = false;
                this.labelSection.Visible = false;
                this.labelPosition.Visible = false;

                this.panelXX.Visible = false;
                this.panelXY.Visible = false;
                this.panelXZ.Visible = false;
                this.panelXT.Visible = false;
                this.panelXPower.Visible = false;

                this.panelApply.Visible = false;
            }
        }
        #endregion

        #region ConfigurationForm Members
        protected override int OnSaveToMemory()
        {
            int ret = 0;
            PositionRepository[] positionRepositories = null;
            Part owner = null;
            AccordionListView listView = null;
            Dictionary<string, IXyztPositionData> positionDatas = null;

            if((ret = base.OnSaveToMemory()) != 0) return ret;

            if(this.tabPageXPositions.Visible == false) return ret;

            positionRepositories = ElementList.GetByTypeAndLocator<PositionRepository>(this.Target.Locator.ToString(), true);

            for(int i = 0; i < positionRepositories.Length; i++)
            {
                owner = ElementList.GetOwner<Part>(positionRepositories[i]);

                if(this.m_AccordionItems.ContainsKey(owner.Alias) == false) continue;

                listView = this.m_AccordionItems[owner.Alias];

                string section = null;

                if(positionRepositories[i].Owner is Part)
                    section = "Common position";
                else if(positionRepositories[i].Owner is IContainServiceExecutor serviceExecutor && string.IsNullOrEmpty(serviceExecutor.ServiceName) == false)
                    section = $"{serviceExecutor.ServiceName} position";
                else
                    section = $"{positionRepositories[i].Owner.Alias} position";

                positionDatas = positionRepositories[i].Configuration.Body.Positions;

                listView.GetContents(section, positionRepositories[i].Name, positionRepositories[i].SelectedSize, ref positionDatas);

                positionRepositories[i].Configuration.Body.Positions = positionDatas;
            }

            for(int i = 0; i < positionRepositories.Length; i++)
            {
                ElementConfigurator.Save(positionRepositories[i].Configuration);
                positionRepositories[i].ApplyConfiguration(positionRepositories[i].Configuration);
            }

            return ret;
        }

        protected override int OnRead()
        {
            int ret = 0;
            PositionRepository[] positionRepositories = null;
            Part owner = null;
            AccordionListView listView = null;

            if((ret = base.OnRead()) != 0) return ret;

            if(this.tabPageXPositions.Visible == false) return ret;

            positionRepositories = ElementList.GetByTypeAndLocator<PositionRepository>(this.Target.Locator.ToString(), true);

            for(int i = 0; i < positionRepositories.Length; i++)
            {
                owner = ElementList.GetOwner<Part>(positionRepositories[i]);

                if(this.m_AccordionItems.ContainsKey(owner.Alias) == false) continue;

                listView = this.m_AccordionItems[owner.Alias];

                string section = null;

                if(positionRepositories[i].Owner is Part)
                    section = "Common position";
                else if(positionRepositories[i].Owner is IContainServiceExecutor serviceExecutor && string.IsNullOrEmpty(serviceExecutor.ServiceName) == false)
                    section = $"{serviceExecutor.ServiceName} position";
                else
                    section = $"{positionRepositories[i].Owner.Alias} position";

                listView.SetContents(section, positionRepositories[i].Name, positionRepositories[i].SelectedSize, positionRepositories[i].Configuration.Body.Positions);
            }

            this.RedrawItem();

            return ret;
        }
        #endregion

        #region ChildForm Members
        protected override void OnPrepare()
        {
            AccordionListView listView = null;
            Part owner = null;
            PositionRepository[] positionRepositories = null;
            AccordionListViewItem item = null;

            base.OnPrepare();

            if(this.Target is Equipment)
            {
                this.tabPageXPositions.Visible = false;
                return;
            }

            if(Sys.Equipment is GeneralSemiEquipment equipment)
            {
                this.tabPageXPositions.Visible = equipment.PositionVisible;

                if(equipment.PositionVisible == false) return;
            }

            positionRepositories = ElementList.GetByTypeAndLocator<PositionRepository>(this.Target.Locator.ToString(), true);

            for(int i = 0; i < positionRepositories.Length; i++)
            {
                positionRepositories[i].AppliedConfiguration += GeneralTabControlElementConfigurationForm_AppliedConfiguration;
                positionRepositories[i].SelectedChange += GeneralTabControlElementConfigurationForm_SelectedChange;

                owner = ElementList.GetOwner<Part>(positionRepositories[i]);

                if(this.m_AccordionItems.ContainsKey(owner.Alias) == false)
                {
                    listView = new AccordionListView(owner.Alias);
                    listView.Selected += Item_Selected;
                    listView.Width = this.flowLayoutPanel1.Width - this.flowLayoutPanel1.Margin.Left - this.flowLayoutPanel1.Margin.Right;

                    this.flowLayoutPanel1.Controls.Add(listView);

                    this.m_AccordionItems.Add(owner.Alias, listView);
                }
                else
                    listView = this.m_AccordionItems[owner.Alias];

                string section = null;

                if(positionRepositories[i].Owner is Part)
                    section = "Common position";
                else if(positionRepositories[i].Owner is IContainServiceExecutor serviceExecutor && string.IsNullOrEmpty(serviceExecutor.ServiceName) == false)
                    section = $"{serviceExecutor.ServiceName} position";
                else
                    section = $"{positionRepositories[i].Owner.Alias} position";

                if(positionRepositories[i] is XyztPositionRepository xyztPositionRepository)
                {
                    item = new AccordionListViewItem();
                    item.Tag = xyztPositionRepository;
                    item.Dock = DockStyle.Top;
                    item.SetColumns(
                        xyztPositionRepository.Actions,
                        xyztPositionRepository.EnableX,
                        xyztPositionRepository.EnableY,
                        xyztPositionRepository.EnableZ,
                        xyztPositionRepository.EnableT,
                        xyztPositionRepository.EnablePower);

                    listView.AddItem(item, section);
                }
                else if(positionRepositories[i] is TargetXyztPositionRepository targetXyztPositionRepository)
                {
                    foreach(Element element in targetXyztPositionRepository.Targets)
                    {
                        item = new AccordionListViewItem();
                        item.Tag = targetXyztPositionRepository;
                        item.Dock = DockStyle.Top;
                        item.SetColumns(
                            targetXyztPositionRepository.Actions,
                            targetXyztPositionRepository.EnableX,
                            targetXyztPositionRepository.EnableY,
                            targetXyztPositionRepository.EnableZ,
                            targetXyztPositionRepository.EnableT,
                            targetXyztPositionRepository.EnablePower);

                        listView.AddItem(item, section, element.Alias);
                    }
                }
                else if(positionRepositories[i] is TransferXyztPositionRepository transferXyztPositionRepository)
                {
                    foreach(TransferItem transferItem in transferXyztPositionRepository.Transferable.TransferItems)
                    {
                        foreach(int secondaryPort in transferItem.TransferredPorts)
                        {
                            item = new AccordionListViewItem();
                            item.Tag = transferXyztPositionRepository;
                            item.Dock = DockStyle.Top;
                            item.SetColumns(
                                transferXyztPositionRepository.Actions,
                                transferXyztPositionRepository.EnableX,
                                transferXyztPositionRepository.EnableY,
                                transferXyztPositionRepository.EnableZ,
                                transferXyztPositionRepository.EnableT,
                                transferXyztPositionRepository.EnablePower);

                            listView.AddItem(item, section, this.GetPortName(transferItem.TransferredModule, secondaryPort));
                        }
                    }
                }
            }
        }
        #endregion
    }

    public class AccordionListView : UserControl
    {
        #region Define

        #endregion

        #region Field
        private Button m_HeaderButton;
        private Panel m_ContainerPanel;
        private bool m_Expanded;
        private IXyztPositionData m_SelectedData;
        private Dictionary<string, Label> m_Sections;
        private Dictionary<string, AccordionListViewItem> m_Items;
        #endregion

        #region Event
        public event EventHandler Selected;
        #endregion

        #region Constructor
        public AccordionListView(string text)
        {
            this.m_Expanded = false;
            this.m_Sections = new Dictionary<string, Label>();
            this.m_Items = new Dictionary<string, AccordionListViewItem>();

            this.m_ContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 0,
                Visible = false,
                Padding = new Padding(10, 0, 0, 0)
            };

            this.m_HeaderButton = new Button
            {
                Text = "▼ " + text,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.ControlLight,
            };
            this.m_HeaderButton.Click += HeaderButton_Click;

            this.Controls.Add(this.m_ContainerPanel);
            this.Controls.Add(this.m_HeaderButton);

            this.Text = text;

            this.Height = this.m_HeaderButton.Height + 2;
        }
        #endregion

        #region Property
        public new string Text { get; private set; }
        #endregion

        #region Event Handlers
        private void HeaderButton_Click(object sender, EventArgs e)
        {
            int height = this.m_HeaderButton.Height + 2;

            this.m_Expanded = !this.m_Expanded;

            this.m_ContainerPanel.Visible = this.m_Expanded;

            this.m_HeaderButton.Text = (this.m_Expanded ? "▼ " : "▶ ") + this.m_HeaderButton.Text.Substring(2);

            if(this.m_Expanded == true)
            {
                for(int i = 0; i < this.m_ContainerPanel.Controls.Count; i++)
                {
                    height += this.m_ContainerPanel.Controls[i].Height;
                }
            }

            this.Height = height;
        }

        private void Item_Selected(object sender, EventArgs e)
        {
            if(sender is AccordionListViewItem item)
                this.Selected?.Invoke(item, e);
        }
        #endregion

        #region Method
        public void AddItem(AccordionListViewItem item, string section, string group = null)
        {
            Label label = null;
            int childIndex = 0;
            string key = null;
            try
            {
                if(this.m_Sections.ContainsKey(section) == false)
                {
                    if(this.m_ContainerPanel.Controls.Count != 0)
                    {
                        label = new Label
                        {
                            AutoSize = false,
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent,
                            Height = 10,
                        };

                        this.m_ContainerPanel.Controls.Add(label);
                    }

                    label = new Label
                    {
                        AutoSize = false,
                        Dock = DockStyle.Top,
                        Text = section,
                        BackColor = Color.LightGray,
                        BorderStyle = BorderStyle.FixedSingle,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    this.m_ContainerPanel.Controls.Add(label);

                    this.m_Sections.Add(section, label);
                }
                else
                    label = this.m_Sections[section];

                if(group != null)
                    item.Group = group;

                if(item.Tag is PositionRepository positionRepository)
                    key = $"{section}_{group}_{positionRepository.Name}";

                this.m_Items.Add(key, item);

                item.Section = section;
                item.Selected += Item_Selected;

                childIndex = this.m_ContainerPanel.Controls.GetChildIndex(label);

                this.m_ContainerPanel.Controls.Add(item);
                this.m_ContainerPanel.Controls.SetChildIndex(item, childIndex);
            }
            catch(System.ArgumentException ex)
            {
                Log.Write("Exception",$"{key}");

                throw ex;
            }
        }

        public void SetContents(string section, string name, int size, Dictionary<string, IXyztPositionData> positions)
        {
            string key = null;
            string group = null;

            foreach(KeyValuePair<string, IXyztPositionData> position in positions)
            {
                if(position.Value.MaterialSize != size) continue;

                if(position.Value is TargetXyztPositionRepository.TargetXyztKeyPosition target)
                    group = $"{ElementList.GetByLocator(target.TargetName).Alias}";
                if(position.Value is TransferXyztPositionRepository.TransferXyztKeyPosition transfer)
                    group = $"{this.GetPortName(transfer.Secondary, transfer.SecondaryPort)}";

                key = $"{section}_{group}_{name}";

                if(this.m_Items.ContainsKey(key) == false) continue;

                this.m_Items[key].SetContents(size, positions);
            }
        }

        public void GetContents(string section, string name, int size, ref Dictionary<string, IXyztPositionData> positions)
        {
            string key = null;
            string group = null;

            foreach(KeyValuePair<string, IXyztPositionData> position in positions)
            {
                if(position.Value.MaterialSize != size) continue;

                if(position.Value is TargetXyztPositionRepository.TargetXyztKeyPosition target)
                    group = $"{ElementList.GetByLocator(target.TargetName).Alias}";
                if(position.Value is TransferXyztPositionRepository.TransferXyztKeyPosition transfer)
                    group = $"{this.GetPortName(transfer.Secondary, transfer.SecondaryPort)}";

                key = $"{section}_{group}_{name}";

                if(this.m_Items.ContainsKey(key) == false) continue;

                this.m_Items[key].GetContents(size, ref positions);
            }
        }

        private string GetPortName(string transferName, int portIndex)
        {
            ITransfer transfer = Sys.Equipment.Modules.GetByName(transferName) as ITransfer;

            if(transfer == null) return "";

            return transfer.Ports[portIndex].MaterialStorablePart.Alias;
        }
        #endregion
    }

    public class AccordionListViewItem : Panel
    {
        #region Field
        protected int m_SelectedSize;
        protected TableLayoutPanel m_Table;
        protected int m_RowHeight;
        private Control m_SelectedControl;
        private Label m_GroupLabel;
        private string m_Group;
        private int m_SelectedRowIndex;
        #endregion

        #region Event
        public event EventHandler Selected;
        #endregion

        #region Constructor
        public AccordionListViewItem()
        {
            this.m_RowHeight = 25;

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.Transparent
            };

            this.m_Table = table;
            this.m_SelectedRowIndex = -1;

            this.Controls.Add(this.m_Table);

            this.BackColor = Color.White;
        }
        #endregion

        #region Property
        public string Section { get; set; }

        public string Group
        {
            get { return this.m_Group; }
            set
            {
                if(this.m_GroupLabel == null)
                {
                    Label label = new Label
                    {
                        Dock = DockStyle.Left,
                        AutoSize = false,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.LightGray,
                        BorderStyle = BorderStyle.FixedSingle,
                        Width = 240,
                    };

                    this.m_GroupLabel = label;

                    this.Controls.Add(label);
                }

                this.m_GroupLabel.Text = value;
                this.m_Group = value;
            }
        }

        public bool EnableX { get; set; }

        public bool EnableY { get; set; }

        public bool EnableZ { get; set; }

        public bool EnableT { get; set; }

        public bool EnablePower { get; set; }

        public IXyztPositionData Data { get; private set; }

        public IXyztPositionData CurrentData { get; private set; }
        #endregion

        #region Event Handlers
        private void C_Click(object sender, EventArgs e)
        {
            Control clicked = (Control)sender;

            int row = this.m_Table.GetRow(clicked);

            this.m_SelectedRowIndex = row;

            this.Defocus();

            // 새로운 선택 적용
            foreach(Control ctrl in this.m_Table.Controls)
            {
                if(this.m_Table.GetRow(ctrl) == row)
                    ctrl.BackColor = Color.LightBlue;
            }

            (IXyztPositionData, IXyztPositionData) data = ((IXyztPositionData, IXyztPositionData))this.m_Table.GetControlFromPosition(0, row).Tag;

            this.Data = data.Item1;
            this.CurrentData = data.Item2;
            this.Selected?.Invoke(this, EventArgs.Empty);

            this.m_SelectedControl = clicked;
        }
        #endregion

        #region Method
        public void SetColumns(string[] actions, bool enableX, bool enableY, bool enableZ, bool enableT, bool enablePower)
        {
            int columnCount = 1;

            this.EnableX = enableX;
            this.EnableY = enableY;
            this.EnableZ = enableZ;
            this.EnableT = enableT;
            this.EnablePower = enablePower;

            if(enableX == true)
                columnCount++;

            if(enableY == true)
                columnCount++;

            if(enableZ == true)
                columnCount++;

            if(enableT == true)
                columnCount++;

            if(enablePower == true)
                columnCount++;

            this.ReleaseClickEvent();

            this.m_Table.ColumnCount = columnCount;
            this.m_Table.RowCount = 0;

            this.m_Table.ColumnStyles.Clear();
            this.m_Table.RowStyles.Clear();
            this.m_Table.Controls.Clear();

            this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            if(enableX == true)
                this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            if(enableY == true)
                this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            if(enableZ == true)
                this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            if(enableT == true)
                this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            if(enablePower == true)
                this.m_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            foreach(var action in actions)
            {
                int rowIndex = this.m_Table.RowCount++;
                int columnIndex = 0;

                this.m_Table.RowStyles.Add(new RowStyle(SizeType.Absolute, this.m_RowHeight));

                this.m_Table.Controls.Add(this.OnCreateLabel($"{action}"), columnIndex++, rowIndex);

                if(this.EnableX == true)
                    this.m_Table.Controls.Add(this.OnCreateLabel(), columnIndex++, rowIndex);

                if(this.EnableY == true)
                    this.m_Table.Controls.Add(this.OnCreateLabel(), columnIndex++, rowIndex);

                if(this.EnableZ == true)
                    this.m_Table.Controls.Add(this.OnCreateLabel(), columnIndex++, rowIndex);

                if(this.EnableT == true)
                    this.m_Table.Controls.Add(this.OnCreateLabel(), columnIndex++, rowIndex);

                if(this.EnablePower == true)
                    this.m_Table.Controls.Add(this.OnCreateLabel(), columnIndex++, rowIndex);
            }

            this.RegisteClickEvent();

            this.Height = this.m_Table.RowCount * this.m_RowHeight + this.m_Table.RowCount + 1;
        }

        public void SetContents(int size, Dictionary<string, IXyztPositionData> positions)
        {
            int columnIndex = 0;
            int rowIndex = 0;

            foreach(KeyValuePair<string, IXyztPositionData> position in positions)
            {
                columnIndex = 0;

                if(position.Value is TargetXyztPositionRepository.TargetXyztKeyPosition target && this.Group != ElementList.GetByLocator(target.TargetName).Alias) continue;
                if(position.Value is TransferXyztPositionRepository.TransferXyztKeyPosition transfer && this.Group != this.GetPortName(transfer.Secondary, transfer.SecondaryPort)) continue;

                if(position.Value.MaterialSize != size) continue;

                Control control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                if(this.Data != null && position.Value.Action == this.Data.Action)
                {
                    this.Data = position.Value;
                    this.CurrentData = (IXyztPositionData)CopyUtility.GetDeepCopy(position.Value);
                }

                control.Tag = (position.Value, (IXyztPositionData)CopyUtility.GetDeepCopy(position.Value));

                if(this.EnableX == true)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Black;
                        label.Text = $"X [mm] : {position.Value.Position.X.ToString("0.000")}";
                    }
                }

                if(this.EnableY == true)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Black;
                        label.Text = $"Y [mm] : {position.Value.Position.Y.ToString("0.000")}";
                    }
                }

                if(this.EnableZ == true)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Black;
                        label.Text = $"Z [mm] : {position.Value.Position.Z.ToString("0.000")}";
                    }
                }

                if(this.EnableT == true)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Black;
                        label.Text = $"T [°]: {position.Value.Position.T.ToString("0.000")}";
                    }
                }

                if(this.EnablePower == true)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex++, rowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Black;
                        label.Text = $"Power [%] : {position.Value.Power.ToString()}";
                    }
                }

                rowIndex++;
            }
        }

        public void GetContents(int size, ref Dictionary<string, IXyztPositionData> positions)
        {
            int rowIndex = 0;

            foreach(KeyValuePair<string, IXyztPositionData> position in positions)
            {
                if(position.Value is TargetXyztPositionRepository.TargetXyztKeyPosition target && this.Group != ElementList.GetByLocator(target.TargetName).Alias) continue;
                if(position.Value is TransferXyztPositionRepository.TransferXyztKeyPosition transfer && this.Group != this.GetPortName(transfer.Secondary, transfer.SecondaryPort)) continue;

                if(position.Value.MaterialSize != size) continue;

                Control control = this.m_Table.GetControlFromPosition(0, rowIndex);

                (IXyztPositionData, IXyztPositionData) data = ((IXyztPositionData, IXyztPositionData))control.Tag;

                position.Value.Base = data.Item2.Base;
                position.Value.Offset = data.Item2.Offset;
                position.Value.Power = data.Item2.Power;

                rowIndex++;
            }
        }

        protected Label OnCreateLabel(string text = null)
        {
            return new Label { Text = text, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, Margin = new Padding(0) };
        }

        protected void RegisteClickEvent()
        {
            foreach(Control c in this.m_Table.Controls)
            {
                c.Click += C_Click;
            }
        }

        protected void ReleaseClickEvent()
        {
            foreach(Control c in this.m_Table.Controls)
            {
                c.Click -= C_Click;
            }
        }

        private string GetPortName(string transferName, int portIndex)
        {
            ITransfer transfer = Sys.Equipment.Modules.GetByName(transferName) as ITransfer;

            if(transfer == null) return "";

            return transfer.Ports[portIndex].MaterialStorablePart.Alias;
        }

        public void Defocus()
        {
            // 이전 선택 해제
            if(this.m_SelectedControl != null)
            {
                foreach(Control ctrl in this.m_Table.Controls)
                {
                    if(this.m_Table.GetRow(ctrl) == this.m_Table.GetRow(this.m_SelectedControl))
                        ctrl.BackColor = Color.White;
                }
            }

            this.m_SelectedControl = null;
        }

        public void ApplyContent(Xyzt baseValue, Xyzt offsetValue, int? power = null)
        {
            Control control = null;

            int columnIndex = 0;

            this.CurrentData.Base = baseValue;
            this.CurrentData.Offset = offsetValue;

            this.m_Table.GetControlFromPosition(0, this.m_SelectedRowIndex).Tag = (this.Data, this.CurrentData);

            if(this.EnableX == true)
            {
                columnIndex++;

                if(this.Data.Base.X != this.CurrentData.Base.X || this.Data.Offset.X != this.CurrentData.Offset.X)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex, this.m_SelectedRowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Red;
                        label.Text = $"X [mm] : {this.CurrentData.Position.X.ToString("0.000")}";
                    }
                }
            }

            if(this.EnableY == true)
            {
                columnIndex++;

                if(this.Data.Base.Y != this.CurrentData.Base.Y || this.Data.Offset.Y != this.CurrentData.Offset.Y)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex, this.m_SelectedRowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Red;
                        label.Text = $"Y [mm] : {this.CurrentData.Position.Y.ToString("0.000")}";
                    }
                }
            }

            if(this.EnableZ == true)
            {
                columnIndex++;

                if(this.Data.Base.Z != this.CurrentData.Base.Z || this.Data.Offset.Z != this.CurrentData.Offset.Z)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex, this.m_SelectedRowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Red;
                        label.Text = $"Z [mm] : {this.CurrentData.Position.Z.ToString("0.000")}";
                    }
                }
            }

            if(this.EnableT == true)
            {
                columnIndex++;

                if(this.Data.Base.T != this.CurrentData.Base.T || this.Data.Offset.T != this.CurrentData.Offset.T)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex, this.m_SelectedRowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Red;
                        label.Text = $"T [°] : {this.CurrentData.Position.T.ToString("0.000")}";
                    }
                }
            }

            if(this.EnablePower == true && power != null)
            {
                columnIndex++;
                this.CurrentData.Power = power.Value;

                if(this.Data.Power != this.CurrentData.Power)
                {
                    control = this.m_Table.GetControlFromPosition(columnIndex, this.m_SelectedRowIndex);

                    if(control is Label label)
                    {
                        label.ForeColor = Color.Red;
                        label.Text = $"Power [%] : {this.CurrentData.Power.ToString()}";
                    }
                }
            }
        }
        #endregion
    }
}
