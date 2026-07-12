using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Initialization
{
    public sealed class AxisInitializeInterlockService
    {
        private readonly CDT320_Machine _machine;
        private readonly Func<IEnumerable<BaseAxis>> _axesProvider;
        private readonly Func<bool> _autoStoppedProvider;
        private readonly Func<bool> _manualStoppedProvider;
        private readonly object _digitalInputGate = new object();
        private readonly Dictionary<string, BaseDigitalInput> _digitalInputs =
            new Dictionary<string, BaseDigitalInput>(StringComparer.OrdinalIgnoreCase);

        public AxisInitializeInterlockService(
            CDT320_Machine machine,
            Func<IEnumerable<BaseAxis>> axesProvider)
            : this(machine, axesProvider, null, null)
        {
        }

        public AxisInitializeInterlockService(
            CDT320_Machine machine,
            Func<IEnumerable<BaseAxis>> axesProvider,
            Func<bool> autoStoppedProvider,
            Func<bool> manualStoppedProvider)
        {
            _machine = machine;
            _axesProvider = axesProvider;
            _autoStoppedProvider = autoStoppedProvider;
            _manualStoppedProvider = manualStoppedProvider;
        }

        public bool VerifyStep(AxisInitializeStep step, out string reason)
        {
            reason = "";
            try
            {
                if (step == null || step.Interlocks == null || step.Interlocks.Count == 0)
                    return true;

                foreach (var rule in step.Interlocks)
                {
                    if (rule == null || !rule.Enabled)
                        continue;

                    if (!VerifyRule(rule, out reason))
                    {
                        string correctiveAction = string.IsNullOrWhiteSpace(rule.Description)
                            ? ""
                            : ", 조치사항=" + rule.Description.Trim();
                        string message = "초기화 인터락 실패. step=" + step.StepNo +
                            ", group=" + step.GroupName +
                            ", target=" + rule.TargetType + ":" + rule.Name +
                            ", expected=" + rule.ExpectedState +
                            ", reason=" + reason + correctiveAction;
                        Log.Write("Main", "SYSTEM", "AxisInitializeInterlock", message + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "INIT-INTERLOCK", "MachineController", message);
                        reason = message;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "초기화 인터락 확인 중 예외 발생: " + ex.Message;
                Log.Write("Main", "SYSTEM", "AxisInitializeInterlock",
                    "Initialize interlock verify failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-INTERLOCK-EX", "MachineController", reason);
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                string targetType = rule.TargetType ?? "";
                if (string.Equals(targetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase))
                    return VerifyAxisRule(rule, out reason);
                if (string.Equals(targetType, AxisInitializeInterlockTarget.Cylinder, StringComparison.OrdinalIgnoreCase))
                    return VerifyCylinderRule(rule, out reason);
                if (string.Equals(targetType, AxisInitializeInterlockTarget.DigitalInput, StringComparison.OrdinalIgnoreCase))
                    return VerifyDigitalInputRule(rule, out reason);
                if (string.Equals(targetType, AxisInitializeInterlockTarget.Resource, StringComparison.OrdinalIgnoreCase))
                    return VerifyResourceRule(rule, out reason);
                if (string.Equals(targetType, AxisInitializeInterlockTarget.Machine, StringComparison.OrdinalIgnoreCase))
                    return VerifyMachineRule(rule, out reason);
                if (string.Equals(targetType, AxisInitializeInterlockTarget.Material, StringComparison.OrdinalIgnoreCase))
                    return VerifyMaterialRule(rule, out reason);

                reason = "지원하지 않는 TargetType입니다. targetType=" + targetType;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyAxisRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                BaseAxis axis = FindAxis(rule.Name);
                if (axis == null)
                {
                    reason = "축을 찾을 수 없습니다. axis=" + rule.Name;
                    return false;
                }

                string state = rule.ExpectedState ?? "";
                if (string.Equals(state, AxisInitializeInterlockState.ServoOn, StringComparison.OrdinalIgnoreCase))
                    return Check(axis.IsServoOn, "축 Servo가 OFF입니다. axis=" + axis.Name, out reason);
                if (string.Equals(state, AxisInitializeInterlockState.HomeDone, StringComparison.OrdinalIgnoreCase))
                    return Check(axis.IsHomeDone, "축 HomeDone이 아닙니다. axis=" + axis.Name, out reason);
                if (string.Equals(state, AxisInitializeInterlockState.AlarmOff, StringComparison.OrdinalIgnoreCase))
                    return Check(!axis.IsAlarm, "축 Alarm 상태입니다. axis=" + axis.Name + ", code=" + axis.AlarmCode, out reason);
                if (string.Equals(state, AxisInitializeInterlockState.Stopped, StringComparison.OrdinalIgnoreCase))
                    return Check(!axis.IsMoving, "축이 정지 상태가 아닙니다. axis=" + axis.Name, out reason);
                bool requiresExactPosition = string.Equals(
                    state,
                    AxisInitializeInterlockState.AtPosition,
                    StringComparison.OrdinalIgnoreCase);
                bool requiresAtOrBelowPosition = string.Equals(
                    state,
                    AxisInitializeInterlockState.AtOrBelowPosition,
                    StringComparison.OrdinalIgnoreCase);
                if (requiresExactPosition || requiresAtOrBelowPosition)
                {
                    double expectedPosition = rule.ExpectedPosition;
                    string expectedName = string.IsNullOrWhiteSpace(rule.PositionName)
                        ? expectedPosition.ToString("0.###", CultureInfo.InvariantCulture)
                        : rule.PositionName.Trim();
                    if (!string.IsNullOrWhiteSpace(rule.PositionName) &&
                        !TryResolveAxisPosition(axis, rule.PositionName, out expectedPosition))
                    {
                        reason = "축 지정 위치를 찾을 수 없습니다. axis=" + axis.Name +
                            ", position=" + rule.PositionName;
                        return false;
                    }

                    double tolerance = rule.Tolerance > 0.0 ? rule.Tolerance : 0.01;
                    bool atPosition = axis.IsHomeDone &&
                        (requiresAtOrBelowPosition
                            ? axis.ActualPosition <= expectedPosition + tolerance
                            : Math.Abs(axis.ActualPosition - expectedPosition) <= tolerance);
                    return Check(atPosition,
                        "축이 HomeDone이 아니거나 지정 위치 조건을 만족하지 않습니다. axis=" + axis.Name +
                        ", actual=" + axis.ActualPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", expected=" + expectedPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", comparison=" + (requiresAtOrBelowPosition ? "AtOrBelow" : "Exact") +
                        ", position=" + expectedName +
                        ", tolerance=" + tolerance.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", homeDone=" + axis.IsHomeDone,
                        out reason);
                }
                if (string.Equals(state, AxisInitializeInterlockState.SharedRailHomeClear, StringComparison.OrdinalIgnoreCase))
                    return VerifySharedRailHomeClear(axis, out reason);

                reason = "지원하지 않는 Axis ExpectedState입니다. state=" + state;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyCylinderRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                BaseCylinder cylinder = FindNode<BaseCylinder>(rule.Name);
                if (cylinder == null)
                {
                    reason = "실린더를 찾을 수 없습니다. cylinder=" + rule.Name;
                    return false;
                }

                string state = rule.ExpectedState ?? "";
                if (string.Equals(state, AxisInitializeInterlockState.Fwd, StringComparison.OrdinalIgnoreCase))
                    return Check(cylinder.IsFwd, "실린더가 전진 상태가 아닙니다. cylinder=" + cylinder.Name, out reason);
                if (string.Equals(state, AxisInitializeInterlockState.Bwd, StringComparison.OrdinalIgnoreCase))
                    return Check(cylinder.IsBwd, "실린더가 후진 상태가 아닙니다. cylinder=" + cylinder.Name, out reason);

                reason = "지원하지 않는 Cylinder ExpectedState입니다. state=" + state;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyDigitalInputRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                string state = rule.ExpectedState ?? "";
                if (string.Equals(rule.Name, AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(state, AxisInitializeInterlockState.On, StringComparison.OrdinalIgnoreCase) &&
                    _machine != null && _machine.InputFeederUnit != null)
                {
                    return Check(_machine.InputFeederUnit.IsWaferFeederAvoidPositionCheck(),
                        "InputFeederY가 Avoid 위치가 아니거나 Wafer Feeder Avoid 센서가 ON 상태가 아닙니다.",
                        out reason);
                }

                if (string.Equals(rule.Name, AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(state, AxisInitializeInterlockState.On, StringComparison.OrdinalIgnoreCase) &&
                    _machine != null && _machine.OutputFeederUnit != null)
                {
                    return Check(_machine.OutputFeederUnit.IsBinFeederAvoidPositionCheck(),
                        "OutputFeederY가 Avoid 위치가 아니거나 Bin Feeder Avoid 센서가 ON 상태가 아닙니다.",
                        out reason);
                }

                BaseDigitalInput input = FindDigitalInput(rule.Name);
                if (input == null)
                {
                    reason = "DI를 찾을 수 없습니다. input=" + rule.Name;
                    return false;
                }

                if (string.Equals(state, AxisInitializeInterlockState.On, StringComparison.OrdinalIgnoreCase))
                    return Check(input.IsOn, "DI가 ON 상태가 아닙니다. input=" + input.Name, out reason);
                if (string.Equals(state, AxisInitializeInterlockState.Off, StringComparison.OrdinalIgnoreCase))
                    return Check(input.IsOff, "DI가 OFF 상태가 아닙니다. input=" + input.Name, out reason);

                reason = "지원하지 않는 DigitalInput ExpectedState입니다. state=" + state;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyResourceRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                string state = rule.ExpectedState ?? "";
                if (string.Equals(state, AxisInitializeInterlockState.AllOk, StringComparison.OrdinalIgnoreCase))
                    return Check(_machine != null && _machine.ResourcesUnit != null && _machine.ResourcesUnit.AllOk,
                        "Resource 상태가 정상(AllOk)이 아닙니다.", out reason);

                reason = "지원하지 않는 Resource ExpectedState입니다. state=" + state;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyMachineRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                string state = rule.ExpectedState ?? "";
                if (string.Equals(state, AxisInitializeInterlockState.AutoStopped, StringComparison.OrdinalIgnoreCase))
                {
                    return Check(_autoStoppedProvider != null && _autoStoppedProvider(),
                        "Auto 또는 Sequence가 정지 상태가 아닙니다.", out reason);
                }

                if (string.Equals(state, AxisInitializeInterlockState.ManualStopped, StringComparison.OrdinalIgnoreCase))
                {
                    return Check(_manualStoppedProvider != null && _manualStoppedProvider(),
                        "Manual 동작이 정지 상태가 아닙니다.", out reason);
                }

                if (string.Equals(state, AxisInitializeInterlockState.AllAxesStopped, StringComparison.OrdinalIgnoreCase))
                    return VerifyAllAxesStopped(out reason);

                reason = "지원하지 않는 Machine ExpectedState입니다. state=" + state;
                return false;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyMaterialRule(AxisInitializeInterlockRule rule, out string reason)
        {
            reason = "";
            try
            {
                string state = rule.ExpectedState ?? "";
                if (!string.Equals(state, AxisInitializeInterlockState.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "지원하지 않는 Material ExpectedState입니다. state=" + state;
                    return false;
                }

                MaterialLocationKind location;
                if (!Enum.TryParse(rule.Name ?? "", true, out location) || location == MaterialLocationKind.Unknown)
                {
                    reason = "지원하지 않는 Material 위치입니다. location=" + rule.Name;
                    return false;
                }

                if (location == MaterialLocationKind.InputFeeder)
                    return VerifyInputFeederEmpty(out reason);
                if (location == MaterialLocationKind.OutputFeeder)
                    return VerifyOutputFeederEmpty(out reason);

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(location);
                return Check(wafer == null,
                    "Material 데이터가 존재합니다. location=" + location +
                    ", waferId=" + (wafer != null ? wafer.WaferId : "") +
                    ", state=" + (wafer != null ? wafer.State.ToString() : ""),
                    out reason);
            }
            catch (Exception ex)
            {
                reason = "Material 상태 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyAllAxesStopped(out string reason)
        {
            reason = "";
            try
            {
                if (_axesProvider == null)
                {
                    reason = "축 목록 Provider가 없습니다.";
                    return false;
                }

                var movingAxes = new List<string>();
                int axisCount = 0;
                foreach (BaseAxis axis in _axesProvider())
                {
                    if (axis == null)
                        continue;

                    axisCount++;
                    if (axis.IsMoving)
                        movingAxes.Add(axis.Name);
                }

                if (axisCount == 0)
                {
                    reason = "확인할 축이 없습니다.";
                    return false;
                }

                return Check(movingAxes.Count == 0,
                    "이동 중인 축이 있습니다. axes=" + string.Join(",", movingAxes.ToArray()),
                    out reason);
            }
            catch (Exception ex)
            {
                reason = "전체 축 정지 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyInputFeederEmpty(out string reason)
        {
            reason = "";
            try
            {
                InputFeederUnit feeder = _machine != null ? _machine.InputFeederUnit : null;
                if (feeder == null)
                {
                    reason = "InputFeederUnit을 찾을 수 없습니다.";
                    return false;
                }

                bool dataEmpty = feeder.IsWaferFeederTransferDataEmpty();
                bool ringDetected = feeder.IsWaferFeederRingCheck();
                if (dataEmpty && !ringDetected)
                    return true;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                reason = "InputFeeder가 비어 있지 않거나 자재 데이터와 센서 상태가 일치하지 않습니다." +
                    " transferDataEmpty=" + dataEmpty +
                    ", currentWafer=" + (feeder.CurrentWaferMaterial != null ? feeder.CurrentWaferMaterial.WaferId : "-") +
                    ", materialState=" + feeder.CurrentMaterialState +
                    ", storedWafer=" + (wafer != null ? wafer.WaferId : "-") +
                    ", ringDetected=" + ringDetected;
                return false;
            }
            catch (Exception ex)
            {
                reason = "InputFeeder 자재 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyOutputFeederEmpty(out string reason)
        {
            reason = "";
            try
            {
                OutputFeederUnit feeder = _machine != null ? _machine.OutputFeederUnit : null;
                if (feeder == null)
                {
                    reason = "OutputFeederUnit을 찾을 수 없습니다.";
                    return false;
                }

                bool dataEmpty = feeder.IsFeederTransferDataEmpty();
                bool ringDetected = feeder.IsBinFeederRingCheck();
                if (dataEmpty && !ringDetected)
                    return true;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                reason = "OutputFeeder가 비어 있지 않거나 자재 데이터와 센서 상태가 일치하지 않습니다." +
                    " transferDataEmpty=" + dataEmpty +
                    ", materialState=" + feeder.CurrentMaterialState +
                    ", storedWafer=" + (wafer != null ? wafer.WaferId : "-") +
                    ", ringDetected=" + ringDetected;
                return false;
            }
            catch (Exception ex)
            {
                reason = "OutputFeeder 자재 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifySharedRailHomeClear(BaseAxis axis, out string reason)
        {
            reason = "";
            try
            {
                if (axis == null)
                {
                    reason = "SharedRail Home 확인 축이 없습니다.";
                    return false;
                }

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(_machine);
                if (service == null || !service.IsSharedRailAxis(axis))
                {
                    reason = "SharedRailX에 등록되지 않은 축입니다. axis=" + axis.Name;
                    return false;
                }

                double probeTarget = ResolveSharedRailHomeProbeTarget(axis);
                string clearanceReason;
                if (!service.VerifySingleAxisMove(axis, probeTarget, out clearanceReason))
                {
                    reason = "SharedRail Home 탐색 경로의 안전거리가 부족합니다. axis=" + axis.Name +
                        ", current=" + axis.ActualPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", probeTarget=" + probeTarget.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", homeDirection=" + (axis.Setup != null ? axis.Setup.HomeDirection.ToString() : "Unknown") +
                        ", detail=" + clearanceReason;
                    return false;
                }

                if (!service.VerifySingleAxisMove(axis, 0.0, out clearanceReason))
                {
                    reason = "SharedRail Home 완료 위치의 안전거리가 부족합니다. axis=" + axis.Name +
                        ", current=" + axis.ActualPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", homeTarget=0.000, detail=" + clearanceReason;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "SharedRail Home 안전거리 확인 중 예외가 발생했습니다. axis=" +
                    (axis != null ? axis.Name : "-") + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveSharedRailHomeProbeTarget(BaseAxis axis)
        {
            try
            {
                if (axis == null || axis.Setup == null || !axis.Setup.SoftLimitEnabled)
                    return 0.0;

                return axis.Setup.HomeDirection == HomeDirection.Ccw
                    ? Math.Min(0.0, axis.Setup.SoftLimitMinus)
                    : Math.Max(0.0, axis.Setup.SoftLimitPlus);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializeInterlock",
                    "SharedRail Home probe target resolve failed. axis=" +
                    (axis != null ? axis.Name : "-") + ", error=" + ex.Message + " - Failed");
                return 0.0;
            }
            finally
            {
            }
        }

        private bool TryResolveAxisPosition(BaseAxis axis, string positionName, out double position)
        {
            position = 0.0;
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(positionName))
                    return false;

                string normalizedPositionName = positionName.Trim();

                OutputStageUnit outputStage = _machine != null ? _machine.OutputStageUnit : null;
                if (string.Equals(normalizedPositionName, "ProcessPosition", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsAxisName(axis, "OutputGoodStageZ", "GoodStage_StageZ") &&
                        outputStage != null && outputStage.Recipe != null && outputStage.Recipe.GoodStageZ != null)
                    {
                        position = outputStage.Recipe.GoodStageZ.ProcessPosition;
                        return true;
                    }

                    return false;
                }

                if (!string.Equals(normalizedPositionName, "AvoidPosition", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (IsAxisName(axis, "OutputNGStageY", "NgStage_StageY") &&
                    outputStage != null && outputStage.Recipe != null && outputStage.Recipe.NGStageY != null)
                {
                    position = outputStage.Recipe.NGStageY.AvoidPosition;
                    return true;
                }

                if (IsAxisName(axis, "OutputGoodStageY", "GoodStage_StageY") &&
                    outputStage != null && outputStage.Recipe != null && outputStage.Recipe.GoodStageY != null)
                {
                    position = outputStage.Recipe.GoodStageY.AvoidPosition;
                    return true;
                }

                if (IsAxisName(axis, "OutputGoodStageZ", "GoodStage_StageZ") &&
                    outputStage != null && outputStage.Recipe != null && outputStage.Recipe.GoodStageZ != null)
                {
                    position = outputStage.Recipe.GoodStageZ.AvoidPosition;
                    return true;
                }

                InputStageUnit inputStage = _machine != null ? _machine.InputStageUnit : null;
                if (IsAxisName(axis, "InputStageY", "StageY", "WaferStageY") &&
                    inputStage != null && inputStage.Recipe != null && inputStage.Recipe.WaferY != null)
                {
                    position = inputStage.Recipe.WaferY.AvoidPosition;
                    return true;
                }

                InputFeederUnit inputFeeder = _machine != null ? _machine.InputFeederUnit : null;
                if (IsAxisName(axis, "InputFeederY", "FeederY") &&
                    inputFeeder != null && inputFeeder.Recipe != null)
                {
                    position = inputFeeder.Recipe.AvoidPosition;
                    return true;
                }

                OutputFeederUnit outputFeeder = _machine != null ? _machine.OutputFeederUnit : null;
                if (IsAxisName(axis, "OutputFeederY", "FeederY_Output") &&
                    outputFeeder != null && outputFeeder.Recipe != null)
                {
                    position = outputFeeder.Recipe.AvoidPosition;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializeInterlock",
                    "Initialize axis position resolve failed. axis=" +
                    (axis != null ? axis.Name : "-") + ", position=" + positionName +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsAxisName(BaseAxis axis, params string[] names)
        {
            try
            {
                if (axis == null || names == null)
                    return false;

                foreach (string name in names)
                {
                    if (!string.IsNullOrWhiteSpace(name) &&
                        string.Equals(axis.Name, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializeInterlock",
                    "Initialize axis name compare failed. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool Check(bool condition, string failReason, out string reason)
        {
            reason = condition ? "" : failReason;
            return condition;
        }

        private BaseAxis FindAxis(string axisName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(axisName) || _axesProvider == null)
                    return null;

                foreach (var axis in _axesProvider())
                {
                    if (axis != null && string.Equals(axis.Name, axisName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return axis;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private BaseDigitalInput FindDigitalInput(string inputName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(inputName))
                    return null;

                string normalizedName = inputName.Trim();
                BaseDigitalInput input;
                lock (_digitalInputGate)
                {
                    if (_digitalInputs.TryGetValue(normalizedName, out input) && input != null)
                        return input;
                }

                input = FindNode<BaseDigitalInput>(normalizedName);
                if (input != null)
                {
                    lock (_digitalInputGate)
                    {
                        _digitalInputs[normalizedName] = input;
                    }
                    return input;
                }

                DioDefault catalog = AjinIoCatalog.FindInput(normalizedName);
                input = catalog != null ? AjinFactory.CreateDigitalInput(catalog) : null;
                if (input != null)
                {
                    lock (_digitalInputGate)
                    {
                        _digitalInputs[normalizedName] = input;
                    }
                }
                return input;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializeInterlock",
                    "Digital input resolve failed. input=" + inputName +
                    ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private T FindNode<T>(string name) where T : class
        {
            try
            {
                if (_machine == null || string.IsNullOrWhiteSpace(name))
                    return null;

                return FindNodeRecursive<T>(_machine, name.Trim(), new HashSet<object>());
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private T FindNodeRecursive<T>(object node, string name, HashSet<object> visited) where T : class
        {
            try
            {
                if (node == null || visited.Contains(node))
                    return null;

                visited.Add(node);

                T typed = node as T;
                BaseEquipmentNode equipmentNode = node as BaseEquipmentNode;
                if (typed != null && equipmentNode != null &&
                    string.Equals(equipmentNode.Name, name, StringComparison.OrdinalIgnoreCase))
                    return typed;

                foreach (PropertyInfo property in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.GetIndexParameters().Length > 0)
                        continue;

                    object value;
                    try { value = property.GetValue(node, null); }
                    catch { continue; }

                    if (value == null || value is string)
                        continue;

                    var enumerable = value as System.Collections.IEnumerable;
                    if (enumerable != null && !(value is BaseEquipmentNode))
                    {
                        foreach (var item in enumerable)
                        {
                            T found = FindNodeRecursive<T>(item, name, visited);
                            if (found != null)
                                return found;
                        }

                        continue;
                    }

                    if (value is BaseEquipmentNode || value is BaseDigitalInput || value is BaseCylinder)
                    {
                        T found = FindNodeRecursive<T>(value, name, visited);
                        if (found != null)
                            return found;
                    }
                }

                return null;
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
}
