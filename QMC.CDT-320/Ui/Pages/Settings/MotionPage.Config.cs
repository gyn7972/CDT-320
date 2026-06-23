using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Controls;
using QMC.Common;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// MotionPage ? CONFIG ?. <see cref="AxisSetup"/> / <see cref="AxisConfig"/> ?
    /// ?? ??? ??? ???? ?? ? ???/Enum ?????? ??????.
    /// ???? ?? ??? ?? STATUS ?(<see cref="MotionPage"/>.Status partial)?? ??.
    /// </summary>
    public partial class MotionPage
    {
        // ?? ?? ? = AxisSetup/AxisConfig ? ??? 1:1 ????.

        private static readonly string[] CFG_NAMES =
        {
            "OUTPUT MODE",       // Setup.PulseOutput
            "INPUT MODE",        // Setup.EncoderInput
            "INPUT SOURCE",      // Setup.InputSource
            "SERVO LEVEL",       // Setup.ServoOnLevel
            "MAX VELOCITY",      // Config.MaxVelocity
            "PULSES/UNIT",       // Setup.PulsesPerUnit
        };

        private static readonly string[] INP_NAMES =
        {
            "INPOSITION",        // Setup.InPosition
            "TOLERANCE",         // Config.InPositionTolerance
        };

        private static readonly string[] LIMIT_NAMES =
        {
            "POS LEVEL",         // Setup.PositiveLimitLevel
            "NEG LEVEL",         // Setup.NegativeLimitLevel
            "SW POSITIVE",       // Setup.SoftLimitPlus
            "SW NEGATIVE",       // Setup.SoftLimitMinus
            "SW ENABLED",        // Setup.SoftLimitEnabled
        };

        private static readonly string[] EMG_NAMES =
        {
            "ALARM LEVEL",       // Setup.AlarmLevel
            "EMG LEVEL",         // Setup.EmergencyLevel
            "STOP MODE",         // Setup.StopMode
        };

        private static readonly string[] HOME_NAMES =
        {
            "DIRECTION",         // Setup.HomeDirection
            "SIGNAL",            // Setup.HomeSignal
            "OFFSET",            // Setup.HomeOffset
            "TIMEOUT(ms)",       // Setup.HomeTimeoutMs
        };

        private static readonly string[] ALARM_NAMES =
        {
            "RESET LEVEL",       // Setup.AlarmResetLevel
            "ALARM VALUE",       // axis.IsAlarm  (read-only)
            "ALARM CODE",        // axis.AlarmCode (read-only)
        };

        private static readonly string[] PROFILE_NAMES =
        {
            "PROFILE",           // Setup.ProfileMode
            "ACC JERK %",        // Setup.AccJerkPercent
            "DEC JERK %",        // Setup.DecJerkPercent
        };

        // ?? ? enum ??. ?? ?? ??.
        private static readonly Dictionary<string, Type> ENUM_TYPES =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "OUTPUT MODE",  typeof(PulseOutput) },
                { "INPUT MODE",   typeof(EncoderInput) },
                { "INPUT SOURCE", typeof(InputSource) },
                { "SERVO LEVEL",  typeof(ActiveLevel) },
                { "INPOSITION",   typeof(InPosition) },
                { "POS LEVEL",    typeof(ActiveLevel) },
                { "NEG LEVEL",    typeof(ActiveLevel) },
                { "ALARM LEVEL",  typeof(ActiveLevel) },
                { "EMG LEVEL",    typeof(ActiveLevel) },
                { "STOP MODE",    typeof(StopMode) },
                { "DIRECTION",    typeof(HomeDirection) },
                { "SIGNAL",       typeof(HomeSignal) },
                { "RESET LEVEL",  typeof(ActiveLevel) },
                { "PROFILE",      typeof(AxisProfileMode) },
            };

        // ?????????????????????????????????????????????
        //  ???
        // ?????????????????????????????????????????????

        private void InitializeConfigPanels()
        {
            try
            {
                pgConfig.DefineItems(CFG_NAMES);
                pgInposition.DefineItems(INP_NAMES);
                pgLimit.DefineItems(LIMIT_NAMES);
                pgEmergency.DefineItems(EMG_NAMES);
                pgHome.DefineItems(HOME_NAMES);
                pgAlarm.DefineItems(ALARM_NAMES);
                pgPositionClear.DefineItems(PROFILE_NAMES);

                // ?? ?? ?? ?? (ALARM VALUE / CODE ? ???? ??)
                MarkEditable(pgConfig, CFG_NAMES);
                MarkEditable(pgInposition, INP_NAMES);
                MarkEditable(pgLimit, LIMIT_NAMES);
                MarkEditable(pgEmergency, EMG_NAMES);
                MarkEditable(pgHome, HOME_NAMES);
                MarkEditable(pgAlarm, new[] { "RESET LEVEL" });
                MarkEditable(pgPositionClear, PROFILE_NAMES);

                pgConfig.ItemClicked += OnConfigItemClicked;
                pgInposition.ItemClicked += OnConfigItemClicked;
                pgLimit.ItemClicked += OnConfigItemClicked;
                pgEmergency.ItemClicked += OnConfigItemClicked;
                pgHome.ItemClicked += OnConfigItemClicked;
                pgAlarm.ItemClicked += OnConfigItemClicked;
                pgPositionClear.ItemClicked += OnConfigItemClicked;

                // ????? PROFILE ???? ???
                grpPositionClear.Text = "PROFILE";

                grid.SelectionChanged += (s, e) => RefreshConfigForSelected();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-CFG",
                    "MotionPage",
                    "InitializeConfigPanels failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static void MarkEditable(QMC.CDT_320.Ui.Controls.ParamGrid grid, IEnumerable<string> names)
        {
            if (grid == null || names == null) return;
            foreach (var n in names) grid.SetEditable(n, true);
        }

        // ?????????????????????????????????????????????
        //  ??? ??
        // ?????????????????????????????????????????????

        /// <summary>?? ??? ?? setup/config ?? CONFIG ?? ????.</summary>
        private void RefreshConfigForSelected()
        {
            try
            {
                BaseAxis axis = SelectedAxis();
                if (axis == null) { ClearAllConfig(); return; }

                AxisSetup setup = axis.Setup;
                AxisConfig cfg = axis.Config;

                if (setup != null)
                {
                    pgConfig.SetValue("OUTPUT MODE", setup.PulseOutput.ToString());
                    pgConfig.SetValue("INPUT MODE", setup.EncoderInput.ToString());
                    pgConfig.SetValue("INPUT SOURCE", setup.InputSource.ToString());
                    pgConfig.SetValue("SERVO LEVEL", setup.ServoOnLevel.ToString().ToUpperInvariant());
                    pgConfig.SetValue("PULSES/UNIT", FormatPulsesPerDisplayUnit(setup.PulsesPerUnit, axis));
                }
                if (cfg != null)
                    pgConfig.SetValue("MAX VELOCITY", FormatAxisValue(cfg.MaxVelocity, axis, "0.###"));

                if (setup != null)
                    pgInposition.SetValue("INPOSITION", setup.InPosition.ToString().ToUpperInvariant());
                if (cfg != null)
                    pgInposition.SetValue("TOLERANCE", FormatAxisValue(cfg.InPositionTolerance, axis, "0.####"));

                if (setup != null)
                {
                    pgLimit.SetValue("POS LEVEL", setup.PositiveLimitLevel.ToString().ToUpperInvariant());
                    pgLimit.SetValue("NEG LEVEL", setup.NegativeLimitLevel.ToString().ToUpperInvariant());
                    pgLimit.SetValue("SW POSITIVE", FormatAxisValue(setup.SoftLimitPlus, axis, "0.###"));
                    pgLimit.SetValue("SW NEGATIVE", FormatAxisValue(setup.SoftLimitMinus, axis, "0.###"));
                    pgLimit.SetValue("SW ENABLED", setup.SoftLimitEnabled ? "ON" : "OFF");

                    pgEmergency.SetValue("ALARM LEVEL", setup.AlarmLevel.ToString().ToUpperInvariant());
                    pgEmergency.SetValue("EMG LEVEL", setup.EmergencyLevel.ToString().ToUpperInvariant());
                    pgEmergency.SetValue("STOP MODE", setup.StopMode.ToString());

                    pgHome.SetValue("DIRECTION", setup.HomeDirection.ToString());
                    pgHome.SetValue("SIGNAL", setup.HomeSignal.ToString());
                    pgHome.SetValue("OFFSET", FormatAxisValue(setup.HomeOffset, axis, "0.###"));
                    pgHome.SetValue("TIMEOUT(ms)", setup.HomeTimeoutMs.ToString(CultureInfo.InvariantCulture));

                    pgAlarm.SetValue("RESET LEVEL", setup.AlarmResetLevel.ToString().ToUpperInvariant());

                    pgPositionClear.SetValue("PROFILE", setup.ProfileMode.ToString());
                    pgPositionClear.SetValue("ACC JERK %", setup.AccJerkPercent.ToString(CultureInfo.InvariantCulture));
                    pgPositionClear.SetValue("DEC JERK %", setup.DecJerkPercent.ToString(CultureInfo.InvariantCulture));
                }

                pgAlarm.SetValue("ALARM VALUE", axis.IsAlarm ? "ON" : "OFF", axis.IsAlarm);
                pgAlarm.SetValue("ALARM CODE", "0x" + axis.AlarmCode.ToString("X4"), axis.IsAlarm);

                // STATUS ?? ???? ???? ?? ??
                RefreshStatusForSelected();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-CFG",
                    "MotionPage",
                    "RefreshConfigForSelected failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>Timer ?? ??: ?? ??? STATUS ?? ?? ??.</summary>
        private void RefreshConfigDynamic()
        {
            try
            {
                BaseAxis axis = SelectedAxis();
                if (axis == null) return;

                pgAlarm.SetValue("ALARM VALUE", axis.IsAlarm ? "ON" : "OFF", axis.IsAlarm);
                pgAlarm.SetValue("ALARM CODE", "0x" + axis.AlarmCode.ToString("X4"), axis.IsAlarm);

                RefreshStatusDynamic();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-CFG",
                    "MotionPage",
                    "RefreshConfigDynamic failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ClearAllConfig()
        {
            try
            {
                pgConfig.ClearValues();
                pgInposition.ClearValues();
                pgLimit.ClearValues();
                pgEmergency.ClearValues();
                pgHome.ClearValues();
                pgAlarm.ClearValues();
                pgPositionClear.ClearValues();
                ClearStatus();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-CFG",
                    "MotionPage",
                    "ClearAllConfig failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // ?????????????????????????????????????????????
        //  ?? (?? ? ??? / Enum picker ? ??)
        // ?????????????????????????????????????????????

        private void OnConfigItemClicked(string name)
        {
            try
            {
                BaseAxis axis = SelectedAxis();
                if (axis == null || axis.Setup == null || axis.Config == null) return;

                if (ENUM_TYPES.TryGetValue(name, out var enumType))
                {
                    string current = GetCurrentText(axis, name);
                    string picked = ShowEnumDialog(name, enumType, current);
                    if (picked == null) return;
                    ApplyEnumValue(axis, name, enumType, picked);
                }
                else
                {
                    string current = GetCurrentText(axis, name);
                    using (var dlg = new NumericKeypadDialog(name, current, string.Empty))
                    {
                        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                        ApplyTextValue(axis, name, dlg.ValueText ?? string.Empty);
                    }
                }

                SaveMotionAxisSettings();
                RefreshConfigForSelected();

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC", "CFG-EDIT",
                    axis.Name + " " + name);
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-CFG",
                    "MotionPage",
                    "OnConfigItemClicked failed [" + name + "]: " + ex.Message);
            }
            finally
            {
            }
        }

        private string ShowEnumDialog(string title, Type enumType, string current)
        {
            var names = new List<string>(Enum.GetNames(enumType));
            using (var dlg = new EnumPickerDialog(title, names, current))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return null;
                return dlg.SelectedValue;
            }
        }

        private static string GetCurrentText(BaseAxis axis, string name)
        {
            AxisSetup s = axis.Setup;
            AxisConfig c = axis.Config;
            switch (name)
            {
                // Pulse 출력 모드 표시
                case "OUTPUT MODE":   return s.PulseOutput.ToString();
                // Encoder 입력 모드 표시
                case "INPUT MODE":    return s.EncoderInput.ToString();
                // Encoder 입력 소스 표시
                case "INPUT SOURCE":  return s.InputSource.ToString();
                // Servo ON 레벨 표시
                case "SERVO LEVEL":   return s.ServoOnLevel.ToString();
                // 최대 속도 표시
                case "MAX VELOCITY":  return FormatAxisValue(c.MaxVelocity, axis, "0.###");
                // 단위당 Pulse 표시
                case "PULSES/UNIT":   return FormatPulsesPerDisplayUnit(s.PulsesPerUnit, axis);
                // InPosition 설정 표시
                case "INPOSITION":    return s.InPosition.ToString();
                // InPosition 허용오차 표시
                case "TOLERANCE":     return FormatAxisValue(c.InPositionTolerance, axis, "0.####");
                // Plus Limit 레벨 표시
                case "POS LEVEL":     return s.PositiveLimitLevel.ToString();
                // Minus Limit 레벨 표시
                case "NEG LEVEL":     return s.NegativeLimitLevel.ToString();
                // Plus Soft Limit 표시
                case "SW POSITIVE":   return FormatAxisValue(s.SoftLimitPlus, axis, "0.###");
                // Minus Soft Limit 표시
                case "SW NEGATIVE":   return FormatAxisValue(s.SoftLimitMinus, axis, "0.###");
                // Soft Limit 사용 여부 표시
                case "SW ENABLED":    return s.SoftLimitEnabled ? "ON" : "OFF";
                // Alarm 레벨 표시
                case "ALARM LEVEL":   return s.AlarmLevel.ToString();
                // Emergency 레벨 표시
                case "EMG LEVEL":     return s.EmergencyLevel.ToString();
                // 정지 모드 표시
                case "STOP MODE":     return s.StopMode.ToString();
                // Home 방향 표시
                case "DIRECTION":     return s.HomeDirection.ToString();
                // Home 신호 표시
                case "SIGNAL":        return s.HomeSignal.ToString();
                // Home Offset 표시
                case "OFFSET":        return FormatAxisValue(s.HomeOffset, axis, "0.###");
                // Home Timeout 표시
                case "TIMEOUT(ms)":   return s.HomeTimeoutMs.ToString(CultureInfo.InvariantCulture);
                // Alarm Reset 레벨 표시
                case "RESET LEVEL":   return s.AlarmResetLevel.ToString();
                // Profile 모드 표시
                case "PROFILE":       return s.ProfileMode.ToString();
                // 가속 Jerk 표시
                case "ACC JERK %":    return s.AccJerkPercent.ToString(CultureInfo.InvariantCulture);
                // 감속 Jerk 표시
                case "DEC JERK %":    return s.DecJerkPercent.ToString(CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }

        private static void ApplyEnumValue(BaseAxis axis, string name, Type enumType, string text)
        {
            object v;
            try { v = Enum.Parse(enumType, text, ignoreCase: true); }
            catch { return; }

            AxisSetup s = axis.Setup;
            switch (name)
            {
                // Pulse 출력 모드 저장
                case "OUTPUT MODE":   s.PulseOutput = (PulseOutput)v; break;
                // Encoder 입력 모드 저장
                case "INPUT MODE":    s.EncoderInput = (EncoderInput)v; break;
                // Encoder 입력 소스 저장
                case "INPUT SOURCE":  s.InputSource = (InputSource)v; break;
                // Servo ON 레벨 저장
                case "SERVO LEVEL":   s.ServoOnLevel = (ActiveLevel)v; break;
                // InPosition 방식 저장
                case "INPOSITION":    s.InPosition = (InPosition)v; break;
                // Plus Limit 레벨 저장
                case "POS LEVEL":     s.PositiveLimitLevel = (ActiveLevel)v; break;
                // Minus Limit 레벨 저장
                case "NEG LEVEL":     s.NegativeLimitLevel = (ActiveLevel)v; break;
                // Alarm 레벨 저장
                case "ALARM LEVEL":   s.AlarmLevel = (ActiveLevel)v; break;
                // Emergency 레벨 저장
                case "EMG LEVEL":     s.EmergencyLevel = (ActiveLevel)v; break;
                // 정지 모드 저장
                case "STOP MODE":     s.StopMode = (StopMode)v; break;
                // Home 방향 저장
                case "DIRECTION":     s.HomeDirection = (HomeDirection)v; break;
                // Home 신호 저장
                case "SIGNAL":        s.HomeSignal = (HomeSignal)v; break;
                // Alarm Reset 레벨 저장
                case "RESET LEVEL":   s.AlarmResetLevel = (ActiveLevel)v; break;
                // Profile 모드 저장
                case "PROFILE":       s.ProfileMode = (AxisProfileMode)v; break;
            }
        }

        private static void ApplyTextValue(BaseAxis axis, string name, string text)
        {
            AxisSetup s = axis.Setup;
            AxisConfig c = axis.Config;
            switch (name)
            {
                // 최대 속도 저장
                case "MAX VELOCITY":
                    if (TryParseDouble(text, out var maxVel)) c.MaxVelocity = AxisUnitConverter.FromDisplay(maxVel, axis);
                    break;
                // 단위당 Pulse 저장
                case "PULSES/UNIT":
                    if (TryParseDouble(text, out var ppu)) s.PulsesPerUnit = FromDisplayPulsesPerUnit(ppu, axis);
                    break;
                // InPosition 허용오차 저장
                case "TOLERANCE":
                    if (TryParseDouble(text, out var tol)) c.InPositionTolerance = AxisUnitConverter.FromDisplay(tol, axis);
                    break;
                // Plus Soft Limit 저장
                case "SW POSITIVE":
                    if (TryParseDouble(text, out var swP)) s.SoftLimitPlus = AxisUnitConverter.FromDisplay(swP, axis);
                    break;
                // Minus Soft Limit 저장
                case "SW NEGATIVE":
                    if (TryParseDouble(text, out var swN)) s.SoftLimitMinus = AxisUnitConverter.FromDisplay(swN, axis);
                    break;
                // Soft Limit 사용 여부 저장
                case "SW ENABLED":
                    s.SoftLimitEnabled = ParseOnOff(text, s.SoftLimitEnabled);
                    break;
                // Home Offset 저장
                case "OFFSET":
                    if (TryParseDouble(text, out var off)) s.HomeOffset = AxisUnitConverter.FromDisplay(off, axis);
                    break;
                // Home Timeout 저장
                case "TIMEOUT(ms)":
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var to)) s.HomeTimeoutMs = to;
                    break;
                // 가속 Jerk 저장
                case "ACC JERK %":
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var aj)) s.AccJerkPercent = aj;
                    break;
                // 감속 Jerk 저장
                case "DEC JERK %":
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dj)) s.DecJerkPercent = dj;
                    break;
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string FormatPulsesPerDisplayUnit(double nativePulsesPerUnit, BaseAxis axis)
        {
            try
            {
                string unit = AxisUnitConverter.DisplayUnitFor(axis);
                double value = nativePulsesPerUnit;
                if (AxisUnitConverter.Normalize(unit) == AxisUnitConverter.Micrometer)
                    value = nativePulsesPerUnit / 1000.0;
                return value.ToString("0.###", CultureInfo.InvariantCulture);
            }
            catch
            {
                return nativePulsesPerUnit.ToString("0.###", CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private static double FromDisplayPulsesPerUnit(double displayPulsesPerUnit, BaseAxis axis)
        {
            try
            {
                string unit = AxisUnitConverter.DisplayUnitFor(axis);
                if (AxisUnitConverter.Normalize(unit) == AxisUnitConverter.Micrometer)
                    return displayPulsesPerUnit * 1000.0;
                return displayPulsesPerUnit;
            }
            catch
            {
                return displayPulsesPerUnit;
            }
            finally
            {
            }
        }

        private static bool ParseOnOff(string text, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            string t = text.Trim();
            if (string.Equals(t, "ON", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(t, "OFF", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(t, "TRUE", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(t, "FALSE", StringComparison.OrdinalIgnoreCase)) return false;
            if (t == "1") return true;
            if (t == "0") return false;
            return fallback;
        }
    }
}

