using QMC.Common.Motion;
using System;
using System.Threading.Tasks;

namespace QMC.CDT_320.Ui.Controls
{
    public enum JogAxisControlKind
    {
        Auto,
        Vertical,
        Horizontal,
        Cross,
        CrossWithT
    }

    public sealed class JogAxisItem
    {
        public string AxisName { get; set; }
        public string PlusText { get; set; }
        public string MinusText { get; set; }
        public string Unit { get; set; }
        public double DisplayScale { get; set; }
        public BaseAxis Axis { get; set; }
        public Func<double> ActualPositionGetter { get; set; }
        public Func<JogAxisItem, int, JogSpeedType, double, double, Task<int>> StepMoveAsync { get; set; }
        public Func<JogAxisItem, int, JogSpeedType, double, Task<int>> ContinuousMoveAsync { get; set; }
        public Func<JogAxisItem, Task<int>> StopAsync { get; set; }
        public JogAxisControlKind ControlKind { get; set; }

        public string DisplayUnit
        {
            get
            {
                try
                {
                    if (Axis != null)
                        return AxisUnitConverter.DisplayUnitFor(Axis);
                    return string.IsNullOrWhiteSpace(Unit) ? AxisUnitConverter.Millimeter : AxisUnitConverter.Normalize(Unit);
                }
                catch
                {
                    return AxisUnitConverter.Millimeter;
                }
                finally
                {
                }
            }
        }

        public JogAxisItem()
        {
            try
            {
                AxisName = "AXIS";
                PlusText = "+";
                MinusText = "-";
                Unit = string.Empty;
                DisplayScale = 1.0;
                ControlKind = JogAxisControlKind.Auto;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static JogAxisItem Single(string axisName, BaseAxis axis, string unit, double displayScale, string plusText, string minusText)
        {
            try
            {
                return new JogAxisItem
                {
                    AxisName = axisName,
                    Axis = axis,
                    Unit = unit,
                    DisplayScale = displayScale <= 0 ? 1.0 : displayScale,
                    PlusText = plusText,
                    MinusText = minusText,
                    ControlKind = JogAxisControlKind.Auto
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public JogAxisItem WithControlKind(JogAxisControlKind controlKind)
        {
            try
            {
                ControlKind = controlKind;
                return this;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public double GetActualPosition()
        {
            try
            {
                if (ActualPositionGetter != null)
                    return ActualPositionGetter();

                return Axis != null ? Axis.ActualPosition : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        public double GetDisplayPosition()
        {
            try
            {
                if (Axis != null)
                    return AxisUnitConverter.ToDisplay(GetActualPosition(), Axis);

                double scale = DisplayScale <= 0 ? 1.0 : DisplayScale;
                return GetActualPosition() * scale;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        public double FromDisplayDistance(double displayDistance)
        {
            try
            {
                if (Axis != null)
                    return AxisUnitConverter.FromDisplay(displayDistance, Axis);

                double scale = DisplayScale <= 0 ? 1.0 : DisplayScale;
                return displayDistance / scale;
            }
            catch
            {
                return displayDistance;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteStepAsync(int direction, JogSpeedType speedType, double customSpeed, double axisStepDistance)
        {
            try
            {
                if (StepMoveAsync != null)
                    return await StepMoveAsync(this, direction, speedType, customSpeed, axisStepDistance);

                if (Axis == null)
                    return -1;

                return await Axis.MoveJogStepAsync(direction, speedType, axisStepDistance, customSpeed);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteContinuousAsync(int direction, JogSpeedType speedType, double customSpeed)
        {
            try
            {
                if (ContinuousMoveAsync != null)
                    return await ContinuousMoveAsync(this, direction, speedType, customSpeed);

                if (Axis == null)
                    return -1;

                Axis.MoveJogContinuous(direction, speedType, customSpeed);
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteStopAsync()
        {
            try
            {
                if (StopAsync != null)
                    return await StopAsync(this);

                if (Axis == null)
                    return -1;

                Axis.StopJog();
                await Task.CompletedTask;
                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
