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

        private System.Windows.Forms.Timer _refresh;
        private ToolTip _workTimeToolTip;
        private bool _eventsHooked;
        private readonly object _materialDisplaySync = new object();
        private MaterialDisplaySnapshot _materialDisplayCache = new MaterialDisplaySnapshot();
        private DateTime _lastMaterialDisplayRefreshUtc = DateTime.MinValue;
        private int _materialDisplayRefreshQueued;
        private string _fallbackProjectName;
        private bool _fallbackProjectNameLoaded;
        private readonly Label[] _frontColletUseValues = new Label[4];
        private readonly Label[] _rearColletUseValues = new Label[4];
        private readonly List<VisionViewerSource> _visionSources = new List<VisionViewerSource>();
        private readonly List<CameraViewBase> _visionCameras = new List<CameraViewBase>();

        public WorkMainPage()
        {
            InitializeComponent();
            bool designerMode = IsDesignerMode();

            BindDesignerMetricLabels();

            if (!designerMode)
            {
                RebuildVisionPanel();
                StyleMapTabs();
                ApplyBottomGroupSizing();
                WireRuntimeEvents();
                InitializeWorkTimeToolTips();
                HookStateEvents();
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

                _visionSources.Add(source);
                _visionCameras.Add(camera);

                camera.HandleCreated += (s, e) => StartPassiveVisionCamera(camera);
                if (camera.IsHandleCreated)
                    StartPassiveVisionCamera(camera);
            }
            catch
            {
            }
        }

        private static void StartPassiveVisionCamera(CameraViewBase camera)
        {
            if (camera == null || camera.IsDisposed)
                return;

            try { camera.StartLive(); } catch { }
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

        private void ApplyBottomGroupSizing()
        {
            if (rootLayout == null || grpInfo == null || grpTime == null)
                return;

            int available = rootLayout.ClientSize.Height - rootLayout.Padding.Vertical;
            int bottomRow = Math.Max(0, (int)(available * 0.35F));
            int groupHeight = Math.Max(230, bottomRow - BottomPanelReserveHeight);
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

        private void OnMaterialStateChanged(MaterialSnapshot snapshot)
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
            snap.StageInfo =
                "STAGE\r\nTOTAL : " + total +
                "\r\nGOOD : " + good +
                "\r\nNG : " + ng +
                "\r\nPICK : " + material.PickedCount;
            snap.Live = ctrl == null ? "Idle" : "Live  [" + ctrl.Status + "]";

            string project = "--";
            try { project = host?.Machine?.Recipe?.ProductId ?? "--"; } catch { }
            if (lot != null && !string.IsNullOrEmpty(lot.RecipeName))
            {
                project = lot.RecipeName;
            }
            else
            {
                project = ResolveFallbackProjectName();
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
            snap.Lot = ResolveDisplayLotId(stats, lot, material);

            return snap;
        }

        private string ResolveFallbackProjectName()
        {
            if (_fallbackProjectNameLoaded)
                return string.IsNullOrEmpty(_fallbackProjectName) ? "--" : _fallbackProjectName;

            try
            {
                _fallbackProjectNameLoaded = true;
                _fallbackProjectName = "--";

                var list = RecipeStore.List();
                if (list != null && list.Count > 0)
                    _fallbackProjectName = System.IO.Path.GetFileNameWithoutExtension(list[0]);
            }
            catch
            {
                _fallbackProjectName = "--";
            }

            return _fallbackProjectName;
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

        /// <summary>작업 맵 탭을 플랫 오너드로우로 스타일링한다(선택 탭=액센트, 나머지=연회색).</summary>
        private void StyleMapTabs()
        {
            if (mapTabControl == null)
                return;

            mapTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            mapTabControl.SizeMode = TabSizeMode.Fixed;
            mapTabControl.BackColor = Color.White;
            mapTabControl.Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
            mapTabControl.Padding = new Point(6, 1);
            UpdateMapTabWidth();
        }

        private void MapTabControl_SizeChanged(object sender, EventArgs e)
        {
            UpdateMapTabWidth();
        }

        /// <summary>탭 3개가 좌우로 꽉 차도록 각 탭 폭을 컨트롤 폭에 맞춰 균등 분할한다.</summary>
        private void UpdateMapTabWidth()
        {
            if (mapTabControl == null || mapTabControl.TabCount == 0)
                return;

            int w = (mapTabControl.ClientSize.Width - 4) / mapTabControl.TabCount;
            if (w < 40)
                w = 40;
            if (mapTabControl.ItemSize.Width != w || mapTabControl.ItemSize.Height != 21)
                mapTabControl.ItemSize = new Size(w, 21);
        }

        private void MapTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tc = sender as TabControl;
            if (tc == null || e.Index < 0 || e.Index >= tc.TabPages.Count)
                return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle r = tc.GetTabRect(e.Index);
            bool selected = e.Index == tc.SelectedIndex;

            // 탭 스트립을 콘텐츠와 같은 흰색으로 채워 카드와 자연스럽게 이어지게(모던 언더라인 탭).
            using (var b = new SolidBrush(Color.White))
                e.Graphics.FillRectangle(b, r);

            Color fg = selected ? AccentColor : Color.FromArgb(51, 65, 85);   // 캡션과 동일한 slate-700 로 통일
            using (Font font = new Font(tc.Font.FontFamily, 8F, FontStyle.Bold))   // 선택/미선택 모두 굵게
                TextRenderer.DrawText(
                    e.Graphics, tc.TabPages[e.Index].Text, font, r, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // 선택 탭: 하단 강조 언더라인 바.
            if (selected)
                using (var b = new SolidBrush(AccentColor))
                    e.Graphics.FillRectangle(b, r.Left + 12, r.Bottom - 2, Math.Max(1, r.Width - 24), 2);
        }

        private static void SetText(Control control, string text)
        {
            if (control == null)
                return;
            text = text ?? string.Empty;
            if (!string.Equals(control.Text, text, StringComparison.Ordinal))
                control.Text = text;
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

        private static string ResolveDisplayLotId(ProductionStatsSnapshot stats, Lot lot, MaterialDisplaySnapshot material)
        {
            if (!string.IsNullOrEmpty(stats.ActiveLotId))
                return stats.ActiveLotId;
            if (lot != null && !string.IsNullOrEmpty(lot.LotID))
                return lot.LotID;
            if (!string.IsNullOrEmpty(material.LotId))
                return material.LotId;
            return "(no lot)";
        }

        private static MaterialDisplaySnapshot BuildMaterialDisplaySnapshot()
        {
            var display = new MaterialDisplaySnapshot();

            try
            {
                MaterialSnapshot state = MaterialStorage.State;
                if (state == null)
                    return display;

                display.LotId = state.LotId ?? string.Empty;

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
                if (!string.IsNullOrEmpty(wafer.CassetteLotId) && string.IsNullOrEmpty(display.LotId))
                    display.LotId = wafer.CassetteLotId;

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
            public string LotId = string.Empty;
        }
    }
}
