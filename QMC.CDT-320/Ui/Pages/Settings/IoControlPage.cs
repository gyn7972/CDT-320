using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Logging;
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
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshRows();
            HandleCreated += (s, e) =>
            {
                EnsureLoaded();
                _timer.Start();
            };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void ApplyRuntimeUi()
        {
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
                var row = diGrid.Rows[diGrid.Rows.Add(item.Name, item.Module, item.Bit, "")];
                row.Tag = item.Port;
            }
            foreach (var item in collector.Outputs)
            {
                var row = doGrid.Rows[doGrid.Rows.Add(item.Name, item.Module, item.Bit, "")];
                row.Tag = item;
            }

            lblStatus.Text = "Loaded DI " + collector.Inputs.Count + " / DO " + collector.Outputs.Count + ".";
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
            string error;
            if (!output.Write(on, out error))
            {
                ShowIoError(error);
                return;
            }
            EventLogger.Write(EventKind.Event, "QMC", "IO-DO", output.Name + "=" + (on ? "ON" : "OFF"));
            RefreshRows();
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
            EventLogger.Write(EventKind.Alarm, "QMC", "IO-DO", message);
            MessageBox.Show(this, message, "I/O Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private class IoItem<T>
        {
            public string Name;
            public int Module;
            public int Bit;
            public T Port;
        }

        private class OutputTarget
        {
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
                error = ValidateWriteTarget();
                if (error != null) return false;
                EnsureRealPort();
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
                if (!AjinSystem.IsOpen) return;
                Port = new AjinDigitalOutput(Name, Module, Bit, Nc);
            }

            private string ValidateWriteTarget()
            {
                if (!AjinSystem.IsOpen)
                    return "Ajin board is not open. Cannot write DO to the real board.";

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
                           ". Use the bit offset inside the module, not the global Y number. Example: Y046 on 32-point modules is Module=1, Bit=14.";

                return null;
            }
        }

        private class IoCollector
        {
            public readonly List<IoItem<BaseDigitalInput>> Inputs = new List<IoItem<BaseDigitalInput>>();
            public readonly List<OutputTarget> Outputs = new List<OutputTarget>();

            private readonly HashSet<object> _visited = new HashSet<object>();
            private readonly HashSet<BaseDigitalInput> _seenInputs = new HashSet<BaseDigitalInput>();
            private readonly HashSet<BaseDigitalOutput> _seenOutputs = new HashSet<BaseDigitalOutput>();
            private readonly HashSet<string> _seenOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void Scan(object root)
            {
                Visit(root, "", 0);
                AddConfiguredOutputs();
                Inputs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                Outputs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
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

            private void AddInput(BaseDigitalInput port, string path)
            {
                if (!_seenInputs.Add(port)) return;
                Inputs.Add(new IoItem<BaseDigitalInput>
                {
                    Name = DisplayName(path, port.Name),
                    Module = port.Setup.ModuleNo,
                    Bit = port.Setup.BitNo,
                    Port = port
                });
            }

            private void AddOutput(BaseDigitalOutput port, string path)
            {
                if (!_seenOutputs.Add(port)) return;
                var name = DisplayName(path, port.Name);
                DioMap map = null;
                if (AjinConfigStore.Current != null &&
                    AjinConfigStore.Current.DigitalOutputs != null)
                {
                    AjinConfigStore.Current.DigitalOutputs.TryGetValue(port.Name, out map);
                }

                Outputs.Add(new OutputTarget
                {
                    Name = name,
                    Module = map != null ? map.Module : port.Setup.ModuleNo,
                    Bit = map != null ? map.Bit : port.Setup.BitNo,
                    Nc = map != null ? map.Nc : port.Setup.IsNormallyClosed,
                    Port = port
                });
                _seenOutputNames.Add(port.Name);
            }

            private void AddConfiguredOutputs()
            {
                var cfg = AjinConfigStore.Current;
                if (cfg == null || cfg.DigitalOutputs == null) return;

                foreach (var kv in cfg.DigitalOutputs)
                {
                    if (kv.Value == null) continue;
                    if (_seenOutputNames.Contains(kv.Key)) continue;
                    Outputs.Add(new OutputTarget
                    {
                        Name = kv.Key,
                        Module = kv.Value.Module,
                        Bit = kv.Value.Bit,
                        Nc = kv.Value.Nc,
                        Port = AjinSystem.IsOpen
                            ? (BaseDigitalOutput)new AjinDigitalOutput(kv.Key, kv.Value.Module, kv.Value.Bit, kv.Value.Nc)
                            : null
                    });
                    _seenOutputNames.Add(kv.Key);
                }
            }

            private static string DisplayName(string path, string name)
            {
                if (string.IsNullOrEmpty(path)) return name;
                return path + " (" + name + ")";
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
