using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class AxisJogPopup : Form
    {
        private readonly List<BaseAxis> _axes;
        private readonly CDT320_Machine _machine;
        private readonly List<IUnitJogController> _unitJogControllers;
        private readonly Timer _posTimer = new Timer();
        private JogAxisItem _selectedItem;

        public AxisJogPopup(IEnumerable<BaseAxis> axes)
            : this(axes, null)
        {
        }

        public AxisJogPopup(IEnumerable<BaseAxis> axes, CDT320_Machine machine)
        {
            try
            {
                _axes = SortAxes(axes).ToList();
                _machine = machine;
                _unitJogControllers = EnumerateUnitJogControllers(machine).ToList();

                InitializeComponent();
                BindAxes();
                WireEvents();
                BindSelectedAxis();

                _posTimer.Interval = 200;
                _posTimer.Tick += (s, e) => RefreshPosition();
                Load += (s, e) => _posTimer.Start();
                FormClosed += async (s, e) => await StopJogAsync(true);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Axis jog popup initialize failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private BaseAxis SelectedAxis
        {
            get
            {
                try
                {
                    AxisListItem item = selectAxisList.SelectedItem as AxisListItem;
                    return item != null ? item.Axis : null;
                }
                catch
                {
                    return null;
                }
                finally
                {
                }
            }
        }

        private static IEnumerable<BaseAxis> SortAxes(IEnumerable<BaseAxis> axes)
        {
            try
            {
                return (axes ?? Enumerable.Empty<BaseAxis>())
                    .Where(a => a != null)
                    .OrderBy(a => a.Setup != null && a.Setup.AxisNo >= 0 ? a.Setup.AxisNo : int.MaxValue)
                    .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return Enumerable.Empty<BaseAxis>();
            }
            finally
            {
            }
        }

        private void BindAxes()
        {
            try
            {
                selectAxisList.Items.Clear();
                foreach (BaseAxis axis in _axes)
                    selectAxisList.Items.Add(new AxisListItem(axis));

                if (selectAxisList.Items.Count > 0)
                    selectAxisList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Axis list bind failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "JOG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            try
            {
                selectAxisList.SelectedIndexChanged += async (s, e) =>
                {
                    await StopJogAsync(true);
                    BindSelectedAxis();
                };
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Axis jog event bind failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void BindSelectedAxis()
        {
            try
            {
                BaseAxis axis = SelectedAxis;
                if (axis == null)
                {
                    _selectedItem = null;
                    jogPositionListControl.SetItems(null);
                    jogAxisMoveControl.SetItems(null);
                    return;
                }

                _selectedItem = CreateJogAxisItem(axis);
                jogPositionListControl.SetItems(new[] { _selectedItem });
                jogAxisMoveControl.SetItems(new[] { _selectedItem });
                RefreshPosition();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Selected axis bind failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "JOG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private JogAxisItem CreateJogAxisItem(BaseAxis axis)
        {
            try
            {
                string plusText;
                string minusText;
                ResolveJogButtonText(axis, out plusText, out minusText);

                JogAxisItem item = JogAxisItem.Single(DisplayNameWithNo(axis), axis, AxisUnitConverter.DisplayUnitFor(axis), 1.0, plusText, minusText);
                item.StepMoveAsync = ExecuteStepAsync;
                item.ContinuousMoveAsync = ExecuteContinuousAsync;
                item.StopAsync = StopItemAsync;
                return item;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteStepAsync(JogAxisItem item, int direction, JogSpeedType speedType, double customSpeed, double axisStepDistance)
        {
            try
            {
                if (item == null || item.Axis == null)
                    return -1;

                EnsureServoOn(item.Axis);
                item.Axis.UpdateStatus();
                if (item.Axis.IsMoving)
                    return -2;

                int? unitStepResult = await ExecuteUnitStepAsync(item, direction, speedType, customSpeed, axisStepDistance);
                if (unitStepResult.HasValue)
                    return unitStepResult.Value;

                int result = await item.Axis.MoveJogStepAsync(direction, speedType, axisStepDistance, customSpeed);
                EventLogger.Write(EventKind.Event, "UI", "JOG-POPUP", item.AxisName + " step jog result=" + result);
                return result;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Step jog failed: " + ex.Message);
                throw;
            }
            finally
            {
                RefreshPosition();
            }
        }

        private async Task<int> ExecuteContinuousAsync(JogAxisItem item, int direction, JogSpeedType speedType, double customSpeed)
        {
            try
            {
                if (item == null || item.Axis == null)
                    return -1;

                EnsureServoOn(item.Axis);
                int? unitContinuousResult = await ExecuteUnitContinuousAsync(item, direction, speedType, customSpeed);
                if (unitContinuousResult.HasValue)
                    return unitContinuousResult.Value;

                item.Axis.MoveJogContinuous(direction, speedType, customSpeed);
                EventLogger.Write(EventKind.Event, "UI", "JOG-POPUP", item.AxisName + " continuous jog start.");
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Continuous jog failed: " + ex.Message);
                throw;
            }
            finally
            {
                RefreshPosition();
            }
        }

        private async Task<int> StopItemAsync(JogAxisItem item)
        {
            try
            {
                if (item == null || item.Axis == null)
                    return -1;

                int? unitStopResult = await ExecuteUnitStopAsync(item);
                if (unitStopResult.HasValue)
                    return unitStopResult.Value;

                item.Axis.StopJog();
                EventLogger.Write(EventKind.Event, "UI", "JOG-POPUP", item.AxisName + " jog stop.");
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POPUP", "Jog stop failed: " + ex.Message);
                throw;
            }
            finally
            {
                RefreshPosition();
            }
        }

        private async Task<int> StopJogAsync(bool force)
        {
            try
            {
                return await jogAxisMoveControl.StopAllAsync(force);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POPUP", "Stop all jog failed: " + ex.Message);
                return -1;
            }
            finally
            {
                RefreshPosition();
            }
        }

        private async Task<int?> ExecuteUnitStepAsync(
            JogAxisItem item,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            IUnitJogController controller = FindUnitJogController(item);
            if (controller == null)
                return null;

            return await controller.JogStepAsync(item.Axis, direction, speedType, customSpeed, axisStepDistance);
        }

        private async Task<int?> ExecuteUnitContinuousAsync(
            JogAxisItem item,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            IUnitJogController controller = FindUnitJogController(item);
            if (controller == null)
                return null;

            return await controller.JogContinuousAsync(item.Axis, direction, speedType, customSpeed);
        }

        private async Task<int?> ExecuteUnitStopAsync(JogAxisItem item)
        {
            IUnitJogController controller = FindUnitJogController(item);
            if (controller == null)
                return null;

            return await controller.StopJogAsync(item.Axis);
        }

        private IUnitJogController FindUnitJogController(JogAxisItem item)
        {
            if (item == null || item.Axis == null || _unitJogControllers == null)
                return null;

            foreach (IUnitJogController controller in _unitJogControllers)
            {
                if (controller != null && controller.CanHandleJogAxis(item.Axis))
                    return controller;
            }

            return null;
        }

        private static IEnumerable<IUnitJogController> EnumerateUnitJogControllers(CDT320_Machine machine)
        {
            if (machine == null)
                yield break;

            HashSet<object> visited = new HashSet<object>();

            if (machine.Units != null)
            {
                foreach (object unit in machine.Units)
                {
                    foreach (IUnitJogController controller in EnumerateUnitJogControllers(unit, visited))
                        yield return controller;
                }
            }

            foreach (PropertyInfo property in machine.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try
                {
                    value = property.GetValue(machine, null);
                }
                catch
                {
                    continue;
                }

                foreach (IUnitJogController controller in EnumerateUnitJogControllers(value, visited))
                    yield return controller;
            }
        }

        private static IEnumerable<IUnitJogController> EnumerateUnitJogControllers(object node, HashSet<object> visited)
        {
            if (node == null || visited == null || visited.Contains(node))
                yield break;

            visited.Add(node);

            IUnitJogController controller = node as IUnitJogController;
            if (controller != null)
                yield return controller;

            PropertyInfo componentsProperty = node.GetType().GetProperty("Components", BindingFlags.Instance | BindingFlags.Public);
            if (componentsProperty == null)
                yield break;

            IEnumerable components = null;
            try
            {
                components = componentsProperty.GetValue(node, null) as IEnumerable;
            }
            catch
            {
                components = null;
            }

            if (components == null)
                yield break;

            foreach (object component in components)
            {
                foreach (IUnitJogController child in EnumerateUnitJogControllers(component, visited))
                    yield return child;
            }
        }

        private static void EnsureServoOn(BaseAxis axis)
        {
            try
            {
                if (axis != null && !axis.IsServoOn)
                    axis.ServoOn();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RefreshPosition()
        {
            try
            {
                if (_selectedItem != null && _selectedItem.Axis != null)
                    _selectedItem.Axis.UpdateStatus();

                jogPositionListControl.RefreshState();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POPUP", "Jog position refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static void ResolveJogButtonText(BaseAxis axis, out string plusText, out string minusText)
        {
            try
            {
                string text = AxisText(axis);
                if (text.IndexOf("INDEX", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    plusText = "Next";
                    minusText = "Prev";
                    return;
                }

                if (HasAxisLetter(text, "X"))
                {
                    plusText = "X+";
                    minusText = "X-";
                    return;
                }

                if (HasAxisLetter(text, "Y"))
                {
                    plusText = "Y+";
                    minusText = "Y-";
                    return;
                }

                if (HasAxisLetter(text, "Z"))
                {
                    plusText = "Z+";
                    minusText = "Z-";
                    return;
                }

                if (HasAxisLetter(text, "T"))
                {
                    plusText = "T+";
                    minusText = "T-";
                    return;
                }

                plusText = "+";
                minusText = "-";
            }
            catch
            {
                plusText = "+";
                minusText = "-";
            }
            finally
            {
            }
        }

        private static bool HasAxisLetter(string name, string letter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                string s = name.ToUpperInvariant();
                string l = letter.ToUpperInvariant();

                if (Regex.IsMatch(s, Regex.Escape(l) + @"[0-9]*$"))
                    return true;

                return Regex.IsMatch(s, @"(^|[^A-Z0-9])" + Regex.Escape(l) + @"[0-9]*([^A-Z0-9]|$)");
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static string AxisText(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return string.Empty;

                string display = axis.Setup != null ? axis.Setup.DisplayName : string.Empty;
                return (axis.Name + " " + display).Trim();
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private static string DisplayNameWithNo(BaseAxis axis)
        {
            try
            {
                int axisNo = axis != null && axis.Setup != null ? axis.Setup.AxisNo : -1;
                string display = axis != null && axis.Setup != null && !string.IsNullOrWhiteSpace(axis.Setup.DisplayName)
                    ? axis.Setup.DisplayName
                    : axis != null ? axis.Name : "AXIS";

                return (axisNo >= 0 ? axisNo.ToString("00", CultureInfo.InvariantCulture) + " - " : string.Empty) + display;
            }
            catch
            {
                return "AXIS";
            }
            finally
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _posTimer.Dispose();
                    if (components != null)
                        components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private sealed class AxisListItem
        {
            public AxisListItem(BaseAxis axis)
            {
                try
                {
                    Axis = axis;
                }
                finally
                {
                }
            }

            public BaseAxis Axis { get; private set; }

            public override string ToString()
            {
                try
                {
                    int axisNo = Axis != null && Axis.Setup != null ? Axis.Setup.AxisNo : -1;
                    string display = Axis != null && Axis.Setup != null && !string.IsNullOrWhiteSpace(Axis.Setup.DisplayName)
                        ? Axis.Setup.DisplayName
                        : Axis != null ? Axis.Name : string.Empty;

                    return axisNo.ToString("00", CultureInfo.InvariantCulture) + " - " + display;
                }
                catch
                {
                    return "--";
                }
                finally
                {
                }
            }
        }
    }
}
