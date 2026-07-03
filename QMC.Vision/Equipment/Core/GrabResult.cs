using System;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>카메라 1장 grab 결과.</summary>
    public class GrabResult : IDisposable
    {
        public Bitmap Image      { get; private set; }
        public DateTime GrabTime { get; }
        public int  Width        => Image?.Width  ?? 0;
        public int  Height       => Image?.Height ?? 0;
        public int  FrameNumber  { get; }
        public string Source     { get; }
        public bool   IsSuccess  { get; }
        public string ErrorMessage { get; }

        public GrabResult(Bitmap image, int frameNumber = 0, string source = "",
                          bool success = true, string errorMessage = null)
        {
            Image        = image;
            GrabTime     = DateTime.Now;
            FrameNumber  = frameNumber;
            Source       = source ?? "";
            IsSuccess    = success;
            ErrorMessage = errorMessage;
        }

        public static GrabResult Fail(string msg, string source = "")
            => new GrabResult(null, 0, source, false, msg);

        public static GrabResult Success(Bitmap image, int frameNumber = 0, string source = "loaded")
            => new GrabResult(image, frameNumber, source, true, null);

        /// <summary>이미지 소유권을 호출자로 이전 — 이후 Dispose 해도 이미지는 살아있다.
        /// 고해상도(수백 MB) 프레임을 사본 없이 넘길 때 사용(복제 비용 제거).</summary>
        public Bitmap DetachImage() { var img = Image; Image = null; return img; }

        public void Dispose() { Image?.Dispose(); }
    }
}
