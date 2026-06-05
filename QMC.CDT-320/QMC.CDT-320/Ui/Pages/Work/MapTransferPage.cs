using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    /// <summary>
    /// INPUT 맵 전환 / OUTPUT 맵 전환 공용.
    /// 좌측 큰 맵 영역 + 우측 설정 값 + MODE + 조그 패드.
    /// Stage 58 — 빈 panel placeholder 대신 실제 DieMapView 로 교체. 현재 활성 Lot 이 있으면
    /// 그 진행 상황을 격자 색상으로 표시, 없으면 첫번째 Recipe 의 Frame 으로 빈 격자 표시.
    /// </summary>
    public class MapTransferPage : PageBase
    {
        private DieMapView _mapView;
        private Label      _vAxisX, _vAxisY, _vBinRank, _vDieNum, _vChipW, _vChipH, _vPitchX, _vPitchY, _vWaferDia;
        private System.Windows.Forms.Timer _refresh;
        private string _i18nTitle;

        public MapTransferPage(string titleI18n)
        {
            _i18nTitle = titleI18n;
            Controls.Add(CreateSectionHeader(titleI18n));

            // 상단 얇은 상태 바 (Project / Barcode / Bin)
            var tinyStatus = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = UiTheme.StatusBarBg };
            tinyStatus.Controls.Add(new Label { Location = new Point(10, 3),  AutoSize = true, Text = Lang.T("status.mapEmpty"),        Tag = "i18n:status.mapEmpty",        ForeColor = Color.White, Font = UiTheme.StatusBarFont });
            tinyStatus.Controls.Add(new Label { Location = new Point(80, 3),  AutoSize = true, Text = Lang.T("status.project") + " " + GetCurrentProjectName(), ForeColor = Color.White, Font = UiTheme.StatusBarFont });
            tinyStatus.Controls.Add(new Label { Location = new Point(400, 3), AutoSize = true, Text = Lang.T("status.barcode"),        Tag = "i18n:status.barcode",         ForeColor = Color.White, Font = UiTheme.StatusBarFont });
            tinyStatus.Controls.Add(new Label { Location = new Point(680, 3), AutoSize = true, Text = Lang.T("status.bin"),            Tag = "i18n:status.bin",             ForeColor = Color.White, Font = UiTheme.StatusBarFont });
            Controls.Add(tinyStatus);
            Controls.SetChildIndex(tinyStatus, 0);

            // 좌측: 다이맵 헤더 + DieMapView
            var mapHdr = new Label
            {
                Location = new Point(8, 66), Size = new Size(1000, 26),
                Text = Lang.T("recipe.inputMapCreate"), Tag = "i18n:recipe.inputMapCreate",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(mapHdr);

            _mapView = new DieMapView
            {
                Location = new Point(8, 96),
                Size = new Size(1000, 820),
                Caption = Lang.T(_i18nTitle)
            };
            Controls.Add(_mapView);

            // 우측 컬럼
            int x = 1030;
            int y = 66;

            _vChipW    = AddPair(x, y, 110, 110, "칩 가로",     "0"); y += 32;
            _vChipH    = AddPair(x, y, 110, 110, "칩 세로",     "0"); y += 32;
            _vPitchX   = AddPair(x, y, 110, 110, "칩 PITCH X",  "0"); y += 32;
            _vPitchY   = AddPair(x, y, 110, 110, "칩 PITCH Y",  "0"); y += 32;
            _vWaferDia = AddPair(x, y, 110, 110, "Wafer 지름", "0"); y += 32;
            _vAxisX    = AddPair(x, y, 110, 110, "Axis X",      "0"); y += 32;
            _vAxisY    = AddPair(x, y, 110, 110, "Axis Y",      "0"); y += 32;
            _vBinRank  = AddPair(x, y, 110, 110, "BIN RANK",    "0"); y += 32;
            _vDieNum   = AddPair(x, y, 110, 110, "Die Number",  "0/0"); y += 32;

            // MODE 라디오
            var grp = new GroupBox
            {
                Location = new Point(x, y + 10), Size = new Size(230, 130),
                Text = "MODE", Font = UiTheme.SectionFont
            };
            var r1 = new RadioButton { Location = new Point(12, 22), AutoSize = true, Text = "STANDARD",           Checked = true };
            var r2 = new RadioButton { Location = new Point(12, 48), AutoSize = true, Text = "START INDEX" };
            var r3 = new RadioButton { Location = new Point(12, 74), AutoSize = true, Text = "SELECT PICK STATUS" };
            var r4 = new RadioButton { Location = new Point(12, 100),AutoSize = true, Text = "DRAG PICK STATUS" };
            grp.Controls.Add(r1); grp.Controls.Add(r2); grp.Controls.Add(r3); grp.Controls.Add(r4);
            Controls.Add(grp);

            y += 150;
            var btnSave = new Controls.ActionButton { Location = new Point(x, y), Size = new Size(220, 40), Text = "SELECT PICK STATUS SAVE" };
            Controls.Add(btnSave);

            // 조그 패드
            y += 60;
            var jog = new Panel { Location = new Point(x, y), Size = new Size(230, 220), BackColor = UiTheme.MainBg };
            jog.Controls.Add(new Button { Location = new Point(10, 10), Size = new Size(60, 60), Text = "▲", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            jog.Controls.Add(new Button { Location = new Point(10, 80), Size = new Size(60, 60), Text = "◀", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            jog.Controls.Add(new Button { Location = new Point(80, 80), Size = new Size(60, 60), Text = "▶", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            jog.Controls.Add(new Button { Location = new Point(10, 150),Size = new Size(60, 60), Text = "▼", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            jog.Controls.Add(new Button { Location = new Point(150, 10),Size = new Size(60, 60), Text = "↺", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            jog.Controls.Add(new Button { Location = new Point(150, 80),Size = new Size(60, 60), Text = "↻", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            Controls.Add(jog);

            y += 230;
            Controls.Add(new Controls.ActionButton { Location = new Point(x, y),      Size = new Size(220, 40), Text = "매뉴얼 얼라인 완료" });
            Controls.Add(new Controls.ActionButton { Location = new Point(x, y + 50), Size = new Size(220, 40), Text = "NEEDLE BLOCK 하강" });
            Controls.Add(new Controls.ActionButton { Location = new Point(x, y + 100),Size = new Size(220, 40), Text = "THETA MATCH MOVE" });
            Controls.Add(new Controls.ActionButton { Location = new Point(x, y + 150),Size = new Size(220, 40), Text = "X_Y_MATCH MOVE" });
            var btnClose = new Button { Location = new Point(x, y + 210), Size = new Size(220, 40), Text = "CLOSE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.FromArgb(0xD0,0xD0,0xD0) };
            Controls.Add(btnClose);

            // 셀 클릭 시 우측 표 갱신
            _mapView.CellClicked += (entry) =>
            {
                if (entry == null) return;
                if (_vAxisX != null)  _vAxisX.Text  = entry.X.ToString("F2");
                if (_vAxisY != null)  _vAxisY.Text  = entry.Y.ToString("F2");
                if (_vBinRank != null)_vBinRank.Text= entry.BinCode.ToString();
                if (_vDieNum != null) _vDieNum.Text = string.Format("[{0},{1}] / {2}", entry.GridX, entry.GridY,
                                                          (_mapView.Map != null ? _mapView.Map.TotalCells : 0));
            };

            if (!IsDesignerMode())
            {
                BuildOrFetchMap();
                _refresh = new System.Windows.Forms.Timer { Interval = 1500 };
                _refresh.Tick += (s, e) => { try { ApplyLotProgress(); } catch { } };
                _refresh.Start();
            }
        }

        private string GetCurrentProjectName()
        {
            try
            {
                var lot = LotStorage.ActiveLot;
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName)) return lot.RecipeName;
                var list = RecipeStore.List();
                if (list != null && list.Count > 0) return System.IO.Path.GetFileNameWithoutExtension(list[0]);
            }
            catch { }
            return "—";
        }

        private void BuildOrFetchMap()
        {
            try
            {
                // 첫 RecipeProject 의 Frame 으로 wafer 격자 생성
                var list = RecipeStore.List();
                if (list == null || list.Count == 0) return;
                var rp = RecipeStore.Load(list[0]);
                if (rp == null || rp.Frame == null) return;
                var frame = new DieTapeFrame
                {
                    ObjId  = rp.Frame.FrameSpecName,
                    GridX  = Math.Max(1, rp.Frame.GridX),
                    GridY  = Math.Max(1, rp.Frame.GridY),
                    PitchX = rp.Frame.PitchX,
                    PitchY = rp.Frame.PitchY,
                    OriginX = 0, OriginY = 0,
                    Rotate = TapeFrameRotate.None
                };
                var map = DieMapGenerator.Generate(frame);
                _mapView.Map = map;

                if (_vChipW != null)    _vChipW.Text    = (rp.Die != null ? rp.Die.WidthMm  : 1.0).ToString("F3");
                if (_vChipH != null)    _vChipH.Text    = (rp.Die != null ? rp.Die.HeightMm : 1.0).ToString("F3");
                if (_vPitchX != null)   _vPitchX.Text   = rp.Frame.PitchX.ToString("F3");
                if (_vPitchY != null)   _vPitchY.Text   = rp.Frame.PitchY.ToString("F3");
                if (_vWaferDia != null) _vWaferDia.Text = rp.Frame.OuterDiameterMm.ToString("F0");
            }
            catch { }
        }

        // 활성 Lot 의 ProcessedDies / Good / NG 를 DieMap 셀에 누적해서 색상 갱신
        private void ApplyLotProgress()
        {
            var map = _mapView?.Map;
            if (map == null) return;
            var lot = LotStorage.ActiveLot;
            if (lot == null) { _mapView.Invalidate(); return; }

            int processed = lot.ProcessedDies;
            int good      = lot.GoodCount;
            int filled = 0, gFilled = 0;
            foreach (var e in map.Entries)
            {
                if (filled < processed)
                {
                    if (gFilled < good) { e.Result = DieResult.Good; e.BinCode = QMC.CDT320.Bin.BinCodeMap.GoodBin; gFilled++; }
                    else { e.Result = DieResult.NG; e.BinCode = 110; }
                    filled++;
                }
                else
                {
                    e.Result = DieResult.Unknown; e.BinCode = 0;
                }
            }
            if (_vDieNum != null) _vDieNum.Text = processed + " / " + map.TotalCells;
            _mapView.Invalidate();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private Label AddPair(int x, int y, int labelW, int valueW, string label, string value)
        {
            Controls.Add(new Label { Location = new Point(x, y),            Size = new Size(labelW, 28), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            var v = new Label { Location = new Point(x + labelW, y),   Size = new Size(valueW, 28), Text = value, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0,0,6,0), BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(v);
            return v;
        }
    }
}
