using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class WaferMapProcessSettingsDialog : Form
    {
        private readonly RecipeProject _project;
        private readonly Action<WaferMapProcessSettings, WaferMapProcessSettings, WaferMapProcessSettingsDialog> _saveAndApply;
        public bool InputUsesRemote => cmbInputSource.SelectedIndex == 0;

        private WaferMapProcessSettings _input;
        private WaferMapProcessSettings _output;
        private bool _loading = true;
        private int _role;
        private static readonly WaferMapSourceFormat[] Formats =
        {
            WaferMapSourceFormat.Legacy, WaferMapSourceFormat.Samsung,
            WaferMapSourceFormat.Camtek, WaferMapSourceFormat.Circle, WaferMapSourceFormat.Other
        };

        public WaferMapProcessSettingsDialog(RecipeProject project,
            Action<WaferMapProcessSettings, WaferMapProcessSettings, WaferMapProcessSettingsDialog> saveAndApply)
        {
            InitializeComponent();
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _saveAndApply = saveAndApply ?? throw new ArgumentNullException(nameof(saveAndApply));
            _input = WaferMapProcessService.CloneSettings(project.InputMapProcessing);
            _output = WaferMapProcessService.CloneSettings(project.OutputMapProcessing);
            cmbInputSource.SelectedIndex = RecipeInputMapSource.UsesRemote(project, QMC.CDT320.AppSettingsStore.Current) ? 0 : 1;
            lblSourceHint.Text = project.InputUseRemoteWaferMap.HasValue
                ? "입력 사용 모드는 레시피별로 저장됩니다. 출력은 항상 등록 맵을 사용합니다."
                : "기존 공통 설정을 불러왔습니다. 저장하면 이 레시피의 사용 모드로 고정됩니다.";
            lblRecipe.Text = "레시피: " + project.FileName + "   ·   입력 / 출력 설정";
            mapView.CellTextResolver = entry => entry.LogicalGridX == 0 && entry.LogicalGridY == 0
                ? "1,1" : entry.SequenceNo > 0 ? entry.SequenceNo.ToString(CultureInfo.InvariantCulture) : "";
            mapView.CellColorResolver = entry => entry.LogicalGridX == 0 && entry.LogicalGridY == 0
                ? Color.FromArgb(252, 183, 63) : entry.IsTarget ? Color.FromArgb(133, 189, 221) : Color.FromArgb(222, 226, 232);
            mapView.LegendItemsResolver = () => new[]
            {
                Tuple.Create("대상", Color.FromArgb(133, 189, 221)),
                Tuple.Create("제외", Color.FromArgb(222, 226, 232)),
                Tuple.Create("기준 1,1", Color.FromArgb(252, 183, 63))
            };
            mapView.CellClicked += ShowCell;
            cmbRole.SelectedIndex = 0;
            LoadRole();
        }

        private void cmbRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            SaveRole();
            _role = cmbRole.SelectedIndex;
            LoadRole();
        }

        private void LoadRole()
        {
            _loading = true;
            WaferMapProcessSettings settings = (_role == 0 ? _input : _output) ?? new WaferMapProcessSettings();
            WaferMapSourceFormat format = settings.Format == WaferMapSourceFormat.Rad ? WaferMapSourceFormat.Samsung : settings.Format;
            cmbFormat.Items.Clear();
            if (_role == 0)
            {
                cmbFormat.Items.AddRange(new object[] { "기존 설정 사용", "삼성 (RAD)", "CAMTEK", "서클", "타업체 (미지원 형식)" });
                cmbFormat.SelectedIndex = Array.IndexOf(Formats, format);
            }
            else
            {
                cmbFormat.Items.Add("등록 맵 사용 · 구분자 미적용");
                cmbFormat.SelectedIndex = 0;
            }
            cmbFormat.Enabled = _role == 0 && InputUsesRemote;
            lblFormat.Text = _role == 0 ? "원격 맵 구분자" : "등록 맵 사용";
            cmbRotation.SelectedIndex = settings.RotationDegrees == 180 ? 1 : settings.RotationDegrees == 0 ? 0 : -1;
            cmbOrigin.SelectedIndex = (int)settings.GridOrigin;
            _loading = false;
            RefreshPreview();
        }

        private WaferMapProcessSettings ReadSettings()
        {
            if (cmbFormat.SelectedIndex < 0 || cmbOrigin.SelectedIndex < 0 || cmbRotation.SelectedIndex < 0)
                throw new InvalidDataException("맵 구분자·회전·원점을 모두 선택하세요.");
            var settings = new WaferMapProcessSettings
            {
                // 이전 Output 저장 키는 보존하되 형식 필터로 사용하지 않는다.
                Format = _role == 0 ? Formats[cmbFormat.SelectedIndex] : (_output != null ? _output.Format : WaferMapSourceFormat.Legacy),
                RotationDegrees = cmbRotation.SelectedIndex == 1 ? 180 : 0,
                GridOrigin = (WaferMapGridOrigin)cmbOrigin.SelectedIndex
            };
            return settings.Format == WaferMapSourceFormat.Legacy && settings.RotationDegrees == 0 &&
                settings.GridOrigin == WaferMapGridOrigin.TopLeft ? null : settings;
        }

        private void SaveRole()
        {
            if (_role == 0) _input = ReadSettings(); else _output = ReadSettings();
        }

        private void settings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading)
            {
                cmbFormat.Enabled = _role == 0 && InputUsesRemote;
                RefreshPreview();
            }
        }

        private void RefreshPreview()
        {
            try
            {
                WaferMapProcessSettings settings = ReadSettings();
                RecipeMapKind kind = _role == 0 ? RecipeMapKind.Input : _role == 1 ? RecipeMapKind.GoodBin : RecipeMapKind.NgBin;
                string configured = RecipeMapPaths.ConfiguredFileName(_project, kind);
                string path = RecipeMapPaths.ResolveConfiguredPath(configured);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    ShowMapPreparationRequired("등록된 역할 맵이 없습니다.");
                    return;
                }
                DieMap source = DieMapGenerator.Load(path);
                if (source == null) throw new InvalidDataException("등록된 맵 파일을 읽을 수 없습니다.");
                PickupSubset pickup = _role == 0 ? (_project.InputPickup ?? _project.Pickup) : (_project.OutputPickup ?? _project.Pickup);
                var bins = _role == 0 ? RecipeMapPreviewService.LoadSavedInputBins(_project.FileName) : null;
                DieMap map = RecipeMapPreviewService.Create(source, settings, pickup, kind, bins).Map;
                mapView.SetMap(map, true);
                var ordered = map.Entries.Where(entry => entry.IsTarget && entry.SequenceNo > 0).OrderBy(entry => entry.SequenceNo).ToList();
                lblStatus.ForeColor = Color.FromArgb(35, 60, 80);
                lblStatus.Text = "등록 맵 미리보기 · 대상 " + ordered.Count + "개 · 회전 " + (settings == null ? 0 : settings.RotationDegrees) +
                    "° · 1,1 기준 " + cmbOrigin.Text + "\r\n" +
                    "회전·원점과 작업 대상/제외" + (_role == 0 ? "·레시피 BIN 필터" : "") +
                    "를 반영한 공정 순서입니다. 주황색 셀은 1,1입니다.";
                lblSelected.Text = ordered.Count == 0 ? "현재 설정으로 공정 대상이 없습니다." : "";
                if (ordered.Count > 0) ShowCell(ordered[0]);
            }
            catch (Exception ex)
            {
                mapView.SetMap(null, true);
                lblStatus.ForeColor = Color.Firebrick;
                lblStatus.Text = "미리보기 확인: " + ex.Message;
                lblSelected.Text = "";
            }
        }

        private void ShowMapPreparationRequired(string detail)
        {
            mapView.SetMap(null, true);
            lblSelected.Text = "";
            lblStatus.ForeColor = Color.FromArgb(145, 88, 20);
            lblStatus.Text = "맵 준비 필요 · 구분자/회전/원점 설정은 먼저 저장할 수 있습니다.\r\n" + detail +
                "\r\n등록 모드는 웨이퍼 맵 불러오기 또는 맵 생성을 진행하세요. 구분자는 원격 입력 다운로드에만 적용됩니다.";
        }

        private void ShowCell(DieMapEntry entry)
        {
            if (entry == null) return;
            lblSelected.Text = string.Format(CultureInfo.InvariantCulture,
                "{0}  |  공정 #{1}  |  BIN {2}",
                WaferMapProcessService.FormatMapPosition(entry), entry.SequenceNo, entry.BinCode);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveRole();
                _saveAndApply(_input, _output, this);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferMapProcessSettings", "공정 맵 설정 저장/적용 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show("공정 맵 설정을 적용하지 못했습니다.\r\n" + ex.Message,
                    "레시피 맵 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
