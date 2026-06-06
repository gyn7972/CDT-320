using System;
using System.Drawing;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Sim
{
    public class SimInspector : IInspector
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        private readonly Random _rnd = new Random();

        public SimInspector(string id)
        {
            Id = id;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
        }

        public InspectionResult Inspect(Bitmap image)
        {
            var r = new InspectionResult { RoiName = Id };
            bool pass = _rnd.NextDouble() > 0.05;
            r.IsPass = pass;
            r.Items.Add(new InspectionItem { Name = "Width",   Value = (200 + _rnd.NextDouble()).ToString("F2"), IsPass = pass });
            r.Items.Add(new InspectionItem { Name = "Height",  Value = (150 + _rnd.NextDouble()).ToString("F2"), IsPass = pass });
            r.Items.Add(new InspectionItem { Name = "Chipping",Value = (_rnd.NextDouble() * 0.3).ToString("F3"), IsPass = pass });
            return r;
        }

        public void LoadParameters(string path) { }
        public void SaveParameters(string path) { }
    }
}
