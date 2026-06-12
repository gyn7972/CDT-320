using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Core.Inspectors;
using QMC.Vision.Config;
using QMC.Common.Recipes;

// P1 스모크 — 디스크립터 공급자 전수: DescribeParameters 열거 + getter/setter 왕복 무예외 + 계층 집계.
class P1Smoke
{
    static int _fail = 0;
    static int Main()
    {
        var providers = new List<IParameterProvider>
        {
            new SimPatternFinder("WaferVision/ReticleFinder"),
            new SimInspector("BottomInspection/SurfaceInspector"),
            new BottomInspectionParameters(),
            new SideInspectionParameters(),
            new DieGapInspectionParameters(),
            new DistortionParameters(),
            new VisionScaleParameters(),
            new VisionSettingsParameters(new VisionSettings()),
            new CameraParameters(new AlgorithmCameraMapping { Algorithm = "Wafer" }),
            new LightingParameters("BottomInspection/SurfaceInspector", new InspectionLightSetting { ControllerPort = "COM3", Channel = 1 }),
        };

        int total = 0;
        var byLayer = new Dictionary<ParameterLayer, int> { {ParameterLayer.Setup,0}, {ParameterLayer.Config,0}, {ParameterLayer.Recipe,0} };
        foreach (var p in providers)
        {
            try
            {
                var ds = p.DescribeParameters().ToList();
                total += ds.Count;
                foreach (var d in ds)
                {
                    byLayer[d.Layer]++;
                    // getter/setter 왕복(값 보존) — 무예외 확인
                    object v = d.Getter();
                    d.Setter(v);
                    if (d.Validator != null && v != null) d.Validator(v);
                }
                Console.WriteLine($"  [OK] {p.ParameterTarget,-40} {ds.Count} params");
            }
            catch (Exception ex) { _fail++; Console.WriteLine($"  [FAIL] {p.ParameterTarget}: {ex.Message}"); }
        }
        Console.WriteLine($"TOTAL descriptors={total}  Setup={byLayer[ParameterLayer.Setup]} Config={byLayer[ParameterLayer.Config]} Recipe={byLayer[ParameterLayer.Recipe]}");
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
