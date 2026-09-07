using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Stats;
using QMC.CDT320.VisionComm;
using QMC.Common.Ui.Controls;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class WorkMainPage : PageBase
    {
        // UI 적용 주기(250~500ms). 표시 갱신만 수행하며 저장/모션 루프와 분리된다.
        private const int RefreshIntervalMs = 500;
        private const int MaterialRefreshIntervalMs = 1000;
        private const int BottomPanelReserveHeight = 56;
        // workInfoBody 0행(LOT 입력줄)의 Designer 고정 높이와 반드시 같아야 한다.
        private const int LotInputRowHeight = 34;

        private System.Windows.Forms.Timer _refresh;
        private ToolTip _workTimeToolTip;
        private bool _eventsHooked;
        private bool _lotStartPending;
        private readonly object _materialDisplaySync = new object();
        private MaterialDisplaySnapshot _materialDisplayCache = new MaterialDisplaySnapshot();
        private DateTime _lastMaterialDisplayRefreshUtc = DateTime.MinValue;
        private int _materialDisplayRefreshQueued;
        private readonly Label[] _frontColletUseValues = new Label[4];
        private readonly Label[] _rearColletUseValues = new Label[4];
        private readonly List<VisionViewerSource> _visionSources = new List<VisionViewerSource>();
        private readonly List<CameraViewBase> _visionCameras = new List<CameraViewBase>();

        public WorkMainPage()
        {
            InitializeComponent();
            InitializeReworkSelector();
            bool designerMode = IsDesignerMode();

            BindDesignerMetricLabels();

            if (!designerMode)
            {
                RebuildVisionPanel();
                ApplyBottomGroupSizing();
                WireRuntimeEvents();
                InitializeWorkTimeToolTips();
                HookStateEvents();
                WireLotIdScannerInput();
                RefreshLotUi();
                EnsureRefreshTimer();
            }
        }

        private void BindDesignerMetricLabels()
        {
            _frontColletUseValues[0] = lblFrontCollet1Designer;
            _frontColletUseValues[1] = lblFrontCollet2Designer;
            _frontColletUseValues[2] = lblFrontCollet3Designer;
            _frontColletUseValues[3] = lblFrontCollet4Designer;
            _rearColletUseValues[0] = lblRearCollet1Designer;
            _rearColletUseValues[1] = lblRearCollet2Designer;
            _rearColletUseValues[2] = lblRearCollet3Designer;
            _rearColletUseValues[3] = lblRearCollet4Designer;
        }

        private void InitializeReworkSelector()
        {
            if (cmbReworkCount == null)
                return;

            cmbReworkCount.BeginUpdate();
            try
            {
                cmbReworkCount.Items.Clear();
                for (int value = LotSessionService.MinReworkCount;
                     value <= LotSessionService.MaxReworkCount;
                     value++)
                {
                    cmbReworkCount.Items.Add(value);
                }

                cmbReworkCount.SelectedItem = LotSessionService.DefaultReworkCount;
            }
            finally
            {
                cmbReworkCount.EndUpdate();
            }
        }

        private int GetSelectedReworkCount()
        {
            if (cmbReworkCount != null && cmbReworkCount.SelectedItem is int)
                return (int)cmbReworkCount.SelectedItem;

            return LotSessionService.DefaultReworkCount;
        }

        private void SetSelectedReworkCount(int value)
        {
            if (cmbReworkCount == null)
                return;

            int normalized = value >= LotSessionService.MinReworkCount &&
                             value <= LotSessionService.MaxReworkCount
                ? value
                : LotSessionService.DefaultReworkCount;
            cmbReworkCount.SelectedItem = normalized;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_workTimeToolTip != null)
                {
                    _workTimeToolTip.Dispose();
                    _workTimeToolTip = null;
                }

                if (_refresh != null)
                {
                    _refresh.Stop();
                    _refresh.Dispose();
                    _refresh = null;
                }

                DisposeVisionSources();
            }

            base.Dispose(disposing);
        }

        private void RebuildVisionPanel()
        {
            if (visionPanel == null)
                return;

            bool designerMode = IsDesignerMode();
            if (!designerMode)
                DisposeVisionSources();

            visionPanel.SuspendLayout();
            try
            {
                visionPanel.Controls.Clear();
                visionPanel.BackColor = Color.Black;
                visionPanel.Padding = new Padding(0);

                TableLayoutPanel mainLayout = new TableLayoutPanel
                {
                    BackColor = Color.Black,
                    ColumnCount = 2,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    RowCount = 1
                };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                TableLayoutPanel leftLayout = new TableLayoutPanel
                {
                    BackColor = Color.Black,
                    ColumnCount = 1,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    RowCount = 2
                };
                leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                TableLayoutPanel rightLayout = new TableLayoutPanel
                {
                    BackColor = Color.Black,
                    ColumnCount = 1,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    RowCount = 3
                };
                rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3F));
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3F));
                rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3F));

                if (designerMode && lblStageInfo != null)
                    lblStageInfo.Text = BuildVisionInfoText("WAFER VISION", 640, 480);

                int waferPort = designerMode ? 0 : VisionViewerPorts.Wafer;
                int bottomPort = designerMode ? 0 : VisionViewerPorts.BottomInspection;
                int binPort = designerMode ? 0 : VisionViewerPorts.Bin;
                int rearPort = designerMode ? 0 : VisionViewerPorts.RearSideVision;
                int frontPort = designerMode ? 0 : VisionViewerPorts.FrontSideVision;

                leftLayout.Controls.Add(BuildVisionTile("WAFER VISION", waferPort, lblStageInfo), 0, 0);
                leftLayout.Controls.Add(BuildVisionTile("BIN VISION", binPort), 0, 1);
                rightLayout.Controls.Add(BuildVisionTile("REAR SIDE VISION", rearPort), 0, 0);
                rightLayout.Controls.Add(BuildVisionTile("BOTTOM VISION", bottomPort), 0, 1);
                rightLayout.Controls.Add(BuildVisionTile("FRONT SIDE VISION", frontPort), 0, 2);

                mainLayout.Controls.Add(leftLayout, 0, 0);
                mainLayout.Controls.Add(rightLayout, 1, 0);
                visionPanel.Controls.Add(mainLayout);
            }
            finally
            {
                visionPanel.ResumeLayout(true);
            }
        }

        private Control BuildVisionTile(string title, int viewerPort, out Label infoLabel)
        {
            infoLabel = CreateVisionInfoLabel(BuildVisionInfoText(title, 640, 480));
            return BuildVisionTile(title, viewerPort, infoLabel);
        }

        private Control BuildVisionTile(string title, int viewerPort)
        {
            Label infoLabel;
            return BuildVisionTile(title, viewerPort, out infoLabel);
        }

        private Control BuildVisionTile(string title, int viewerPort, Label infoLabel)
        {
            Panel tile = new Panel
            {
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                Padding = new Padding(0)
            };

            bool designerMode = IsDesignerMode();
            Control display;
            CameraViewBase camera = null;

            if (designerMode)
            {
                display = new Panel
                {
                    BackColor = Color.Black,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Name = "preview" + title.Replace(" ", "")
                };
            }
            else
            {
                camera = new CameraViewBase
                {
                    BackColor = Color.Black,
                    Dock = DockStyle.Fill,
                    Name = "camera" + title.Replace(" ", ""),
                    ShowToolbar = false
                };
                SetCameraInfoText(camera, BuildVisionInfoText(title, 640, 480));
                display = camera;
            }

            tile.Controls.Add(display);

            if (designerMode && infoLabel != null)
            {
                infoLabel.AutoSize = true;
                infoLabel.BackColor = Color.Black;
                infoLabel.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
                infoLabel.ForeColor = Color.LightGreen;
                infoLabel.Location = new Point(8, 8);
                infoLabel.Margin = new Padding(0);
                infoLabel.Padding = new Padding(0);
                tile.Controls.Add(infoLabel);
                infoLabel.BringToFront();
            }

            if (!designerMode)
                AttachPassiveVisionSource(camera, infoLabel, title, viewerPort);
            return tile;
        }

        private static Label CreateVisionInfoLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                BackColor = Color.Black,
                Font = new Font("Consolas", 8.5F, FontStyle.Regular),
                ForeColor = Color.LightGreen,
                Location = new Point(8, 8),
                Margin = new Padding(0),
                Padding = new Padding(0),
                Text = text
            };
        }

        private void AttachPassiveVisionSource(CameraViewBase camera, Label infoLabel, string title, int viewerPort)
        {
            if (camera == null || viewerPort <= 0)
                return;

            try
            {
                string host = string.IsNullOrWhiteSpace(VisionHub.Host) ? "127.0.0.1" : VisionHub.Host.Trim();
                VisionViewerSource source = new VisionViewerSource(host, viewerPort, 2000, null);
                source.FrameMeta += meta => OnVisionFrameMeta(camera, infoLabel, title, meta);
                camera.AttachSource(source);
                camera.ShowLiveLabel = false;

                _visionSources.Add(source);
                _visionCameras.Add(camera);

                int[] pending = new int[1];
                camera.HandleCreated += (s, e) => StartPassiveVisionGrabImageStream(source, camera, title, viewerPort, pending);
                if (camera.IsHandleCreated)
                    StartPassiveVisionGrabImageStream(source, camera, title, viewerPort, pending);
            }
            catch
            {
            }
        }

        private void StartPassiveVisionGrabImageStream(VisionViewerSource source, CameraViewBase camera, string title, int viewerPort, int[] pending)
        {
            if (source == null || camera == null || camera.IsDisposed)
                return;

            try
            {
                source.StartGrabImageStream(bmp => OnPassiveVisionGrabImageFrame(camera, bmp, pending));
                LogPassiveVisionGrabImageStarted(title, viewerPort);
            }
            catch (Exception ex)
            {
                LogPassiveVisionGrabImageFailed(title, viewerPort, ex.Message);
            }
        }

        private void OnPassiveVisionGrabImageFrame(CameraViewBase camera, Bitmap bmp, int[] pending)
        {
            if (bmp == null)
                return;

            if (pending == null)
            {
                try { bmp.Dispose(); } catch { }
                return;
            }

            if (Interlocked.CompareExchange(ref pending[0], 1, 0) != 0)
            {
                try { bmp.Dispose(); } catch { }
                return;
            }

            try
            {
                if (camera == null || camera.IsDisposed || !camera.IsHandleCreated)
                {
                    try { bmp.Dispose(); } catch { }
                    Interlocked.Exchange(ref pending[0], 0);
                    return;
                }

                camera.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (camera != null && !camera.IsDisposed)
                            camera.SetImage(bmp);
                    }
                    finally
                    {
                        try { bmp.Dispose(); } catch { }
                        Interlocked.Exchange(ref pending[0], 0);
                    }
                }));
            }
            catch
            {
                try { bmp.Dispose(); } catch { }
                Interlocked.Exchange(ref pending[0], 0);
            }
        }

        private static void LogPassiveVisionGrabImageStarted(string title, int viewerPort)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "VISION",
                    "VISION-GRAB-VIEW",
                    "Work 화면 Vision Grab 이미지 수신을 시작했습니다. title=" +
                    (title ?? string.Empty) + ", viewerPort=" + viewerPort);
            }
            catch { }
        }

        private static void LogPassiveVisionGrabImageFailed(string title, int viewerPort, string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-GRAB-VIEW",
                    "Work 화면 Vision Grab 이미지 수신 시작 실패. title=" +
                    (title ?? string.Empty) + ", viewerPort=" + viewerPort +
                    ", reason=" + reason);
            }
            catch { }
        }

        private void OnVisionFrameMeta(CameraViewBase camera, Label infoLabel, string title, VisionFrameMeta meta)
        {
            if (meta == null || IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (IsDisposed || camera == null || camera.IsDisposed)
                        return;

                    string text = BuildVisionInfoText(title, meta);
                    SetCameraInfoText(camera, text);

                    try { camera.SetVerdict(meta.Verdict, meta.VerdictPass); } catch { }
                    try { camera.SetResultLines(meta.ResultLines); } catch { }

                    if (infoLabel != null)
                        SetText(infoLabel, text);
                }));
            }
            catch
            {
            }
        }

        private static string BuildVisionInfoText(string title, VisionFrameMeta meta)
        {
            string module = meta == null || string.IsNullOrWhiteSpace(meta.Module)
                ? "STAGE"
                : meta.Module.Trim();
            int width = meta != null ? meta.Width : 640;
            int height = meta != null ? meta.Height : 480;
            return BuildVisionInfoText(title, module, width, height);
        }

        private static string BuildVisionInfoText(string title, int width, int height)
        {
            return BuildVisionInfoText(title, "STAGE", width, height);
        }

        private static string BuildVisionInfoText(string title, string module, int width, int height)
        {
            string size = width > 0 && height > 0
                ? "W:" + width + " H:" + height
                : "W:640 H:480";
            string fixedTitle = string.IsNullOrWhiteSpace(title) ? "VISION" : title.Trim();
            string bodyTitle = string.IsNullOrWhiteSpace(module) ? "STAGE" : module.Trim();
            return fixedTitle + "\r\n" + bodyTitle + "\r\n" + size;
        }

        private static void SetCameraInfoText(CameraViewBase camera, string text)
        {
            try
            {
                if (camera != null)
                    camera.InfoText = text;
            }
            catch
            {
            }
        }

        private void DisposeVisionSources()
        {
            for (int i = 0; i < _visionCameras.Count; i++)
            {
                try { _visionCameras[i].StopLive(); } catch { }
            }

            for (int i = 0; i < _visionSources.Count; i++)
            {
                try { _visionSources[i].Dispose(); } catch { }
            }

            _visionCameras.Clear();
            _visionSources.Clear();
        }

        private void WireRuntimeEvents()
        {
            if (rootLayout != null)
                rootLayout.SizeChanged += (s, e) => ApplyBottomGroupSizing();

            if (btnTestAlarm != null)
                btnTestAlarm.Click += btnTestAlarm_Click;
        }

        // ── LOT 관리 (2026-07-27 신규) ─────────────────────────────────────────────
        // LOT 시작/완료는 LotSessionService 하나만 호출한다. 화면은 결과 표시와 버튼 상태만 담당한다.

        // [P1 2026-08-21] 핸드 바코드 스캐너(키보드 웨지)가 LOT ID 끝에 쏘는 Enter 처리.
        // 경고음 없이 입력을 정규화(고정형 리더와 동일한 STX/ETX/공백 제거)하고
        // [LOT 시작] 버튼으로 포커스만 옮긴다 — 오스캔 방지를 위해 자동 시작은 하지 않는다.
        private void WireLotIdScannerInput()
        {
            if (txtLotId == null)
                return;

            txtLotId.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                e.Handled = true;
                e.SuppressKeyPress = true;

                try
                {
                    string normalized = QMC.CDT320.VisionComm.BarcodeSerialAdapter.NormalizePayload(txtLotId.Text);
                    if (!string.Equals(txtLotId.Text, normalized, StringComparison.Ordinal))
                        txtLotId.Text = normalized;
                    txtLotId.SelectionStart = txtLotId.TextLength;

                    if (btnLotStart != null && btnLotStart.Enabled)
                        btnLotStart.Focus();
                }
                catch (Exception ex)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning, "UI", "LOT-SCAN-INPUT",
                        "LOT ID 스캔 입력 처리 실패: " + ex.Message);
                }
            };
        }

        private async void btnLotStart_Click(object sender, EventArgs e)
        {
            await StartLotAsync();
        }

        private async Task StartLotAsync()
        {
            if (_lotStartPending)
                return;

            _lotStartPending = true;
            try
            {
                // LOT ID는 USE와 무관한 필수값이다. 네트워크 접근 전에 먼저 검사한다.
                string lotId = (txtLotId.Text ?? "").Trim();
                if (lotId.Length == 0)
                {
                    QMC.Common.MessageDialog.Show(this, "LOT ID를 입력하세요.", "LOT 시작 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Form1 host = ParentForm as Form1 ?? FindForm() as Form1;
                if (host == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 화면을 찾을 수 없습니다.", "LOT",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int reworkCount = GetSelectedReworkCount();
                string activeRecipeName = host.ActiveRecipeName;
                CDT320_Machine machine = host.Machine;
                AppSettings startSettings = AppSettingsStore.Current;
                bool checkedUse = startSettings != null && startSettings.UseLotNetworkWaferMap;
                string checkedFolder = checkedUse ? LotWaferMapFetchService.ResolveNetworkFolder() : "";
                RefreshLotUi(true);
                string folderError = await LotWaferMapFetchService.CheckLotStartFolderAsync(lotId);
                if (IsDisposed || Disposing || host.IsDisposed || host.Disposing)
                    return;
                if (!string.IsNullOrEmpty(folderError))
                {
                    QMC.Common.MessageDialog.Show(this, folderError, "LOT 시작 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 백그라운드 검사 완료부터 UI 재개 사이에 바뀐 설정도 이전 검사로 승인하지 않는다.
                AppSettings currentSettings = AppSettingsStore.Current;
                if (currentSettings == null || currentSettings.UseLotNetworkWaferMap != checkedUse ||
                    (checkedUse && !string.Equals(checkedFolder, LotWaferMapFetchService.ResolveNetworkFolder(), StringComparison.Ordinal)))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "확인 중 네트워크 웨이퍼맵 설정이 변경되어 LOT을 시작하지 않았습니다. 현재 설정을 확인한 후 다시 시작하세요.",
                        "LOT 시작 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ReferenceEquals(machine, host.Machine) ||
                    !string.Equals(activeRecipeName, host.ActiveRecipeName, StringComparison.Ordinal))
                {
                    QMC.Common.MessageDialog.Show(this,
                        "확인 중 장비 또는 활성 레시피가 변경되어 LOT을 시작하지 않았습니다. 현재 레시피를 확인한 후 다시 시작하세요.",
                        "LOT 시작 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string reason;
                if (!LotSessionService.TryStartLot(
                    machine,
                    activeRecipeName,
                    lotId,
                    reworkCount,
                    out reason))
                {
                    QMC.Common.MessageDialog.Show(this, reason, "LOT 시작",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshLotUi();

                QMC.Common.MessageDialog.Show(this,
                    "LOT을 시작했습니다.\r\nLOT ID: " + LotSessionService.ActiveLotId +
                    "\r\nRework: " + LotSessionService.ActiveReworkCount +
                    "\r\n\r\n레시피에도 LOT ID를 기록했습니다.",
                    "LOT 시작", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "LOT 시작 실패: " + ex.Message, "LOT 시작",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _lotStartPending = false;
                if (!IsDisposed && !Disposing)
                    RefreshLotUi(true);
            }
        }

        // [P4 2026-08-22] 픽업 BIN 선택 — 작업(LOT) 단위. 저장은 MaterialStateService(상태 파일, 재기동 유지),
        // 적용은 다음 웨이퍼의 맵 적용 시점부터. LOT 완료/레시피 변경 시 자동 ALL 복귀.
        private void btnBinSelect_Click(object sender, EventArgs e)
        {
            try
            {
                // [검토수정 2026-08-22] BIN 필터는 LOT 네트워크 맵 모드에서만 실제로 적용된다(레시피 맵
                // 경로에는 필터가 없음). 모드가 꺼진 상태에서 저장을 허용하면 "저장했습니다" 안내와 실제
                // 동작(전 BIN 픽업)이 어긋나 혼입 사고로 이어진다 — fail-closed로 차단하고 안내한다.
                if (!QMC.CDT320.Sequencing.InputStageDieMappingSequence.IsLotNetworkWaferMapModeActive())
                {
                    QMC.Common.MessageDialog.Show(this,
                        "BIN 선택은 LOT 네트워크 웨이퍼맵 모드에서만 적용됩니다.\r\n" +
                        "설정 → 일반에서 NETWORK WAFER MAP FOLDER 경로와 USE를 켠 뒤 사용하세요.",
                        "픽업 BIN 선택", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string lotId = MaterialStateService.GetProductionLotId();

                // [P5 2026-08-24] 기준 맵 = 가장 최근 수신한 웨이퍼 맵(바코드=파일명이라 사전 조회 불가).
                // 첫 웨이퍼 수신 전에는 참조가 없어 목록이 비고, 기본(All)은 그대로 사용 가능하다.
                QMC.CDT320.Lots.LotWaferMapSlotInfo reference;
                QMC.CDT320.Lots.LotWaferMapFetchService.TryGetLatestFetchedInfo(out reference);

                string mode;
                System.Collections.Generic.List<int> bins;
                MaterialStateService.GetPickupBinSelection(out mode, out bins);

                using (var dialog = new QMC.CDT_320.Ui.Dialogs.BinSelectDialog(lotId, reference, mode, bins))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    string failReason;
                    if (!MaterialStateService.TrySetPickupBinSelection(
                        dialog.SelectedMode, dialog.SelectedBins, "BinSelectDialog", out failReason))
                    {
                        QMC.Common.MessageDialog.Show(this, "BIN 선택 저장 실패:\r\n" + failReason,
                            "픽업 BIN 선택", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    QMC.Common.MessageDialog.Show(this,
                        "픽업 BIN 선택을 저장했습니다: " + MaterialStateService.DescribePickupBinSelection() +
                        "\r\n\r\n다음 웨이퍼(맵 적용 시점)부터 반영됩니다.",
                        "픽업 BIN 선택", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "BIN 선택 실패: " + ex.Message, "픽업 BIN 선택",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLotComplete_Click(object sender, EventArgs e)
        {
            try
            {
                Form1 host = ParentForm as Form1 ?? FindForm() as Form1;
                if (host == null || host.Controller == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 화면을 찾을 수 없습니다.", "LOT",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 운전 중 완료는 막는다. 카세트 교체를 위해 잠시 멈춘 것과 LOT 종료를 구분해야 한다.
                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    QMC.Common.MessageDialog.Show(this,
                        "자동 운전 중에는 LOT을 완료할 수 없습니다.\r\n정지 후 다시 시도하세요.",
                        "LOT 완료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string activeLotId = LotSessionService.ActiveLotId;
                int activeReworkCount = LotSessionService.ActiveReworkCount;
                if (QMC.Common.MessageDialog.Show(this,
                        "LOT 완료하시겠습니까?\r\n\r\nLOT ID: " + activeLotId +
                        "\r\nRework: " + activeReworkCount +
                        "\r\n완료 후에는 새 LOT을 시작해야 자동 운전이 가능합니다.",
                        "LOT 완료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                string reason;
                if (!LotSessionService.TryCompleteLot(host.Controller.Stats, out reason))
                {
                    QMC.Common.MessageDialog.Show(this, reason, "LOT 완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshLotUi();
                QMC.Common.MessageDialog.Show(this,
                    "LOT을 완료했습니다.\r\nLOT ID: " + activeLotId +
                    "\r\nRework: " + activeReworkCount,
                    "LOT 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "LOT 완료 실패: " + ex.Message, "LOT 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        /// <summary>
        /// LOT 진행 이력 창을 연다.
        /// [LOT 관리 2026-07-27] 작업 정보 화면에 리스트를 넣으면 기존 정보 타일 값이 잘리므로
        /// 이력은 별도 창으로 뺐다. 이력은 Log\Lots JSON 으로 남아 재시작해도 유지된다.
        /// </summary>
        private void btnLotHistory_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new QMC.CDT_320.Ui.Dialogs.LotHistoryDialog())
                {
                    dialog.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "LOT 진행 이력을 열지 못했습니다: " + ex.Message,
                    "LOT 진행 이력", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        /// <summary>LOT 입력줄의 버튼 상태를 갱신한다.</summary>
        private void RefreshLotUi(bool preserveInput = false)
        {
            try
            {
                if (txtLotId == null || btnLotStart == null || btnLotComplete == null ||
                    cmbReworkCount == null)
                    return;

                bool active = LotSessionService.IsLotActive;
                bool canStart = !active && !_lotStartPending;

                txtLotId.Enabled = canStart;
                btnLotStart.Enabled = canStart;
                btnLotComplete.Enabled = active && !_lotStartPending;
                cmbReworkCount.Enabled = canStart;
                btnLotStart.BackColor = canStart
                    ? Color.FromArgb(21, 128, 61)
                    : Color.FromArgb(150, 150, 150);
                btnLotComplete.BackColor = active
                    ? Color.FromArgb(217, 119, 6)
                    : Color.FromArgb(150, 150, 150);

                if (active)
                {
                    txtLotId.Text = LotSessionService.ActiveLotId;
                    SetSelectedReworkCount(LotSessionService.ActiveReworkCount);
                }
                else if (!preserveInput && !_lotStartPending)
                {
                    // LOT 완료 후 이전 ID가 입력창에 남아 활성 LOT처럼 보이지 않게 생산 LOT 상태와 맞춘다.
                    txtLotId.Text = MaterialStateService.GetProductionLotId();
                    SetSelectedReworkCount(LotSessionService.DefaultReworkCount);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "UI", "LOT-REWORK-REFRESH",
                    "LOT/Rework 화면 상태 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void btnCcs_Click(object sender, EventArgs e)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    QMC.CDT_320.Ui.Security.UserSession.Name,
                    "CCS-CHECK",
                    "CCS check button clicked.");
            }
            catch { }

            QMC.Common.MessageDialog.Show(
                "CCS check page will be connected in the next work step.",
                btnCcs.Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnTestAlarm_Click(object sender, EventArgs e)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    QMC.CDT_320.Ui.Security.UserSession.Name,
                    "TEST-ALARM",
                    "Main ALARM button clicked. TEST-ALARM will be raised.");

                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Critical,
                    "TEST-ALARM",
                    "WorkMainPage",
                    "Main 화면 ALARM 버튼에 의해 테스트 알람이 발생했습니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "UI",
                    "TEST-ALARM",
                    "Main ALARM button failed: " + ex.Message);
            }
        }

        private void ApplyBottomGroupSizing()
        {
            if (rootLayout == null || grpInfo == null || grpTime == null)
                return;

            // [LOT 관리 2026-07-27] 작업 정보 안에 LOT 입력줄(34px)이 한 줄 늘었다.
            // 최소 높이를 그만큼 올려주지 않으면 화면이 작을 때 Collet 타일 3행이
            // 39px 까지 눌려 값이 잘린다(타일 1개 = caption 17px + 값 20px + 여백 ≈ 42px 필요).
            // 실측: grpInfo 230px -> 타일 39px(잘림) / 264px -> 타일 50px(정상).
            const int WorkInfoMinimumHeight = 230 + LotInputRowHeight;
            int available = rootLayout.ClientSize.Height - rootLayout.Padding.Vertical;
            int bottomRow = Math.Max(0, (int)(available * 0.35F));
            int groupHeight = Math.Max(WorkInfoMinimumHeight, bottomRow - BottomPanelReserveHeight);
            groupHeight = Math.Min(Math.Max(0, bottomRow), groupHeight);

            grpInfo.Dock = DockStyle.Top;
            grpTime.Dock = DockStyle.Top;
            grpInfo.Height = groupHeight;
            grpTime.Height = groupHeight;
        }

        private void InitializeWorkTimeToolTips()
        {
            try
            {
                _workTimeToolTip = new ToolTip
                {
                    AutoPopDelay = 30000,
                    InitialDelay = 400,
                    ReshowDelay = 100,
                    ShowAlways = true
                };

                SetMetricToolTip(
                    lblUphCaption,
                    lblUph,
                    "UPH(Units Per Hour)\r\n" +
                    "UPH: 최근 20개 Die Place 완료 간격의 다이당 평균 ms를 1시간 기준으로 환산합니다.\r\n" +
                    "1M Qty: 최근 60초 안에 Place 완료된 Die 수입니다.");
                SetMetricToolTip(
                    lblUphCaption,
                    lblRecentMinuteUph,
                    "최근 1분 생산 수량\r\n" +
                    "최근 60초 안에 Place 완료된 Die 수입니다.");

                SetMetricToolTip(
                    lblCycleCaption,
                    lblCycle,
                    "Cycle Time\r\n" +
                    "최근 20개 Die의 처리 시간을 다이당 ms로 평균낸 값입니다.\r\n" +
                    "처리 기록이 없으면 0 ms로 표시합니다.");

                SetMetricToolTip(
                    lblMtbfCaption,
                    lblMtbf,
                    "MTBF(Mean Time Between Failures)\r\n" +
                    "가동시간을 이상 정지 횟수로 나눈 평균 고장 간격입니다.\r\n" +
                    "이상 정지 횟수가 없으면 00:00:00으로 표시합니다.");

                SetMetricToolTip(
                    lblMttrCaption,
                    lblMttr,
                    "MTTR(Mean Time To Repair)\r\n" +
                    "이상 정지 시간을 이상 정지 횟수로 나눈 평균 복구 시간입니다.\r\n" +
                    "알람/이상정지 기준으로 누적됩니다.");

                SetMetricToolTip(
                    lblRateCaption,
                    lblRate,
                    "가동률\r\n" +
                    "가동시간 / 부하시간 x 100 으로 계산합니다.\r\n" +
                    "부하시간은 가동 + 정상정지 + 이상정지 시간을 기준으로 보정합니다.");
            }
            catch
            {
            }
        }

        private void HookStateEvents()
        {
            if (_eventsHooked)
                return;

            // Lot 변경은 비-UI 스레드에서 올 수 있으므로 BeginInvoke 로만 가볍게 표시 갱신을 예약한다.
            LotStorage.ActiveLotChanged += OnActiveLotChanged;
            MaterialStateService.StateChanged += OnMaterialStateChanged;
            _eventsHooked = true;
        }

        private void EnsureRefreshTimer()
        {
            if (IsDesignerMode() || _refresh != null)
                return;

            _refresh = new System.Windows.Forms.Timer { Interval = RefreshIntervalMs };
            _refresh.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                RefreshAll();
                QueueMaterialDisplayRefresh(false);
            };
        }

        private void UnhookStateEvents()
        {
            if (!_eventsHooked)
                return;

            LotStorage.ActiveLotChanged -= OnActiveLotChanged;
            MaterialStateService.StateChanged -= OnMaterialStateChanged;
            _eventsHooked = false;
        }

        private void OnActiveLotChanged(Lot lot)
        {
            try
            {
                if (!IsHandleCreated || IsDisposed)
                    return;

                BeginInvoke((Action)(() =>
                {
                    // LOT 입력줄/리스트는 화면이 보이지 않아도 상태를 맞춰 둔다(다시 열었을 때 즉시 정확).
                    RefreshLotUi();
                    if (ShouldRefreshVisible(this))
                    {
                        QueueMaterialDisplayRefresh(true);
                        RefreshAll();
                    }
                }));
            }
            catch
            {
            }
        }

        // 시그널 전용 계약 — 라이브 상태 객체는 전달되지 않으며,
        // 실제 집계는 QueueMaterialDisplayRefresh가 ReadState(_stateSync) 안에서 수행한다.
        private void OnMaterialStateChanged()
        {
            QueueMaterialDisplayRefresh(false);
        }

        private void RefreshAll()
        {
            try
            {
                WorkMainDisplaySnapshot snapshot = BuildDisplaySnapshot();
                ApplyDisplaySnapshot(snapshot);
            }
            catch
            {
                // Ignore transient refresh failures while the form is closing or machine state is changing.
            }
        }

        /// <summary>
        /// Lot / Controller counter / Cycle 통계를 한 번에 읽어 표시 문자열 스냅샷을 만든다.
        /// (서비스 접근을 Build 한 곳에 모으고, 실제 컨트롤 반영은 Apply 에서 diff 로 처리한다.)
        /// </summary>
        private WorkMainDisplaySnapshot BuildDisplaySnapshot()
        {
            var snap = new WorkMainDisplaySnapshot();

            Form1 host = ParentForm as Form1 ?? FindForm() as Form1;
            var ctrl = host?.Controller;
            var lot = LotStorage.ActiveLot;

            // 작업 시간/UPH 통계는 엔진 스냅샷 1회 읽기로 끝낸다(계산은 엔진이 수행, UI는 표시만).
            ProductionStatsSnapshot stats = ctrl?.Stats?.GetSnapshot() ?? ProductionStatsSnapshot.Empty;
            MaterialDisplaySnapshot material = GetCachedMaterialDisplaySnapshot();
            string productionLotId = material.LotId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(productionLotId) ||
                !string.Equals(stats.ActiveLotId, productionLotId, StringComparison.Ordinal))
            {
                stats = ProductionStatsSnapshot.Empty;
            }
            if (lot != null &&
                (string.IsNullOrWhiteSpace(productionLotId) ||
                 !string.Equals(lot.LotID, productionLotId, StringComparison.Ordinal)))
            {
                lot = null;
            }
            bool useMaterialCounters = stats.ProcessedDies <= 0 && material.HasMaterial;

            int total = useMaterialCounters ? material.ProcessedCount : stats.ProcessedDies;
            int good = useMaterialCounters ? material.GoodCount : stats.GoodCount;
            int ng = useMaterialCounters ? material.NgCount : stats.NgCount;
            if (material.ProcessedCount > total)
                total = material.ProcessedCount;
            if (material.GoodCount > good)
                good = material.GoodCount;
            if (material.NgCount > ng)
                ng = material.NgCount;

            int currBin = -1;
            if (lot?.BinDistribution != null && lot.BinDistribution.Count > 0)
            {
                int max = -1;
                foreach (var kv in lot.BinDistribution)
                {
                    if (kv.Value > max)
                    {
                        max = kv.Value;
                        currBin = kv.Key;
                    }
                }
            }
            if (currBin < 0 && material.CurrentBinCode > 0)
                currBin = material.CurrentBinCode;

            snap.TotalChip = material.CurrentInputTargetCount > 0
                ? material.CurrentInputProcessedCount + " / " + material.CurrentInputTargetCount
                : "0";
            snap.BinNum = currBin >= 0 ? currBin.ToString() : "--";
            snap.OutputGood = FormatOutputStageCount(material.OutputGoodPlacedCount, material.OutputGoodTotalCount);
            snap.OutputNg = FormatOutputStageCount(material.OutputNgPlacedCount, material.OutputNgTotalCount);
            snap.StageInfo =
                "STAGE\r\nTOTAL : " + total +
                "\r\nGOOD : " + good +
                "\r\nNG : " + ng +
                "\r\nPICK : " + material.PickedCount;
            snap.Live = ctrl == null ? "Idle" : "Live  [" + ctrl.Status + "]";

            // 기존 조건: 활성 Lot이 없으면 ResolveFallbackProjectName()이 "레시피 목록의 첫 번째 파일"을 표시해
            //           현재 사용 중인 레시피와 무관한 이름(예: 7_7_Test)이 나왔다.
            // 이전 기준: ① 활성 Lot의 RecipeName을 최우선으로 썼는데, 이 값은 LOT 시작 시점에 고정된 스냅샷이라
            //           이후 레시피를 바꿔도 갱신되지 않아 옛 이름(예: JMB)이 계속 표시됐다(상단 상태바와 불일치).
            // 현재 기준(사용자 확정 2026-08-05): 상단 상태바 Project Name과 항상 같은 "현재 활성 레시피"를 표시한다.
            //           ① ActiveRecipeName(상단바와 동일) -> ② Recipe.ProductId -> ③ 활성 Lot의 RecipeName -> ④ "--"
            string project = ResolveActiveProjectName(host);
            if ((string.IsNullOrWhiteSpace(project) || project == "--") &&
                lot != null && !string.IsNullOrWhiteSpace(lot.RecipeName))
            {
                project = lot.RecipeName;
            }

            snap.Project = project;
            snap.PickFail = (ctrl?.PickFailCount ?? 0) + " ea";
            snap.PlaceFail = (ctrl?.PlaceFailCount ?? 0) + " ea";
            snap.BinQty = good + " ea";
            int[] frontCollets = GetFrontColletUseCounts(ctrl);
            int[] rearCollets = GetRearColletUseCounts(ctrl);
            int frontColletTotal = SumCountArray(frontCollets);
            int rearColletTotal = SumCountArray(rearCollets);
            snap.FrontCollets = BuildCountTextArray(frontCollets, 4);
            snap.RearCollets = BuildCountTextArray(rearCollets, 4);
            snap.Collet1 = frontColletTotal.ToString();
            snap.Collet2 = rearColletTotal.ToString();
            snap.Needle = FormatTwoDigitCount(Math.Max(ctrl?.NeedleUseCount ?? 0, frontColletTotal + rearColletTotal)) + " ea";

            // 화면 수량은 Material 복구값을 참고할 수 있지만, UPH/Cycle은 통계 엔진 값만 사용한다.
            // 복구된 Material 누적 수량과 방금 시작한 통계 시간을 섞으면 UPH가 비정상적으로 커진다.
            int statsTotal = stats.ProcessedDies;
            int statsGood = stats.GoodCount;

            // 가동/정지 시간은 엔진이 확정한 초 값을 hh:mm:ss로 포맷한다.
            double upSeconds = stats.UpSeconds;
            double measuredLoadSeconds = upSeconds + stats.NormalDownSeconds + stats.ErrorDownSeconds;
            double loadSeconds = stats.LoadSeconds > 0 ? stats.LoadSeconds : measuredLoadSeconds;
            if (measuredLoadSeconds > loadSeconds)
                loadSeconds = measuredLoadSeconds;
            if (loadSeconds < upSeconds)
                loadSeconds = upSeconds;
            snap.Load = FormatTs(TimeSpan.FromSeconds(loadSeconds));
            snap.Up = FormatTs(TimeSpan.FromSeconds(upSeconds));
            snap.ContUp = FormatTs(TimeSpan.FromSeconds(stats.ContUpSeconds));
            snap.NormDown = FormatTs(TimeSpan.FromSeconds(stats.NormalDownSeconds));
            snap.ErrDown = FormatTs(TimeSpan.FromSeconds(stats.ErrorDownSeconds));
            snap.ErrCnt = stats.ErrorCount + " ea";
            snap.Recovery = FormatTs(TimeSpan.FromSeconds(stats.RecoverySeconds));

            // UPH 화면 기본값은 순간 UPH(실효 UPH는 엔진 스냅샷에 함께 보관됨).
            double uph = 0.0;
            if (statsTotal > 0 || statsGood > 0)
                uph = stats.UphInstant > 0 ? stats.UphInstant : stats.UphEffective;
            snap.Uph = uph.ToString("F2");
            snap.RecentMinuteUph = "1M " + stats.RecentMinuteDies + " ea";

            snap.Mtbf = FormatTs(TimeSpan.FromSeconds(stats.MtbfSeconds));
            snap.Mttr = FormatTs(TimeSpan.FromSeconds(stats.MttrSeconds));

            // CYCLE TIME = 다이당 ms (최근 20 다이 Rolling 평균).
            double cycleMs = statsTotal > 0 ? stats.CycleMsPerDieRolling : 0.0;
            snap.Cycle = ((int)Math.Round(cycleMs)) + " ms";

            // 가동률(%) = 가동시간 / 부하시간 × 100 (수율이 아님).
            double uptimeRate = loadSeconds > 0 ? upSeconds / loadSeconds * 100.0 : 0.0;
            snap.Rate = uptimeRate.ToString("F2") + " %";
            snap.Lot = ResolveDisplayLotId(material);

            return snap;
        }

        /// <summary>
        /// 작업 정보 화면의 프로젝트 이름을 현재 사용 중인 레시피로 해석한다.
        /// 상단 상태바(Form1.RefreshProjectName -> lblProjectValue)와 동일한 ActiveRecipeName을 우선 사용해
        /// 두 화면의 표시가 항상 일치하게 한다.
        /// </summary>
        private static string ResolveActiveProjectName(Form1 host)
        {
            try
            {
                if (host != null)
                {
                    if (!string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                        return host.ActiveRecipeName;

                    if (host.Controller != null && !string.IsNullOrWhiteSpace(host.Controller.ActiveRecipeName))
                        return host.Controller.ActiveRecipeName;

                    string productId = host.Machine != null && host.Machine.Recipe != null
                        ? host.Machine.Recipe.ProductId
                        : null;
                    if (!string.IsNullOrWhiteSpace(productId))
                        return productId;
                }

                return "--";
            }
            catch
            {
                return "--";
            }
            finally
            {
            }
        }

        private MaterialDisplaySnapshot GetCachedMaterialDisplaySnapshot()
        {
            lock (_materialDisplaySync)
            {
                return _materialDisplayCache ?? new MaterialDisplaySnapshot();
            }
        }

        private void QueueMaterialDisplayRefresh(bool force)
        {
            try
            {
                if (IsDisposed)
                    return;

                if (!force)
                {
                    DateTime lastRefreshUtc;
                    lock (_materialDisplaySync)
                    {
                        lastRefreshUtc = _lastMaterialDisplayRefreshUtc;
                    }

                    if (lastRefreshUtc != DateTime.MinValue &&
                        (DateTime.UtcNow - lastRefreshUtc).TotalMilliseconds < MaterialRefreshIntervalMs)
                    {
                        return;
                    }
                }

                if (Interlocked.CompareExchange(ref _materialDisplayRefreshQueued, 1, 0) != 0)
                    return;

                Task.Run(() =>
                {
                    try
                    {
                        MaterialDisplaySnapshot snapshot = BuildMaterialDisplaySnapshot();
                        lock (_materialDisplaySync)
                        {
                            _materialDisplayCache = snapshot ?? new MaterialDisplaySnapshot();
                            _lastMaterialDisplayRefreshUtc = DateTime.UtcNow;
                        }

                        if (!IsDisposed && IsHandleCreated)
                        {
                            try
                            {
                                BeginInvoke((Action)(() =>
                                {
                                    if (ShouldRefreshVisible(this))
                                        RefreshAll();
                                }));
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _materialDisplayRefreshQueued, 0);
                    }
                });
            }
            catch
            {
                Interlocked.Exchange(ref _materialDisplayRefreshQueued, 0);
            }
        }

        /// <summary>스냅샷을 라벨에 반영한다. 값이 다를 때만 Text 를 바꿔 레이아웃 churn 을 줄인다.</summary>
        private void ApplyDisplaySnapshot(WorkMainDisplaySnapshot s)
        {
            if (s == null)
                return;

            SetText(lblTotalChip, s.TotalChip);
            SetText(lblBinNum, s.BinNum);
            SetText(lblOutputGood, s.OutputGood);
            SetText(lblOutputNg, s.OutputNg);
            SetText(lblProject, s.Project);
            SetText(lblPickFail, s.PickFail);
            SetText(lblPlaceFail, s.PlaceFail);
            SetText(lblBinQty, s.BinQty);
            SetText(lblCollet1, s.Collet1);
            SetText(lblCollet2, s.Collet2);
            SetText(lblNeedle, s.Needle);
            SetTextArray(_frontColletUseValues, s.FrontCollets);
            SetTextArray(_rearColletUseValues, s.RearCollets);
            SetText(lblLoad, s.Load);
            SetText(lblUp, s.Up);
            SetText(lblContUp, s.ContUp);
            SetText(lblNormDown, s.NormDown);
            SetText(lblErrDown, s.ErrDown);
            SetText(lblErrCnt, s.ErrCnt);
            SetText(lblRecovery, s.Recovery);
            SetText(lblUph, s.Uph);
            SetText(lblRecentMinuteUph, s.RecentMinuteUph);
            SetText(lblMtbf, s.Mtbf);
            SetText(lblMttr, s.Mttr);
            SetText(lblCycle, s.Cycle);
            SetText(lblRate, s.Rate);
            SetText(lblLot, s.Lot);
        }

        private void SetMetricToolTip(Control caption, Control value, string text)
        {
            try
            {
                if (_workTimeToolTip == null)
                    return;

                if (caption != null)
                    _workTimeToolTip.SetToolTip(caption, text);
                if (value != null)
                    _workTimeToolTip.SetToolTip(value, text);
            }
            catch
            {
            }
        }

        private static void SetText(Control control, string text)
        {
            if (control == null)
                return;
            text = text ?? string.Empty;
            if (!string.Equals(control.Text, text, StringComparison.Ordinal))
                control.Text = text;
        }

        private static string FormatOutputStageCount(int placed, int total)
        {
            return total > 0 ? placed + " / " + total : "- / -";
        }

        private static void SetTextArray(Label[] labels, string[] values)
        {
            if (labels == null || values == null)
                return;

            int count = Math.Min(labels.Length, values.Length);
            for (int i = 0; i < count; i++)
                SetText(labels[i], values[i]);
        }

        private static int[] GetFrontColletUseCounts(MachineController ctrl)
        {
            try
            {
                return ctrl?.Machine?.PickerFrontUnit?.ColletUseCounts;
            }
            catch
            {
                return null;
            }
        }

        private static int[] GetRearColletUseCounts(MachineController ctrl)
        {
            try
            {
                return ctrl?.Machine?.PickerRearUnit?.ColletUseCounts;
            }
            catch
            {
                return null;
            }
        }

        private static string[] BuildCountTextArray(int[] counts, int count)
        {
            string[] values = new string[count];
            for (int i = 0; i < count; i++)
            {
                int value = counts != null && i < counts.Length ? Math.Max(0, counts[i]) : 0;
                values[i] = FormatTwoDigitCount(value) + " ea";
            }

            return values;
        }

        private static int SumCountArray(int[] counts)
        {
            if (counts == null)
                return 0;

            int total = 0;
            for (int i = 0; i < counts.Length; i++)
                total += Math.Max(0, counts[i]);
            return total;
        }

        private static string FormatTwoDigitCount(int value)
        {
            return Math.Max(0, value).ToString("00");
        }

        private static string FormatTs(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        private static string ResolveDisplayLotId(MaterialDisplaySnapshot material)
        {
            if (!string.IsNullOrEmpty(material.LotId))
                return material.LotId;
            return "(no lot)";
        }

        private static MaterialDisplaySnapshot BuildMaterialDisplaySnapshot()
        {
            var display = new MaterialDisplaySnapshot();

            try
            {
                // [계약 보강 2026-08-07] ThreadPool에서 시퀀스가 변이 중인 State를 락 없이 순회하지 않도록
                // 집계 전체를 ReadState(_stateSync) 안에서 수행한다. display는 카운터/문자열 사본이라
                // 락 밖(UI 캐시/표시)에서 안전하게 쓸 수 있다.
                MaterialStateService.ReadState(state =>
                    FillMaterialDisplaySnapshot(state, display));
            }
            catch
            {
                // 화면 표시 보강 실패는 생산 로직에 영향을 주지 않는다.
            }
            finally
            {
            }

            return display;
        }

        // MaterialStateService.ReadState(_stateSync) 안에서 호출된다 — 라이브 State를 읽어 표시 카운터를 채운다.
        private static void FillMaterialDisplaySnapshot(MaterialSnapshot state, MaterialDisplaySnapshot display)
        {
            if (state == null)
                return;

            display.LotId = MaterialStateService.GetProductionLotId();

            WaferMaterial currentInputWafer = ResolveCurrentInputStageWafer(state);
            string currentInputWaferId = currentInputWafer != null ? currentInputWafer.WaferId : string.Empty;
            HashSet<string> currentInputDieIds = BuildCurrentInputDieIdSet(currentInputWafer);

            if (state.Dies != null)
            {
                foreach (DieMaterial die in state.Dies)
                {
                    if (die == null || !die.IsInputTarget)
                        continue;

                    display.HasMaterial = true;
                    display.TargetCount++;

                    if (die.Result == DieResult.Good)
                    {
                        display.GoodCount++;
                        display.ProcessedCount++;
                    }
                    else if (die.Result == DieResult.NG)
                    {
                        display.NgCount++;
                        display.ProcessedCount++;
                    }
                    else if (IsOutputLocation(die.CurrentLocation))
                    {
                        display.ProcessedCount++;
                    }

                    if (IsPickerLocation(die.CurrentLocation))
                        display.PickedCount++;

                    if (die.Output_BinCode > 0)
                        display.CurrentBinCode = die.Output_BinCode;

                    AccumulateCurrentInputWaferCount(display, die, currentInputWaferId, currentInputDieIds);
                }
            }

            ApplyOutputReceiveSlotFallback(state, display);
            AccumulateOutputStageReceiveCount(state, MaterialLocationKind.OutputStageGood, out display.OutputGoodPlacedCount, out display.OutputGoodTotalCount);
            AccumulateOutputStageReceiveCount(state, MaterialLocationKind.OutputStageNg, out display.OutputNgPlacedCount, out display.OutputNgTotalCount);
        }

        private static WaferMaterial ResolveCurrentInputStageWafer(MaterialSnapshot state)
        {
            if (state == null || state.Wafers == null)
                return null;

            WaferMaterial selected = null;
            foreach (WaferMaterial wafer in state.Wafers)
            {
                if (wafer == null ||
                    wafer.CurrentLocation == null ||
                    wafer.CurrentLocation.Kind != MaterialLocationKind.InputStage ||
                    WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty ||
                    string.IsNullOrWhiteSpace(wafer.WaferId))
                {
                    continue;
                }

                if (selected == null || wafer.UpdatedAt > selected.UpdatedAt)
                    selected = wafer;
            }

            return selected;
        }

        private static HashSet<string> BuildCurrentInputDieIdSet(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null || wafer.DieIds == null || wafer.DieIds.Count == 0)
                    return null;

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string dieId in wafer.DieIds)
                {
                    if (!string.IsNullOrWhiteSpace(dieId))
                        set.Add(dieId);
                }

                return set.Count > 0 ? set : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static void AccumulateCurrentInputWaferCount(
            MaterialDisplaySnapshot display,
            DieMaterial die,
            string currentInputWaferId,
            HashSet<string> currentInputDieIds)
        {
            if (display == null || die == null || string.IsNullOrWhiteSpace(currentInputWaferId))
                return;

            if (!string.Equals(die.WaferID_Input, currentInputWaferId, StringComparison.OrdinalIgnoreCase))
                return;

            if (currentInputDieIds != null &&
                (string.IsNullOrWhiteSpace(die.DieId) || !currentInputDieIds.Contains(die.DieId)))
            {
                return;
            }

            display.CurrentInputTargetCount++;
            if (IsCurrentInputWaferDieProcessed(die))
                display.CurrentInputProcessedCount++;
        }

        private static bool IsCurrentInputWaferDieProcessed(DieMaterial die)
        {
            if (die == null)
                return false;

            if (die.Result == DieResult.Good || die.Result == DieResult.NG)
                return true;

            if (IsPickerLocation(die.CurrentLocation) || IsOutputLocation(die.CurrentLocation))
                return true;

            return HasInputPickVisionInspection(die);
        }

        private static bool HasInputPickVisionInspection(DieMaterial die)
        {
            if (die == null || die.Inspections == null)
                return false;

            foreach (DieInspectionRecord record in die.Inspections)
            {
                if (record == null)
                    continue;

                if (string.Equals(record.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase) &&
                    record.Result != MaterialInspectionResult.Unknown)
                    return true;
            }

            return false;
        }

        private static void ApplyOutputReceiveSlotFallback(MaterialSnapshot state, MaterialDisplaySnapshot display)
        {
            if (state == null || state.Wafers == null)
                return;

            int slotProcessed = 0;
            int slotGood = 0;
            int slotNg = 0;

            foreach (WaferMaterial wafer in state.Wafers)
            {
                if (wafer == null || !IsOutputWafer(wafer.CurrentLocation))
                    continue;

                display.HasMaterial = true;
                int waferReceived = Math.Max(0, wafer.OutputReceiveNextIndex);
                int slotDetected = 0;

                if (wafer.OutputReceiveSlots == null)
                {
                    slotProcessed += waferReceived;
                    continue;
                }

                for (int i = 0; i < wafer.OutputReceiveSlots.Count; i++)
                {
                    OutputReceiveSlotMaterial slot = wafer.OutputReceiveSlots[i];
                    if (slot == null)
                        continue;

                    bool received = !string.IsNullOrEmpty(slot.DieUid) ||
                                    (wafer.OutputReceiveNextIndex > 0 && i < wafer.OutputReceiveNextIndex);
                    if (received)
                        slotDetected++;

                    if (slot.Result == DieResult.Good)
                        slotGood++;
                    else if (slot.Result == DieResult.NG)
                        slotNg++;

                    if (slot.BinCode > 0)
                        display.CurrentBinCode = slot.BinCode;
                }

                if (slotDetected > waferReceived)
                    waferReceived = slotDetected;
                slotProcessed += waferReceived;
            }

            if (slotGood > display.GoodCount)
                display.GoodCount = slotGood;
            if (slotNg > display.NgCount)
                display.NgCount = slotNg;
            if (slotProcessed > display.ProcessedCount)
                display.ProcessedCount = slotProcessed;
        }

        // 출력 스테이지 웨이퍼는 GetWaferAtLocation과 같은 술어(위치 일치 + 비Empty, 첫 항목)로 골라 맵 캡션과 일치시킨다.
        private static WaferMaterial ResolveOutputStageWafer(MaterialSnapshot state, MaterialLocationKind stageKind)
        {
            if (state == null || state.Wafers == null)
                return null;

            foreach (WaferMaterial wafer in state.Wafers)
            {
                if (wafer != null &&
                    wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == stageKind &&
                    WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty)
                {
                    return wafer;
                }
            }

            return null;
        }

        // placed/total은 시퀀스 완료 판정(IsOutputReceiveSlotPending)과 같은 슬롯 기준이다.
        // 대상 슬롯이 없으면 수령 계획 총수와 안착 다이 ID 수로 대체한다(시퀀스 폴백과 동일).
        private static void AccumulateOutputStageReceiveCount(
            MaterialSnapshot state,
            MaterialLocationKind stageKind,
            out int placed,
            out int total)
        {
            placed = 0;
            total = 0;

            WaferMaterial wafer = ResolveOutputStageWafer(state, stageKind);
            if (wafer == null)
                return;

            if (wafer.OutputReceiveSlots != null)
            {
                foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots)
                {
                    if (slot == null || !slot.IsTarget)
                        continue;

                    total++;
                    if (slot.Result != DieResult.Unknown || !string.IsNullOrWhiteSpace(slot.DieUid))
                        placed++;
                }
            }

            if (total > 0)
                return;

            total = wafer.OutputReceiveTotalCount;
            if (wafer.DieIds != null)
            {
                foreach (string dieId in wafer.DieIds)
                {
                    if (!string.IsNullOrWhiteSpace(dieId))
                        placed++;
                }
            }
        }

        private static bool IsOutputLocation(MaterialLocation location)
        {
            if (location == null)
                return false;

            MaterialLocationKind kind = location.Kind;
            return kind == MaterialLocationKind.OutputStageGood ||
                   kind == MaterialLocationKind.OutputStageNg ||
                   kind == MaterialLocationKind.OutputFeeder ||
                   kind == MaterialLocationKind.OutputCassette;
        }

        private static bool IsOutputWafer(MaterialLocation location)
        {
            return IsOutputLocation(location);
        }

        private static bool IsPickerLocation(MaterialLocation location)
        {
            if (location == null)
                return false;

            return location.Kind == MaterialLocationKind.PickerFront ||
                   location.Kind == MaterialLocationKind.PickerRear;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                UnhookStateEvents();
                _refresh?.Stop();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HookStateEvents();
            EnsureRefreshTimer();
            UpdateRefreshTimer();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRefreshTimer();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateRefreshTimer();
        }

        private void UpdateRefreshTimer()
        {
            try
            {
                if (_refresh == null || IsDisposed)
                    return;

                if (ShouldRefreshVisible(this))
                {
                    QueueMaterialDisplayRefresh(true);
                    RefreshAll();
                    if (!_refresh.Enabled)
                        _refresh.Start();
                }
                else if (_refresh.Enabled)
                {
                    _refresh.Stop();
                }
            }
            catch
            {
            }
        }

        /// <summary>작업 메인 화면 표시용 스냅샷 모델. (UI 표시 문자열만 보관)</summary>
        private sealed class WorkMainDisplaySnapshot
        {
            public string TotalChip;
            public string BinNum;
            public string OutputGood;
            public string OutputNg;
            public string StageInfo;
            public string Live;
            public string Project;
            public string PickFail;
            public string PlaceFail;
            public string BinQty;
            public string Collet1;
            public string Collet2;
            public string Needle;
            public string[] FrontCollets;
            public string[] RearCollets;
            public string Load;
            public string Up;
            public string ContUp;
            public string NormDown;
            public string ErrDown;
            public string ErrCnt;
            public string Recovery;
            public string Uph;
            public string RecentMinuteUph;
            public string Mtbf;
            public string Mttr;
            public string Cycle;
            public string Rate;
            public string Lot;
        }

        private sealed class MaterialDisplaySnapshot
        {
            public bool HasMaterial;
            public int TargetCount;
            public int ProcessedCount;
            public int GoodCount;
            public int NgCount;
            public int PickedCount;
            public int CurrentBinCode = -1;
            public int CurrentInputTargetCount;
            public int CurrentInputProcessedCount;
            public int OutputGoodPlacedCount;
            public int OutputGoodTotalCount;
            public int OutputNgPlacedCount;
            public int OutputNgTotalCount;
            public string LotId = string.Empty;
        }
    }
}
