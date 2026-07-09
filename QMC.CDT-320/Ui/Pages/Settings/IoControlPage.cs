using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.Common.Logging;
using QMC.Common.IO;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Live DI monitor and DO maintenance control page.
    /// </summary>
    public partial class IoControlPage : PageBase
    {
        private readonly Timer _timer;
        private bool _loaded;

        public IoControlPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);   // 그룹 생성 前에 정규화(Motion 순서) → 이후 그룹 패딩·폰트 유지
            BuildLayout();
            TightenActionButtons();
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                {
                    _timer.Stop();
                    return;
                }

                RefreshRows();
            };
            HandleCreated += (s, e) =>
            {
                EnsureLoaded();
                if (ShouldRefreshVisible(this))
                    _timer.Start();
            };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (_timer == null)
                    return;

                if (ShouldRefreshVisible(this))
                    _timer.Start();
                else
                    _timer.Stop();
            }
            catch { }
        }

        /// <summary>Motion 페이지와 같은 4행 비율로 재구성하여 ACTION 위치와 버튼 크기 기준을 맞춘다.</summary>
        private void BuildLayout()
        {
            SuspendLayout();
            try
            {
                Controls.Clear();

                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 4,
                    Padding = Padding.Empty
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));

                var headerLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    BackColor = UiTheme.StatusBarBg
                };
                headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
                headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                lblHeader.AutoSize = false;
                lblHeader.Dock = DockStyle.Fill;
                lblHeader.Margin = new Padding(0);
                lblStatus.AutoSize = false;
                lblStatus.Dock = DockStyle.Fill;
                lblStatus.Margin = new Padding(0);
                lblStatus.Padding = new Padding(0);
                lblStatus.TextAlign = ContentAlignment.MiddleLeft;
                headerLayout.Controls.Add(lblHeader, 0, 0);
                headerLayout.Controls.Add(lblStatus, 1, 0);
                root.Controls.Add(headerLayout, 0, 0);

                var bodyLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                GroupBox grpDi = MakeIoGroup("DIGITAL INPUT");
                diGrid.Dock = DockStyle.Fill;
                grpDi.Controls.Add(diGrid);

                GroupBox grpDo = MakeIoGroup("DIGITAL OUTPUT");
                doGrid.Dock = DockStyle.Fill;
                grpDo.Controls.Add(doGrid);

                bodyLayout.Controls.Add(grpDi, 0, 0);
                bodyLayout.Controls.Add(grpDo, 1, 0);
                root.Controls.Add(bodyLayout, 0, 1);
                root.SetRowSpan(bodyLayout, 2);

                GroupBox grpAction = MakeIoGroup("ACTION");
                var actionRow = new TableLayoutPanel
                {
                    BackColor = Color.White,
                    Dock = DockStyle.Fill,
                    ColumnCount = 14,
                    RowCount = 1,
                    Margin = Padding.Empty,
                    Padding = new Padding(1)
                };
                for (int i = 0; i < 14; i++)
                    actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 14F));
                actionRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Control[] btns = { btnRefresh, btnDoOn, btnDoOff, btnPulse };
                for (int i = 0; i < btns.Length; i++)
                {
                    btns[i].Dock = DockStyle.Fill;
                    actionRow.Controls.Add(btns[i], i, 0);
                }
                grpAction.Controls.Add(actionRow);
                root.Controls.Add(grpAction, 0, 3);

                Controls.Add(root);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        /// <summary>Motion 페이지 ACTION 버튼과 같은 크기 계산 규칙을 사용한다.</summary>
        private void TightenActionButtons()
        {
            foreach (Control b in new Control[] { btnRefresh, btnDoOn, btnDoOff, btnPulse })
            {
                b.Dock = DockStyle.Fill;
                b.Margin = new Padding(2);
                b.Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
                b.ForeColor = System.Drawing.Color.White;
                b.BackColor = System.Drawing.Color.FromArgb(128, 128, 128);
                b.MinimumSize = new Size(72, 28);
            }
        }

        private static GroupBox MakeIoGroup(string text)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.Black,
                Font = UiTheme.SectionFont,
                Padding = new Padding(1, 9, 1, 1),
                Margin = new Padding(0),
                TabStop = false
            };
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = "DIGITAL LINK";
            lblStatus.Text = "Live hardware I/O. DO commands are written directly to the mapped Ajin output.";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
            lblStatus.BackColor = UiTheme.StatusBarBg;
            lblStatus.ForeColor = Color.White;
            lblStatus.Font = UiTheme.SectionFont;
            lblDiTitle.Font = UiTheme.SectionFont;
            lblDoTitle.Font = UiTheme.SectionFont;
            actionsPanel.BackColor = UiTheme.OptionPanelBg;
        }

        private void WireEvents()
        {
            btnRefresh.Click += (s, e) =>
            {
                _loaded = false;
                EnsureLoaded();
                RefreshRows();
            };
            btnDoOn.Click += (s, e) => WriteSelected(true);
            btnDoOff.Click += (s, e) => WriteSelected(false);
            btnPulse.Click += async (s, e) => await PulseSelectedAsync(200);
            doGrid.CellClick += DoGrid_CellClick;
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            diGrid.Rows.Clear();
            doGrid.Rows.Clear();

            var host = FindForm() as Form1;
            if (host == null || host.Machine == null)
            {
                lblStatus.Text = "Machine is not ready.";
                return;
            }

            var collector = new IoCollector();
            collector.Scan(host.Machine);

            foreach (var item in collector.Inputs)
            {
                var row = diGrid.Rows[diGrid.Rows.Add(item.No, item.Address, item.Name, item.Module, item.Bit, "")];
                row.Tag = item.Port;
            }
            foreach (var item in collector.Outputs)
            {
                var row = doGrid.Rows[doGrid.Rows.Add(item.No, item.Address, item.Name, item.Module, item.Bit, "")];
                row.Tag = item;
            }

            lblStatus.Text = "( Loaded DI " + collector.Inputs.Count + " / DO " + collector.Outputs.Count
                + (!AjinFactory.IsRealBoardReady ? " · SIM" : "") + " )";
            RefreshRows();
        }

        private void RefreshRows()
        {
            foreach (DataGridViewRow row in diGrid.Rows)
            {
                var port = row.Tag as BaseDigitalInput;
                if (port == null) continue;
                try { port.UpdateStatus(); } catch { }
                SetState(row, port.IsOn);
            }
            foreach (DataGridViewRow row in doGrid.Rows)
            {
                var item = row.Tag as OutputTarget;
                if (item == null) continue;
                try { item.UpdateStatus(); } catch { }
                SetState(row, item.IsOn);
            }
        }

        private static void SetState(DataGridViewRow row, bool on)
        {
            var stateCell = row.Cells[row.Cells.Count - 1];
            stateCell.Value = on ? "ON" : "OFF";
            row.DefaultCellStyle.BackColor = on ? Color.FromArgb(230, 255, 230) : Color.White;
        }

        private OutputTarget SelectedOutput()
        {
            if (doGrid.CurrentRow == null) return null;
            return doGrid.CurrentRow.Tag as OutputTarget;
        }

        private void WriteSelected(bool on)
        {
            var output = SelectedOutput();
            if (output == null) return;
            WriteOutput(output, on);
        }

        private void WriteOutput(OutputTarget output, bool on)
        {
            if (output == null) return;
            string error;
            if (!output.Write(on, out error))
            {
                ShowIoError(error);
                return;
            }
            EventLogger.Write(EventKind.Event, "QMC", "IO-DO", output.Name + "=" + (on ? "ON" : "OFF"));
            RefreshRows();
        }

        private void DoGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex != doGrid.Columns.Count - 1) return;

            var row = doGrid.Rows[e.RowIndex];
            var output = row.Tag as OutputTarget;
            if (output == null) return;

            bool next = !output.IsOn;
            string message = output.Name + " output을 " + (next ? "ON" : "OFF") + " 하시겠습니까?";
            var result = QMC.Common.MessageDialog.Show(this, message, "I/O Control",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            WriteOutput(output, next);
        }

        private async Task PulseSelectedAsync(int ms)
        {
            var output = SelectedOutput();
            if (output == null) return;
            string error;
            if (!output.Write(true, out error))
            {
                ShowIoError(error);
                return;
            }
            EventLogger.Write(EventKind.Event, "QMC", "IO-DO", output.Name + "=PULSE " + ms + "ms");
            await Task.Delay(ms).ContinueWith(_ => { });
            if (!output.Write(false, out error))
            {
                ShowIoError(error);
                return;
            }
            RefreshRows();
        }

        private void ShowIoError(string message)
        {
            if (string.IsNullOrEmpty(message)) message = "I/O write failed.";
            lblStatus.Text = message;
            QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "IO-DO", "QMC", message);
            QMC.Common.MessageDialog.Show(this, message, "I/O Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal class IoItem<T>
        {
            public int No;
            public string Address;
            public string Name;
            public int Module;
            public int Bit;
            public T Port;
        }

        internal class OutputTarget
        {
            public int No;
            public string Address;
            public string Name;
            public int Module;
            public int Bit;
            public bool Nc;
            public BaseDigitalOutput Port;

            public bool IsOn
            {
                get { return Port != null && Port.IsOn; }
            }

            public bool Write(bool on, out string error)
            {
                EnsureRealPort();
                error = ValidateWriteTarget();
                if (error != null) return false;
                if (Port == null)
                {
                    error = Name + " output port is not ready.";
                    return false;
                }

                Port.Write(on);
                return true;
            }

            public void UpdateStatus()
            {
                EnsureRealPort();
                if (Port != null) Port.UpdateStatus();
            }

            private void EnsureRealPort()
            {
                if (Port is AjinDigitalOutput) return;
                if (!AjinFactory.IsRealBoardReady) return;
                Port = new AjinDigitalOutput(Name, Module, Bit, Nc);
            }

            private string ValidateWriteTarget()
            {
                if (!AjinFactory.IsRealBoardReady)
                    return null;

                int moduleCount;
                int ret = AXD.GetModuleCount(out moduleCount);
                if (ret != 0)
                    return Name + " DO module count check failed. AlarmCode=" + ret;

                if (Module < 0 || Module >= moduleCount)
                    return Name + " DO module is invalid. Module=" + Module + ", ModuleCount=" + moduleCount + ".";

                int outputCount;
                ret = AXD.GetOutputCount(Module, out outputCount);
                if (ret != 0)
                    return Name + " DO output count check failed. Module=" + Module + ", AlarmCode=" + ret;

                if (Bit < 0 || Bit >= outputCount)
                    return Name + " DO bit offset is invalid. Module=" + Module + ", Bit=" + Bit +
                           ", OutputCount=" + outputCount +
                           ". Use the bit offset inside the AXL DIO module, not the global Y number. Check the module number with AxdInfoGetOutputCount/board scan.";

                return null;
            }
        }

        internal class CylinderTarget
        {
            public string Name;
            public BaseCylinder Cylinder;
        }

        internal class IoCollector
        {
            public readonly List<IoItem<BaseDigitalInput>> Inputs = new List<IoItem<BaseDigitalInput>>();
            public readonly List<OutputTarget> Outputs = new List<OutputTarget>();
            public readonly List<CylinderTarget> Cylinders = new List<CylinderTarget>();

            private readonly HashSet<object> _visited = new HashSet<object>();
            private readonly HashSet<BaseDigitalInput> _seenInputs = new HashSet<BaseDigitalInput>();
            private readonly HashSet<BaseDigitalOutput> _seenOutputs = new HashSet<BaseDigitalOutput>();
            private readonly HashSet<BaseCylinder> _seenCylinders = new HashSet<BaseCylinder>();
            private readonly HashSet<string> _seenInputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _seenInputAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _seenOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _seenOutputAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void Scan(object root)
            {
                Visit(root, "", 0);
                AddConfiguredInputs();
                AddConfiguredOutputs();
                Inputs.Sort(CompareInput);
                Outputs.Sort(CompareOutput);
            }

            private void Visit(object obj, string path, int depth)
            {
                if (obj == null || depth > 8) return;

                var input = obj as BaseDigitalInput;
                if (input != null)
                {
                    AddInput(input, path);
                    return;
                }

                var output = obj as BaseDigitalOutput;
                if (output != null)
                {
                    AddOutput(output, path);
                    return;
                }

                var cylinder = obj as BaseCylinder;
                if (cylinder != null)
                {
                    AddCylinder(cylinder);
                    Visit(cylinder.OutFwd, Join(path, "OutFwd"), depth + 1);
                    Visit(cylinder.OutBwd, Join(path, "OutBwd"), depth + 1);
                    Visit(cylinder.InFwd, Join(path, "InFwd"), depth + 1);
                    Visit(cylinder.InBwd, Join(path, "InBwd"), depth + 1);
                    return;
                }

                var type = obj.GetType();
                if (IsLeaf(type)) return;
                if (!_visited.Add(obj)) return;

                var enumerable = obj as IEnumerable;
                if (enumerable != null && !(obj is string))
                {
                    int i = 0;
                    foreach (var child in enumerable)
                    {
                        Visit(child, Join(path, "[" + i + "]"), depth + 1);
                        i++;
                    }
                    return;
                }

                if (!ShouldInspect(type)) return;
                foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                    object value = null;
                    try { value = prop.GetValue(obj, null); } catch { }
                    Visit(value, Join(path, prop.Name), depth + 1);
                }
            }

            private void AddCylinder(BaseCylinder cylinder)
            {
                if (cylinder == null || !_seenCylinders.Add(cylinder)) return;
                CylinderDefault catalog = AjinIoCatalog.FindCylinder(cylinder.Name);
                if (catalog == null) return;

                Cylinders.Add(new CylinderTarget
                {
                    Name = catalog.Name,
                    Cylinder = cylinder
                });
            }

            private void AddInput(BaseDigitalInput port, string path)
            {
                if (!_seenInputs.Add(port)) return;
                DioDefault catalog = AjinIoCatalog.FindInput(port.Name);
                if (catalog == null && port is AjinDigitalInput)
                    catalog = AjinIoCatalog.FindInput(port.Setup.ModuleNo, port.Setup.BitNo);
                if (catalog == null) return;

                DioMap map = null;
                if (AjinConfigStore.Current != null &&
                    AjinConfigStore.Current.DigitalInputs != null)
                {
                    AjinConfigStore.Current.DigitalInputs.TryGetValue(catalog.Name, out map);
                }

                int module = map != null ? map.Module : port.Setup.ModuleNo;
                int bit = map != null ? map.Bit : port.Setup.BitNo;
                if (IsConfiguredInput(port, map) && !_seenInputAddresses.Add(AddressKey(module, bit))) return;

                Inputs.Add(new IoItem<BaseDigitalInput>
                {
                    No = catalog.No,
                    Address = catalog.Address,
                    Name = catalog.Name,
                    Module = module,
                    Bit = bit,
                    Port = port
                });
                _seenInputNames.Add(catalog.Name);
            }

            private void AddOutput(BaseDigitalOutput port, string path)
            {
                if (!_seenOutputs.Add(port)) return;
                DioDefault catalog = AjinIoCatalog.FindOutput(port.Name);
                if (catalog == null && port is AjinDigitalOutput)
                    catalog = AjinIoCatalog.FindOutput(port.Setup.ModuleNo, port.Setup.BitNo);
                if (catalog == null) return;

                DioMap map = null;
                if (AjinConfigStore.Current != null &&
                    AjinConfigStore.Current.DigitalOutputs != null)
                {
                    AjinConfigStore.Current.DigitalOutputs.TryGetValue(catalog.Name, out map);
                }

                int module = map != null ? map.Module : port.Setup.ModuleNo;
                int bit = map != null ? map.Bit : port.Setup.BitNo;
                if (IsConfiguredOutput(port, map) && !_seenOutputAddresses.Add(AddressKey(module, bit))) return;

                Outputs.Add(new OutputTarget
                {
                    No = catalog.No,
                    Address = catalog.Address,
                    Name = catalog.Name,
                    Module = module,
                    Bit = bit,
                    Nc = map != null ? map.Nc : port.Setup.IsNormallyClosed,
                    Port = port
                });
                _seenOutputNames.Add(catalog.Name);
            }

            private void AddConfiguredInputs()
            {
                var cfg = AjinConfigStore.Current;

                foreach (var catalog in AjinIoCatalog.DigitalInputs)
                {
                    DioMap map;
                    if (cfg == null || cfg.DigitalInputs == null ||
                        !cfg.DigitalInputs.TryGetValue(catalog.Name, out map) || map == null)
                        map = new DioMap { No = catalog.No, Address = catalog.Address, Module = catalog.Module, Bit = catalog.Bit, Nc = catalog.Nc };

                    if (_seenInputNames.Contains(catalog.Name)) continue;
                    if (!_seenInputAddresses.Add(AddressKey(map.Module, map.Bit))) continue;
                    Inputs.Add(new IoItem<BaseDigitalInput>
                    {
                        No = catalog.No,
                        Address = catalog.Address,
                        Name = catalog.Name,
                        Module = map.Module,
                        Bit = map.Bit,
                        Port = AjinFactory.CreateDigitalInput(catalog)
                    });
                    _seenInputNames.Add(catalog.Name);
                }
            }

            private void AddConfiguredOutputs()
            {
                var cfg = AjinConfigStore.Current;

                foreach (var catalog in AjinIoCatalog.DigitalOutputs)
                {
                    DioMap map;
                    if (cfg == null || cfg.DigitalOutputs == null ||
                        !cfg.DigitalOutputs.TryGetValue(catalog.Name, out map) || map == null)
                        map = new DioMap { No = catalog.No, Address = catalog.Address, Module = catalog.Module, Bit = catalog.Bit, Nc = catalog.Nc };

                    if (_seenOutputNames.Contains(catalog.Name)) continue;
                    if (!_seenOutputAddresses.Add(AddressKey(map.Module, map.Bit))) continue;
                    Outputs.Add(new OutputTarget
                    {
                        No = catalog.No,
                        Address = catalog.Address,
                        Name = catalog.Name,
                        Module = map.Module,
                        Bit = map.Bit,
                        Nc = map.Nc,
                        Port = AjinFactory.CreateDigitalOutput(catalog)
                    });
                    _seenOutputNames.Add(catalog.Name);
                }
            }

            private static int CompareInput(IoItem<BaseDigitalInput> a, IoItem<BaseDigitalInput> b)
            {
                return CompareIo(a.No, a.Address, a.Name, b.No, b.Address, b.Name);
            }

            private static int CompareOutput(OutputTarget a, OutputTarget b)
            {
                return CompareIo(a.No, a.Address, a.Name, b.No, b.Address, b.Name);
            }

            private static int CompareIo(int noA, string addressA, string nameA, int noB, string addressB, string nameB)
            {
                if (noA > 0 && noB > 0)
                    return noA.CompareTo(noB);
                if (noA > 0) return -1;
                if (noB > 0) return 1;

                int addressCompare = string.Compare(addressA, addressB, StringComparison.OrdinalIgnoreCase);
                if (addressCompare != 0) return addressCompare;
                return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsConfiguredOutput(BaseDigitalOutput port, DioMap map)
            {
                return map != null || port is AjinDigitalOutput;
            }

            private static bool IsConfiguredInput(BaseDigitalInput port, DioMap map)
            {
                return map != null || port is AjinDigitalInput;
            }

            private static string AddressKey(int module, int bit)
            {
                return module.ToString() + ":" + bit.ToString();
            }

            private static string Join(string prefix, string name)
            {
                if (string.IsNullOrEmpty(prefix)) return name;
                if (name.StartsWith("[")) return prefix + name;
                return prefix + "." + name;
            }

            private static bool ShouldInspect(Type type)
            {
                return type.Namespace != null &&
                       (type.Namespace.StartsWith("QMC.CDT320") ||
                        type.Namespace.StartsWith("QMC.Common"));
            }

            private static bool IsLeaf(Type type)
            {
                return type.IsPrimitive ||
                       type.IsEnum ||
                       type == typeof(string) ||
                       type == typeof(decimal) ||
                       type == typeof(DateTime);
            }
        }
    }
}


