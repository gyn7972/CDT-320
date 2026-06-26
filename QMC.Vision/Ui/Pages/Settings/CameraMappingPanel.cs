using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 알고리즘 1개의 카메라 매핑 + 카메라 파라미터 편집 패널.
    /// Stage 96 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 콤보채움·바인딩·연결/라이브 로직은 Code.
    /// </summary>
    public partial class CameraMappingPanel : UserControl
    {
        private string _algorithm;
        private bool _suspendBinding;

        // C1 — 카메라 설정 SSOT = 모듈(BaseUnit) Config/Recipe. 패널은 AlgorithmCameraMapping 을
        // 워킹 버퍼로만 쓰고, 로드는 module.ExportCameraMapping / 저장은 module.ImportCameraMapping+SaveSettings.
        private AlgorithmCameraMapping _buffer;

        /// <summary>현재 알고리즘의 운영 모듈(Form1) — 미해결(테스트/디자인 등) 시 null.</summary>
        private Modules.IVisionModule Module()
            => string.IsNullOrEmpty(_algorithm) ? null : (FindForm() as Form1)?.ResolveModule(_algorithm);

        /// <summary>현재 활성 레시피명(Machine.CurrentRecipeName). 미해결 시 "default".
        /// 카메라 Recipe(노출 등 품목별)는 이 레시피로 저장/로드한다. 설비 고정 Config 는 SaveSettings 로 전역 1벌.</summary>
        private string ActiveRecipeName()
            => (FindForm() as Form1)?.Machine?.CurrentRecipeName ?? "default";

        // ── 연결 상태 ──
        private ICamera _activeCam;
        private bool _activeCamOwned;
        private bool _isLive;
        private int _uiPending;   // 라이브 프레임 UI 적체 방지(이전 프레임 처리 중이면 새 프레임 드롭)
        private DateTime _fpsT0 = DateTime.Now;
        private int _fpsCount;
        private SynchronizationContext _uiCtx;

        // MVS 확장 파라미터 — 그룹별 접이식 그리드(런타임 생성, 기본 접힘). 코어 파라미터 그리드와 분리.
        private ParameterGridControl _mfsGrid;   // .mfs 파일 경로 + 불러오기/저장(기본 펼침)
        private ParameterGridControl _imgGrid;
        private ParameterGridControl _acqGrid;
        private ParameterGridControl _ioGrid;

        public CameraMappingPanel()
        {
            InitializeComponent();
            // __COLLAPSIBLE_WRAP__
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                QMC.Vision.Ui.Controls.CollapsibleGrids.Wrap(this._gridLightAssign, Lang.T("set.cam.secLight"));
            }
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _uiCtx = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _paramGrid.ParameterValueChanged += OnParamChanged;
            _scaleGrid.ParameterValueChanged += OnScaleParamChanged;
            _camPreview.ShowToolbar = true;   // 공용 CameraView 내장 툴바
            UpdateConnectButtons();
            WireLightGrid();
            InitNodeGroupGrids();
            ApplyWaferLayout();
            Lang.LanguageChanged += OnCamLangChanged;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { Lang.LanguageChanged -= OnCamLangChanged; } catch { }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 — 그리드 항목명/헤더 재구성 + 태그 컨트롤 재번역(버퍼 유지, 조명지정 미변경).</summary>
        private void OnCamLangChanged()
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (!string.IsNullOrEmpty(_algorithm))
                    {
                        _lblAlgorithm.Text = Lang.T("set.cam.title") + " — " + Lang.Algo(_algorithm) + "  (" + _algorithm + ")";
                        BindFields();
                    }
                    Lang.Apply(this);
                    ApplyWaferLayout();
                }));
            }
            catch { }
        }

        /// <summary>카메라 파라미터 항목 목록 — Handler ParameterGridItem 팩토리 규칙대로 buffer 에 getter/setter 바인딩.
        /// 항목 추가/삭제는 이 메소드에서 줄 단위로(고정 폼 재배치 불필요).</summary>
        private List<ParameterGridItem> BuildParamItems(AlgorithmCameraMapping m)
        {
            // Trigger: 저장값 m.TriggerMode(CameraTriggerMode 이름) ↔ UI(Mode On/Off + Source) 분해/합성 — 기존 규칙 보존.
            Func<bool> isOff = () => string.IsNullOrEmpty(m.TriggerMode)
                                        || m.TriggerMode == nameof(CameraTriggerMode.Continuous);
            Func<string> curSrc = () => isOff() ? nameof(CameraTriggerMode.Software) : m.TriggerMode;

            var srcOptions = Enum.GetNames(typeof(CameraTriggerMode))
                .Where(v => v != nameof(CameraTriggerMode.Continuous))
                .Select(v => new ParameterGridOption(v, v));
            var pixOptions = Enum.GetNames(typeof(CameraPixelFormat))
                .Select(v => new ParameterGridOption(v, v));

            var items = new List<ParameterGridItem>
            {
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.exposure"), "μs", ParameterGridScope.Recipe,
                    () => m.ExposureUs, v => m.ExposureUs = v), 1, 1000000),
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.gain"), "dB", ParameterGridScope.Recipe,
                    () => m.Gain, v => m.Gain = v), 0, 48),
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.frameRate"), "fps", ParameterGridScope.Recipe,
                    () => m.FrameRate, v => m.FrameRate = v), 1, 1000),
                ParameterGridItem.Selection(Lang.T("set.cam.trigMode"), "", ParameterGridScope.Config,
                    () => isOff() ? "Off" : "On",
                    v => { if ((string)v == "On") m.TriggerMode = curSrc();
                           else m.TriggerMode = nameof(CameraTriggerMode.Continuous); },
                    new[] { new ParameterGridOption("Off", "Off"), new ParameterGridOption("On", "On") }),
                ParameterGridItem.Selection(Lang.T("set.cam.trigSrc"), "", ParameterGridScope.Config,
                    () => curSrc(),
                    v => { if (!isOff()) m.TriggerMode = (string)v; },
                    srcOptions),
                ParameterGridItem.Selection(Lang.T("set.cam.pixFmt"), "", ParameterGridScope.Config,
                    () => m.PixelFormat ?? "Mono8",
                    v => m.PixelFormat = (string)v,
                    pixOptions),
                // 그랩 전 지연: MVS 카메라 노드가 아닌 핸들러 그랩 직전 대기(소프트웨어) — (SW) 표기로 구분.
                WithRange(ParameterGridItem.Int(Lang.T("set.cam.delayGrab") + " (SW)", "ms", ParameterGridScope.Recipe,
                    () => m.DelayBeforeGrabMs, v => m.DelayBeforeGrabMs = v), 0, 60000),
                WithRange(ParameterGridItem.Int(Lang.T("set.cam.roiOffX"), "px", ParameterGridScope.Recipe,
                    () => m.RoiOffsetX, v => m.RoiOffsetX = v), 0, 8000),
                WithRange(ParameterGridItem.Int(Lang.T("set.cam.roiOffY"), "px", ParameterGridScope.Recipe,
                    () => m.RoiOffsetY, v => m.RoiOffsetY = v), 0, 8000),
                WithRange(ParameterGridItem.Int(Lang.T("set.cam.roiW"), "px", ParameterGridScope.Recipe,
                    () => m.RoiWidth, v => m.RoiWidth = v), 0, 8000),
                WithRange(ParameterGridItem.Int(Lang.T("set.cam.roiH"), "px", ParameterGridScope.Recipe,
                    () => m.RoiHeight, v => m.RoiHeight = v), 0, 8000),
            };
            // 코어 카메라 파라미터(노출/게인/트리거/픽셀/ROI)만 이 그리드에 둔다.
            // MVS 확장 파라미터는 그룹별 접이식 그리드(_imgGrid/_acqGrid/_ioGrid)로 분리.
            return items;
        }

        /// <summary>카메라 노드 카탈로그(<see cref="QMC.Vision.Config.CameraNodeCatalog"/>) → 파라미터 그리드 항목.
        /// <paramref name="group"/> 에 해당하는 행만 생성한다(그룹별 접이식 그리드용).
        /// 각 행은 mapping 의 NodeParams(노드명↔값)에 get/set 바인딩된다. 미저장 노드는 카탈로그 기본값을 표시한다.
        /// 항목 추가/삭제는 카탈로그(<c>All</c>)에서 줄 단위로 — 이 코드는 수정 불필요.</summary>
        private List<ParameterGridItem> BuildNodeCatalogItems(AlgorithmCameraMapping m, string group)
        {
            var list = new List<ParameterGridItem>();
            foreach (var def in QMC.Vision.Config.CameraNodeCatalog.All)
            {
                if (!string.Equals(def.Group, group, StringComparison.OrdinalIgnoreCase)) continue;
                var d = def;   // 클로저 캡처 안전
                string label = d.Label;   // 그룹명은 그리드 제목바에 표시되므로 라벨엔 미포함
                switch (d.Kind)
                {
                    case CameraParamKind.Float:
                        list.Add(WithRange(ParameterGridItem.Double(label, d.Unit, ParameterGridScope.Config,
                            () => GetNodeDouble(m, d), v => m.SetNode(d.Node, v.ToString(CultureInfo.InvariantCulture))),
                            d.Min, d.Max));
                        break;
                    case CameraParamKind.Int:
                        list.Add(WithRange(ParameterGridItem.Int(label, d.Unit, ParameterGridScope.Config,
                            () => GetNodeInt(m, d), v => m.SetNode(d.Node, v.ToString(CultureInfo.InvariantCulture))),
                            d.Min, d.Max));
                        break;
                    case CameraParamKind.Bool:
                        list.Add(ParameterGridItem.Bool(label, ParameterGridScope.Config,
                            () => GetNodeBool(m, d), v => m.SetNode(d.Node, v ? "True" : "False")));
                        break;
                    case CameraParamKind.Enum:
                        var opts = (d.Options ?? new string[0]).Select(o => new ParameterGridOption(o, o));
                        list.Add(ParameterGridItem.Selection(label, d.Unit, ParameterGridScope.Config,
                            () => m.GetNode(d.Node) ?? d.Default,
                            v => m.SetNode(d.Node, (string)v),
                            opts));
                        break;
                    // Command 타입은 현재 카탈로그에 없음 — 필요 시 Action 행으로 추가.
                }
            }
            return list;
        }

        private static double GetNodeDouble(AlgorithmCameraMapping m, QMC.Vision.Config.CameraNodeDef d)
        {
            var s = m.GetNode(d.Node) ?? d.Default;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static int GetNodeInt(AlgorithmCameraMapping m, QMC.Vision.Config.CameraNodeDef d)
        {
            var s = m.GetNode(d.Node) ?? d.Default;
            return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static bool GetNodeBool(AlgorithmCameraMapping m, QMC.Vision.Config.CameraNodeDef d)
        {
            var s = m.GetNode(d.Node) ?? d.Default;
            if (bool.TryParse(s, out var b)) return b;
            return s == "1" || string.Equals(s, "on", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>.mfs(MVS Feature Save) 경로 + 불러오기/저장 버튼 행. 카메라 전체 노드값 일괄 적용/저장.</summary>
        private List<ParameterGridItem> BuildMfsItems(AlgorithmCameraMapping m)
        {
            return new List<ParameterGridItem>
            {
                ParameterGridItem.FilePath("카메라 설정 파일(.mfs)", ParameterGridScope.Config,
                    () => m.MvsFeatureFilePath ?? "", v => m.MvsFeatureFilePath = v?.Trim() ?? "",
                    "MVS Feature 파일 (*.mfs)|*.mfs|모든 파일 (*.*)|*.*"),
                ParameterGridItem.Action(".mfs → 카메라 적용", "불러오기", ParameterGridScope.Config, LoadMfsToCamera),
                ParameterGridItem.Action("카메라 → .mfs 저장(파일)", "파일저장", ParameterGridScope.Config, SaveCameraToMfs),
                ParameterGridItem.Action("카메라에 영구 저장(UserSet1)", "카메라저장", ParameterGridScope.Config, SaveToCameraUserSet),
            };
        }

        /// <summary>지정된 .mfs 파일을 연결된 카메라에 일괄 적용(FeatureLoad). 연결/정지 상태 확인.</summary>
        private void LoadMfsToCamera()
        {
            try
            {
                var m = CurrentMapping();
                if (m == null) return;
                string path = m.MvsFeatureFilePath;
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = ".mfs 경로가 비었거나 파일이 없습니다."; return; }
                if (_activeCam == null || !_activeCam.IsOpen)
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "먼저 Connect 후 .mfs를 적용하세요."; return; }
                if (_isLive)
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "Live 중에는 .mfs 적용 불가 — Live Stop 후 시도하세요."; return; }

                if (_activeCam.LoadFeatures(path, out var err))
                { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = ".mfs 적용 완료 — " + System.IO.Path.GetFileName(path); }
                else
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = ".mfs 적용 실패: " + err; }
            }
            catch (Exception ex)
            { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = ".mfs 적용 예외: " + ex.Message; }
        }

        /// <summary>현재 연결된 카메라의 전체 노드값을 .mfs로 저장(FeatureSave). 경로 미지정 시 다이얼로그.</summary>
        private void SaveCameraToMfs()
        {
            try
            {
                var m = CurrentMapping();
                if (m == null) return;
                if (_activeCam == null || !_activeCam.IsOpen)
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "먼저 Connect 후 .mfs로 저장하세요."; return; }

                string path = m.MvsFeatureFilePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Filter = "MVS Feature 파일 (*.mfs)|*.mfs|모든 파일 (*.*)|*.*";
                        dlg.Title = "카메라 설정 .mfs 저장";
                        dlg.FileName = (_activeCam.Info?.Model ?? "camera") + ".mfs";
                        if (dlg.ShowDialog(this) != DialogResult.OK) return;
                        path = dlg.FileName;
                        m.MvsFeatureFilePath = path;
                        try { _mfsGrid?.RefreshValues(); } catch { }
                    }
                }

                if (_activeCam.SaveFeatures(path, out var err))
                { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = ".mfs 저장 완료 — " + System.IO.Path.GetFileName(path) + "  (경로 영속하려면 [저장] 클릭)"; }
                else
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = ".mfs 저장 실패: " + err; }
            }
            catch (Exception ex)
            { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = ".mfs 저장 예외: " + ex.Message; }
        }

        /// <summary>현재 카메라 값을 카메라 내부 UserSet1(플래시)에 영구 저장 — 전원 꺼도 유지. 확인 후 실행.</summary>
        private void SaveToCameraUserSet()
        {
            try
            {
                if (_activeCam == null || !_activeCam.IsOpen)
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "먼저 Connect 후 카메라에 저장하세요."; return; }
                if (_isLive)
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "Live 중에는 카메라 저장 불가 — Live Stop 후 시도하세요."; return; }

                var confirm = MessageBox.Show(this,
                    "현재 카메라 값을 카메라 내부 UserSet1에 영구 저장합니다.\n전원을 꺼도 유지되며, 부팅 시 UserSet1로 로드됩니다. 계속할까요?",
                    "카메라에 영구 저장", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                if (_activeCam.SaveToCameraUserSet("UserSet1", out var err))
                { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = "카메라 UserSet1에 영구 저장 완료(전원 꺼도 유지)."; }
                else
                { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "카메라 저장 실패: " + err; }
            }
            catch (Exception ex)
            { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "카메라 저장 예외: " + ex.Message; }
        }

        /// <summary>스케일/좌표변환 항목 — 전용 그리드(_scaleGrid). 모듈별 Config 스코프.</summary>
        private List<ParameterGridItem> BuildScaleItems(AlgorithmCameraMapping m)
        {
            return new List<ParameterGridItem>
            {
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.scaleX"), "mm/px", ParameterGridScope.Config,
                    () => m.ScaleX, v => m.ScaleX = v), 0.0000001, 1000),
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.scaleY"), "mm/px", ParameterGridScope.Config,
                    () => m.ScaleY, v => m.ScaleY = v), 0.0000001, 1000),
                ParameterGridItem.Bool(Lang.T("set.cam.invX"), ParameterGridScope.Config,
                    () => m.InvertedX, v => m.InvertedX = v),
                ParameterGridItem.Bool(Lang.T("set.cam.invY"), ParameterGridScope.Config,
                    () => m.InvertedY, v => m.InvertedY = v),
                ParameterGridItem.Bool(Lang.T("set.cam.rot90"), ParameterGridScope.Config,
                    () => m.IsRotated, v => m.IsRotated = v),
                ParameterGridItem.Bool(Lang.T("set.cam.returnMm"), ParameterGridScope.Config,
                    () => m.ReturnMmCoordinates, v => m.ReturnMmCoordinates = v),

                // 캘리브레이션 입력 — '스케일 계산' 버튼이 이 값으로 산출
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.chipW"), "mm", ParameterGridScope.Config,
                    () => m.CalibChipWidthMm, v => m.CalibChipWidthMm = v), 0, 1000),
                WithRange(ParameterGridItem.Double(Lang.T("set.cam.chipH"), "mm", ParameterGridScope.Config,
                    () => m.CalibChipHeightMm, v => m.CalibChipHeightMm = v), 0, 1000),

                // 모듈 시뮬 이미지 — 핸들러 GRAB 시 카메라 대신 이 이미지를 그랩(테스트용)
                ParameterGridItem.Bool("시뮬 이미지 사용(GRAB)", ParameterGridScope.Config,
                    () => m.SimUseSavedImage, v => m.SimUseSavedImage = v),
                ParameterGridItem.FilePath("시뮬 이미지 경로(GRAB)", ParameterGridScope.Config,
                    () => m.SimSavedImagePath ?? "", v => m.SimSavedImagePath = v?.Trim() ?? "",
                    "이미지 파일 (*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일 (*.*)|*.*"),
            };
        }

        /// <summary>숫자 항목에 범위 Validator 부착(기존 NumericUpDown Min/Max 보존).</summary>
        private static ParameterGridItem WithRange(ParameterGridItem item, double min, double max)
        {
            item.Validator = o => { double d = Convert.ToDouble(o); return d >= min && d <= max; };
            return item;
        }

        /// <summary>그리드 값 변경 — 상호의존(Trigger Mode/Source) 표시 동기화.</summary>
        private void OnParamChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try { (sender as ParameterGridControl)?.RefreshValues(); } catch { }
        }

        private void OnScaleParamChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try { _scaleGrid.RefreshValues(); } catch { }
            // X반전/Y반전/90°회전 토글 → 미리보기에 즉시 반영(저장 전에도 확인 가능).
            UpdateDisplayOrientation();
        }

        /// <summary>현재 버퍼의 X반전/Y반전/90°회전 플래그를 미리보기(_camPreview) 표시 방향변환에 반영.
        /// 그랩/라이브로 들어오는 이미지가 이 방향으로 표시된다. 저장 시 모듈 Config 로 영속되어 재로드 후에도 유지.</summary>
        private void UpdateDisplayOrientation()
        {
            if (_camPreview == null) return;
            var m = CurrentMapping();
            if (m == null) { _camPreview.DisplayOrientation = System.Drawing.RotateFlipType.RotateNoneFlipNone; return; }
            _camPreview.DisplayOrientation = CameraView.OrientationFromFlags(m.InvertedX, m.InvertedY, m.IsRotated);
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnAnyFieldChanged(object sender, EventArgs e) => OnFieldChanged();
        private void OnDiscoverClick(object sender, EventArgs e) => DiscoverCameras();
        private void OnMilDcfCheckedChanged(object sender, EventArgs e) { UpdateMilDcfVisibility(); OnMilFieldChanged(); }
        private void OnMilTextChanged(object sender, EventArgs e) => OnMilFieldChanged();
        private void OnMilBrowseClick(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "DCF 파일 (*.dcf)|*.dcf|모든 파일 (*.*)|*.*";
                dlg.Title = "MIL DCF 파일 선택";
                try
                {
                    string cur = _txtMilDcf?.Text;
                    if (!string.IsNullOrWhiteSpace(cur))
                    {
                        string dir = System.IO.Path.GetDirectoryName(cur);
                        if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                            dlg.InitialDirectory = dir;
                        if (System.IO.File.Exists(cur)) dlg.FileName = System.IO.Path.GetFileName(cur);
                    }
                }
                catch { }
                if (dlg.ShowDialog(this) == DialogResult.OK && _txtMilDcf != null)
                    _txtMilDcf.Text = dlg.FileName;   // TextChanged → OnMilTextChanged → cfg.MilDcfPath 저장
            }
        }
        private void OnScaleCalcClick(object sender, EventArgs e) => CalcScaleFromChip();
        private void OnSaveClick(object sender, EventArgs e) => SaveAll();
        private void OnLoadClick(object sender, EventArgs e) => LoadFromDisk();
        private void OnCancelClick(object sender, EventArgs e) => CancelChanges();
        private void OnResetClick(object sender, EventArgs e) => ResetToDefaults();
        private void OnApplyClick(object sender, EventArgs e) => ApplyToRunningModule();
        private void OnConnectClick(object sender, EventArgs e) => ToggleConnect();
        // 테스트그랩/Live는 CameraView 내장 툴바로 통일(중복 버튼 제거). Connect 만 유지.

        // ── 조명 컨트롤러/페이지 지정 (모듈 Setup.LightPages) ──
        // 카메라=조명 1:1 하드웨어이므로 모듈 노드(이 패널)에서 지정. 채널 레벨은 검사별([레시피]).
        // 편집은 그리드에만 반영, 저장(SaveAll)/취소(CancelChanges)와 함께 영속/되돌림.
        private bool _suspendLight;

        private void WireLightGrid()
        {
            if (_gridLightAssign == null) return;
            _gridLightAssign.EditMode = DataGridViewEditMode.EditOnEnter;
            // 콤보 선택 즉시 셀 값 커밋(미커밋이면 수집 시 구 값 0 을 읽는 버그 방지).
            _gridLightAssign.CurrentCellDirtyStateChanged += OnLightGridDirty;
        }

        private void OnLightGridDirty(object sender, EventArgs e)
        {
            if (_gridLightAssign.IsCurrentCellDirty) _gridLightAssign.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void OnLightGridDataError(object sender, DataGridViewDataErrorEventArgs e) => e.ThrowException = false;

        private void OnLightCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_suspendLight) return;
            // 컨트롤러 변경 시: 그 행 Page 셀 콤보 items 를 새 컨트롤러 PageCount 로 재구성(표시 정합). 범위 밖 page 만 0.
            if (e.RowIndex >= 0 && !_gridLightAssign.Rows[e.RowIndex].IsNewRow
                && _gridLightAssign.Columns[e.ColumnIndex].Name == "ControllerPort")
            {
                _suspendLight = true;
                try
                {
                    string port = _gridLightAssign.Rows[e.RowIndex].Cells["ControllerPort"].Value as string;
                    _gridLightAssign.Rows[e.RowIndex].Cells["LightName"].Value = GetLightName(port);
                    int pc = SetLightPageCellItems(e.RowIndex, port);
                    int cur = 0; int.TryParse(_gridLightAssign.Rows[e.RowIndex].Cells["Page"].Value?.ToString(), out cur);
                    _gridLightAssign.Rows[e.RowIndex].Cells["Page"].Value = ((cur >= 0 && cur <= pc - 1) ? cur : 0).ToString();
                }
                catch { }
                _suspendLight = false;
            }
        }

        private void OnLightRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (_suspendLight) return;
            SetLightStatus("지정 변경됨 — [저장] 클릭 시 모듈에 반영.", false);
        }

        /// <summary>포트로 컨트롤러의 사람용 이름(Name) 조회 — 인벤토리(LightSystemSetup)에서. 없으면 빈 문자열.</summary>
        private static string GetLightName(string port)
        {
            if (string.IsNullOrEmpty(port)) return "";
            var ce = LightSystemSetupStore.Current?.GetController(port);
            return ce?.Name ?? "";
        }

        /// <summary>그 행 Page 셀(콤보) items = 컨트롤러 PageCount("0".."N-1", string). int items 는 FormattedValue 표시가 깨짐. PageCount 반환.</summary>
        private int SetLightPageCellItems(int rowIndex, string port)
        {
            var cell = _gridLightAssign.Rows[rowIndex].Cells["Page"] as DataGridViewComboBoxCell;
            var ce = LightSystemSetupStore.Current?.GetController(port);
            int pc = (ce != null && ce.PageCount > 0) ? ce.PageCount : 1;
            if (cell != null)
            {
                cell.Items.Clear();
                for (int p = 0; p < pc; p++) cell.Items.Add(p.ToString());
            }
            return pc;
        }

        /// <summary>컨트롤러 콤보 = 인벤토리 PortName, 페이지 콤보 = 0 ~ (전 컨트롤러 최대 PageCount-1) superset(string).</summary>
        private void RefreshLightCombos()
        {
            ControllerPort.Items.Clear();
            int maxPage = 1;
            var ctrls = LightSystemSetupStore.Current?.Controllers ?? new List<LightControllerEntry>();
            foreach (var c in ctrls)
            {
                if (!string.IsNullOrEmpty(c.PortName)) ControllerPort.Items.Add(c.PortName);
                if (c.PageCount > maxPage) maxPage = c.PageCount;
            }
            Page.Items.Clear();
            for (int p = 0; p < maxPage; p++) Page.Items.Add(p.ToString());
        }

        private void EnsureLightCtrlItem(string port)
        {
            if (string.IsNullOrEmpty(port)) return;
            if (!ControllerPort.Items.Contains(port)) ControllerPort.Items.Add(port);
        }

        /// <summary>모듈 Setup.LightPages → 그리드 행 바인딩.</summary>
        private void BindLightAssign()
        {
            var mod = Module();
            var msetup = mod?.Setup as Modules.VisionModuleSetupBase;
            if (msetup == null)   // 모듈 미해결: 비활성 + 명시 메시지
            {
                _suspendLight = true;
                try { _gridLightAssign.Rows.Clear(); } catch { }
                _suspendLight = false;
                _gridLightAssign.Enabled = false;
                SetLightStatus("조명 지정 불러올 수 없음 — 운영 모듈 미해결", true);
                return;
            }
            _gridLightAssign.Enabled = true;
            RefreshLightCombos();

            var pages = msetup.LightPages ?? new List<LightPageRef>();
            _suspendLight = true;
            try
            {
                _gridLightAssign.Rows.Clear();
                foreach (var pr in pages)
                {
                    EnsureLightCtrlItem(pr.ControllerPort);
                    int idx = _gridLightAssign.Rows.Add();
                    _gridLightAssign.Rows[idx].Cells["ControllerPort"].Value = pr.ControllerPort;
                    _gridLightAssign.Rows[idx].Cells["LightName"].Value = GetLightName(pr.ControllerPort);
                    SetLightPageCellItems(idx, pr.ControllerPort);
                    _gridLightAssign.Rows[idx].Cells["Page"].Value = pr.Page.ToString();
                }
            }
            finally { _suspendLight = false; }
            SetLightStatus(pages.Count == 0 ? "지정 없음 — 행을 추가해 컨트롤러/페이지를 지정하세요." : "", false);
        }

        /// <summary>그리드 → LightPageRef 목록(컨트롤러 PageCount 초과 page 보정, (port,page) 중복 제거).</summary>
        private List<LightPageRef> CollectLightPages()
        {
            // 진행 중 콤보 편집 확정.
            if (_gridLightAssign.IsCurrentCellInEditMode && _gridLightAssign.CurrentCell != null
                && _gridLightAssign.EditingControl is ComboBox ec && ec.SelectedItem != null)
                try { _gridLightAssign.CurrentCell.Value = ec.SelectedItem; } catch { }
            try { _gridLightAssign.EndEdit(); } catch { }

            var list = new List<LightPageRef>();
            foreach (DataGridViewRow r in _gridLightAssign.Rows)
            {
                if (r.IsNewRow) continue;
                string port = r.Cells["ControllerPort"].Value as string;
                if (string.IsNullOrEmpty(port)) continue;
                int page = 0;
                int.TryParse(r.Cells["Page"].Value?.ToString(), out page);
                var ce = LightSystemSetupStore.Current?.GetController(port);
                if (page < 0) page = 0;
                if (ce != null && ce.PageCount > 0 && page > ce.PageCount - 1) page = ce.PageCount - 1;
                if (!list.Any(x => string.Equals(x.ControllerPort, port, StringComparison.OrdinalIgnoreCase) && x.Page == page))
                    list.Add(new LightPageRef { ControllerPort = port, Page = page });
            }
            return list;
        }

        private void SetLightStatus(string msg, bool err)
        {
            if (_lblLightStatus == null) return;
            _lblLightStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray;
            _lblLightStatus.Text = msg;
        }

        public void SelectAlgorithm(string algorithm)
        {
            _algorithm = algorithm;
            _buffer = null;   // 모듈 Config/Recipe 에서 새로 로드
            _lblAlgorithm.Text = Lang.T("set.cam.title") + " — " + Lang.Algo(algorithm) + "  (" + algorithm + ")";
            BindFields();
            BindLightAssign();
            ResetScrollAsync();
        }

        /// <summary>Container 의 ActiveControl 변경 영향이 끝난 뒤 scroll position 을 원점으로 강제 복귀.</summary>
        private void ResetScrollAsync()
        {
            if (_leftScroll == null) return;
            BeginInvoke((Action)(() =>
            {
                try { _leftScroll.AutoScrollPosition = Point.Empty; } catch { }
            }));
        }

        private AlgorithmCameraMapping CurrentMapping()
        {
            if (string.IsNullOrEmpty(_algorithm)) return null;
            if (_buffer == null || !string.Equals(_buffer.Algorithm, _algorithm, StringComparison.OrdinalIgnoreCase))
                _buffer = LoadBuffer();
            return _buffer;
        }

        /// <summary>모듈 Config/Recipe → 워킹 버퍼. C3a — 모듈 미해결 시 null(구 algorithm_camera.json fallback 폐지).</summary>
        private AlgorithmCameraMapping LoadBuffer()
        {
            var mod = Module();
            if (mod == null) return null;
            var m = mod.ExportCameraMapping();
            m.Algorithm = _algorithm;
            return m;
        }

        private void BindFields()
        {
            var m = CurrentMapping();
            if (m == null)   // C3a — 운영 모듈 미해결: 명시 메시지 + 입력 비활성(조용한 구경로 금지)
            {
                if (_body != null) _body.Enabled = false;
                if (_lblStatus != null) { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "설정 불러올 수 없음 — 운영 모듈 미해결"; }
                try { _paramGrid.SetItems(new List<ParameterGridItem>()); } catch { }   // 스테일 행 제거
                try { _scaleGrid.SetItems(new List<ParameterGridItem>()); } catch { }
                try { _mfsGrid?.SetItems(new List<ParameterGridItem>()); } catch { }
                try { _imgGrid?.SetItems(new List<ParameterGridItem>()); } catch { }
                try { _acqGrid?.SetItems(new List<ParameterGridItem>()); } catch { }
                try { _ioGrid?.SetItems(new List<ParameterGridItem>()); } catch { }
                SizeLeftGrids();   // 빈 그리드도 헤더 높이로 축소(120F 빈 박스 방지 — GENERAL 동일)
                System.Diagnostics.Debug.WriteLine("[CameraMappingPanel] 모듈 미해결: " + _algorithm);
                return;
            }
            if (_body != null) _body.Enabled = true;
            _suspendBinding = true;
            try
            {
                SetSelectedById(_cbCameraId, m.CameraId);
                _paramGrid.SetItems(BuildParamItems(m));   // 카메라 파라미터 = 리스트(항목 추가/삭제 용이)
                _scaleGrid.SetItems(BuildScaleItems(m));   // 스케일/좌표변환 전용 그리드
                _mfsGrid?.SetItems(BuildMfsItems(m));                          // .mfs 경로 + 불러오기/저장
                _imgGrid?.SetItems(BuildNodeCatalogItems(m, "Image Format"));   // MVS 확장 — Image Format 그룹
                _acqGrid?.SetItems(BuildNodeCatalogItems(m, "Acquisition"));    // MVS 확장 — Acquisition 그룹
                _ioGrid?.SetItems(BuildNodeCatalogItems(m, "IO Output(Strobe)")); // MVS 확장 — IO Output(Strobe) 그룹
                SizeLeftGrids();                           // 그리드 행 높이를 내용에 맞춰 고정(GENERAL 동일 — 세로 늘어남 방지)
                _camPreview.AttachModule(Module());        // 툴바 Grab/Live 대상 = 현재 모듈
                UpdateDisplayOrientation();                // X반전/Y반전/90°회전 → 미리보기 표시 방향 반영

                var cfg = VisionConfigStore.Current;
                if (_txtMilDcf != null) _txtMilDcf.Text = cfg?.MilDcfPath ?? "";
                // 저장된 DCF 가 있으면 체크박스를 자동 ON (그래야 경로칸이 보임)
                if (_chkMilDcf != null) _chkMilDcf.Checked = !string.IsNullOrEmpty(cfg?.MilDcfPath);
            }
            finally { _suspendBinding = false; }
            UpdateMilVisibility();
        }

        /// <summary>좌측 파라미터/스케일 그리드 행 높이를 내용에 맞춰 고정(GENERAL SizeGrids 동일).
        /// 행 2=_paramGrid, 행 4=_scaleGrid. 맨 아래 Percent 100 스페이서가 남는 공간 흡수.</summary>
        /// <summary>MVS 확장 파라미터용 그룹별 접이식 그리드(런타임 생성). 카탈로그 Group 단위로 분리하고
        /// 기본 접힘으로 시작한다. Designer 미수정(인덱스 의존 코드 보존) — _left 에 행을 동적 추가한다.</summary>
        private void InitNodeGroupGrids()
        {
            _mfsGrid = CreateGroupGrid("[카메라 설정] .mfs 불러오기·파일저장 / 카메라 영구저장(UserSet)");
            _imgGrid = CreateGroupGrid("[Image Format] 이미지 포맷 (Reverse/Binning/패턴)");
            _acqGrid = CreateGroupGrid("[Acquisition] 취득/트리거/노출 (HDR 포함)");
            _ioGrid  = CreateGroupGrid("[IO Output] 스트로브 / 라인");
            // 코어 카메라 파라미터(_paramGrid) 바로 아래에 오도록 _left 스택을 재구성.
            RelayoutLeftStack();
            // 길어 보이지 않도록 MVS 확장 노드 그룹은 기본 접힘. .mfs 그룹은 펼친 상태(주요 동작).
            _imgGrid.SetCollapsed(true);
            _acqGrid.SetCollapsed(true);
            _ioGrid.SetCollapsed(true);
        }

        /// <summary>그룹 접이식 그리드 1개 생성(제목바 클릭으로 접기/펼치기). 값 변경/접힘 이벤트 연결.</summary>
        private ParameterGridControl CreateGroupGrid(string title)
        {
            var g = new ParameterGridControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 0, 4),
                BackColor = Color.FromArgb(245, 246, 248),
                Title = title
            };
            g.ParameterValueChanged += OnParamChanged;
            g.CollapsedChanged += OnGroupGridCollapsedChanged;
            return g;
        }

        /// <summary>_left 세로 스택을 원하는 순서로 재배치한다. MVS 확장 그룹 그리드를 코어 카메라
        /// 파라미터(_paramGrid) 바로 아래에 끼워 넣는다. 조명 그리드는 Wrap 으로 감싼 패널(있으면)을 그대로 재배치.
        /// (행 높이는 SizeLeftGrids/ApplyWaferLayout 가 컨트롤 위치로 다시 조정 — 인덱스 의존 제거.)</summary>
        private void RelayoutLeftStack()
        {
            if (_left == null) return;
            // 조명 그리드는 CollapsibleGrids.Wrap 으로 감싸져 _left 에는 래퍼 패널이 들어가 있다.
            Control lightCtrl = (_gridLightAssign?.Parent?.Parent as QMC.Vision.Ui.Controls.CollapsibleGridPanel)
                                ?? (Control)_gridLightAssign;
            _left.SuspendLayout();
            try
            {
                _left.Controls.Clear();
                _left.RowStyles.Clear();
                _left.RowCount = 14;
                int r = 0;
                AddStackRow(ref r, _camRow,          40F);
                AddStackRow(ref r, _secParam,        28F);
                AddStackRow(ref r, _paramGrid,       120F);
                AddStackRow(ref r, _mfsGrid,         120F);   // ← 카메라 파라미터 바로 아래(.mfs 경로/버튼)
                AddStackRow(ref r, _imgGrid,         120F);
                AddStackRow(ref r, _acqGrid,         120F);
                AddStackRow(ref r, _ioGrid,          120F);
                AddStackRow(ref r, _secScale,        28F);
                AddStackRow(ref r, _scaleGrid,       120F);
                AddStackRow(ref r, _milRow,          30F);
                AddStackRow(ref r, _lblLightAssign,  28F);
                AddStackRow(ref r, lightCtrl,        170F);
                AddStackRow(ref r, _lblLightStatus,  22F);
                _left.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));   // 하단 스페이서
            }
            finally { _left.ResumeLayout(); }
        }

        /// <summary>_left 다음 행에 컨트롤을 배치하고 행 인덱스를 1 증가시킨다.</summary>
        private void AddStackRow(ref int row, Control c, float height)
        {
            _left.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            if (c != null) _left.Controls.Add(c, 0, row);
            row++;
        }

        /// <summary>그룹 그리드 접힘/펼침 시 행 높이를 다시 맞춰 레이아웃 갱신.</summary>
        private void OnGroupGridCollapsedChanged(object sender, EventArgs e) => SizeLeftGrids();

        private void SizeLeftGrids()
        {
            try
            {
                // 모든 파라미터 그리드 행 높이를 내용에 맞춰 조정(컨트롤 위치로 행 조회 — 인덱스 의존 제거).
                SizeGroupGridRow(_paramGrid);
                SizeGroupGridRow(_mfsGrid);
                SizeGroupGridRow(_imgGrid);
                SizeGroupGridRow(_acqGrid);
                SizeGroupGridRow(_ioGrid);
                SizeGroupGridRow(_scaleGrid);
                // _left 가 AutoSize 라 행 높이 변경 시 _leftScroll 가 스크롤 범위를 자동 갱신(스크롤은 좌측 컬럼만, 미리보기 고정).
            }
            catch { }
        }

        /// <summary>파라미터 그리드의 _left 행 높이를 PreferredGridHeight 로 맞춘다(컨트롤 위치로 행 조회).</summary>
        private void SizeGroupGridRow(ParameterGridControl g)
        {
            if (g == null || _left == null) return;
            var pos = _left.GetPositionFromControl(g);
            if (pos.Row >= 0 && pos.Row < _left.RowStyles.Count)
                _left.RowStyles[pos.Row].Height = g.PreferredGridHeight;
        }

        /// <summary>_left 에서 특정 컨트롤의 행 높이를 설정(컨트롤 위치로 행 조회).</summary>
        private void SetLeftRowHeight(Control c, float height)
        {
            if (c == null || _left == null) return;
            var pos = _left.GetPositionFromControl(c);
            if (pos.Row >= 0 && pos.Row < _left.RowStyles.Count)
                _left.RowStyles[pos.Row].Height = height;
        }

        /// <summary>섹션 제목을 각 그리드의 접기 헤더(주황 라인 포함)로 이동하고, 기존 회색 타이틀 행을 제거한다.</summary>
        private void ApplyWaferLayout()
        {
            try
            {
                _paramGrid.Title = Lang.T("set.cam.secParam");
                _scaleGrid.Title = Lang.T("set.cam.secScale");
                var lightPanel = _gridLightAssign.Parent?.Parent as QMC.Vision.Ui.Controls.CollapsibleGridPanel;
                if (lightPanel != null) lightPanel.Title = Lang.T("set.cam.secLight");

                // 기존 회색 섹션 타이틀 라벨 숨김 + 해당 행 높이 제거(제목은 접기 헤더로 이동)
                foreach (var lbl in new Control[] { _secParam, _secScale, _lblLightAssign })
                    if (lbl != null) lbl.Visible = false;
                SetLeftRowHeight(_secParam, 0F);
                SetLeftRowHeight(_secScale, 0F);
                SetLeftRowHeight(_lblLightAssign, 0F);
            }
            catch { }
        }

        /// <summary>"Mil/..." 카메라가 선택됐을 때만 "DCF 직접 지정" 체크박스를 노출.</summary>
        private void UpdateMilVisibility()
        {
            string id = ItemToId(_cbCameraId?.SelectedItem) ?? _cbCameraId?.Text;
            bool isMil = !string.IsNullOrEmpty(id) && id.StartsWith("Mil/", StringComparison.OrdinalIgnoreCase);
            if (_chkMilDcf != null) _chkMilDcf.Visible = isMil;
            UpdateMilDcfVisibility();
        }

        /// <summary>DCF 경로칸은 MIL 카메라 선택 + "DCF 직접 지정" 체크 시에만 표시(기본 숨김).</summary>
        private void UpdateMilDcfVisibility()
        {
            bool show = _chkMilDcf != null && _chkMilDcf.Visible && _chkMilDcf.Checked;
            if (_lblMil != null) _lblMil.Visible = show;
            if (_txtMilDcf != null) _txtMilDcf.Visible = show;
            if (_btnMilBrowse != null) _btnMilBrowse.Visible = show;
        }

        /// <summary>MIL DCF 는 전역 VisionSettings 에 보관 (per-algorithm 아님). 체크 해제 시 비움 → enumerate 가 M_DEFAULT 사용.</summary>
        private void OnMilFieldChanged()
        {
            if (_suspendBinding) return;
            var cfg = VisionConfigStore.Current;
            if (cfg == null) return;
            bool useDcf = _chkMilDcf != null && _chkMilDcf.Checked;
            cfg.MilDcfPath = useDcf ? (_txtMilDcf?.Text ?? "") : "";
        }

        private static decimal Clamp(decimal v, decimal min, decimal max)
            => v < min ? min : (v > max ? max : v);

        private void OnFieldChanged()
        {
            if (_suspendBinding) return;
            var m = CurrentMapping();
            if (m == null) return;
            // 숫자/Pixel/Trigger 는 그리드 setter 가 buffer 에 즉시 반영. 여기선 CameraId 만 flush.
            m.CameraId = ItemToId(_cbCameraId.SelectedItem) ?? _cbCameraId.Text;
            UpdateMilVisibility();
        }

        /// <summary>Validation — 빈 CameraId 거부, ROI 부분 입력 (W/H 한쪽만 0) 경고.</summary>
        private bool Validate(out string error)
        {
            error = null;
            var m = CurrentMapping();
            if (m == null) { error = "no mapping"; return false; }
            if (string.IsNullOrWhiteSpace(m.CameraId)) { error = "CameraId 비어 있음"; return false; }
            if (m.ExposureUs <= 0) { error = "Exposure 가 0 이하"; return false; }
            // ROI 부분 입력 경고 (둘 다 0 = full, 둘 다 양수 = OK, 그 외 = 모호)
            if ((m.RoiWidth > 0) != (m.RoiHeight > 0))
            {
                error = "ROI Width / Height 는 둘 다 0(full) 또는 둘 다 양수여야 함";
                return false;
            }
            return true;
        }

        private void DiscoverCameras()
        {
            try
            {
                _lblStatus.Text = "카메라 검색 중..."; _lblStatus.Refresh();
                var list = CameraFactory.EnumerateAll();
                var prevId = ItemToId(_cbCameraId.SelectedItem) ?? _cbCameraId.Text;
                _cbCameraId.Items.Clear();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var info in list)
                    if (!string.IsNullOrEmpty(info.Id) && seen.Add(info.Id))
                        _cbCameraId.Items.Add(new DeviceListItem(info));

                foreach (var fb in new[] { "Sim/Wafer", "Sim/Bin", "Sim/BottomInsp", "Sim/FrontSide", "Sim/RearSide", "Sim/0" })
                    if (seen.Add(fb)) _cbCameraId.Items.Add(fb);

                var m = CurrentMapping();
                var target = (m != null && !string.IsNullOrEmpty(m.CameraId)) ? m.CameraId : prevId;
                SetSelectedById(_cbCameraId, target);
                _lblStatus.Text = $"검색 완료 — {list.Count} 대 발견";
            }
            catch (Exception ex) { _lblStatus.Text = "검색 실패: " + ex.Message; }
        }

        private void SaveAll()
        {
            OnFieldChanged();
            if (!Validate(out var err)) { _lblStatus.Text = "저장 거부 — " + err; _lblStatus.ForeColor = Color.Firebrick; return; }
            var mod = Module();
            if (mod == null)   // C3a — 모듈 미해결: 저장 불가(구 store fallback 폐지)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "저장 불가 — 운영 모듈 미해결";
                return;
            }
            // 카메라 설정 SSOT = 모듈 Config/Recipe
            mod.ImportCameraMapping(_buffer);
            // 조명 지정(컨트롤러/페이지) = 모듈 Setup.LightPages — 카메라와 함께 영속.
            int lightCount = -1;
            var msetup = mod.Setup as Modules.VisionModuleSetupBase;
            if (msetup != null) { msetup.LightPages = CollectLightPages(); lightCount = msetup.LightPages.Count; }
            mod.SaveSettings();
            mod.SaveRecipe(ActiveRecipeName());   // 카메라 Recipe(노출 등 품목별) = 활성 레시피에 저장(구 "default" 하드코딩 수정)
            OnMilFieldChanged();
            VisionConfigStore.Save();   // MIL DCF/System 등 전역 설정 영속

            // 저장 = 영속 + (이미 열려있는 카메라면) 즉시 파라미터 적용. 닫힌 카메라는 건드리지 않음(예기치 않은 Open 방지).
            string applyNote = "";
            try
            {
                var cam = mod.Camera;
                if (cam != null && cam.IsOpen)
                {
                    if (AlgorithmCameraBinder.TryApplyParameters(cam, _buffer, out var applyErr))
                        applyNote = " · 카메라 적용됨";
                    else
                        applyNote = " · 적용 경고: " + applyErr;
                }
            }
            catch (Exception ex) { applyNote = " · 적용 실패: " + ex.Message; }

            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = $"저장 완료 — 모듈 [{mod.StorageKey}] Config/Recipe" + applyNote;
            if (lightCount >= 0) SetLightStatus($"조명 지정 {lightCount}건 저장됨.", false);
        }

        /// <summary>[불러오기] — 모듈 Config/Recipe 를 디스크에서 재로드 후 재바인딩(GENERAL 불러오기와 동일 개념).</summary>
        private void LoadFromDisk()
        {
            var mod = Module();
            if (mod == null)
            {
                if (_lblStatus != null) { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "불러오기 불가 — 운영 모듈 미해결"; }
                return;
            }
            try { mod.LoadSettings(); mod.LoadRecipe(ActiveRecipeName()); } catch { }   // 활성 레시피 카메라 Recipe 로드(구 "default" 수정)
            _buffer = null;       // 디스크에서 재로드된 모듈 Config/Recipe 로 버퍼 재생성
            BindFields();
            BindLightAssign();
            if (_lblStatus != null) { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = "불러옴 — 저장된 설정 적용"; }
        }

        private void CancelChanges()
        {
            // 미저장 편집은 버퍼에만 존재 → 버퍼를 버리고 모듈 Config/Recipe 에서 재로드(라이브 카메라 무영향).
            _buffer = null;
            BindFields();
            BindLightAssign();   // 조명 지정도 모듈 저장값으로 되돌림
            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = "취소됨 — 저장된 값으로 되돌림";
        }

        private void ResetToDefaults()
        {
            if (string.IsNullOrEmpty(_algorithm)) return;
            var dialog = MessageBox.Show(
                $"[{VisionAlgorithm.Label(_algorithm)}] 항목을 기본값으로 되돌립니다.\n저장하지 않으면 디스크에는 반영되지 않습니다. 계속할까요?",
                "기본값 복원", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog != DialogResult.Yes) return;

            // 기본 매핑 산출(임시 subset) → 워킹 버퍼에만 반영. 저장 시 모듈 Config/Recipe 로 영속.
            var fresh = new AlgorithmCameraSubset();
            fresh.EnsureDefaults();
            var def = fresh.Get(_algorithm);
            _buffer = def ?? new AlgorithmCameraMapping { Algorithm = _algorithm };
            _buffer.Algorithm = _algorithm;
            BindFields();
            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = "기본값 복원 — 저장 필요";
        }

        private void ApplyToRunningModule()
        {
            OnFieldChanged();
            if (!Validate(out var verr)) { _lblStatus.Text = "적용 거부 — " + verr; _lblStatus.ForeColor = Color.Firebrick; return; }
            var m = CurrentMapping();
            var form = this.FindForm() as Form1;
            if (form == null || m == null) { _lblStatus.Text = "메인 폼을 찾을 수 없음"; return; }
            // 모듈 Config/Recipe(SSOT)에 먼저 반영 후 라이브 카메라 Rebind(교체/파라미터 적용).
            Module()?.ImportCameraMapping(m);
            try
            {
                if (form.RebindAlgorithmCamera(_algorithm, m, out var rebindErr))
                {
                    _lblStatus.ForeColor = Color.DarkSlateGray;
                    _lblStatus.Text = $"[{VisionAlgorithm.Label(_algorithm)}] 실행 모듈에 적용됨";
                }
                else
                {
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = "적용 실패: " + rebindErr;
                }
            }
            catch (Exception ex) { _lblStatus.Text = "적용 실패: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
        }

        /// <summary>칩 이미지를 그랩해 측정 다이얼로그(두 점 측정)로 Scale X/Y(mm/px)를 산출 → 버퍼 반영(저장 필요).</summary>
        private void CalcScaleFromChip()
        {
            var mod = Module();
            var m = CurrentMapping();
            if (mod == null || m == null)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "스케일 계산 불가 — 운영 모듈 미해결";
                return;
            }
            if (_isLive)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "Live 중에는 스케일 계산 불가. Live Stop 후 시도하세요.";
                return;
            }

            GrabResult grab = null;
            ICamera tempCam = null;
            try
            {
                // 이미 미리보기에 표시된 프레임(테스트그랩/Live)이 있으면 그걸 사용, 없으면 단발 그랩.
                Bitmap src = _camPreview?.CurrentFrame;
                if (src == null)
                {
                    _lblStatus.Text = "측정용 이미지 그랩 중..."; _lblStatus.Refresh();
                    if (_activeCam != null)
                    {
                        grab = _activeCam.Grab(3000);
                    }
                    else
                    {
                        tempCam = AlgorithmCameraBinder.CreateAndApply(m);
                        tempCam.TriggerMode = CameraTriggerMode.Software;
                        grab = tempCam.Grab(3000);
                    }
                    if (grab == null || !grab.IsSuccess || grab.Image == null)
                    {
                        _lblStatus.ForeColor = Color.Firebrick;
                        _lblStatus.Text = "그랩 실패 — 스케일 계산 불가";
                        return;
                    }
                    _camPreview?.SetFrame(grab);   // 미리보기에도 표시
                    src = grab.Image;
                }

                using (var dlg = new QMC.Vision.Ui.Dialogs.ScaleCalibrationDialog(src, m.CalibChipWidthMm, m.CalibChipHeightMm))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                    {
                        _lblStatus.ForeColor = Color.DarkSlateGray;
                        _lblStatus.Text = "스케일 계산 취소됨";
                        return;
                    }
                    m.ScaleX = dlg.ResultScaleX;
                    m.ScaleY = dlg.ResultScaleY;
                    m.CalibChipWidthMm = dlg.ResultChipWidthMm;   // 그리드 동기화
                    m.CalibChipHeightMm = dlg.ResultChipHeightMm;
                    try { _scaleGrid.RefreshValues(); } catch { }
                    _lblStatus.ForeColor = Color.DarkSlateGray;
                    _lblStatus.Text = $"스케일 계산됨 — X={m.ScaleX:F6}  Y={m.ScaleY:F6} mm/px  (저장 필요)";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "스케일 계산 예외 — " + ex.Message;
            }
            finally
            {
                try { grab?.Dispose(); } catch { }
                if (tempCam != null) { try { tempCam.Dispose(); } catch { } }
            }
        }

        private void TestGrab()
        {
            OnFieldChanged();
            var m = CurrentMapping();
            if (m == null) return;
            if (_isLive)
            {
                _lblStatus.Text = "Live 중에는 단발 그랩 불가. Live Stop 후 시도하세요.";
                _lblStatus.ForeColor = Color.Firebrick;
                return;
            }
            _lblStatus.Text = "테스트 그랩 중..."; _lblStatus.Refresh();

            ICamera cam = _activeCam;
            bool ownCam = false;
            try
            {
                if (cam == null)
                {
                    cam = AlgorithmCameraBinder.CreateAndApply(m);
                    ownCam = true;
                }
                cam.TriggerMode = CameraTriggerMode.Software;
                using (var g = cam.Grab(3000))
                {
                    if (g.IsSuccess && g.Image != null)
                    {
                        _camPreview.SetFrame(g);   // 공용 CameraView (SetFrame 이 내부 복제)
                        _lblStatus.ForeColor = Color.DarkSlateGray;
                        _lblStatus.Text = $"그랩 OK — {g.Width}x{g.Height}  Exposure={m.ExposureUs}μs  Gain={m.Gain}dB";
                    }
                    else { _lblStatus.Text = "그랩 실패: " + (g.ErrorMessage ?? "-"); _lblStatus.ForeColor = Color.Firebrick; }
                }
            }
            catch (Exception ex) { _lblStatus.Text = "예외: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            finally { if (ownCam) { try { cam?.Dispose(); } catch { } } }
        }

        // ──────────────────────────────────────────
        //  Connect / Disconnect / Live
        // ──────────────────────────────────────────

        private void ToggleConnect()
        {
            if (_activeCam != null) Disconnect();
            else Connect();
            UpdateConnectButtons();
        }

        private void Connect()
        {
            OnFieldChanged();
            if (!Validate(out var err)) { _lblStatus.Text = "연결 거부 — " + err; _lblStatus.ForeColor = Color.Firebrick; return; }
            var m = CurrentMapping();
            try
            {
                // 운영 모듈(Form1)이 같은 카메라 보유 중이면 borrow (exclusive 점유 충돌 회피).
                var form = FindForm() as Form1;
                var mod = form?.ResolveModule(_algorithm);
                if (mod != null)
                {
                    bool sameId = string.Equals(mod.Camera?.Info?.Id, m.CameraId, StringComparison.OrdinalIgnoreCase);
                    if (!sameId || mod.Camera == null || !mod.Camera.IsOpen)
                    {
                        if (!form.RebindAlgorithmCamera(_algorithm, m, out var rebindErr))
                        {
                            _lblStatus.ForeColor = Color.Firebrick;
                            _lblStatus.Text = "운영 모듈 Rebind 실패: " + rebindErr;
                            return;
                        }
                    }
                    var borrowed = mod.Camera;
                    if (borrowed == null || !borrowed.IsOpen)
                    {
                        _lblStatus.ForeColor = Color.Firebrick;
                        _lblStatus.Text = "운영 모듈 카메라가 열려있지 않음 (CameraId=" + m.CameraId + ")";
                        return;
                    }
                    borrowed.FrameReceived += Cam_FrameReceived;
                    borrowed.ConnectionChanged += Cam_ConnectionChanged;
                    _activeCam = borrowed;
                    _activeCamOwned = false;
                    _lblStatus.ForeColor = Color.DarkSlateGray;
                    _lblStatus.Text = $"Connected (운영 모듈 공유) — {m.CameraId}";
                    return;
                }

                // Fallback: 단독 모드 → 새 인스턴스 직접 open.
                var cam = AlgorithmCameraBinder.CreateAndApply(m, out var openErr, out var applyErr);
                if (cam == null || !cam.IsOpen)
                {
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = "Open 실패: " + (openErr ?? "(unknown)");
                    try { cam?.Dispose(); } catch { }
                    return;
                }
                cam.FrameReceived += Cam_FrameReceived;
                cam.ConnectionChanged += Cam_ConnectionChanged;
                _activeCam = cam;
                _activeCamOwned = true;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = string.IsNullOrEmpty(applyErr)
                    ? $"Connected — {m.CameraId}"
                    : $"Connected (Apply warn: {applyErr})";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Connect error: " + ex.Message;
                _lblStatus.ForeColor = Color.Firebrick;
                if (_activeCamOwned) { try { _activeCam?.Dispose(); } catch { } }
                _activeCam = null;
                _activeCamOwned = false;
            }
        }

        private void Disconnect()
        {
            if (_activeCam == null) return;
            try { if (_isLive) _activeCam.StopLive(); } catch { }
            _isLive = false;
            try { _activeCam.FrameReceived -= Cam_FrameReceived; } catch { }
            try { _activeCam.ConnectionChanged -= Cam_ConnectionChanged; } catch { }
            if (_activeCamOwned)
            {
                try { _activeCam.Close(); } catch { }
                try { _activeCam.Dispose(); } catch { }
            }
            _activeCam = null;
            _activeCamOwned = false;
            if (_lblStatus != null) { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = "Disconnected"; }
        }

        private void LiveStart()
        {
            if (_activeCam == null || _isLive) return;
            try
            {
                _fpsT0 = DateTime.Now; _fpsCount = 0;
                System.Threading.Interlocked.Exchange(ref _uiPending, 0);
                _activeCam.TriggerMode = CameraTriggerMode.Continuous;
                _activeCam.StartLive();
                _isLive = true;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = "Live started";
            }
            catch (Exception ex) { _lblStatus.Text = "Live start error: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            UpdateConnectButtons();
        }

        private void LiveStop()
        {
            if (_activeCam == null || !_isLive) return;
            try
            {
                _activeCam.StopLive();
                _isLive = false;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = "Live stopped";
            }
            catch (Exception ex) { _lblStatus.Text = "Live stop error: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            UpdateConnectButtons();
        }

        private void Cam_FrameReceived(GrabResult r)
        {
            if (r == null || !r.IsSuccess || r.Image == null) return;
            // UI 스레드가 직전 프레임을 아직 처리 중이면 이번 프레임은 버린다(최신 프레임만 유지).
            // 고FPS/고해상도(GigE·MIL)에서 Post 가 적체되어 버퍼·메모리가 밀리는 것을 방지.
            if (System.Threading.Interlocked.CompareExchange(ref _uiPending, 1, 0) != 0) return;
            Bitmap bmp;
            try { bmp = (Bitmap)r.Image.Clone(); }
            catch { System.Threading.Interlocked.Exchange(ref _uiPending, 0); return; }
            _uiCtx.Post(_ => ShowLiveFrame(bmp), null);
        }

        private void ShowLiveFrame(Bitmap bmp)
        {
            try
            {
                if (bmp == null) return;
                if (_camPreview == null) { bmp.Dispose(); return; }
                int w = bmp.Width, h = bmp.Height;   // dispose 전에 크기 캡처(해제 후 접근 시 ArgumentException)
                _camPreview.SetImage(bmp);           // 내부 복제 → 원본 dispose 안전
                bmp.Dispose();
                _fpsCount++;
                var dt = (DateTime.Now - _fpsT0).TotalSeconds;
                if (dt >= 1.0)
                {
                    _lblStatus.Text = $"Live  {_fpsCount / dt:F1} FPS  ({w}x{h})";
                    _fpsCount = 0; _fpsT0 = DateTime.Now;
                }
            }
            finally { System.Threading.Interlocked.Exchange(ref _uiPending, 0); }
        }

        private void Cam_ConnectionChanged(CameraConnectionEvent ev)
        {
            _uiCtx.Post(_ =>
            {
                if (_lblStatus != null) _lblStatus.Text = "[evt] " + ev;
                UpdateConnectButtons();
            }, null);
        }

        private void UpdateConnectButtons()
        {
            if (_btnConnect == null) return;
            bool connected = _activeCam != null;
            _btnConnect.Text = connected ? "Disconnect" : "Connect";
            _btnConnect.BackColor = connected ? Color.IndianRed : UiTheme.Accent;
            // 연결 중엔 카메라/매핑 변경 잠금
            _cbCameraId.Enabled = !connected;
            if (_btnDiscover != null) _btnDiscover.Enabled = !connected;
            _btnApply.Enabled = !connected;
        }

        // ──────────────────────────────────────────
        //  ComboBox 아이템 wrapper / helpers
        // ──────────────────────────────────────────

        /// <summary>"UserDefinedName [Model] IP" 표시용 wrapper. 매핑 저장값은 Info.Id (IP) 그대로.</summary>
        private class DeviceListItem
        {
            public CameraInfo Info { get; }
            public DeviceListItem(CameraInfo info) { Info = info; }
            public string Id => Info?.Id;
            public override string ToString()
            {
                if (Info == null) return "";
                if (Info.Transport == CameraTransport.Sim) return Info.Id;
                var uid = string.IsNullOrWhiteSpace(Info.UserDefinedName) ? "(no UserID)" : Info.UserDefinedName;
                return $"{uid}   [{Info.Model}]   {Info.IpAddress}";
            }
        }

        private static string ItemToId(object item)
        {
            if (item is DeviceListItem d) return d.Id;
            return item as string;
        }

        private static bool ItemMatches(object item, string id)
        {
            var s = ItemToId(item);
            return s != null && s.Equals(id, StringComparison.OrdinalIgnoreCase);
        }

        private static void SetSelectedById(ComboBox cb, string id)
        {
            if (string.IsNullOrEmpty(id)) { cb.SelectedIndex = -1; cb.Text = ""; return; }
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (ItemMatches(cb.Items[i], id)) { cb.SelectedIndex = i; cb.Text = cb.Items[i].ToString(); return; }
            }
            cb.Items.Add(id);
            cb.SelectedIndex = cb.Items.Count - 1;
            cb.Text = id;
        }
    }
}
