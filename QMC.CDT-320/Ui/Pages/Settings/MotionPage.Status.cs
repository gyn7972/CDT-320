using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// MotionPage ? STATUS ?. ????? ??? ? ?? ???? ?? ??? ?? ????.
    /// ?? ???? <see cref="AjinAxis.ReadLiveStatus"/> ? <see cref="AxisLiveStatus"/> ? ????.
    /// ?? ??? ??? ??? ??.
    /// </summary>
    public partial class MotionPage
    {
        private ParamGrid pgStatusConfig;
        private ParamGrid pgStatusInposition;
        private ParamGrid pgStatusLimit;
        private ParamGrid pgStatusEmergency;
        private ParamGrid pgStatusHome;
        private ParamGrid pgStatusAlarm;
        private ParamGrid pgStatusPosition;

        private static readonly string[] STATUS_CFG =
        {
            "OUTPUT MODE", "INPUT MODE", "Z PHASE LEVEL", "SERVO LEVEL",
            "MAX VELOCITY", "UNIT/PULSE",
        };

        private static readonly string[] STATUS_INP =
        {
            "ENABLE", "LEVEL", "VALUE",
        };

        private static readonly string[] STATUS_LIMIT =
        {
            "POS ACTION", "POS LEVEL", "POS VALUE",
            "NEG ACTION", "NEG LEVEL", "NEG VALUE",
            "SW POSITIVE", "SW NEGATIVE",
        };

        private static readonly string[] STATUS_EMG =
        {
            "AMP FAULT LEVEL", "AMP FAULT VALUE",
        };

        private static readonly string[] STATUS_HOME =
        {
            "SENSOR LEVEL", "SENSOR VALUE",
        };

        private static readonly string[] STATUS_ALARM =
        {
            "RESET LEVEL", "ALARM VALUE", "ALARM CODE",
        };

        private static readonly string[] STATUS_POS =
        {
            "ACTUAL", "COMMAND", "ERROR",
        };

        private void InitializeStatusPanels()
        {
            try
            {
                if (tabStatus == null) return;

                // 3 ?? ?3 ?? ???? (CONFIG.tab ? ??)
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 3,
                    BackColor = Color.WhiteSmoke,
                    Padding = new Padding(4),
                };
                for (int i = 0; i < 3; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
                for (int i = 0; i < 3; i++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3));

                pgStatusConfig     = AddStatusGroup(layout, 0, 0, "CONFIG", STATUS_CFG);
                pgStatusInposition = AddStatusGroup(layout, 1, 0, "INPOSITION", STATUS_INP);
                pgStatusLimit      = AddStatusGroup(layout, 2, 0, "LIMIT", STATUS_LIMIT);
                pgStatusEmergency  = AddStatusGroup(layout, 0, 1, "EMERGENCY", STATUS_EMG);
                pgStatusHome       = AddStatusGroup(layout, 1, 1, "HOME", STATUS_HOME);
                pgStatusAlarm      = AddStatusGroup(layout, 2, 1, "ALARM", STATUS_ALARM);
                pgStatusPosition   = AddStatusGroup(layout, 0, 2, "POSITION", STATUS_POS);

                tabStatus.Controls.Clear();
                tabStatus.Controls.Add(layout);
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-STS",
                    "MotionPage",
                    "InitializeStatusPanels failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static ParamGrid AddStatusGroup(TableLayoutPanel layout, int col, int row, string title, string[] names)
        {
            var grp = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            };
            var pg = new ParamGrid
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
            };
            grp.Controls.Add(pg);
            layout.Controls.Add(grp, col, row);
            pg.DefineItems(names);
            return pg;
        }

        /// <summary>STATUS ? ??/?? ?? ? ?? ??.</summary>
        private void RefreshStatusForSelected()
        {
            try
            {
                if (pgStatusConfig == null) return; // ????

                BaseAxis axis = SelectedAxis();
                if (axis == null) { ClearStatus(); return; }

                AjinAxis ajin = axis as AjinAxis;
                AxisLiveStatus live = ajin != null ? ajin.ReadLiveStatus() : null;
                if (live == null) { ClearStatus(); return; }

                pgStatusConfig.SetValue("OUTPUT MODE", live.OutputMethod.ToString());
                pgStatusConfig.SetValue("INPUT MODE", live.EncoderMethod.ToString());
                pgStatusConfig.SetValue("Z PHASE LEVEL", live.ZPhaseLevel.ToString().ToUpperInvariant());
                pgStatusConfig.SetValue("SERVO LEVEL", live.ServoOnLevel.ToString().ToUpperInvariant());
                pgStatusConfig.SetValue("MAX VELOCITY", live.MaxVelocity.ToString("N0"));
                pgStatusConfig.SetValue("UNIT/PULSE", live.MoveUnit.ToString("0.###") + " / " + live.PulsePerUnit);

                pgStatusInposition.SetValue("ENABLE", live.InPositionEnabled ? "ENABLE" : "DISABLE");
                pgStatusInposition.SetValue("LEVEL", live.InPositionLevel.ToString().ToUpperInvariant());
                pgStatusInposition.SetValue("VALUE", live.InPositionValue ? "ON" : "OFF");

                pgStatusLimit.SetValue("POS ACTION", live.PositiveLimitAction.ToString());
                pgStatusLimit.SetValue("POS LEVEL", live.PositiveLimitLevel.ToString().ToUpperInvariant());
                pgStatusLimit.SetValue("POS VALUE", live.PositiveLimitValue ? "ON" : "OFF", live.PositiveLimitValue);
                pgStatusLimit.SetValue("NEG ACTION", live.NegativeLimitAction.ToString());
                pgStatusLimit.SetValue("NEG LEVEL", live.NegativeLimitLevel.ToString().ToUpperInvariant());
                pgStatusLimit.SetValue("NEG VALUE", live.NegativeLimitValue ? "ON" : "OFF", live.NegativeLimitValue);
                pgStatusLimit.SetValue("SW POSITIVE", live.SoftLimitPositive.ToString("N0"));
                pgStatusLimit.SetValue("SW NEGATIVE", live.SoftLimitNegative.ToString("N0"));

                pgStatusEmergency.SetValue("AMP FAULT LEVEL", live.AmpFaultLevel.ToString().ToUpperInvariant());
                pgStatusEmergency.SetValue("AMP FAULT VALUE", live.AmpFaultValue ? "ON" : "OFF", live.AmpFaultValue);

                pgStatusHome.SetValue("SENSOR LEVEL", live.HomeSensorLevel.ToString().ToUpperInvariant());
                pgStatusHome.SetValue("SENSOR VALUE", live.HomeSensorValue ? "ON" : "OFF");

                pgStatusAlarm.SetValue("RESET LEVEL", live.AmpResetLevel.ToString().ToUpperInvariant());
                pgStatusAlarm.SetValue("ALARM VALUE", live.IsAlarm ? "ON" : "OFF", live.IsAlarm);
                pgStatusAlarm.SetValue("ALARM CODE", "0x" + live.AlarmCode.ToString("X4"), live.IsAlarm);

                pgStatusPosition.SetValue("ACTUAL", live.ActualPosition.ToString("F1"));
                pgStatusPosition.SetValue("COMMAND", live.CommandPosition.ToString("F1"));
                pgStatusPosition.SetValue("ERROR", live.PositionError.ToString("F1"));
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-STS",
                    "MotionPage",
                    "RefreshStatusForSelected failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>?? ????? ?? - ??? ? ???? ??? ??.</summary>
        private void RefreshStatusDynamic()
        {
            RefreshStatusForSelected();
        }

        private void ClearStatus()
        {
            try
            {
                pgStatusConfig?.ClearValues();
                pgStatusInposition?.ClearValues();
                pgStatusLimit?.ClearValues();
                pgStatusEmergency?.ClearValues();
                pgStatusHome?.ClearValues();
                pgStatusAlarm?.ClearValues();
                pgStatusPosition?.ClearValues();
            }
            catch
            {
            }
        }
    }
}

