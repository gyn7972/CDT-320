using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 픽커 1개 표시 — 모드에 따라 단일 이미지(Bottom/Bin) 또는 4채널(Side)을 보여준다.
    ///  • 단일: VisionImageView 1개(검출 오버레이 포함)
    ///  • 4채널: Front ch1/ch2, Back ch1/ch2 (측면 이미지 4000×700 스트립) — 라벨 + VisionImageView 4개
    /// 크로스라인 ON/OFF 는 하위 모든 뷰에 전파된다.
    /// </summary>
    public class PickerView : UserControl
    {
        private static readonly string[] ChannelNames =
            { "Front channel 1", "Front channel 2", "Back channel 1", "Back channel 2" };

        private readonly VisionImageView _single = new VisionImageView();
        private readonly TableLayoutPanel _channelHost = new TableLayoutPanel();
        private readonly VisionImageView[] _ch = new VisionImageView[4];

        public PickerView()
        {
            BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);

            _single.Dock = DockStyle.Fill;
            Controls.Add(_single);

            _channelHost.Dock = DockStyle.Fill;
            _channelHost.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);
            _channelHost.ColumnCount = 1;
            _channelHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _channelHost.RowCount = 4;
            for (int i = 0; i < 4; i++)
            {
                _channelHost.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                var cell = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 2), BackColor = Color.FromArgb(0x14, 0x14, 0x17) };
                var lbl = new Label
                {
                    Dock = DockStyle.Top, Height = 14,
                    Text = ChannelNames[i],
                    ForeColor = Color.Gainsboro,
                    Font = new Font("Segoe UI", 7.5F),
                    Padding = new Padding(4, 1, 0, 0)
                };
                _ch[i] = new VisionImageView { Dock = DockStyle.Fill };
                cell.Controls.Add(_ch[i]);
                cell.Controls.Add(lbl);
                _channelHost.Controls.Add(cell, 0, i);
            }
            _channelHost.Visible = false;
            Controls.Add(_channelHost);
        }

        /// <summary>단일 이미지 모드(Bottom/Bin) — 이미지 + 검출 오버레이.</summary>
        public void SetSingle(Bitmap bmp, PointF[] box, bool pass, string verdict, string[] lines, PointF[] marks)
        {
            _single.SetImage(bmp);
            _single.SetOverlay(box, pass, verdict, lines, marks);
            ShowSingle();
        }

        /// <summary>4채널 모드(Side) — Front ch1/2, Back ch1/2 이미지.</summary>
        public void SetChannels(Bitmap[] images)
        {
            if (images != null)
                for (int i = 0; i < 4 && i < images.Length; i++)
                    _ch[i].SetImage(images[i]);
            ShowChannels();
        }

        /// <summary>4채널 모드 빈칸(Side 기본) — 모든 채널 NO IMAGE + 오버레이 제거. 시퀀서가 채움.</summary>
        public void ClearChannels()
        {
            for (int i = 0; i < 4; i++) { _ch[i].SetImage(null); _ch[i].ClearOverlay(); }
            ShowChannels();
        }

        /// <summary>4채널 중 한 채널 이미지+검출 오버레이 설정(Side 시퀀서/실데이터).</summary>
        public void SetChannel(int idx, Bitmap bmp, PointF[] box, bool pass, string verdict, string[] lines, PointF[] marks)
        {
            if (idx < 0 || idx >= 4) return;
            _ch[idx].SetImage(bmp);
            _ch[idx].SetOverlay(box, pass, verdict, lines, marks);
            ShowChannels();
        }

        private void ShowSingle()
        {
            _channelHost.Visible = false;
            _single.Visible = true; _single.BringToFront();
        }
        private void ShowChannels()
        {
            _single.Visible = false;
            _channelHost.Visible = true; _channelHost.BringToFront();
        }

        /// <summary>크로스라인 ON/OFF 를 모든 하위 뷰에 적용.</summary>
        public void SetCrossline(bool on)
        {
            _single.Crossline = on;
            foreach (var v in _ch) if (v != null) v.Crossline = on;
        }
    }
}
