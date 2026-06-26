using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 픽커 1개 표시 — 모드에 따라 단일 이미지(Bottom/Bin) 또는 4채널(Side)을 보여준다.
    ///  • 단일: VisionImageView 1개(검출 오버레이 포함)
    ///  • 4채널: Front ch1/ch2, Back ch1/ch2 (측면 이미지 4000×700 스트립) — 라벨 + VisionImageView 4개
    /// 크로스라인 ON/OFF 는 하위 모든 뷰에 전파된다.
    /// 레이아웃/컨트롤 = PickerView.Designer.cs. 본 파일은 로직(표시 모드/오버레이)만.
    /// </summary>
    public partial class PickerView : UserControl
    {
        // Designer 의 채널 뷰 4개를 인덱스 접근용 배열로 묶는다(_ch[0..3]).
        private readonly VisionImageView[] _ch;

        public PickerView()
        {
            InitializeComponent();
            _ch = new[] { _ch0, _ch1, _ch2, _ch3 };
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

        /// <summary>4채널 중 한 채널 이미지+검출 오버레이 설정(Side 시퀀서/실데이터). 반환: 이미지 표시 성공.</summary>
        public bool SetChannel(int idx, Bitmap bmp, PointF[] box, bool pass, string verdict, string[] lines, PointF[] marks)
        {
            if (idx < 0 || idx >= 4) return false;
            bool ok = _ch[idx].SetImage(bmp);
            _ch[idx].SetOverlay(box, pass, verdict, lines, marks);
            ShowChannels();
            return ok;
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
