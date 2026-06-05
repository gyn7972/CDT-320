using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

using Alarms = QMC.Common.Alarms;
namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 - INPUT/OUTPUT/LOWER/BOTTOM/SIDE VISION 공용 페이지.
    /// 좌측 대형 STAGE VISION + 우측: ROI 설정/ACTION/AUTO SCALE/GRAY LEVEL/조그/속도 + 하단 MATCH RESULT + PARAMETER.
    /// </summary>
    public class VisionRecipePage : PageBase
    {
        public VisionRecipePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // 좌측 STAGE VISION
            Controls.Add(RecipeLayout.CameraView(8, 40, 540, 520, titleI18n));

            // ROI 설정 헤더
            Controls.Add(RecipeLayout.OrangeBar(560, 40, 480, "ROI 설정"));
            Controls.Add(new Label { Location = new Point(860, 40), Size = new Size(180, 26), Text = "1# Match Image", BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White, Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10,0,0,0) });

            // SELECT
            var selectGrp = new GroupBox { Location = new Point(564, 70), Size = new Size(210, 60), Text = "SELECT", Font = UiTheme.SectionFont };
            selectGrp.Controls.Add(new RadioButton { Location = new Point(8,  22), AutoSize = true, Text = "Main", Checked = true });
            selectGrp.Controls.Add(new RadioButton { Location = new Point(62, 22), AutoSize = true, Text = "Chip" });
            selectGrp.Controls.Add(new RadioButton { Location = new Point(8,  38), AutoSize = true, Text = "Sub" });
            selectGrp.Controls.Add(new RadioButton { Location = new Point(62, 38), AutoSize = true, Text = "Cross" });
            Controls.Add(selectGrp);

            // INDEX
            var idxGrp = new GroupBox { Location = new Point(564, 134), Size = new Size(210, 60), Text = "INDEX", Font = UiTheme.SectionFont };
            idxGrp.Controls.Add(new RadioButton { Location = new Point(8,  22), AutoSize = true, Text = "1", Checked = true });
            idxGrp.Controls.Add(new RadioButton { Location = new Point(48, 22), AutoSize = true, Text = "2" });
            idxGrp.Controls.Add(new RadioButton { Location = new Point(88, 22), AutoSize = true, Text = "4" });
            idxGrp.Controls.Add(new RadioButton { Location = new Point(130,22), AutoSize = true, Text = "8" });
            Controls.Add(idxGrp);

            // 방향 버튼
            Controls.Add(new Button { Location = new Point(620, 200), Size = new Size(70, 40), Text = "▲", Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat });
            Controls.Add(new Button { Location = new Point(564, 240), Size = new Size(70, 40), Text = "◀", Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat });
            Controls.Add(new Button { Location = new Point(676, 240), Size = new Size(70, 40), Text = "▶", Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat });
            Controls.Add(new Button { Location = new Point(620, 280), Size = new Size(70, 40), Text = "▼", Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat });

            // 썸네일
            var thumb = new Panel { Location = new Point(800, 70), Size = new Size(240, 230), BackColor = Color.FromArgb(0x40,0x40,0x40), BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(thumb);

            // ACTION
            Controls.Add(RecipeLayout.OrangeBar(560, 326, 480, "ACTION"));
            int ay = 360, ax = 564;
            string[] actions = { "GRAB", "MATCH", "FAST SHUTTER", "SMALL ROI", "MATCH MOVE", "IMAGE SAVE", "THETA MATCH MOVE" };
            foreach (var a in actions)
            {
                var actionButton = new ActionButton { Location = new Point(ax, ay), Size = new Size(150, 42), Text = a };
                // OS-03 (Stage 60 cycle 4) — GRAB / MATCH / FAST SHUTTER 버튼 실 핸들러 연결
                string actionName = a;
                actionButton.Click += async (s, e) =>
                {
                    try
                    {
                        QMC.CDT320.Logging.EventLogger.Write(
                            QMC.CDT320.Logging.EventKind.Event,
                            QMC.CDT_320.Ui.Security.UserSession.Name,
                            "VISION-ACTION", "Click: " + actionName + " (titleI18n=" + titleI18n + ")");

                        var wafer = QMC.CDT320.VisionComm.VisionHub.Wafer;

                        switch (actionName)
                        {
                            case "GRAB":
                                if (wafer != null && wafer.IsConnected)
                                {
                                    bool ok = await wafer.ExposeAsync(0, 3000);
                                    MessageBox.Show("GRAB " + (ok ? "성공" : "실패"),
                                        "Vision GRAB", MessageBoxButtons.OK,
                                        ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                                }
                                else MessageBox.Show("Wafer Vision 미연결 (TCP 5100)",
                                    "GRAB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                            case "MATCH":
                                if (wafer != null && wafer.IsConnected)
                                {
                                    var result = await wafer.MatchAsync("ReticleFinder", 0, 5000);
                                    string msg = result == null
                                        ? "MATCH 실패 (결과 null)"
                                        : "MATCH 결과: x=" + result.X.ToString("F2") + ", y=" + result.Y.ToString("F2") +
                                          ", angle=" + result.AngleDeg.ToString("F2") + ", score=" + result.Score.ToString("F2");
                                    MessageBox.Show(msg, "Vision MATCH",
                                        MessageBoxButtons.OK,
                                        result != null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                                }
                                else MessageBox.Show("Wafer Vision 미연결 (TCP 5100)",
                                    "MATCH", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                            case "FAST SHUTTER":
                                MessageBox.Show("FAST SHUTTER 모드 — Vision PC 측 SetExposureUs API 호출 필요\n" +
                                                "(다음 stage 에서 hub.Wafer.SetFastShutterAsync 추가 예정)",
                                    "FAST SHUTTER", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            case "SMALL ROI":
                            case "MATCH MOVE":
                            case "IMAGE SAVE":
                            case "THETA MATCH MOVE":
                                MessageBox.Show(actionName + " 기능은 다음 stage 에서 구현됩니다.",
                                    "Vision Action", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.Alarms.AlarmManager.Raise(
                            QMC.Common.Alarms.AlarmSeverity.Warning,
                            "VisionMatchFail", "VisionRecipePage",
                            actionName + " 예외: " + ex.GetType().Name + ": " + ex.Message);
                    }
                };
                Controls.Add(actionButton);
                ax += 158;
                if (ax > 900) { ax = 564; ay += 48; }
            }

            // AUTO SCALE + GRAY LEVEL  (Stage 60 — ACTION 7번째 버튼과 겹치던 이슈 수정: y +38)
            Controls.Add(RecipeLayout.OrangeBar(560, 514, 300, "AUTO SCALE"));
            Controls.Add(RecipeLayout.OrangeBar(864, 514, 180, "GRAY LEVEL"));
            int sy = 544;
            RecipeLayout.AddPair(this, 564, sy, 100, 100, "CALC SCALE X", "0.00000");
            RecipeLayout.AddPair(this, 764, sy, 80,  80,  "PITCH",        "1000"); sy += 32;
            RecipeLayout.AddPair(this, 564, sy, 100, 100, "CALC SCALE Y", "0.00000");
            Controls.Add(new Button { Location = new Point(764, sy), Size = new Size(80, 28), Text = "AUTO SCALE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.FromArgb(0xD0,0xD0,0xD0) });
            sy += 32;
            RecipeLayout.AddPair(this, 564, sy, 100, 100, "렌즈배율", "0.22x");
            Controls.Add(new Label { Location = new Point(864, 544), Size = new Size(180, 60), Text = "255", BackColor = Color.White, ForeColor = Color.Black, Font = new Font("맑은 고딕", 32F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle });

            // 조그 영역 (AXIS X/Y) + 속도  (위와 동일 +38 시프트)
            Controls.Add(RecipeLayout.OrangeBar(560, 678, 480, "조그 운전"));
            Controls.Add(new Button  { Location = new Point(564, 712), Size = new Size(80,  30), Text = "저속",     FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            Controls.Add(new Button  { Location = new Point(648, 712), Size = new Size(100, 30), Text = "조그 이동", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            Controls.Add(new TextBox { Location = new Point(756, 712), Size = new Size(100, 26), Text = "0 um",      Font = UiTheme.ValueFont });
            Controls.Add(RecipeLayout.XyJog(600, 748, 360, 230, withTheta: true));

            // 하단 MATCH RESULT + PARAMETER
            var dg = new DataGridView
            {
                Location = new Point(8, 580), Size = new Size(540, 360),
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F)
            };
            dg.Columns.Add("NO", "No"); dg.Columns.Add("PX", "Pos X"); dg.Columns.Add("PY", "Pos Y"); dg.Columns.Add("ANG", "Angle"); dg.Columns.Add("SC", "Score");
            dg.Rows.Add(1, "", "", "", "");
            Controls.Add(dg);
            Controls.Add(RecipeLayout.OrangeBar(8, 566, 540, "MATCH RESULT"));

            // 속도 (위와 동일 +38 시프트)
            Controls.Add(RecipeLayout.VerticalSpeedBar(1060, 678, 140, 300));
        }
    }
}
