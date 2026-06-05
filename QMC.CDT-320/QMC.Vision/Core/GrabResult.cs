using System;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>카메라 1장 grab 결과.</summary>
    public class GrabResult : IDisposable
    {
        public Bitmap Image      { get; }
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

        public void Dispose() { Image?.Dispose(); }
    }
}
