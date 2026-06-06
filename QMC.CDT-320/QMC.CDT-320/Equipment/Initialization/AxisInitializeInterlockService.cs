using System;
using System.Collections.Generic;
using System.Reflection;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    public sealed class AxisInitializeInterlockService
    {
        private readonly CDT320_Machine _machine;
        private readonly Func<IEnumerable<BaseAxis>> _axesProvider;

        public AxisInitializeInterlockService(
            CDT320_Machine machine,
            Func<IEnumerable<BaseAxis>> axesProvider)
        {
            _machine = machine;
            _axesProvider = axesProvider;
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
                        string message = "초기화 인터락 실패. step=" + step.StepNo +
                            ", group=" + step.GroupName +
                            ", target=" + rule.TargetType + ":" + rule.Name +
                            ", expected=" + rule.ExpectedState +
                            ", reason=" + reason;
                        Log.Write("Main", "SYSTEM", "AxisInitializeInterlock", message + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Warning, "INIT-INTERLOCK", "MachineController", message);
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
                BaseDigitalInput input = FindNode<BaseDigitalInput>(rule.Name);
                if (input == null)
                {
                    reason = "DI를 찾을 수 없습니다. input=" + rule.Name;
                    return false;
                }

                string state = rule.ExpectedState ?? "";
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
