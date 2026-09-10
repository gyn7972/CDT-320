using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using CommonMessageDialog = QMC.Common.MessageDialog;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>파일 사본을 표시하는 웨이퍼맵 뷰어. 공정 맵·설정·캐시를 저장하거나 장비에 적용하지 않는다.</summary>
    public partial class WaferMapViewerDialog : Form
    {
        private const int MaximumListedFiles = 5000;
        private WaferMapViewerData _data;
        private DieMap _viewMap;
        private bool _busy;
        private bool _synchronizing;
        private int _rotationDegrees;
        private string _openedFrom = "파일 미선택";
        private readonly Dictionary<string, Color> _binColors = new Dictionary<string, Color>(StringComparer.Ordinal);

        internal bool AutoOpenCachedMap { get; set; } = true;
        internal DieMap CurrentViewMap { get { return _viewMap; } }
        internal int ViewRotationDegrees { get { return _rotationDegrees; } }
        internal string LoadedFilePath { get { return _data == null ? string.Empty : _data.FilePath; } }

        public WaferMapViewerDialog()
        {
            InitializeComponent();
            _synchronizing = true;
            cmbFormat.SelectedIndex = LotWaferMapFetchService.IsCamtekFormatConfigured ? 1 : 0;
            _synchronizing = false;
            txtFolder.Text = LotWaferMapFetchService.ResolveLocalCacheDirectory();
            UpdateSourceNotice();
            UpdateButtons();
        }

        private async void WaferMapViewerDialog_Shown(object sender, EventArgs e)
        {
            if (!AutoOpenCachedMap || _data != null || _busy) return;
            await RunUiAsync(async delegate
            {
                CachedMapCandidate candidate;
                bool folderExists;
                string folder = txtFolder.Text;
                SetBusy(true, "현재/최근 로컬 맵을 확인하는 중입니다...");
                try
                {
                    candidate = await Task.Run(() => ResolveCachedMapPath(false));
                    folderExists = candidate == null && await Task.Run(() => Directory.Exists(folder));
                }
                finally
                {
                    if (!IsDisposed && !Disposing) SetBusy(false, null);
                }
                if (IsDisposed || Disposing) return;
                if (candidate != null)
                    await LoadFileCoreAsync(candidate.Path, candidate.Source);
                else
                {
                    if (folderExists) await RefreshFolderAsync(folder);
                    if (!IsDisposed && !Disposing) lblStatus.Text = "현재/최근 수신 맵이 없습니다. 파일을 열어 확인하세요.";
                }
            }, "초기 맵 조회");
        }

        private async void btnOpen_Click(object sender, EventArgs e)
        {
            await RunUiAsync(async delegate
            {
                openMapDialog.FileName = string.Empty;
                if (!string.IsNullOrWhiteSpace(txtFolder.Text)) openMapDialog.InitialDirectory = txtFolder.Text;
                if (openMapDialog.ShowDialog(this) == DialogResult.OK) await LoadFileAsync(openMapDialog.FileName);
            }, "파일 열기");
        }

        private async void btnCurrent_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => OpenCachedMapAsync(false), "현재/최근 맵 열기");
        }

        private async void btnRecent_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => OpenCachedMapAsync(true), "최근 수신 맵 열기");
        }

        private async void btnFolder_Click(object sender, EventArgs e)
        {
            await RunUiAsync(async delegate
            {
                folderMapDialog.SelectedPath = txtFolder.Text;
                if (folderMapDialog.ShowDialog(this) == DialogResult.OK)
                    await RefreshFolderAsync(folderMapDialog.SelectedPath);
            }, "폴더 선택");
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => RefreshFolderAsync(txtFolder.Text), "파일 목록 갱신");
        }

        private async void txtFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await RunUiAsync(() => RefreshFolderAsync(txtFolder.Text), "파일 목록 갱신");
        }

        private async void listFiles_DoubleClick(object sender, EventArgs e)
        {
            await RunUiAsync(OpenSelectedFileAsync, "목록 파일 열기");
        }

        private async void listFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await RunUiAsync(OpenSelectedFileAsync, "목록 파일 열기");
        }

        private void cmbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_synchronizing) return;
            RunUiAction(() =>
            {
                // 선택 형식은 다음 파일을 읽을 때 사용한다. 표시 중인 맵은 다시 해석하지 않는다.
                UpdateSourceNotice();
                lblStatus.Text = _data == null
                    ? "선택한 형식으로 확인할 파일을 여세요."
                    : "현재 맵은 유지됩니다. 선택한 형식으로 다음 파일을 여세요.";
            }, "열 파일 형식 선택");
        }

        private async void btnOriginal_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => SetViewRotationAsync(0), "원본 방향 보기");
        }

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            if (_data == null) return;
            await RunUiAsync(() => SetViewRotationAsync(_data.ProcessRotationDegrees), "공정 방향 참고 보기");
        }

        private async void btnRotateLeft_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => SetViewRotationAsync((_rotationDegrees + 270) % 360), "왼쪽 보기 회전");
        }

        private async void btnRotateRight_Click(object sender, EventArgs e)
        {
            await RunUiAsync(() => SetViewRotationAsync((_rotationDegrees + 90) % 360), "오른쪽 보기 회전");
        }

        private void btnFit_Click(object sender, EventArgs e)
        {
            RunUiAction(() => mapViewer.SetMap(_viewMap, true, true), "화면 맞춤");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void mapViewer_CellClicked(DieMapEntry entry)
        {
            RunUiAction(() => ShowSelectedEntry(entry), "다이 주소 표시");
        }

        private void mapViewer_EmptyAreaClicked()
        {
            ShowSelectedEntry(null);
        }

        /// <summary>선택한 형식으로 파일을 읽어 원본 0° 사본을 표시한다. 호출자는 UI 스레드에서 await한다.</summary>
        internal Task LoadFileAsync(string path)
        {
            return LoadFileCoreAsync(path, "직접 선택한 파일");
        }

        private async Task LoadFileCoreAsync(string path, string sourceDescription)
        {
            if (_busy) throw new InvalidOperationException("현재 파일 작업이 끝난 후 다시 시도하세요.");
            bool camtek = cmbFormat.SelectedIndex == 1;
            SetBusy(true, "맵 파일을 읽는 중입니다...");
            try
            {
                WaferMapViewerData loaded = await Task.Run(() => WaferMapViewerData.Load(path, camtek));
                DieMap view = await Task.Run(() => loaded.CreateView(0));
                if (IsDisposed || Disposing) return;
                _data = loaded;
                _rotationDegrees = 0;
                _viewMap = view;
                _openedFrom = sourceDescription;
                txtFile.Text = loaded.FilePath;
                txtFolder.Text = Path.GetDirectoryName(loaded.FilePath);
                PopulateDetails();
                ShowMap(null);
                UpdateSourceNotice();
                lblStatus.Text = "파일 읽기 완료 · 원본 방향 0°";
            }
            catch (Exception ex)
            {
                LogFailure("파일 읽기", path, ex);
                // 읽기 실패 후에도 선택 형식을 유지해 같은 형식의 다른 파일을 다시 열 수 있게 한다.
                throw;
            }
            finally
            {
                if (!IsDisposed && !Disposing) SetBusy(false, null);
            }
            if (!IsDisposed && !Disposing) await RefreshFolderAsync(txtFolder.Text);
        }

        /// <summary>항상 원본에서 독립 사본을 생성한다. 연속 회전이나 재열기로 원본이 변경되지 않는다.</summary>
        internal async Task SetViewRotationAsync(int clockwiseDegrees)
        {
            if (_data == null) return;
            if (_busy) throw new InvalidOperationException("현재 파일 작업이 끝난 후 다시 시도하세요.");
            DieMapEntry selected = mapViewer.SelectedEntry;
            WaferMapViewerData source = _data;
            SetBusy(true, "보기 방향을 바꾸는 중입니다...");
            try
            {
                DieMap view = await Task.Run(() => source.CreateView(clockwiseDegrees));
                if (IsDisposed || Disposing) return;
                _viewMap = view;
                _rotationDegrees = ((clockwiseDegrees % 360) + 360) % 360;
                DieMapEntry retained = selected == null ? null : view.Entries.FirstOrDefault(e =>
                    e.OriginalMapX == selected.OriginalMapX && e.OriginalMapY == selected.OriginalMapY);
                ShowMap(retained);
                lblStatus.Text = "보기 회전 " + _rotationDegrees + "° · 확인용 맵";
            }
            catch (Exception ex)
            {
                LogFailure("보기 회전", LoadedFilePath, ex);
                throw;
            }
            finally
            {
                if (!IsDisposed && !Disposing) SetBusy(false, null);
            }
        }

        private Task OpenSelectedFileAsync()
        {
            FileListItem selected = listFiles.SelectedItem as FileListItem;
            return selected == null ? Task.CompletedTask : LoadFileCoreAsync(selected.Path, "폴더 목록에서 선택한 파일");
        }

        private async Task OpenCachedMapAsync(bool recentOnly)
        {
            SetBusy(true, "수신된 로컬 맵을 찾는 중입니다...");
            CachedMapCandidate candidate;
            try
            {
                candidate = await Task.Run(() => ResolveCachedMapPath(recentOnly));
            }
            finally
            {
                if (!IsDisposed && !Disposing) SetBusy(false, null);
            }
            if (IsDisposed || Disposing) return;
            if (candidate == null)
            {
                lblStatus.Text = recentOnly ? "최근 수신 파일이 로컬에 없습니다." : "현재/최근 수신 파일이 로컬에 없습니다.";
                CommonMessageDialog.Show(this, lblStatus.Text + "\r\n파일 열기로 직접 선택할 수 있습니다.",
                    "웨이퍼맵 보기", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            await LoadFileCoreAsync(candidate.Path, candidate.Source);
        }

        private static CachedMapCandidate ResolveCachedMapPath(bool recentOnly)
        {
            if (!recentOnly)
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string barcode = wafer != null && wafer.BarcodeConfirmed ? (wafer.BarcodeId ?? string.Empty).Trim() : string.Empty;
                // 바코드는 캐시 파일명이며, 경로나 상위 폴더를 입력으로 사용하지 않는다.
                if (barcode.Length > 0 && barcode.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                    string.Equals(Path.GetFileName(barcode), barcode, StringComparison.Ordinal))
                {
                    string currentPath = Path.Combine(LotWaferMapFetchService.ResolveLocalCacheDirectory(), barcode);
                    if (File.Exists(currentPath)) return new CachedMapCandidate(currentPath, "현재 InputStage 확정 바코드의 로컬 파일");
                }
            }
            LotWaferMapSlotInfo latest;
            if (LotWaferMapFetchService.TryGetLatestFetchedInfo(out latest) && latest != null && File.Exists(latest.LocalPath))
                return new CachedMapCandidate(latest.LocalPath, recentOnly ? "최근 수신한 로컬 파일" : "최근 수신한 로컬 파일 (현재 바코드 캐시 없음)");
            return null;
        }

        private async Task RefreshFolderAsync(string folder)
        {
            if (_busy) throw new InvalidOperationException("현재 파일 작업이 끝난 후 다시 시도하세요.");
            SetBusy(true, "폴더 파일 목록을 읽는 중입니다...");
            try
            {
                string fullFolder = Path.GetFullPath(folder);
                string[] files = await Task.Run(() => Directory.EnumerateFiles(fullFolder)
                    .Take(MaximumListedFiles + 1).ToArray());
                bool truncated = files.Length > MaximumListedFiles;
                FileListItem[] items = await Task.Run(() => files.Take(MaximumListedFiles)
                    .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                    .Select(p => new FileListItem(p)).ToArray());
                if (IsDisposed || Disposing) return;
                txtFolder.Text = fullFolder;
                listFiles.BeginUpdate();
                try
                {
                    listFiles.Items.Clear();
                    listFiles.Items.AddRange(items);
                    int selectedIndex = Array.FindIndex(items, item => string.Equals(item.Path, LoadedFilePath, StringComparison.OrdinalIgnoreCase));
                    listFiles.SelectedIndex = selectedIndex;
                }
                finally { listFiles.EndUpdate(); }
                lblFiles.Text = truncated ? "파일 목록 · 첫 5,000개 (직접 열기 가능)" : "파일 목록 · " + items.Length.ToString("N0") + "개";
                lblStatus.Text = truncated ? "파일이 많아 첫 5,000개만 표시합니다. 파일 열기로 다른 파일을 선택할 수 있습니다." :
                    (_data == null ? "파일을 두 번 누르거나 Enter로 여세요." : "읽기 완료 · 보기 " + _rotationDegrees + "° · 확인용 맵");
            }
            catch (Exception ex)
            {
                LogFailure("파일 목록 조회", folder, ex);
                throw;
            }
            finally
            {
                if (!IsDisposed && !Disposing) SetBusy(false, null);
            }
        }

        private void PopulateDetails()
        {
            gridHeaders.Rows.Clear();
            foreach (KeyValuePair<string, string> header in _data.Headers)
                gridHeaders.Rows.Add(header.Key, header.Value);
            _binColors.Clear();
            gridBins.Rows.Clear();
            var groups = _data.SourceMap.Entries.GroupBy(e => _data.GetSourceToken(e))
                .OrderBy(group => group.Key, StringComparer.Ordinal);
            foreach (var group in groups)
            {
                Color color = CreateTokenColor(group.Key);
                _binColors[group.Key] = color;
                int row = gridBins.Rows.Add(string.Empty, group.Key == "@@@" ? "@@@ 마크" : "BIN " + group.Key, group.Count().ToString("N0"));
                gridBins.Rows[row].Cells[0].Style.BackColor = color;
                gridBins.Rows[row].Cells[0].Style.SelectionBackColor = color;
            }
            int absent = _data.SourceMap.TotalCells - _data.SourceMap.Entries.Count;
            int emptyRow = gridBins.Rows.Add(string.Empty, "빈 셀 (___)", absent.ToString("N0"));
            gridBins.Rows[emptyRow].Cells[0].Style.BackColor = mapViewer.BackColor;
            gridBins.Rows[emptyRow].Cells[0].Style.SelectionBackColor = mapViewer.BackColor;
            lblSummary.Text = (_data.IsCamtek ? "캠택 CAMTEK" : "삼성 RAD") + " · " + _data.SourceMap.DieMapX + "열 × " +
                _data.SourceMap.DieMapY + "행\r\n존재 " + _data.SourceMap.Entries.Count.ToString("N0") + " · 빈 셀 " + absent.ToString("N0") +
                "\r\n숫자 BIN은 원본 값으로 표시합니다.";
            btnProcess.Text = "공정 방향 " + _data.ProcessRotationDegrees + "° 참고";
            gridHeaders.ClearSelection();
            gridBins.ClearSelection();
        }

        private void ShowMap(DieMapEntry selected)
        {
            mapViewer.Caption = Path.GetFileName(LoadedFilePath) + " · 보기 " + _rotationDegrees + "° (확인용)";
            mapViewer.SetMap(_viewMap, true);
            mapViewer.SelectedEntry = selected;
            lblRotation.Text = "보기 " + _rotationDegrees + "° · " + _viewMap.DieMapX + "열 × " + _viewMap.DieMapY + "행";
            ShowSelectedEntry(selected);
        }

        private void UpdateSourceNotice()
        {
            string selectedFormat = cmbFormat.SelectedIndex == 1 ? "캠택" : "삼성 RAD";
            string displayedFormat = _data == null ? "없음" : (_data.IsCamtek ? "캠택" : "삼성 RAD");
            lblNotice.Text = "출처: " + _openedFrom + "\r\n현재 표시: " + displayedFormat +
                " · 열 파일 형식: " + selectedFormat + " · 형식 선택은 다음 파일 열기에 적용됩니다.";
        }

        private void ShowSelectedEntry(DieMapEntry entry)
        {
            if (entry == null || _viewMap == null || _data == null)
            {
                lblSelected.Text = "선택한 다이 없음 · 다이를 누르면 BIN, 원본 주소, 현재 보기의 좌하단 1 기준 주소가 표시됩니다.";
                return;
            }
            string token = _data.GetSourceToken(entry);
            lblSelected.Text = (token == "@@@" ? "@@@ 마크" : "BIN " + token) +
                "   |   원본 X,Y = (" + entry.OriginalMapX + ", " + entry.OriginalMapY + ")" +
                "   |   보기 좌하단 1 기준 = (" + (entry.DieMapX + 1) + ", " + (_viewMap.DieMapY - entry.DieMapY) + ")" +
                "   |   보기 " + _rotationDegrees + "°";
        }

        private Color ResolveEntryColor(DieMapEntry entry)
        {
            if (_data == null || entry == null) return Color.Gray;
            Color color;
            return _binColors.TryGetValue(_data.GetSourceToken(entry), out color) ? color : Color.Gray;
        }

        private string ResolveEntryText(DieMapEntry entry)
        {
            return _data == null || entry == null ? string.Empty : _data.GetSourceToken(entry);
        }

        private string ResolveEntryStatus(DieMapEntry entry)
        {
            return "확인용";
        }

        private Tuple<string, Color>[] ResolveMapLegend()
        {
            // 공용 BIN 설정의 static 초기화에 의한 파일 생성도 피하기 위해 범례를 직접 제공한다.
            return new Tuple<string, Color>[0];
        }

        private static Color CreateTokenColor(string token)
        {
            if (token == "@@@") return Color.FromArgb(130, 139, 152);
            int bin;
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out bin)) return Color.SlateGray;
            if (bin == 1) return Color.FromArgb(73, 192, 103);
            if (bin == 0) return Color.FromArgb(117, 122, 135);
            if (bin == 125) return Color.FromArgb(248, 165, 65);
            int seed = bin % 155;
            return Color.FromArgb(80 + seed * 67 % 155, 80 + seed * 103 % 155, 80 + seed * 43 % 155);
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            UseWaitCursor = busy;
            if (status != null) lblStatus.Text = status;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnOpen.Enabled = btnCurrent.Enabled = btnRecent.Enabled = btnFolder.Enabled = btnRefresh.Enabled =
                cmbFormat.Enabled = txtFolder.Enabled = listFiles.Enabled = !_busy;
            btnOriginal.Enabled = btnProcess.Enabled = btnRotateLeft.Enabled = btnRotateRight.Enabled = btnFit.Enabled = !_busy && _data != null;
        }

        private async Task RunUiAsync(Func<Task> action, string operation)
        {
            if (_busy || IsDisposed || Disposing) return;
            try { await action(); }
            catch (Exception ex) { ShowFailure(operation, ex); }
        }

        private void RunUiAction(Action action, string operation)
        {
            if (_busy || IsDisposed || Disposing) return;
            try { action(); }
            catch (Exception ex) { ShowFailure(operation, ex); }
        }

        private void ShowFailure(string operation, Exception ex)
        {
            LogFailure(operation, LoadedFilePath, ex);
            if (IsDisposed || Disposing) return;
            lblStatus.Text = operation + " 실패 · " + ex.Message;
            CommonMessageDialog.Show(this, operation + " 실패\r\n" + ex.Message +
                "\r\n\r\n원본 파일과 실제 공정은 변경되지 않았습니다.",
                "웨이퍼맵 보기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void LogFailure(string operation, string path, Exception ex)
        {
            QMC.Common.Log.Write("Main", "UI", "WaferMapViewerDialog", operation + " 실패. file=" + path + ", exception=" + ex + " - Failed");
        }

        private sealed class FileListItem
        {
            public string Path { get; private set; }
            public FileListItem(string path) { Path = path; }
            public override string ToString() { return System.IO.Path.GetFileName(Path); }
        }

        private sealed class CachedMapCandidate
        {
            public string Path { get; private set; }
            public string Source { get; private set; }
            public CachedMapCandidate(string path, string source) { Path = path; Source = source; }
        }
    }
}
