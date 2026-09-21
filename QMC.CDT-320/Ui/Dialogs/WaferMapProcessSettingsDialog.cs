using QMC.CDT_320.Ui.Localization;
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
    public partial class WaferMapProcessSettingsDialog : Form, ILocalizedView
    {
        private readonly RecipeProject _project;
        private readonly Action<WaferMapProcessSettings, WaferMapProcessSettings, WaferMapProcessSettingsDialog> _saveAndApply;
        public bool InputUsesRemote => cmbInputSource.SelectedIndex == 0;

        private WaferMapProcessSettings _input;
        private WaferMapProcessSettings _output;
        private bool _loading = true;
        private int _role;
        private bool _previewCaptionVisible;
        private int _previewTargetCount;
        private int _previewRotationDegrees;
        private string _previewOriginText;
        private bool _previewHasBinFilter;
        private static readonly WaferMapSourceFormat[] Formats =
        {
            WaferMapSourceFormat.Legacy, WaferMapSourceFormat.Samsung,
            WaferMapSourceFormat.Camtek, WaferMapSourceFormat.Circle, WaferMapSourceFormat.Other
        };

        public WaferMapProcessSettingsDialog(RecipeProject project,
            Action<WaferMapProcessSettings, WaferMapProcessSettings, WaferMapProcessSettingsDialog> saveAndApply)
        {
            InitializeComponent();
            InitializeLanguageBindings();
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _saveAndApply = saveAndApply ?? throw new ArgumentNullException(nameof(saveAndApply));
            _input = WaferMapProcessService.CloneSettings(project.InputMapProcessing);
            _output = WaferMapProcessService.CloneSettings(project.OutputMapProcessing);
            cmbInputSource.SelectedIndex = RecipeInputMapSource.RequestedUsesRemote(project, QMC.CDT320.AppSettingsStore.Current) ? 0 : 1;
            Lang.BindFormat(lblSourceHint, "extraDialog.mapProcess.sourceHint",
                RecipeInputMapSource.DescribeMode(project, QMC.CDT320.AppSettingsStore.Current));
            Lang.BindKey(btnSave, "extraDialog.mapProcess.saveApply");
            Lang.BindFormat(lblRecipe, "extraDialog.mapProcess.recipe", project.FileName);
            mapView.CellTextResolver = entry => entry.LogicalGridX == 0 && entry.LogicalGridY == 0
                ? "1,1" : entry.SequenceNo > 0 ? entry.SequenceNo.ToString(CultureInfo.InvariantCulture) : "";
            mapView.CellColorResolver = entry => entry.LogicalGridX == 0 && entry.LogicalGridY == 0
                ? Color.FromArgb(252, 183, 63) : entry.IsTarget ? Color.FromArgb(133, 189, 221) : Color.FromArgb(222, 226, 232);
            mapView.LegendItemsResolver = () => new[]
            {
                Tuple.Create(Lang.T("extraDialog.mapProcess.target"), Color.FromArgb(133, 189, 221)),
                Tuple.Create(Lang.T("extraDialog.mapProcess.excluded"), Color.FromArgb(222, 226, 232)),
                Tuple.Create(Lang.T("extraDialog.mapProcess.originLegend"), Color.FromArgb(252, 183, 63))
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
            Lang.BindKey(lblFormat, _role == 0 ? "extraDialog.mapProcess.remoteFormat" : "extraDialog.mapProcess.registeredFormat");
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
                _previewCaptionVisible = true;
                _previewTargetCount = ordered.Count;
                _previewRotationDegrees = settings == null ? 0 : settings.RotationDegrees;
                _previewOriginText = cmbOrigin.Text;
                _previewHasBinFilter = _role == 0;
                RefreshPreviewCaption();
                if (ordered.Count == 0)
                    Lang.BindKey(lblSelected, "extraDialog.mapProcess.noTargets");
                else
                    Lang.Bind(lblSelected, "");
                if (ordered.Count > 0) ShowCell(ordered[0]);
            }
            catch (Exception ex)
            {
                mapView.SetMap(null, true);
                lblStatus.ForeColor = Color.Firebrick;
                _previewCaptionVisible = false;
                Lang.BindFormat(lblStatus, "extraDialog.mapProcess.previewError", ex.Message);
                Lang.Bind(lblSelected, "");
            }
        }

        private void ShowMapPreparationRequired(string detail)
        {
            mapView.SetMap(null, true);
            Lang.Bind(lblSelected, "");
            lblStatus.ForeColor = Color.FromArgb(145, 88, 20);
            _previewCaptionVisible = false;
            Lang.BindFormat(lblStatus, "extraDialog.mapProcess.preparationRequired", detail);
        }

        private void ShowCell(DieMapEntry entry)
        {
            if (entry == null) return;
            Lang.BindFormat(lblSelected, "extraDialog.mapProcess.selectedCell",
                WaferMapProcessService.FormatMapPosition(entry), entry.SequenceNo.ToString(CultureInfo.InvariantCulture), entry.BinCode);
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
                QMC.Common.MessageDialog.Show(Lang.Format("extraDialog.mapProcess.applyError", ex.Message),
                    Lang.T("extraDialog.mapProcess.messageTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static string TranslateChoice(string text)
        {
            switch (text)
            {
                case "원격 맵 다운로드": return Lang.T("extraDialog.mapProcess.choice.remoteDownload");
                case "등록 맵 사용 (원격 미사용)": return Lang.T("extraDialog.mapProcess.choice.registeredNoRemote");
                case "입력": return Lang.T("extraDialog.mapProcess.choice.input");
                case "출력 · 양품 미리보기": return Lang.T("extraDialog.mapProcess.choice.goodOutput");
                case "출력 · 불량 미리보기": return Lang.T("extraDialog.mapProcess.choice.ngOutput");
                case "기존 설정 사용": return Lang.T("extraDialog.mapProcess.choice.legacy");
                case "삼성 (RAD)": return Lang.T("extraDialog.mapProcess.choice.samsung");
                case "CAMTEK": return Lang.T("extraDialog.mapProcess.choice.camtek");
                case "서클": return Lang.T("extraDialog.mapProcess.choice.circle");
                case "서클 (등록 생성 맵)": return Lang.T("extraDialog.mapProcess.choice.circleGenerated");
                case "타업체 (미지원 형식)": return Lang.T("extraDialog.mapProcess.choice.other");
                case "등록 맵 사용 · 구분자 미적용": return Lang.T("extraDialog.mapProcess.choice.registeredNoFormat");
                case "0° · 등록 맵 방향 유지": return Lang.T("extraDialog.mapProcess.choice.zeroRotation");
                case "180° 회전": return Lang.T("extraDialog.mapProcess.choice.rotate180");
                case "좌상단 (X→ Y↓)": return Lang.T("extraDialog.mapProcess.choice.topLeft");
                case "좌하단 (X→ Y↑)": return Lang.T("extraDialog.mapProcess.choice.bottomLeft");
                case "우상단 (X← Y↓)": return Lang.T("extraDialog.mapProcess.choice.topRight");
                case "우하단 (X← Y↑)": return Lang.T("extraDialog.mapProcess.choice.bottomRight");
                case "센터 (X→ Y↑)": return Lang.T("extraDialog.mapProcess.choice.center");
                default: return text;
            }
        }

        private void RefreshPreviewCaption()
        {
            if (!_previewCaptionVisible) return;
            Lang.BindFormat(lblStatus, _previewHasBinFilter
                ? "extraDialog.mapProcess.inputPreview"
                : "extraDialog.mapProcess.outputPreview",
                _previewTargetCount, _previewRotationDegrees, TranslateChoice(_previewOriginText));
        }

        public void ApplyLanguage()
        {
            RefreshPreviewCaption();
            mapView.Invalidate();
        }

        private void InitializeLanguageBindings()
        {
            Lang.BindChoices(cmbInputSource, TranslateChoice);
            Lang.BindChoices(cmbRole, TranslateChoice);
            Lang.BindChoices(cmbFormat, TranslateChoice);
            Lang.BindChoices(cmbRotation, TranslateChoice);
            Lang.BindChoices(cmbOrigin, TranslateChoice);
            Lang.BindKey(lblInputSource, "extraDialog.waferMapProcessSettingsDialog.lblInputSource.caption");
            Lang.BindKey(lblRole, "extraDialog.waferMapProcessSettingsDialog.lblRole.caption");
            Lang.BindKey(lblRotation, "extraDialog.waferMapProcessSettingsDialog.lblRotation.caption");
            Lang.BindKey(lblOrigin, "extraDialog.waferMapProcessSettingsDialog.lblOrigin.caption");
            Lang.BindKey(lblNote, "extraDialog.waferMapProcessSettingsDialog.lblNote.caption");
            Lang.BindKey(btnCancel, "extraDialog.waferMapProcessSettingsDialog.btnCancel.caption");
            Lang.BindKey(this, "extraDialog.waferMapProcessSettingsDialog.this.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}
