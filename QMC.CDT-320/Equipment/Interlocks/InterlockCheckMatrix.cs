using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Interlocks
{
    public enum InterlockTargetKind
    {
        Axis,
        Cylinder
    }

    public sealed class InterlockCheckPair
    {
        public InterlockCheckPair(string movingName, string checkName, string sourceCell)
        {
            MovingName = movingName;
            CheckName = checkName;
            MovingKey = InterlockCheckMatrix.NormalizeName(movingName);
            CheckKey = InterlockCheckMatrix.NormalizeName(checkName);
            SourceCell = sourceCell;
        }

        public string MovingName { get; private set; }
        public string CheckName { get; private set; }
        public string MovingKey { get; private set; }
        public string CheckKey { get; private set; }
        public string SourceCell { get; private set; }
        public InterlockTargetKind MovingKind { get { return InterlockCheckMatrix.ResolveKind(MovingKey); } }
        public InterlockTargetKind CheckKind { get { return InterlockCheckMatrix.ResolveKind(CheckKey); } }
    }

    public sealed class InterlockCheckMatrix
    {
        private static readonly string[] CylinderKeys =
        {
            "InputFeederLift",
            "InputFeederClamp",
            "ReticleLift",
            "ReticleSideSlideFront",
            "ReticleSideSlideRear",
            "GoodBinGuideLift",
            "GoodBinGuideClampLift",
            "GoodBinGuideClamp",
            "NGBinGuideLift",
            "NGBinGuideClampLift",
            "NGBinGuideClamp",
            "OutputFeederLift",
            "OutputFeederClamp"
        };

        private static readonly Dictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "FeederY", "WaferFeederY" },
                { "StageY", "WaferStageY" },
                { "StageT", "WaferStageT" },
                { "ExpanderZ", "WaferExpandingZ" },
                { "CameraX", "WaferVisionX" },
                { "NeedleBlockX", "NeedleX" },
                { "WaferY", "WaferStageY" },
                { "WaferT", "WaferStageT" },
                { "GoodBinY", "BinGoodY" },
                { "GoodBinZ", "BinGoodZ" },
                { "NgBinY", "BinNgY" },
                { "Feeder Up/Down", "InputFeederLift" },
                { "Feeder Clamp/UnClamp", "InputFeederClamp" },
                { "Reticle Up/Down", "ReticleLift" },
                { "Reticle Front FW/BW", "ReticleSideSlideFront" },
                { "Reticle Back FW/BW", "ReticleSideSlideRear" },
                { "GoodBinGuide Up/Down", "GoodBinGuideLift" },
                { "GoodBinClamp Up/Down", "GoodBinGuideClampLift" },
                { "GoodBin Clamp/UnClamp", "GoodBinGuideClamp" },
                { "NgBinGuide Up/Down", "NGBinGuideLift" },
                { "NgBinClamp Up/Down", "NGBinGuideClampLift" },
                { "NgBin Clamp/UnClamp", "NGBinGuideClamp" },
                { "BinFeeder Up/Down", "OutputFeederLift" },
                { "BinFeeder Clamp/UnClamp", "OutputFeederClamp" }
            };

        public static readonly InterlockCheckMatrix Default = new InterlockCheckMatrix(CreateDefaultPairs());

        private readonly List<InterlockCheckPair> _pairs;
        private readonly Dictionary<string, List<InterlockCheckPair>> _byMovingKey;

        public InterlockCheckMatrix(IEnumerable<InterlockCheckPair> pairs)
        {
            _pairs = (pairs ?? Enumerable.Empty<InterlockCheckPair>()).ToList();
            _byMovingKey = new Dictionary<string, List<InterlockCheckPair>>(StringComparer.OrdinalIgnoreCase);

            foreach (InterlockCheckPair pair in _pairs)
            {
                List<InterlockCheckPair> list;
                if (!_byMovingKey.TryGetValue(pair.MovingKey, out list))
                {
                    list = new List<InterlockCheckPair>();
                    _byMovingKey[pair.MovingKey] = list;
                }
                list.Add(pair);
            }
        }

        public IReadOnlyList<InterlockCheckPair> AllPairs { get { return _pairs; } }

        public IReadOnlyList<InterlockCheckPair> GetChecksFor(string movingName)
        {
            string key = NormalizeName(movingName);
            List<InterlockCheckPair> list;
            if (!_byMovingKey.TryGetValue(key, out list))
                return new InterlockCheckPair[0];
            return list;
        }

        public bool RequiresCheck(string movingName, string checkName)
        {
            string checkKey = NormalizeName(checkName);
            foreach (InterlockCheckPair pair in GetChecksFor(movingName))
            {
                if (string.Equals(pair.CheckKey, checkKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string trimmed = name.Trim();
            string alias;
            return Aliases.TryGetValue(trimmed, out alias) ? alias : trimmed;
        }

        public static InterlockTargetKind ResolveKind(string normalizedName)
        {
            for (int i = 0; i < CylinderKeys.Length; i++)
            {
                if (string.Equals(CylinderKeys[i], normalizedName, StringComparison.OrdinalIgnoreCase))
                    return InterlockTargetKind.Cylinder;
            }
            return InterlockTargetKind.Axis;
        }

        private static InterlockCheckPair P(string movingName, string checkName, string sourceCell)
        {
            return new InterlockCheckPair(movingName, checkName, sourceCell);
        }

        private static InterlockCheckPair[] CreateDefaultPairs()
        {
            return new[]
            {
                P("WaferLifterZ", "WaferFeederY", "E8"),
                P("WaferLifterZ", "Feeder Up/Down", "F8"),
                P("WaferLifterZ", "Feeder Clamp/UnClamp", "G8"),
                P("WaferLifterZ", "WaferVisionX", "K8"),
                P("WaferFeederY", "WaferLifterZ", "D9"),
                P("WaferFeederY", "Feeder Up/Down", "F9"),
                P("WaferFeederY", "Feeder Clamp/UnClamp", "G9"),
                P("WaferFeederY", "WaferY", "H9"),
                P("WaferFeederY", "WaferT", "I9"),
                P("WaferFeederY", "WaferExpandingZ", "J9"),
                P("Feeder Up/Down", "WaferLifterZ", "D10"),
                P("Feeder Up/Down", "WaferFeederY", "E10"),
                P("Feeder Up/Down", "Feeder Clamp/UnClamp", "G10"),
                P("Feeder Up/Down", "WaferY", "H10"),
                P("Feeder Up/Down", "WaferT", "I10"),
                P("Feeder Up/Down", "WaferExpandingZ", "J10"),
                P("Feeder Clamp/UnClamp", "WaferLifterZ", "D11"),
                P("Feeder Clamp/UnClamp", "WaferFeederY", "E11"),
                P("Feeder Clamp/UnClamp", "Feeder Up/Down", "F11"),
                P("Feeder Clamp/UnClamp", "WaferY", "H11"),
                P("Feeder Clamp/UnClamp", "WaferExpandingZ", "J11"),
                P("WaferY", "WaferFeederY", "E12"),
                P("WaferY", "Feeder Up/Down", "F12"),
                P("WaferY", "Feeder Clamp/UnClamp", "G12"),
                P("WaferY", "WaferExpandingZ", "J12"),
                P("WaferY", "NeedleX", "L12"),
                P("WaferY", "NeedleZ", "M12"),
                P("WaferY", "EjectPinZ", "N12"),
                P("WaferT", "WaferFeederY", "E13"),
                P("WaferT", "Feeder Up/Down", "F13"),
                P("WaferExpandingZ", "WaferFeederY", "E14"),
                P("WaferExpandingZ", "Feeder Up/Down", "F14"),
                P("WaferVisionX", "WaferFeederY", "E15"),
                P("WaferVisionX", "FrontPickerX", "R15"),
                P("WaferVisionX", "RearPickerX", "AD15"),
                P("NeedleX", "WaferY", "H16"),
                P("NeedleX", "NeedleZ", "M16"),
                P("NeedleX", "EjectPinZ", "N16"),
                P("NeedleZ", "WaferY", "H17"),
                P("NeedleZ", "NeedleX", "L17"),
                P("EjectPinZ", "WaferY", "H18"),
                P("EjectPinZ", "NeedleX", "L18"),
                P("Reticle Up/Down", "Reticle Front FW/BW", "P19"),
                P("Reticle Up/Down", "Reticle Back FW/BW", "Q19"),
                P("Reticle Up/Down", "FrontPickerZ0", "U19"),
                P("Reticle Up/Down", "FrontPickerZ1", "W19"),
                P("Reticle Up/Down", "FrontPickerZ2", "Y19"),
                P("Reticle Up/Down", "FrontPickerZ3", "AA19"),
                P("Reticle Up/Down", "RearPickerZ0", "AG19"),
                P("Reticle Up/Down", "RearPickerZ1", "AI19"),
                P("Reticle Up/Down", "RearPickerZ2", "AK19"),
                P("Reticle Up/Down", "RearPickerZ3", "AM19"),
                P("Reticle Front FW/BW", "Reticle Up/Down", "O20"),
                P("Reticle Front FW/BW", "FrontPickerZ0", "U20"),
                P("Reticle Front FW/BW", "FrontPickerZ1", "W20"),
                P("Reticle Front FW/BW", "FrontPickerZ2", "Y20"),
                P("Reticle Front FW/BW", "FrontPickerZ3", "AA20"),
                P("Reticle Front FW/BW", "RearPickerZ0", "AG20"),
                P("Reticle Front FW/BW", "RearPickerZ1", "AI20"),
                P("Reticle Front FW/BW", "RearPickerZ2", "AK20"),
                P("Reticle Front FW/BW", "RearPickerZ3", "AM20"),
                P("Reticle Back FW/BW", "Reticle Up/Down", "O21"),
                P("Reticle Back FW/BW", "FrontPickerZ0", "U21"),
                P("Reticle Back FW/BW", "FrontPickerZ1", "W21"),
                P("Reticle Back FW/BW", "FrontPickerZ2", "Y21"),
                P("Reticle Back FW/BW", "FrontPickerZ3", "AA21"),
                P("Reticle Back FW/BW", "RearPickerZ0", "AG21"),
                P("Reticle Back FW/BW", "RearPickerZ1", "AI21"),
                P("Reticle Back FW/BW", "RearPickerZ2", "AK21"),
                P("Reticle Back FW/BW", "RearPickerZ3", "AM21"),
                P("FrontPickerX", "WaferFeederY", "E22"),
                P("FrontPickerX", "WaferVisionX", "K22"),
                P("FrontPickerX", "Reticle Up/Down", "O22"),
                P("FrontPickerX", "Reticle Front FW/BW", "P22"),
                P("FrontPickerX", "Reticle Back FW/BW", "Q22"),
                P("FrontPickerX", "RearPickerX", "AD22"),
                P("FrontPickerY", "Reticle Up/Down", "O23"),
                P("FrontPickerY", "Reticle Front FW/BW", "P23"),
                P("FrontPickerY", "Reticle Back FW/BW", "Q23"),
                P("FrontPickerY", "RearPickerY", "AE23"),
                P("NgBinY", "FrontPickerZ0", "U46"),
                P("NgBinY", "FrontPickerZ1", "W46"),
                P("NgBinY", "FrontPickerZ2", "Y46"),
                P("NgBinY", "FrontPickerZ3", "AA46"),
                P("NgBinY", "RearPickerZ0", "AG46"),
                P("NgBinY", "RearPickerZ1", "AI46"),
                P("NgBinY", "RearPickerZ2", "AK46"),
                P("NgBinY", "RearPickerZ3", "AM46"),
                P("NgBinY", "GoodBinGuide Up/Down", "AR46"),
                P("NgBinY", "GoodBinClamp Up/Down", "AS46"),
                P("NgBinY", "GoodBin Clamp/UnClamp", "AT46"),
                P("NgBinY", "NgBinGuide Up/Down", "AU46"),
                P("NgBinY", "NgBinClamp Up/Down", "AV46"),
                P("NgBinY", "NgBin Clamp/UnClamp", "AW46"),
                P("NgBinY", "BinFeederY", "AX46"),
                P("NgBinY", "BinFeeder Up/Down", "AY46"),
                P("NgBinY", "BinFeeder Clamp/UnClamp", "AZ46"),
                P("BinVisionX", "FrontPickerX", "R47"),
                P("BinVisionX", "RearPickerX", "AD47"),
                P("BinFeederY", "FrontPickerX", "R54"),
                P("BinFeederY", "RearPickerX", "AD54"),
                P("BinFeederY", "BinVisionX", "AQ54"),
                P("BinFeederY", "BinFeeder Up/Down", "AY54"),
                P("BinFeederY", "BinFeeder Clamp/UnClamp", "AZ54"),
                P("BinFeederY", "BinLifterZ", "BA54"),
                P("BinFeeder Up/Down", "BinFeederY", "AX55"),
                P("BinFeeder Up/Down", "BinFeeder Clamp/UnClamp", "AZ55"),
                P("BinFeeder Up/Down", "BinLifterZ", "BA55"),
                P("BinFeeder Clamp/UnClamp", "BinFeederY", "AX56"),
                P("BinFeeder Clamp/UnClamp", "BinFeeder Up/Down", "AY56"),
                P("BinFeeder Clamp/UnClamp", "BinLifterZ", "BA56"),
                P("BinLifterZ", "BinFeederY", "AX57"),
                P("BinLifterZ", "BinFeeder Up/Down", "AY57"),
                P("BinLifterZ", "BinFeeder Clamp/UnClamp", "AZ57")
            };
        }
    }
}
