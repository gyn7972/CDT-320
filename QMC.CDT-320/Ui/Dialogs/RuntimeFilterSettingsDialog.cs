using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Pick/Place/PickerZ 런타임 보정 필터 설정(컷오프 fc + 이상치/클램프 한계) 편집 창.
    /// PICKER Z 열은 단채널(fc/이상치/클램프 각 1종, T 없음) — Z 폐루프 건(2026-08-16)의 연계 항목.
    /// 저장은 각 서비스 SetFilterSettings 단일 경유 — json 파일을 직접 쓰면 런 중 지연 저장에
    /// 덮여 유실되므로 금지. fc 변경은 alpha만 재계산되고 필터 학습 상태는 유지된다.
    /// 자동 운전 중에도 변경 가능하며 다음 샘플부터 적용된다(2026-08-16 팀장님 승인 —
    /// 기구 이관·리셋과 달리 운전 중 금지 게이트를 두지 않는다).
    /// </summary>
    public partial class RuntimeFilterSettingsDialog : Form
    {
        // 입력 배열 인덱스: 0=fc, 1=이상치 X/Y, 2=이상치 T, 3=클램프 X/Y, 4=클램프 T.
        private static readonly string[] ItemNames =
            { "컷오프 fc", "이상치 X/Y", "이상치 T", "클램프 X/Y", "클램프 T" };

        // PICKER Z 열 인덱스: 0=fc, 1=이상치 Z, 2=클램프 Z (단채널 — T 항목 없음).
        private static readonly string[] ZItemNames = { "컷오프 fc", "이상치 Z", "클램프 Z" };

        private bool _allowClose;
        private TextBox[] _pickInputs;
        private TextBox[] _placeInputs;
        private TextBox[] _zInputs;
        private readonly double[] _loadedPick = new double[5];
        private readonly double[] _loadedPlace = new double[5];
        private readonly double[] _loadedZ = new double[3];

        public RuntimeFilterSettingsDialog()
        {
            InitializeComponent();
            _pickInputs = new[] { tbPickFc, tbPickOutlierXy, tbPickOutlierT, tbPickClampXy, tbPickClampT };
            _placeInputs = new[] { tbPlaceFc, tbPlaceOutlierXy, tbPlaceOutlierT, tbPlaceClampXy, tbPlaceClampT };
            _zInputs = new[] { tbZFc, tbZOutlier, tbZClamp };
            LoadValues();
        }

        // ── Event Methods ─────────────────────────────────────

        private void btnSave_Click(object sender, EventArgs e)
        {
            var errors = new StringBuilder();
            double[] pick = ParseSide("PICK", _pickInputs, errors);
            double[] place = ParseSide("PLACE", _placeInputs, errors);
            double[] z = ParseZ(errors);
            if (errors.Length > 0)
            {
                QMC.Common.MessageDialog.Show("저장하지 않았습니다. 입력을 확인하십시오.\n\n" + errors);
                return;
            }

            bool pickChanged = IsChanged(pick, _loadedPick);
            bool placeChanged = IsChanged(place, _loadedPlace);
            bool zChanged = IsChanged(z, _loadedZ);
            if (!pickChanged && !placeChanged && !zChanged)
            {
                QMC.Common.MessageDialog.Show("변경된 항목이 없습니다.");
                return;
            }

            var result = new StringBuilder();
            if (pickChanged)
            {
                bool ok = PickRuntimeOffsetService.SetFilterSettings(pick[0], pick[1], pick[2], pick[3], pick[4]);
                result.AppendLine("PICK: " + (ok ? "저장 완료" : "저장 실패(로그 확인)"));
            }

            if (placeChanged)
            {
                bool ok = PlaceRuntimeOffsetService.SetFilterSettings(place[0], place[1], place[2], place[3], place[4]);
                result.AppendLine("PLACE: " + (ok ? "저장 완료" : "저장 실패(로그 확인)"));
            }

            if (zChanged)
            {
                bool ok = PickerZRuntimeOffsetService.SetFilterSettings(z[0], z[1], z[2]);
                result.AppendLine("PICKER Z: " + (ok ? "저장 완료" : "저장 실패(로그 확인)"));
            }

            LoadValues();
            QMC.Common.MessageDialog.Show(result.ToString().TrimEnd());
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            Close();
        }

        private void tbFc_TextChanged(object sender, EventArgs e)
        {
            UpdateAlphaLabels();
        }

        private void RuntimeFilterSettingsDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 타이틀바 X는 막고 CLOSE 버튼으로만 닫는다(다른 다이얼로그와 동일 정책).
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        // ── Private Methods ───────────────────────────────────

        /// <summary>양쪽 서비스의 현재 설정을 읽어 입력란과 변경 비교 기준을 갱신한다.</summary>
        private void LoadValues()
        {
            double fc;
            double outlierXy;
            double outlierT;
            double clampXy;
            double clampT;

            PickRuntimeOffsetService.GetFilterSettings(
                out fc, out outlierXy, out outlierT, out clampXy, out clampT);
            FillSide(_pickInputs, _loadedPick, fc, outlierXy, outlierT, clampXy, clampT);

            PlaceRuntimeOffsetService.GetFilterSettings(
                out fc, out outlierXy, out outlierT, out clampXy, out clampT);
            FillSide(_placeInputs, _loadedPlace, fc, outlierXy, outlierT, clampXy, clampT);

            double zOutlier;
            double zClamp;
            PickerZRuntimeOffsetService.GetFilterSettings(out fc, out zOutlier, out zClamp);
            _loadedZ[0] = fc;
            _loadedZ[1] = zOutlier;
            _loadedZ[2] = zClamp;
            for (int i = 0; i < _zInputs.Length; i++)
                _zInputs[i].Text = F(_loadedZ[i]);

            UpdateAlphaLabels();
        }

        private static void FillSide(
            TextBox[] inputs,
            double[] loaded,
            double fc,
            double outlierXy,
            double outlierT,
            double clampXy,
            double clampT)
        {
            loaded[0] = fc;
            loaded[1] = outlierXy;
            loaded[2] = outlierT;
            loaded[3] = clampXy;
            loaded[4] = clampT;
            for (int i = 0; i < inputs.Length; i++)
                inputs[i].Text = F(loaded[i]);
        }

        /// <summary>
        /// 한 열(5항목)을 파싱·범위 검증한다. 위반 항목은 errors에 1줄씩 추가하고,
        /// 전부 정상일 때만 값 배열을 반환한다. 범위는 서비스 공통 상수(이중 방어는 서비스 쪽).
        /// </summary>
        private static double[] ParseSide(string sideName, TextBox[] inputs, StringBuilder errors)
        {
            var values = new double[5];
            bool valid = true;
            for (int i = 0; i < inputs.Length; i++)
            {
                double min = i == 0 ? PickRuntimeOffsetService.MinCutoffFrequency : PickRuntimeOffsetService.MinFilterLimit;
                double max = i == 0 ? PickRuntimeOffsetService.MaxCutoffFrequency : PickRuntimeOffsetService.MaxFilterLimit;

                double value;
                if (!double.TryParse(inputs[i].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    errors.AppendLine(sideName + " " + ItemNames[i] + ": 숫자가 아닙니다. 입력=" + inputs[i].Text);
                    valid = false;
                    continue;
                }

                if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                {
                    errors.AppendLine(
                        sideName + " " + ItemNames[i] + ": 허용 범위(" + F(min) + "~" + F(max) + ")를 벗어났습니다. 입력=" + F(value));
                    valid = false;
                    continue;
                }

                values[i] = value;
            }

            return valid ? values : null;
        }

        /// <summary>PICKER Z 열(3항목: fc/이상치/클램프)을 파싱·범위 검증한다 — ParseSide와 동일 규칙.</summary>
        private double[] ParseZ(StringBuilder errors)
        {
            var values = new double[3];
            bool valid = true;
            for (int i = 0; i < _zInputs.Length; i++)
            {
                double min = i == 0 ? PickerZRuntimeOffsetService.MinCutoffFrequency : PickerZRuntimeOffsetService.MinFilterLimit;
                double max = i == 0 ? PickerZRuntimeOffsetService.MaxCutoffFrequency : PickerZRuntimeOffsetService.MaxFilterLimit;

                double value;
                if (!double.TryParse(_zInputs[i].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    errors.AppendLine("PICKER Z " + ZItemNames[i] + ": 숫자가 아닙니다. 입력=" + _zInputs[i].Text);
                    valid = false;
                    continue;
                }

                if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                {
                    errors.AppendLine(
                        "PICKER Z " + ZItemNames[i] + ": 허용 범위(" + F(min) + "~" + F(max) + ")를 벗어났습니다. 입력=" + F(value));
                    valid = false;
                    continue;
                }

                values[i] = value;
            }

            return valid ? values : null;
        }

        private static bool IsChanged(double[] parsed, double[] loaded)
        {
            if (parsed == null)
                return false;

            for (int i = 0; i < parsed.Length; i++)
            {
                if (Math.Abs(parsed[i] - loaded[i]) > 0.000000001)
                    return true;
            }

            return false;
        }

        private void UpdateAlphaLabels()
        {
            lblPickAlpha.Text = FormatAlpha(tbPickFc.Text);
            lblPlaceAlpha.Text = FormatAlpha(tbPlaceFc.Text);
            lblZAlpha.Text = FormatAlpha(tbZFc.Text);
        }

        private static string FormatAlpha(string fcText)
        {
            double fc;
            if (!double.TryParse(fcText, NumberStyles.Float, CultureInfo.InvariantCulture, out fc) ||
                double.IsNaN(fc) || double.IsInfinity(fc) || fc <= 0.0)
                return "-";

            return F(LowPassFilter.CalculateAlpha(fc));
        }

        private static string F(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
