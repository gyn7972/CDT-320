using System;
using System.Drawing;
using System.IO;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Sim
{
    /// <summary>시뮬 Finder — TrainRoi 중심 좌표를 랜덤 drift로 반환.</summary>
    public class SimPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.5;
        public int    MaxInstances    { get; set; } = 1;

        private readonly Random _rnd = new Random();

        public SimPatternFinder(string id)
        {
            Id = id;
            SearchRoi = new Roi { Name = id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
            TrainRoi  = new Roi { Name = id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
        }

        public void Train(Bitmap image)
        {
            if (image == null || TrainRoi == null) return;
            var rect = TrainRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            TrainImage?.Dispose();
            TrainImage = image.Clone(rect, image.PixelFormat);
        }

        public MatchResult Match(Bitmap image)
        {
            if (image == null) return MatchResult.Fail(Id, "null image");

            var r = new MatchResult { RoiName = Id, Success = true };
            int count = Math.Min(MaxInstances, 1 + _rnd.Next(2));
            for (int i = 0; i < count; i++)
            {
                double driftX = (_rnd.NextDouble() - 0.5) * 4.0;
                double driftY = (_rnd.NextDouble() - 0.5) * 4.0;
                double angle  = (_rnd.NextDouble() - 0.5) * 1.0;
                double score  = 0.85 + _rnd.NextDouble() * 0.12;
                r.Instances.Add(new MatchInstance
                {
                    Index = i,
                    CenterX  = TrainRoi.CenterX + driftX,
                    CenterY  = TrainRoi.CenterY + driftY,
                    AngleDeg = angle,
                    Score    = score
                });
            }
            return r;
        }

        public void LoadParameters(string path) { /* sim: no-op */ }
        public void SaveParameters(string path)
        {
            try { File.WriteAllText(path, "SimPatternFinder: " + Id); } catch { }
        }
    }
}
