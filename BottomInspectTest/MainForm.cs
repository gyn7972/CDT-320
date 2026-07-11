using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Vision.Inspector;

namespace QMC.BottomInspectTest
{
    /// <summary>
    /// Bottom 표면 검사 오프라인 테스트 도구.
    /// 폴더의 이미지 목록을 이전/다음으로 1장씩 검사하거나, [재생]으로 전체를 지정한 쓰레드 수만큼 병렬 검사한다.
    /// 검사부 = QMc.Vision.Inspector.CDTInspector.BottomInspect (bSimulate=true, 입력은 2배 확장 이미지 규약).
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>검사 1건 결과 레코드 — 결과 그리드 행 Tag 로 보관해 클릭 시 재표시.</summary>
        private sealed class InspectRecord
        {
            public string FilePath;
            public BottomResult Result;     // null = 검사부가 null 반환(미검출/스펙아웃)
            public double ElapsedMs;
            public int Worker;
            public float OverlayScale;      // 결과 좌표(1배) → 표시 이미지 좌표 배율
            public string Error;            // 예외 메시지(있으면 실패)
        }

        private readonly TestParameters _params = new TestParameters();
        private readonly List<string> _files = new List<string>();
        private int _currentIndex = -1;

        private CancellationTokenSource _playCts;
        private volatile bool _playing;
        private int _displayBusy;   // 재생 중 표시 스로틀(0=유휴)

        private static readonly string[] ImageExts = { ".png", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };

        public MainForm()
        {
            InitializeComponent();
            grdParams.SelectedObject = _params;
        }

        // ── 이벤트 핸들러 ──────────────────────────────────────────────

        private void btnFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (_playing) { SetStatus("재생 중에는 폴더를 바꿀 수 없습니다.", true); return; }
                using (var dlg = new FolderBrowserDialog { Description = "검사할 이미지 폴더 선택" })
                {
                    if (!string.IsNullOrEmpty(txtFolder.Text) && Directory.Exists(txtFolder.Text))
                        dlg.SelectedPath = txtFolder.Text;
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    LoadFolder(dlg.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "폴더 열기 실패: " + ex.Message, "폴더 선택", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_playing) return;   // 재생 중 목록 클릭은 무시(표시는 결과 그리드에서)
            int idx = lstFiles.SelectedIndex;
            if (idx < 0 || idx >= _files.Count || idx == _currentIndex) return;
            _currentIndex = idx;
            RunSingleAsync(idx);
        }

        private void btnPrev_Click(object sender, EventArgs e) => StepTo(_currentIndex - 1);
        private void btnNext_Click(object sender, EventArgs e) => StepTo(_currentIndex + 1);

        private void btnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                if (_playing) { _playCts?.Cancel(); return; }   // 정지 요청 — 워커들이 현재 건까지 마치고 종료
                if (_files.Count == 0) { SetStatus("이미지 폴더를 먼저 선택하세요.", true); return; }
                StartPlay();
            }
            catch (Exception ex)
            {
                SetStatus("재생 시작 실패: " + ex.Message, true);
            }
        }

        private void lvResults_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected) return;
            var rec = e.Item.Tag as InspectRecord;
            if (rec == null) return;
            try { ShowResult(rec); }
            catch (Exception ex) { SetStatus("표시 실패: " + ex.Message, true); }
        }

        // ── 폴더/이동 ──────────────────────────────────────────────────

        private void LoadFolder(string path)
        {
            _files.Clear();
            _files.AddRange(Directory.EnumerateFiles(path)
                .Where(f => ImageExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase));

            txtFolder.Text = path;
            lblCount.Text = "이미지 " + _files.Count + "개";
            _currentIndex = -1;

            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();
            foreach (var f in _files) lstFiles.Items.Add(Path.GetFileName(f));
            lstFiles.EndUpdate();

            lvResults.Items.Clear();
            SetStatus(_files.Count > 0 ? "폴더 로드 완료 — [다음]으로 첫 이미지를 검사하세요." : "이미지 파일이 없습니다.", _files.Count == 0);
        }

        /// <summary>이전/다음 — 목록 범위로 클램프 후 선택 이동(선택 변경 이벤트가 검사를 수행).</summary>
        private void StepTo(int idx)
        {
            if (_playing) { SetStatus("재생 중입니다 — 정지 후 사용하세요.", true); return; }
            if (_files.Count == 0) { SetStatus("이미지 폴더를 먼저 선택하세요.", true); return; }
            if (idx < 0) idx = 0;
            if (idx >= _files.Count) idx = _files.Count - 1;
            if (idx == _currentIndex) return;
            lstFiles.SelectedIndex = idx;   // → lstFiles_SelectedIndexChanged 가 검사 수행
        }

        // ── 단건 검사(이전/다음/목록 클릭) ──────────────────────────────

        private async void RunSingleAsync(int idx)
        {
            SetBusy(true);
            SetStatus("검사 중... " + Path.GetFileName(_files[idx]));
            try
            {
                InspectRecord rec = await Task.Run(() =>
                {
                    var inspector = new CDTInspector { bSimulate = true };
                    ApplyVisionConfig(inspector);
                    return InspectFile(_files[idx], inspector, 0);
                });
                AddResultRow(rec, select: true);
                ShowResult(rec);
                SetStatus(DescribeResult(rec), rec.Result == null || rec.Result.DefectCode != 0);
            }
            catch (Exception ex)
            {
                SetStatus("검사 실패: " + ex.Message, true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ── 재생(전체 병렬 검사) ────────────────────────────────────────

        private void StartPlay()
        {
            int threadCount = (int)numThreads.Value;
            var indices = new ConcurrentQueue<int>(Enumerable.Range(0, _files.Count));
            _playCts = new CancellationTokenSource();
            var ct = _playCts.Token;
            _playing = true;
            btnPlay.Text = "■ 정지";
            SetBusy(true);
            lvResults.Items.Clear();

            int done = 0, ng = 0, total = _files.Count;
            var swTotal = Stopwatch.StartNew();

            var workers = new List<Task>();
            for (int wi = 0; wi < threadCount; wi++)
            {
                int workerId = wi + 1;
                workers.Add(Task.Run(() =>
                {
                    // 워커별 검사기 인스턴스 — 검사 상태 분리. CUDA 디바이스 버퍼는 CudaContextPool(기본 8개)이 관리.
                    var inspector = new CDTInspector { bSimulate = true };
                    ApplyVisionConfig(inspector);

                    int fileIdx;
                    while (!ct.IsCancellationRequested && indices.TryDequeue(out fileIdx))
                    {
                        InspectRecord rec;
                        try { rec = InspectFile(_files[fileIdx], inspector, workerId); }
                        catch (Exception ex)
                        {
                            rec = new InspectRecord { FilePath = _files[fileIdx], Worker = workerId, Error = ex.Message };
                        }

                        int d = Interlocked.Increment(ref done);
                        if (rec.Error != null || rec.Result == null || rec.Result.DefectCode != 0) Interlocked.Increment(ref ng);

                        try
                        {
                            BeginInvoke((Action)(() =>
                            {
                                AddResultRow(rec, select: false);
                                SetStatus("재생 " + d + "/" + total + "  NG " + ng + "  (" + swTotal.Elapsed.TotalSeconds.ToString("F1") + "s)");
                                // 표시 스로틀 — 직전 표시가 진행 중이면 건너뜀(고해상도 로드가 UI 를 막지 않게)
                                if (Interlocked.CompareExchange(ref _displayBusy, 1, 0) == 0)
                                {
                                    try { ShowResult(rec); }
                                    finally { Interlocked.Exchange(ref _displayBusy, 0); }
                                }
                            }));
                        }
                        catch { /* 폼 종료 중 */ }
                    }
                }, ct));
            }

            Task.WhenAll(workers).ContinueWith(_ =>
            {
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        _playing = false;
                        btnPlay.Text = "▶ 재생";
                        SetBusy(false);
                        swTotal.Stop();
                        string cancelled = ct.IsCancellationRequested ? " (정지됨)" : "";
                        SetStatus("재생 완료" + cancelled + " — " + done + "/" + total + "건, NG " + ng +
                                  ", 총 " + swTotal.Elapsed.TotalSeconds.ToString("F1") + "s" +
                                  (done > 0 ? ", 평균 " + (swTotal.Elapsed.TotalMilliseconds / done).ToString("F0") + "ms/건" : "") +
                                  ", 쓰레드 " + threadCount, ng > 0);
                        _playCts?.Dispose(); _playCts = null;
                    }));
                }
                catch { /* 폼 종료 중 */ }
            });
        }

        // ── 검사 코어 ──────────────────────────────────────────────────

        private void ApplyVisionConfig(CDTInspector inspector)
        {
            var cfg = new VisionConfig();
            cfg.BottomVision.PixelSizeWidthMm = _params.PixelSizeWidthMm;
            cfg.BottomVision.PixelSizeHeightMm = _params.PixelSizeHeightMm;
            inspector.SetVisionConfig(cfg);
        }

        /// <summary>파일 1장 검사 — 그레이 변환 → (옵션) 2배 확장 → BottomInspect. 워커 스레드에서 호출.</summary>
        private InspectRecord InspectFile(string path, CDTInspector inspector, int workerId)
        {
            int w, h;
            byte[] gray = LoadGray(path, out w, out h);

            float overlayScale;
            if (_params.ExpandInput2x)
            {
                gray = Upscale2xBilinear(gray, w, h, out w, out h);
                overlayScale = 1f;   // 결과(1배 환원 좌표) = 파일 원본 좌표
            }
            else
            {
                overlayScale = 2f;   // 파일 자체가 2배 공간 → 결과(1배 환원 좌표) × 2 = 파일 좌표
            }

            var bip = new BottomInspectionParameter
            {
                Images = new List<byte[]> { gray },
                ImageWidth = w,
                ImageHeight = h,
                ChipRoi = new Rectangle(0, 0, w, h),
                Threshold = _params.Threshold,
                SelectedChipType = _params.DarkChip ? InspectionParameterBase.ChipType.Black : InspectionParameterBase.ChipType.White,
                ChippingDepth = _params.ChippingDepth,
                ChippingLength = _params.ChippingLength,
                ChipLowerSpecLimit = new SizeF((float)_params.ChipWidthLower, (float)_params.ChipHeightLower),
                ChipUpperSpecLimit = new SizeF((float)_params.ChipWidthUpper, (float)_params.ChipHeightUpper),
                ForeignObjectSize = _params.ForeignObjectSize,
                FirstPeekValueThreshold = _params.FirstPeekValueThreshold,
                PeekValueThreshold = _params.PeekValueThreshold,
                Stdev = _params.Stdev,
                TopHatRadius = _params.TopHatRadius,
                TopHatThreshold = _params.TopHatThreshold,
                MinForeignAreaFilterSize = _params.MinForeignAreaFilterSize,
                LinkDistance = _params.LinkDistance,
                PortentiolDefactMinSize = _params.PortentiolDefactMinSize,
                UseContaminationInspection = _params.UseContaminationInspection,
                IsSaveGoodImage = _params.SaveInspectImages,
                FileSavePath = _params.SaveInspectImages
                    ? Path.Combine(Path.GetDirectoryName(path) ?? ".", "_inspect_out")
                    : Path.GetTempPath(),
                WaferID = "TEST",
                IndexX = 0,
                IndexY = 0
            };

            var sw = Stopwatch.StartNew();
            BottomResult result = inspector.BottomInspect(bip);
            sw.Stop();

            return new InspectRecord
            {
                FilePath = path,
                Result = result,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Worker = workerId,
                OverlayScale = overlayScale
            };
        }

        /// <summary>이미지 파일 → 8bit 그레이 배열(24bpp 경유 — 인덱스/컬러 포맷 모두 처리).</summary>
        private static byte[] LoadGray(string path, out int w, out int h)
        {
            using (var src = new Bitmap(path))
            {
                w = src.Width; h = src.Height;
                using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, new Rectangle(0, 0, w, h));
                    var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var gray = new byte[w * h];
                        var row = new byte[Math.Abs(data.Stride)];
                        for (int y = 0; y < h; y++)
                        {
                            Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                            int o = y * w;
                            for (int x = 0; x < w; x++)
                            {
                                int i = x * 3;
                                gray[o + x] = (byte)((row[i] + row[i + 1] + row[i + 2]) / 3);
                            }
                        }
                        return gray;
                    }
                    finally { bmp.UnlockBits(data); }
                }
            }
        }

        /// <summary>1배 그레이를 2배 bilinear 확장 — 검사부(bSimulate)의 '2배 확장 이미지' 입력 규약 충족용.</summary>
        private static byte[] Upscale2xBilinear(byte[] src, int w, int h, out int w2, out int h2)
        {
            w2 = w * 2; h2 = h * 2;
            var dst = new byte[w2 * h2];
            int dw = w2;
            Parallel.For(0, h2, y =>
            {
                double sy = (y + 0.5) * 0.5 - 0.5;
                int y0 = (int)Math.Floor(sy);
                double fy = sy - y0;
                int y1 = y0 + 1;
                if (y0 < 0) { y0 = 0; y1 = 0; fy = 0; }
                else if (y1 >= h) { y1 = h - 1; y0 = Math.Min(y0, h - 1); }
                int row = y * dw;
                for (int x = 0; x < dw; x++)
                {
                    double sx = (x + 0.5) * 0.5 - 0.5;
                    int x0 = (int)Math.Floor(sx);
                    double fx = sx - x0;
                    int x1 = x0 + 1;
                    if (x0 < 0) { x0 = 0; x1 = 0; fx = 0; }
                    else if (x1 >= w) { x1 = w - 1; x0 = Math.Min(x0, w - 1); }
                    double v = src[y0 * w + x0] * (1 - fx) * (1 - fy)
                             + src[y0 * w + x1] * fx * (1 - fy)
                             + src[y1 * w + x0] * (1 - fx) * fy
                             + src[y1 * w + x1] * fx * fy;
                    dst[row + x] = (byte)(v + 0.5);
                }
            });
            return dst;
        }

        // ── 표시 ──────────────────────────────────────────────────────

        /// <summary>결과 오버레이 표시 — 파일을 다시 로드해 코너/칩핑/이물 마크를 그린다(UI 스레드).</summary>
        private void ShowResult(InspectRecord rec)
        {
            Bitmap view;
            using (var src = new Bitmap(rec.FilePath))
                view = new Bitmap(src);   // 파일 잠금 해제용 복제

            try
            {
                using (var g = Graphics.FromImage(view))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    float s = rec.OverlayScale;
                    float penW = Math.Max(2f, view.Width / 1500f);

                    var r = rec.Result;
                    if (r != null)
                    {
                        bool pass = r.DefectCode == 0;
                        // 다이 코너
                        if (r.Corners != null && r.Corners.Length == 4)
                        {
                            var pts = r.Corners.Select(p => new PointF(p.X * s, p.Y * s)).ToArray();
                            using (var pen = new Pen(pass ? Color.LimeGreen : Color.Orange, penW))
                                g.DrawPolygon(pen, pts);
                        }
                        // 칩핑(주황 사각)
                        if (r.ChippingInfos != null)
                        {
                            using (var pen = new Pen(Color.DarkOrange, penW))
                                foreach (var ci in r.ChippingInfos)
                                {
                                    if (ci?.Contour == null || ci.Contour.Count == 0) continue;
                                    float minX = ci.Contour.Min(p => p.X) * s, maxX = ci.Contour.Max(p => p.X) * s;
                                    float minY = ci.Contour.Min(p => p.Y) * s, maxY = ci.Contour.Max(p => p.Y) * s;
                                    g.DrawRectangle(pen, minX, minY, Math.Max(4, maxX - minX), Math.Max(4, maxY - minY));
                                }
                        }
                        // 이물(자홍 원)
                        if (r.ForeignInfos != null)
                        {
                            using (var pen = new Pen(Color.Magenta, penW))
                                foreach (var fi in r.ForeignInfos)
                                {
                                    if (fi == null) continue;
                                    g.DrawEllipse(pen, fi.Rect.X * s, fi.Rect.Y * s,
                                        Math.Max(6, fi.Rect.Width * s), Math.Max(6, fi.Rect.Height * s));
                                }
                        }
                    }

                    string caption = Path.GetFileName(rec.FilePath) + "   " + DescribeResult(rec);
                    using (var f = new Font("Consolas", Math.Max(10f, view.Width / 90f), FontStyle.Bold))
                    using (var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    using (var fg = new SolidBrush(r != null && r.DefectCode == 0 ? Color.LimeGreen : Color.OrangeRed))
                    {
                        var sz = g.MeasureString(caption, f);
                        g.FillRectangle(bg, 4, 4, sz.Width + 8, sz.Height + 4);
                        g.DrawString(caption, f, fg, 8, 6);
                    }
                }
            }
            catch { /* 오버레이 실패해도 원본은 표시 */ }

            var old = picView.Image;
            picView.Image = view;
            old?.Dispose();
        }

        private void AddResultRow(InspectRecord rec, bool select)
        {
            var r = rec.Result;
            string verdict = rec.Error != null ? "ERR"
                           : r == null ? "NULL(미검출)"
                           : r.DefectCode == 0 ? "OK" : "NG(" + r.DefectCode + ")";
            double chipMax = r == null ? 0 : Math.Max(Math.Max(r.ChppingTopSize, r.ChppingBottomSize), Math.Max(r.ChppingLeftSize, r.ChppingRightSize));
            double foreign = r == null ? 0 : Math.Max(r.ForeingSize, r.MaxDefactSize);

            var item = new ListViewItem(new[]
            {
                Path.GetFileName(rec.FilePath),
                verdict,
                r != null ? r.Width.ToString("F4") : "-",
                r != null ? r.Height.ToString("F4") : "-",
                r != null ? r.Angle.ToString("F3") : "-",
                r != null ? chipMax.ToString("F4") : "-",
                r != null ? foreign.ToString("F4") : "-",
                rec.ElapsedMs.ToString("F0"),
                rec.Worker.ToString()
            })
            { Tag = rec };
            if (rec.Error != null || r == null || r.DefectCode != 0)
                item.ForeColor = Color.Firebrick;

            lvResults.Items.Add(item);
            item.EnsureVisible();
            if (select) { item.Selected = true; }
        }

        private static string DescribeResult(InspectRecord rec)
        {
            if (rec.Error != null) return "예외: " + rec.Error;
            var r = rec.Result;
            if (r == null) return "검사 결과 없음(null — 미검출/스펙아웃, " + rec.ElapsedMs.ToString("F0") + "ms)";
            return (r.DefectCode == 0 ? "OK" : "NG(code " + r.DefectCode + ")")
                 + "  W " + r.Width.ToString("F4") + "  H " + r.Height.ToString("F4")
                 + "  θ " + r.Angle.ToString("F3")
                 + "  (" + rec.ElapsedMs.ToString("F0") + "ms)";
        }

        private void SetBusy(bool busy)
        {
            btnPrev.Enabled = !busy;
            btnNext.Enabled = !busy;
            btnFolder.Enabled = !busy;
            lstFiles.Enabled = !busy;
            numThreads.Enabled = !busy;
            grdParams.Enabled = !busy;
            // btnPlay 는 재생 중 [정지] 역할이므로 항상 활성.
        }

        private void SetStatus(string msg, bool err = false)
        {
            lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray;
            lblStatus.Text = msg;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _playCts?.Cancel(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
