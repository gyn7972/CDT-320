using System;
using System.Collections.Generic;

// Only the actual preflight service is compiled. These substitutes cannot connect to hardware,
// read operating recipes, download maps, or save Material/Barcode state.
namespace QMC.CDT320
{
    public sealed class AppSettings
    {
        public bool UseLotNetworkWaferMap;
        public string NetworkWaferMapFolder = "memory-network";
    }

    public static class AppSettingsStore
    {
        public static AppSettings Current;
    }

    public sealed class InputStageUnit
    {
        public StageRecipe Recipe = new StageRecipe();
        public Materials.WaferMaterial Wafer = new Materials.WaferMaterial();
        public Materials.WaferMaterial GetCurrentStageWaferMaterial() { return Wafer; }
    }

    public sealed class StageRecipe
    {
        public DieRecipe DieMap = new DieRecipe();
    }

    public sealed class DieRecipe
    {
        public string PickupBinFilterCsv = "1";
        public bool TryGetPickupBinFilter(out HashSet<int> bins)
        {
            bins = new HashSet<int>();
            foreach (string token in (PickupBinFilterCsv ?? "").Split(new[] { ',', ';', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int bin;
                if (int.TryParse(token, out bin) && bin > 0)
                    bins.Add(bin);
            }
            return bins.Count > 0;
        }
    }
}

namespace QMC.CDT320.Materials
{
    public sealed class WaferMaterial
    {
        public string TapeFrameSpecName = "RKE";
        public string BarcodeId = "UNCHANGED";
    }

    public sealed class TapeFrameSpec
    {
        public string Name = "RKE";
        public int DieMapX = 26, DieMapY = 34;
        public double DieSizeX = 10.37, DieSizeY = 7.913, PitchX = 0.1, PitchY = 0.1;
    }

    public static class MaterialSpecs
    {
        public static TapeFrameSpec Frame;
        public static TapeFrameSpec FindFrame(string name)
        {
            return Frame != null && string.Equals(Frame.Name, name, StringComparison.OrdinalIgnoreCase) ? Frame : null;
        }
    }

    public static class MaterialStateService
    {
        public static HashSet<int> Selected;
        public static string NormalizeInputTapeFrameSpecName(string name) { return name ?? ""; }
        public static bool IsPickupBinFilterActive(out HashSet<int> bins)
        {
            bins = Selected == null ? null : new HashSet<int>(Selected);
            return bins != null;
        }
        public static string DescribePickupBinSelection()
        {
            return Selected == null ? "All" : string.Join(",", Selected);
        }
    }
}

namespace QMC.CDT320.Recipes
{
    public sealed class TapeFrameSubset
    {
        public string FrameSpecName = "RKE";
        public int DieMapX = 26, DieMapY = 34;
        public double DieSizeX = 10.37, DieSizeY = 7.913, PitchX = 0.1, PitchY = 0.1;
    }

    public sealed class RecipeProject
    {
        public TapeFrameSubset InputFrame, Frame;
    }

    public static class RecipeStore
    {
        public static RecipeProject Project;
        public static int LoadCalls;
        public static RecipeProject LoadLastOrDefault() { LoadCalls++; return Project; }
    }
}

namespace QMC.CDT320.DieMaps
{
    public sealed class DieMap
    {
        public List<DieMapEntry> Entries = new List<DieMapEntry>();
        public double PitchX = 10.47, PitchY = 8.013;
    }

    public sealed class DieMapEntry
    {
        public int BinCode;
        public bool IsTarget = true;
        public int SequenceNo = 17;
    }

    public static class DieMapGenerator
    {
        public static double CalculateCenterStep(double die, double gap)
        {
            return (!double.IsNaN(die) && !double.IsInfinity(die) && die > 0.0 ? die : 1.0) +
                   (!double.IsNaN(gap) && !double.IsInfinity(gap) && gap >= 0.0 ? gap : 0.0);
        }
    }
}

namespace QMC.Common.Logging
{
    public enum EventKind { Warning }
    public static class EventLogger
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void Write(EventKind kind, params string[] text) { Warnings.Add(string.Join(" | ", text)); }
    }
}

namespace QMC.CDT320.Lots
{
    public sealed class LotWaferMapSlotInfo
    {
        public string LocalPath;
    }

    public static class LotWaferMapFetchService
    {
        public static bool FetchResult;
        public static int FetchCalls, ParseCalls;
        public static string LastBarcode, LastParsePath;
        public static Exception ParseError;
        public static Action DuringFetch;
        public static bool MissingLocalPath;
        public static DieMaps.DieMap Map;
        public static string ResolveNetworkFolder() { return AppSettingsStore.Current.NetworkWaferMapFolder; }
        public static bool TryFetchWaferMapByBarcode(string barcode, out LotWaferMapSlotInfo info, out string reason)
        {
            FetchCalls++;
            LastBarcode = barcode;
            if (DuringFetch != null)
                DuringFetch();
            info = new LotWaferMapSlotInfo { LocalPath = MissingLocalPath ? "" : "memory-cache/" + barcode };
            reason = "SMB timeout: source-specific detail";
            return FetchResult;
        }
        public static DieMaps.DieMap LoadConfiguredFormatOrThrow(string path)
        {
            ParseCalls++;
            LastParsePath = path;
            if (ParseError != null)
                throw ParseError;
            return Map;
        }
    }
}
