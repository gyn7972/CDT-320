using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    public static class VisionInspectionCommands
    {
        public const string InspectSync = "INSPECT_SYNC";
        public const string InspectAsync = "INSPECT_ASYNC";
        public const string MResult = "MRESULT";
        public const string Result = "RESULT";

        public static bool IsInspectionRequest(string command)
        {
            return string.Equals(command, InspectSync, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(command, InspectAsync, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsResultRequest(string command)
        {
            return string.Equals(command, MResult, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(command, Result, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class VisionCameraNames
    {
        public const string Wafer = "WAFER";
        public const string Bottom = "BOTTOM";
        public const string FrontSide = "FRONTSIDE";
        public const string RearSide = "REARSIDE";
        public const string Bin = "BIN";

        public static string FromChannel(AutoVisionChannel channel)
        {
            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return Wafer;
                case AutoVisionChannel.BottomInspection:
                    return Bottom;
                case AutoVisionChannel.FrontSide:
                    return FrontSide;
                case AutoVisionChannel.RearSide:
                    return RearSide;
                case AutoVisionChannel.Bin:
                    return Bin;
                default:
                    return string.Empty;
            }
        }

        public static bool IsKnown(string camera)
        {
            return string.Equals(camera, Wafer, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(camera, Bottom, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(camera, FrontSide, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(camera, RearSide, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(camera, Bin, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class VisionInspectionOperations
    {
        public const string Match = "MATCH";
        public const string Inspect = "INSPECT";
    }

    public static class VisionResultTimings
    {
        public const string Immediate = "IMMEDIATE";
        public const string Deferred = "DEFERRED";
    }

    public sealed class VisionInspectionRequestContext
    {
        public AutoVisionChannel Channel { get; private set; }
        public string Camera { get; private set; }
        public string Finder { get; private set; }
        public string Head { get; private set; }
        public int HeadIndex { get; private set; }
        public int DieIndex { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int VisionChannel { get; private set; }
        public string WaferId { get; private set; }
        public string RecipeId { get; private set; }
        public string LotId { get; private set; }
        public string DieId { get; private set; }
        public string Operation { get; private set; }
        public string ResultTiming { get; private set; }
        public string GroupId { get; private set; }
        public bool RequireMaterialContext { get; private set; }

        public VisionInspectionRequestContext(
            AutoVisionChannel channel,
            string finder,
            int fb,
            int headIndex,
            int dieIndex,
            int gridX,
            int gridY,
            int visionChannel,
            string dieId,
            string waferId,
            string recipeId,
            string lotId,
            string operation,
            string resultTiming,
            string groupId,
            bool requireMaterialContext)
        {
            Channel = channel;
            Camera = VisionCameraNames.FromChannel(channel);
            Finder = finder ?? string.Empty;
            Head = fb == 0 ? "FRONT" : fb == 1 ? "REAR" : string.Empty;
            HeadIndex = headIndex;
            DieIndex = dieIndex;
            GridX = gridX;
            GridY = gridY;
            VisionChannel = visionChannel;
            DieId = dieId ?? string.Empty;
            WaferId = waferId ?? string.Empty;
            RecipeId = recipeId ?? string.Empty;
            LotId = lotId ?? string.Empty;
            Operation = operation ?? string.Empty;
            ResultTiming = resultTiming ?? string.Empty;
            GroupId = groupId ?? string.Empty;
            RequireMaterialContext = requireMaterialContext;
        }
    }

    public static class VisionInspectionContextFactory
    {
        private static readonly object _recipeLotCacheSync = new object();
        private static readonly Dictionary<string, RecipeLotCacheEntry> _recipeLotCache =
            new Dictionary<string, RecipeLotCacheEntry>(StringComparer.OrdinalIgnoreCase);

        private sealed class RecipeLotCacheEntry
        {
            public DateTime LastWriteTimeUtc { get; set; }
            public long FileLength { get; set; }
            public string LotId { get; set; }
        }

        public static VisionInspectionRequestContext CreateManual(
            AutoVisionChannel channel,
            string finder,
            int fb,
            int headIndex,
            int dieIndex,
            int gridX,
            int gridY,
            int visionChannel,
            string operation,
            string resultTiming)
        {
            string waferId = string.Empty;
            string recipeId = string.Empty;
            string lotId = string.Empty;
            MaterialSnapshot snapshot = MaterialStateService.State;
            if (snapshot != null)
            {
                recipeId = snapshot.RecipeName ?? string.Empty;
                lotId = snapshot.LotId ?? string.Empty;
            }

            WaferMaterial inputWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (inputWafer != null)
            {
                waferId = inputWafer.WaferId ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(inputWafer.CassetteLotId))
                    lotId = inputWafer.CassetteLotId;
            }

            return new VisionInspectionRequestContext(
                channel,
                finder,
                fb,
                headIndex,
                dieIndex,
                gridX,
                gridY,
                visionChannel,
                string.Empty,
                waferId,
                recipeId,
                lotId,
                operation,
                resultTiming,
                string.Empty,
                false);
        }

        public static VisionInspectionRequestContext CreateAuto(
            AutoVisionChannel channel,
            string finder,
            int fb,
            int headIndex,
            int dieIndex,
            int gridX,
            int gridY,
            int visionChannel,
            string dieId,
            string preferredWaferId,
            string operation,
            string resultTiming,
            string groupId)
        {
            string waferId = preferredWaferId ?? string.Empty;
            string recipeId = string.Empty;
            string lotId = string.Empty;

            MaterialSnapshot snapshot = MaterialStateService.State;
            DieMaterial die = !string.IsNullOrWhiteSpace(dieId)
                ? MaterialStateService.GetDieMaterial(dieId)
                : null;

            if (string.IsNullOrWhiteSpace(waferId) && die != null)
            {
                waferId = channel == AutoVisionChannel.Bin && !string.IsNullOrWhiteSpace(die.WaferID_Output)
                    ? die.WaferID_Output
                    : die.WaferID_Input;
            }

            if (string.IsNullOrWhiteSpace(waferId) && channel == AutoVisionChannel.Wafer)
            {
                WaferMaterial inputWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (inputWafer != null)
                    waferId = inputWafer.WaferId;
            }

            if (snapshot != null)
            {
                recipeId = snapshot.RecipeName ?? string.Empty;
                lotId = snapshot.LotId ?? string.Empty;

                WaferMaterial wafer = snapshot.Wafers != null
                    ? snapshot.Wafers.FirstOrDefault(w =>
                        w != null && string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (wafer != null && !string.IsNullOrWhiteSpace(wafer.CassetteLotId))
                    lotId = wafer.CassetteLotId;
            }

            if (string.IsNullOrWhiteSpace(lotId))
                lotId = ResolveRecipeLotId(recipeId);

            return new VisionInspectionRequestContext(
                channel,
                finder,
                fb,
                headIndex,
                dieIndex,
                gridX,
                gridY,
                visionChannel,
                dieId,
                waferId,
                recipeId,
                lotId,
                operation,
                resultTiming,
                groupId,
                true);
        }

        private static string ResolveRecipeLotId(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return string.Empty;

            try
            {
                string normalizedRecipeId = recipeId.Trim();
                string recipeFileName = normalizedRecipeId.EndsWith(".Project", StringComparison.OrdinalIgnoreCase)
                    ? normalizedRecipeId
                    : normalizedRecipeId + ".Project";
                string recipePath = Path.Combine(RecipeStore.Dir, recipeFileName);
                bool recipeExists = File.Exists(recipePath);
                DateTime lastWriteTimeUtc = recipeExists ? File.GetLastWriteTimeUtc(recipePath) : DateTime.MinValue;
                long fileLength = recipeExists ? new FileInfo(recipePath).Length : -1L;

                lock (_recipeLotCacheSync)
                {
                    RecipeLotCacheEntry cached;
                    if (_recipeLotCache.TryGetValue(normalizedRecipeId, out cached) &&
                        cached.LastWriteTimeUtc == lastWriteTimeUtc &&
                        cached.FileLength == fileLength)
                    {
                        return cached.LotId;
                    }

                    RecipeProject recipe = RecipeStore.Load(normalizedRecipeId);
                    string lotId = recipe != null && !string.IsNullOrWhiteSpace(recipe.LotId)
                        ? recipe.LotId.Trim()
                        : string.Empty;
                    if (recipe != null || !recipeExists)
                    {
                        _recipeLotCache[normalizedRecipeId] = new RecipeLotCacheEntry
                        {
                            LastWriteTimeUtc = lastWriteTimeUtc,
                            FileLength = fileLength,
                            LotId = lotId
                        };
                    }
                    return lotId;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Warning,
                    "VISION",
                    "AUTO-VISION-RECIPE-LOT",
                    "자동 Vision LOT ID Recipe fallback 로드 실패. recipeId=" + recipeId +
                    ", error=" + ex.Message);
                return string.Empty;
            }
        }
    }

    public static class VisionCorrelationIdGenerator
    {
        private static long _sequence;

        public static string NewRequestId()
        {
            long sequence = Interlocked.Increment(ref _sequence);
            return DateTime.UtcNow.Ticks.ToString("x", CultureInfo.InvariantCulture) +
                   "-" + sequence.ToString("x", CultureInfo.InvariantCulture) +
                   "-" + Guid.NewGuid().ToString("N");
        }

        public static string NewGroupId()
        {
            return "g-" + NewRequestId();
        }
    }

    public sealed class VisionInspectionEnvelope
    {
        public AutoVisionChannel Channel { get; private set; }
        public string Camera { get; private set; }
        public string Command { get; private set; }
        public string Finder { get; private set; }
        public string Head { get; private set; }
        public int HeadIndex { get; private set; }
        public int DieIndex { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int VisionChannel { get; private set; }
        public string WaferId { get; private set; }
        public string RecipeId { get; private set; }
        public string LotId { get; private set; }
        public string DieId { get; private set; }
        public string RequestId { get; private set; }
        public string GroupId { get; private set; }
        public string ResultTiming { get; private set; }
        public string Operation { get; private set; }
        public bool RequireMaterialContext { get; private set; }

        public static VisionInspectionEnvelope Create(VisionInspectionRequestContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            string requestId = VisionCorrelationIdGenerator.NewRequestId();
            string groupId = string.IsNullOrWhiteSpace(context.GroupId) ? requestId : context.GroupId;
            return new VisionInspectionEnvelope
            {
                Channel = context.Channel,
                Camera = context.Camera,
                Command = string.Equals(context.ResultTiming, VisionResultTimings.Immediate, StringComparison.OrdinalIgnoreCase)
                    ? VisionInspectionCommands.InspectSync
                    : VisionInspectionCommands.InspectAsync,
                Finder = context.Finder,
                Head = context.Head,
                HeadIndex = context.HeadIndex,
                DieIndex = context.DieIndex,
                GridX = context.GridX,
                GridY = context.GridY,
                VisionChannel = context.VisionChannel,
                WaferId = context.WaferId,
                RecipeId = context.RecipeId,
                LotId = context.LotId,
                DieId = context.DieId,
                RequestId = requestId,
                GroupId = groupId,
                ResultTiming = context.ResultTiming,
                Operation = context.Operation,
                RequireMaterialContext = context.RequireMaterialContext
            };
        }

        public bool Validate(out string reason)
        {
            reason = string.Empty;
            if (!VisionCameraNames.IsKnown(Camera))
                return Fail("CAMERA가 신규 규약 허용값이 아닙니다. camera=" + Camera, out reason);
            if (!VisionInspectionCommands.IsInspectionRequest(Command))
                return Fail("검사 CMD가 올바르지 않습니다. command=" + Command, out reason);
            if (!VisionInspectionToolMap.IsAllowed(Camera, Finder))
                return Fail("CAMERA와 FINDER 조합이 허용되지 않습니다. camera=" + Camera + ", finder=" + Finder, out reason);
            if (DieIndex < 0 || DieIndex > 9999)
                return Fail("DIE_INDEX는 0~9999 범위여야 합니다. dieIndex=" + DieIndex, out reason);
            if (GridX < -9999 || GridX > 9999 || GridY < -9999 || GridY > 9999)
                return Fail("GRID_X/GRID_Y가 허용범위를 벗어났습니다. grid=" + GridX + ";" + GridY, out reason);
            if ((string.Equals(Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase)) &&
                VisionChannel != 0 && VisionChannel != 1)
                return Fail("Side CHANNEL은 0 또는 1이어야 합니다. channel=" + VisionChannel, out reason);
            if (!string.Equals(Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase) &&
                VisionChannel != 0)
                return Fail("WAFER/BOTTOM/BIN CHANNEL은 0이어야 합니다. channel=" + VisionChannel, out reason);
            if ((string.Equals(Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase)) &&
                (!string.Equals(Head, "FRONT", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(Head, "REAR", StringComparison.OrdinalIgnoreCase)))
                return Fail("BOTTOM/SIDE 요청에는 HEAD가 필요합니다. head=" + Head, out reason);
            if (!string.IsNullOrWhiteSpace(Head) && (HeadIndex < 1 || HeadIndex > 4))
                return Fail("HEAD_INDEX는 1~4 범위여야 합니다. headIndex=" + HeadIndex, out reason);
            if (!string.Equals(Operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase))
                return Fail("operation은 MATCH 또는 INSPECT여야 합니다. operation=" + Operation, out reason);
            if (!VisionInspectionToolMap.IsOperationAllowed(Camera, Finder, Operation))
                return Fail("CAMERA/FINDER ROLE과 operation 조합이 올바르지 않습니다. camera=" + Camera +
                            ", finder=" + Finder + ", operation=" + Operation, out reason);
            if (!string.Equals(ResultTiming, VisionResultTimings.Immediate, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ResultTiming, VisionResultTimings.Deferred, StringComparison.OrdinalIgnoreCase))
                return Fail("result_timing이 올바르지 않습니다. resultTiming=" + ResultTiming, out reason);
            if (RequireMaterialContext &&
                (string.IsNullOrWhiteSpace(WaferId) || string.IsNullOrWhiteSpace(RecipeId) || string.IsNullOrWhiteSpace(LotId)))
                return Fail("자동 검사 필수 자재 문맥이 없습니다. waferId=" + WaferId + ", recipeId=" + RecipeId + ", lotId=" + LotId, out reason);

            string[] values =
            {
                Camera, Command, Finder, Head, WaferId, RecipeId, LotId,
                RequestId, GroupId, ResultTiming, Operation
            };
            for (int i = 0; i < values.Length; i++)
            {
                if (ContainsWireSeparator(values[i]))
                    return Fail("요청 필드에 금지 문자가 있습니다. fieldIndex=" + i, out reason);
            }
            string[] semicolonRestrictedValues =
            {
                WaferId, RecipeId, LotId, RequestId, GroupId, ResultTiming, Operation
            };
            for (int i = 0; i < semicolonRestrictedValues.Length; i++)
            {
                if (!string.IsNullOrEmpty(semicolonRestrictedValues[i]) &&
                    semicolonRestrictedValues[i].IndexOf(';') >= 0)
                    return Fail("ID와 META 값에는 세미콜론을 사용할 수 없습니다. fieldIndex=" + i, out reason);
            }

            return true;
        }

        public string ToLine()
        {
            string reason;
            if (!Validate(out reason))
                throw new InvalidOperationException(reason);

            var sb = new StringBuilder();
            Append(sb, Camera);
            Append(sb, Command);
            Append(sb, Finder);
            Append(sb, Head);
            Append(sb, HeadIndex.ToString(CultureInfo.InvariantCulture));
            Append(sb, DieIndex.ToString(CultureInfo.InvariantCulture));
            Append(sb, GridX.ToString(CultureInfo.InvariantCulture));
            Append(sb, GridY.ToString(CultureInfo.InvariantCulture));
            Append(sb, VisionChannel.ToString(CultureInfo.InvariantCulture));
            Append(sb, WaferId);
            Append(sb, RecipeId);
            Append(sb, LotId);
            Append(sb, "request_id=" + RequestId +
                       ";group_id=" + GroupId +
                       ";result_timing=" + ResultTiming +
                       ";operation=" + Operation);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string value)
        {
            if (sb.Length > 0)
                sb.Append('|');
            sb.Append(value ?? string.Empty);
        }

        private static bool ContainsWireSeparator(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   (value.IndexOf('|') >= 0 || value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0);
        }

        private static bool Fail(string value, out string reason)
        {
            reason = value;
            return false;
        }
    }

    public static class VisionInspectionToolMap
    {
        public static bool IsAllowed(string camera, string finder)
        {
            if (string.IsNullOrWhiteSpace(finder))
                return false;

            if (string.Equals(camera, VisionCameraNames.Wafer, StringComparison.OrdinalIgnoreCase))
            {
                return IsAny(finder,
                    VisionToolIds.Wafer.EjectPinFinder,
                    VisionToolIds.Wafer.ReticleFinder,
                    VisionToolIds.Wafer.AlignDieFinder,
                    VisionToolIds.Wafer.FirstReferenceFinder,
                    VisionToolIds.Wafer.SecondReferenceFinder,
                    VisionToolIds.Wafer.DieFinder,
                    VisionToolIds.Wafer.ScaleFinder);
            }
            if (string.Equals(camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
            {
                return IsAny(finder,
                    VisionToolIds.BottomInspection.ReticleFinder,
                    VisionToolIds.BottomInspection.ColletFinder,
                    VisionToolIds.BottomInspection.DieFinder,
                    VisionToolIds.BottomInspection.SurfaceInspector,
                    VisionToolIds.BottomInspection.FocusFinder,
                    VisionToolIds.BottomInspection.ScaleFinder,
                    VisionToolIds.BottomInspection.COCInspector,
                    VisionToolIds.BottomInspection.DistortionCompensation);
            }
            if (string.Equals(camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase))
            {
                return IsAny(finder,
                    VisionToolIds.FrontSide.DieEdgeFinder,
                    VisionToolIds.FrontSide.FocusFinder,
                    VisionToolIds.FrontSide.SurfaceInspector,
                    VisionToolIds.FrontSide.ChippingInspector);
            }
            if (string.Equals(camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase))
            {
                return IsAny(finder,
                    VisionToolIds.RearSide.DieEdgeFinder,
                    VisionToolIds.RearSide.FocusFinder,
                    VisionToolIds.RearSide.SurfaceInspector,
                    VisionToolIds.RearSide.ChippingInspector);
            }
            if (string.Equals(camera, VisionCameraNames.Bin, StringComparison.OrdinalIgnoreCase))
            {
                return IsAny(finder,
                    VisionToolIds.Bin.ReticleFinder,
                    VisionToolIds.Bin.DieFinder,
                    VisionToolIds.Bin.ScaleFinder,
                    VisionToolIds.Bin.PlacementInspector);
            }
            return false;
        }

        public static bool IsOperationAllowed(string camera, string finder, string operation)
        {
            if (string.Equals(operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(camera, VisionCameraNames.Wafer, StringComparison.OrdinalIgnoreCase))
                {
                    return IsAny(finder,
                        VisionToolIds.Wafer.EjectPinFinder,
                        VisionToolIds.Wafer.ReticleFinder,
                        VisionToolIds.Wafer.AlignDieFinder,
                        VisionToolIds.Wafer.FirstReferenceFinder,
                        VisionToolIds.Wafer.SecondReferenceFinder,
                        VisionToolIds.Wafer.DieFinder);
                }
                if (string.Equals(camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
                {
                    return IsAny(finder,
                        VisionToolIds.BottomInspection.ReticleFinder,
                        VisionToolIds.BottomInspection.ColletFinder,
                        VisionToolIds.BottomInspection.DieFinder);
                }
                if (string.Equals(camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase))
                    return IsAny(finder, VisionToolIds.FrontSide.DieEdgeFinder);
                if (string.Equals(camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase))
                    return IsAny(finder, VisionToolIds.RearSide.DieEdgeFinder);
                if (string.Equals(camera, VisionCameraNames.Bin, StringComparison.OrdinalIgnoreCase))
                {
                    return IsAny(finder,
                        VisionToolIds.Bin.ReticleFinder,
                        VisionToolIds.Bin.DieFinder);
                }
                return false;
            }

            if (string.Equals(operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
                    return IsAny(finder, VisionToolIds.BottomInspection.SurfaceInspector);
                if (string.Equals(camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase))
                {
                    return IsAny(finder,
                        VisionToolIds.FrontSide.SurfaceInspector,
                        VisionToolIds.FrontSide.ChippingInspector);
                }
                if (string.Equals(camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase))
                {
                    return IsAny(finder,
                        VisionToolIds.RearSide.SurfaceInspector,
                        VisionToolIds.RearSide.ChippingInspector);
                }
                if (string.Equals(camera, VisionCameraNames.Bin, StringComparison.OrdinalIgnoreCase))
                    return IsAny(finder, VisionToolIds.Bin.PlacementInspector);
            }

            return false;
        }

        private static bool IsAny(string value, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(value, candidates[i], StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }

    public sealed class VisionRequestHandle
    {
        public VisionInspectionEnvelope Request { get; private set; }
        public DateTime RequestTxAtUtc { get; private set; }
        public DateTime ExposureDoneAtUtc { get; private set; }
        public DateTime MResultTxAtUtc { get; private set; }
        public DateTime MResultRxAtUtc { get; private set; }
        public DateTime ResultTxAtUtc { get; private set; }
        public DateTime ResultRxAtUtc { get; private set; }
        public DateTime FinalizedAtUtc { get; private set; }
        public long RequestTxTimestamp { get; private set; }
        public long ExposureDoneTimestamp { get; private set; }
        public long MResultTxTimestamp { get; private set; }
        public long MResultLastTxTimestamp { get; private set; }
        public long MResultRxTimestamp { get; private set; }
        public long ResultTxTimestamp { get; private set; }
        public long ResultLastTxTimestamp { get; private set; }
        public long ResultRxTimestamp { get; private set; }
        public long FinalizedTimestamp { get; private set; }
        public int MResultPendingCount { get; private set; }
        public int ResultPendingCount { get; private set; }
        public bool IsExposureDone { get; private set; }
        public bool IsMResultDone { get; private set; }
        public bool IsResultDone { get; private set; }
        public bool IsBypassed { get; private set; }
        public string Error { get; private set; }
        public VisionInspectionResult MResult { get; private set; }
        public VisionInspectionResult Result { get; private set; }

        public VisionRequestHandle(VisionInspectionEnvelope request)
        {
            Request = request ?? throw new ArgumentNullException("request");
        }

        public double RequestToExposureDoneMs { get { return ElapsedMilliseconds(RequestTxTimestamp, ExposureDoneTimestamp); } }
        public double ExposureToMResultRequestMs { get { return ElapsedMilliseconds(ExposureDoneTimestamp, MResultTxTimestamp); } }
        public double MResultRoundTripMs { get { return ElapsedMilliseconds(MResultLastTxTimestamp, MResultRxTimestamp); } }
        public double MResultToResultRequestMs { get { return ElapsedMilliseconds(MResultRxTimestamp, ResultTxTimestamp); } }
        public double ExposureToResultRequestMs { get { return ElapsedMilliseconds(ExposureDoneTimestamp, ResultTxTimestamp); } }
        public double ResultRoundTripMs { get { return ElapsedMilliseconds(ResultLastTxTimestamp, ResultRxTimestamp); } }
        public double RequestToFinalMs { get { return ElapsedMilliseconds(RequestTxTimestamp, FinalizedTimestamp); } }

        internal void MarkRequestTx()
        {
            RequestTxTimestamp = Stopwatch.GetTimestamp();
            RequestTxAtUtc = DateTime.UtcNow;
        }

        internal void MarkExposureDone()
        {
            ExposureDoneTimestamp = Stopwatch.GetTimestamp();
            IsExposureDone = true;
            ExposureDoneAtUtc = DateTime.UtcNow;
        }

        internal void MarkStageTx(string command)
        {
            if (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
            {
                MResultLastTxTimestamp = Stopwatch.GetTimestamp();
                if (MResultTxTimestamp == 0)
                {
                    MResultTxTimestamp = MResultLastTxTimestamp;
                    MResultTxAtUtc = DateTime.UtcNow;
                }
            }
            else
            {
                ResultLastTxTimestamp = Stopwatch.GetTimestamp();
                if (ResultTxTimestamp == 0)
                {
                    ResultTxTimestamp = ResultLastTxTimestamp;
                    ResultTxAtUtc = DateTime.UtcNow;
                }
            }
        }
        internal void MarkStagePending(string command)
        {
            if (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
                MResultPendingCount++;
            else
                ResultPendingCount++;
        }
        internal void MarkStageDone(string command, VisionInspectionResult result)
        {
            if (string.Equals(command, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
            {
                MResultRxTimestamp = Stopwatch.GetTimestamp();
                MResult = result;
                IsMResultDone = true;
                MResultRxAtUtc = DateTime.UtcNow;
            }
            else
            {
                ResultRxTimestamp = Stopwatch.GetTimestamp();
                Result = result;
                IsResultDone = true;
                ResultRxAtUtc = DateTime.UtcNow;
                FinalizedAtUtc = ResultRxAtUtc;
                FinalizedTimestamp = ResultRxTimestamp;
            }
        }
        internal void MarkBypassed()
        {
            IsBypassed = true;
            MarkRequestTx();
            MarkExposureDone();
        }
        internal void MarkError(string error)
        {
            Error = error ?? string.Empty;
            FinalizedTimestamp = Stopwatch.GetTimestamp();
            FinalizedAtUtc = DateTime.UtcNow;
        }

        public static double ElapsedMilliseconds(long startTimestamp, long endTimestamp)
        {
            if (startTimestamp <= 0 || endTimestamp < startTimestamp)
                return 0.0;
            return (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        }
    }

    public sealed class VisionInspectionResult
    {
        public string Camera { get; private set; }
        public string Command { get; private set; }
        public string Finder { get; private set; }
        public int DieIndex { get; private set; }
        public string RequestId { get; private set; }
        public string GroupId { get; private set; }
        public string Operation { get; private set; }
        public string Status { get; private set; }
        public bool IsPending { get; private set; }
        public bool IsError { get; private set; }
        public string ErrorCode { get; private set; }
        public string Error { get; private set; }
        public string Raw { get; private set; }
        public Dictionary<string, string> Values { get; private set; }
        public MatchResultDto MatchResult { get; private set; }
        public InspectionResultDto InspectionResult { get; private set; }

        public static VisionInspectionResult Parse(VisionProtocolResponse response, VisionInspectionEnvelope request, string expectedCommand)
        {
            var result = new VisionInspectionResult();
            result.Raw = response != null ? response.RawLine : string.Empty;
            result.Camera = response != null && !string.IsNullOrWhiteSpace(response.Module)
                ? response.Module
                : request != null ? request.Camera : string.Empty;
            result.Command = expectedCommand ?? string.Empty;
            result.DieIndex = request != null ? request.DieIndex : -1;
            string responseFinder = response != null ? response.GetValueAny("finder", "tool") : null;
            string responseRequestId = response != null ? response.GetValueAny("request_id", "requestId", "requestid") : null;
            string responseGroupId = response != null ? response.GetValueAny("group_id", "groupId", "groupid") : null;
            string responseOperation = response != null ? response.GetValue("operation") : null;
            result.Finder = !string.IsNullOrWhiteSpace(responseFinder)
                ? responseFinder
                : request != null ? request.Finder : string.Empty;
            result.RequestId = !string.IsNullOrWhiteSpace(responseRequestId)
                ? responseRequestId
                : request != null ? request.RequestId : string.Empty;
            result.GroupId = !string.IsNullOrWhiteSpace(responseGroupId)
                ? responseGroupId
                : request != null ? request.GroupId : string.Empty;
            result.Operation = !string.IsNullOrWhiteSpace(responseOperation)
                ? responseOperation
                : request != null ? request.Operation : string.Empty;
            result.Values = CopyValues(response);
            result.Status = ResolveStatus(response);
            result.IsPending = string.Equals(result.Status, "PENDING", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(result.Status, "0", StringComparison.OrdinalIgnoreCase);
            result.IsError = response == null || response.IsError ||
                             string.Equals(result.Status, "ERR", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(result.Status, "ERROR", StringComparison.OrdinalIgnoreCase) ||
                             (!result.IsPending && !IsKnownFinalStatus(result.Status));
            result.ErrorCode = response != null ? response.ErrorCode : string.Empty;
            result.Error = response != null ? response.ErrorMessage : "Vision response is null.";
            if (result.IsError && string.IsNullOrWhiteSpace(result.Error))
                result.Error = "Vision 결과 상태가 올바르지 않습니다. status=" + (result.Status ?? string.Empty);

            if (!result.IsError && response != null)
            {
                string validationError;
                if (!TryValidateCorrelatedPayload(
                        response,
                        request,
                        expectedCommand,
                        result.Status,
                        result.IsPending,
                        out validationError))
                {
                    result.IsPending = false;
                    result.IsError = true;
                    result.ErrorCode = "INVALID_PAYLOAD";
                    result.Error = validationError;
                }
            }

            if (!result.IsPending && !result.IsError && response != null)
            {
                string normalizedStatus = ResolveFinalStatus(result.Status, response);
                string normalized = BuildNormalizedAck(response, expectedCommand, normalizedStatus);
                if (request != null && string.Equals(request.Operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase))
                {
                    result.MatchResult = MatchResultDto.Parse(normalized);
                    if (result.MatchResult != null)
                        result.MatchResult.ApplyCorrelatedContext(result);
                }
                else
                {
                    result.InspectionResult = InspectionResultDto.Parse(normalized);
                    if (result.InspectionResult != null)
                    {
                        result.InspectionResult.Raw = response.RawLine;
                        result.InspectionResult.ApplyCorrelatedContext(result);
                    }
                }
            }

            return result;
        }

        public static VisionInspectionResult FromBypass(VisionInspectionEnvelope request, string command, MatchResultDto match, InspectionResultDto inspection)
        {
            return new VisionInspectionResult
            {
                Camera = request != null ? request.Camera : string.Empty,
                Command = command ?? string.Empty,
                Finder = request != null ? request.Finder : string.Empty,
                DieIndex = request != null ? request.DieIndex : -1,
                RequestId = request != null ? request.RequestId : string.Empty,
                GroupId = request != null ? request.GroupId : string.Empty,
                Operation = request != null ? request.Operation : string.Empty,
                Status = inspection != null && !inspection.IsPass ? "FAIL" : "OK",
                Raw = "BYPASS",
                Values = inspection != null && inspection.Values != null
                    ? new Dictionary<string, string>(inspection.Values, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                MatchResult = match,
                InspectionResult = inspection
            };
        }

        private static Dictionary<string, string> CopyValues(VisionProtocolResponse response)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (response == null || response.Values == null)
                return values;
            foreach (KeyValuePair<string, string> item in response.Values)
                values[item.Key] = item.Value;
            return values;
        }

        private static bool TryValidateCorrelatedPayload(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            string expectedCommand,
            string status,
            bool isPending,
            out string error)
        {
            error = string.Empty;
            if (response == null || !response.IsInspectionResult)
                return ValidationFail("RESULT/MRESULT 응답 Envelope가 아닙니다.", out error);
            if (!string.IsNullOrWhiteSpace(expectedCommand) &&
                !string.Equals(response.Command, expectedCommand, StringComparison.OrdinalIgnoreCase))
                return ValidationFail("결과 CMD가 요청 단계와 다릅니다. expected=" + expectedCommand + ", actual=" + response.Command, out error);
            if (request != null &&
                !string.Equals(response.Module, request.Camera, StringComparison.OrdinalIgnoreCase))
                return ValidationFail("결과 CAMERA가 요청과 다릅니다. expected=" + request.Camera + ", actual=" + response.Module, out error);

            if (response.Fields != null && response.Fields.Length > 8)
            {
                if (!TryValidateCanonicalResultContext(response, request, out error))
                    return false;

                string metaStatus = response.GetValueAny("status", "state");
                if (!string.IsNullOrWhiteSpace(metaStatus) &&
                    !string.Equals(metaStatus.Trim(), status, StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("고정 STATUS와 META status가 다릅니다. positional=" + status + ", meta=" + metaStatus, out error);
            }

            string groupId = response.GetValueAny("group_id", "groupId", "groupid");
            if (string.IsNullOrWhiteSpace(groupId))
                return ValidationFail("결과 필수 key가 없습니다. key=group_id", out error);
            if (request != null && !string.Equals(groupId, request.GroupId, StringComparison.Ordinal))
                return ValidationFail("결과 group_id가 요청과 다릅니다. expected=" + request.GroupId + ", actual=" + groupId, out error);

            string profile = response.GetValue("profile");
            if (string.IsNullOrWhiteSpace(profile))
                return ValidationFail("결과 필수 key가 없습니다. key=profile", out error);

            int version;
            if (!response.TryGetInt("ver", out version) || version != 1)
                return ValidationFail("지원하지 않는 결과 Profile 버전입니다. ver=" + (response.GetValue("ver") ?? string.Empty), out error);

            if (request == null)
                return ValidationFail("결과 검증에 요청 문맥이 없습니다.", out error);
            if (!VisionInspectionToolMap.IsOperationAllowed(request.Camera, request.Finder, request.Operation))
                return ValidationFail("요청 CAMERA/FINDER ROLE과 operation 조합이 올바르지 않습니다.", out error);
            if (!IsCorrelatedProfileAllowed(request, expectedCommand, profile))
                return ValidationFail("요청 문맥과 결과 profile 조합이 올바르지 않습니다. camera=" + request.Camera +
                                      ", command=" + expectedCommand + ", operation=" + request.Operation +
                                      ", finder=" + request.Finder + ", profile=" + profile, out error);
            if (isPending)
                return string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase) ||
                       ValidationFail("신규 규약의 대기 STATUS는 PENDING이어야 합니다. status=" + status, out error);

            if (string.Equals(request.Operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase))
                return TryValidateMatchPayload(response, request, profile, status, out error);
            if (string.Equals(request.Operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase))
                return TryValidateInspectionPayload(response, request, expectedCommand, profile, status, out error);

            return ValidationFail("지원하지 않는 Vision operation입니다. operation=" + request.Operation, out error);
        }

        private static bool IsCorrelatedProfileAllowed(
            VisionInspectionEnvelope request,
            string expectedCommand,
            string profile)
        {
            if (string.Equals(request.Operation, VisionInspectionOperations.Match, StringComparison.OrdinalIgnoreCase))
                return string.Equals(expectedCommand, VisionInspectionCommands.Result, StringComparison.OrdinalIgnoreCase) &&
                       IsMatchProfileAllowed(request, profile);
            if (!string.Equals(request.Operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(expectedCommand, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
                return string.Equals(request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(profile, "BOTTOM_SURFACE", StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(expectedCommand, VisionInspectionCommands.Result, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
                return string.Equals(profile, "BOTTOM_SURFACE", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(request.Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase))
                return string.Equals(profile, "SIDE_JUDGE", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(request.Camera, VisionCameraNames.Bin, StringComparison.OrdinalIgnoreCase))
                return string.Equals(profile, "PLACEMENT_POSE", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private static bool TryValidateCanonicalResultContext(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            out string error)
        {
            error = string.Empty;
            if (request == null)
                return true;

            string[] fields = response.Fields;
            if (!string.Equals(fields[0], request.Finder, StringComparison.Ordinal))
                return ValidationFail("결과 FINDER가 요청과 다릅니다. expected=" + request.Finder + ", actual=" + fields[0], out error);
            if (!string.Equals(fields[1], request.Head, StringComparison.OrdinalIgnoreCase))
                return ValidationFail("결과 HEAD가 요청과 다릅니다. expected=" + request.Head + ", actual=" + fields[1], out error);

            int headIndex;
            int dieIndex;
            int channel;
            if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out headIndex) || headIndex != request.HeadIndex)
                return ValidationFail("결과 HEAD_INDEX가 요청과 다릅니다. expected=" + request.HeadIndex + ", actual=" + fields[2], out error);
            if (!int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out dieIndex) || dieIndex != request.DieIndex)
                return ValidationFail("결과 DIE_INDEX가 요청과 다릅니다. expected=" + request.DieIndex + ", actual=" + fields[3], out error);
            bool sideAggregateResult = string.Equals(response.Command, VisionInspectionCommands.Result, StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(request.Operation, VisionInspectionOperations.Inspect, StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(request.ResultTiming, VisionResultTimings.Deferred, StringComparison.OrdinalIgnoreCase) &&
                                       (string.Equals(request.Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(request.Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase));
            int expectedChannel = sideAggregateResult ? 0 : request.VisionChannel;
            if (!int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out channel) || channel != expectedChannel)
                return ValidationFail("결과 CHANNEL이 요청 문맥과 다릅니다. expected=" + expectedChannel + ", actual=" + fields[4], out error);
            if (!string.Equals(fields[5], request.WaferId, StringComparison.Ordinal) ||
                !string.Equals(fields[6], request.RecipeId, StringComparison.Ordinal) ||
                !string.Equals(fields[7], request.LotId, StringComparison.Ordinal))
                return ValidationFail("결과 자재 문맥이 요청과 다릅니다.", out error);

            return true;
        }

        private static bool TryValidateMatchPayload(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            string profile,
            string status,
            out string error)
        {
            error = string.Empty;
            if (!string.Equals(response.Command, VisionInspectionCommands.Result, StringComparison.OrdinalIgnoreCase))
                return ValidationFail("MATCH 결과는 RESULT 단계만 사용할 수 있습니다.", out error);
            if (!IsMatchProfileAllowed(request, profile))
                return ValidationFail("FINDER와 결과 profile 조합이 올바르지 않습니다. finder=" + request.Finder + ", profile=" + profile, out error);

            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                return ValidationFail("MATCH RESULT STATUS는 OK여야 합니다. status=" + status, out error);

            if (string.Equals(profile, "MATCH_POSE", StringComparison.OrdinalIgnoreCase))
            {
                return RequireDouble(response, "x", out error) &&
                       RequireDouble(response, "y", out error) &&
                       RequireDouble(response, "r", out error) &&
                       RequireDouble(response, "score", out error) &&
                       ValidateOptionalDouble(response, "width", out error) &&
                       ValidateOptionalDouble(response, "height", out error);
            }
            if (string.Equals(profile, "MATCH_SCORE", StringComparison.OrdinalIgnoreCase))
            {
                return RequireDouble(response, "score", out error) &&
                       ValidateOptionalDouble(response, "x", out error) &&
                       ValidateOptionalDouble(response, "y", out error) &&
                       ValidateOptionalDouble(response, "r", out error) &&
                       ValidateOptionalDouble(response, "width", out error) &&
                       ValidateOptionalDouble(response, "height", out error);
            }
            return ValidationFail("지원하지 않는 MATCH profile입니다. profile=" + profile, out error);
        }

        private static bool IsMatchProfileAllowed(VisionInspectionEnvelope request, string profile)
        {
            bool isScaleFinder = string.Equals(request.Finder, VisionToolIds.Wafer.ScaleFinder, StringComparison.Ordinal) ||
                                 string.Equals(request.Finder, VisionToolIds.BottomInspection.ScaleFinder, StringComparison.Ordinal) ||
                                 string.Equals(request.Finder, VisionToolIds.Bin.ScaleFinder, StringComparison.Ordinal);
            if (isScaleFinder)
                return false;

            bool isFocusFinder = string.Equals(request.Finder, VisionToolIds.BottomInspection.FocusFinder, StringComparison.Ordinal) ||
                                 string.Equals(request.Finder, VisionToolIds.FrontSide.FocusFinder, StringComparison.Ordinal) ||
                                 string.Equals(request.Finder, VisionToolIds.RearSide.FocusFinder, StringComparison.Ordinal);
            if (isFocusFinder)
                return false;

            bool isWaferDieFinder = string.Equals(request.Camera, VisionCameraNames.Wafer, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(request.Finder, VisionToolIds.Wafer.DieFinder, StringComparison.Ordinal);
            if (isWaferDieFinder && string.Equals(profile, "MATCH_SCORE", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(profile, "MATCH_POSE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryValidateInspectionPayload(
            VisionProtocolResponse response,
            VisionInspectionEnvelope request,
            string expectedCommand,
            string profile,
            string status,
            out string error)
        {
            error = string.Empty;
            if (string.Equals(expectedCommand, VisionInspectionCommands.MResult, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(profile, "BOTTOM_SURFACE", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("MRESULT는 BOTTOM_SURFACE profile만 사용할 수 있습니다.", out error);
                if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("MRESULT 최종 STATUS는 OK여야 합니다. status=" + status, out error);

                // The three canonical correction values are validated immediately after this
                // terminal MRESULT so RESULT can still be drained when their payload is invalid.
                return true;
            }

            if (!string.Equals(expectedCommand, VisionInspectionCommands.Result, StringComparison.OrdinalIgnoreCase))
                return ValidationFail("Inspection 최종 결과 단계가 RESULT가 아닙니다.", out error);
            if (!string.Equals(status, "PASS", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, "FAIL", StringComparison.OrdinalIgnoreCase))
                return ValidationFail("Inspection RESULT STATUS는 PASS/FAIL이어야 합니다. status=" + status, out error);

            bool measureValid;
            if (!RequireFlag(response, "measure_valid", out measureValid, out error))
                return false;
            if (!measureValid)
            {
                if (string.Equals(status, "PASS", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("measure_valid=0 결과는 PASS일 수 없습니다.", out error);
                return RequireString(response, "fail_code", out error) &&
                       RequireString(response, "fail_message", out error);
            }

            if (string.Equals(request.Camera, VisionCameraNames.Bottom, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(profile, "BOTTOM_SURFACE", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("BOTTOM RESULT profile이 올바르지 않습니다. profile=" + profile, out error);
                return TryValidateBottomSurfaceResult(response, out error);
            }
            if (string.Equals(request.Camera, VisionCameraNames.FrontSide, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.Camera, VisionCameraNames.RearSide, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(profile, "SIDE_JUDGE", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("SIDE RESULT profile이 올바르지 않습니다. profile=" + profile, out error);
                return TryValidateSideJudgeResult(response, out error);
            }
            if (string.Equals(request.Camera, VisionCameraNames.Bin, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(profile, "PLACEMENT_POSE", StringComparison.OrdinalIgnoreCase))
                    return ValidationFail("BIN RESULT profile이 올바르지 않습니다. profile=" + profile, out error);
                return TryValidatePlacementResult(response, out error);
            }

            return ValidationFail("지원하지 않는 Inspection CAMERA입니다. camera=" + request.Camera, out error);
        }

        private static bool TryValidateBottomSurfaceResult(VisionProtocolResponse response, out string error)
        {
            error = string.Empty;
            if (!RequireDouble(response, "bottom_width_mm", out error) ||
                !RequireDouble(response, "bottom_height_mm", out error))
                return false;

            string[] doubleMetrics =
            {
                "bottom_item_width", "bottom_item_height", "bottom_item_angle",
                "bottom_item_offset_x", "bottom_item_offset_y",
                "bottom_item_chipping_top", "bottom_item_chipping_right",
                "bottom_item_chipping_bottom", "bottom_item_chipping_left"
            };
            for (int i = 0; i < doubleMetrics.Length; i++)
            {
                if (!ValidateOptionalMetricWithPass(response, doubleMetrics[i], false, out error))
                    return false;
            }

            string[] intMetrics =
            {
                "bottom_item_t_total", "bottom_item_t_gray", "bottom_item_t_die",
                "bottom_item_t_chip", "bottom_item_t_foreign"
            };
            for (int i = 0; i < intMetrics.Length; i++)
            {
                if (!ValidateOptionalMetricWithPass(response, intMetrics[i], true, out error))
                    return false;
            }
            return ValidateOptionalInt(response, "algo_ms", out error);
        }

        private static bool TryValidateSideJudgeResult(VisionProtocolResponse response, out string error)
        {
            error = string.Empty;
            bool ch0Valid;
            bool ch1Valid;
            if (!RequireFlag(response, "ch0_valid", out ch0Valid, out error) ||
                !RequireFlag(response, "ch1_valid", out ch1Valid, out error))
                return false;
            if (!TryValidateSideChannel(response, 0, ch0Valid, out error) ||
                !TryValidateSideChannel(response, 1, ch1Valid, out error))
                return false;
            return ValidateOptionalInt(response, "algo_ms", out error);
        }

        private static bool TryValidateSideChannel(VisionProtocolResponse response, int channel, bool valid, out string error)
        {
            error = string.Empty;
            string prefix = "ch" + channel.ToString(CultureInfo.InvariantCulture) + "_side_item_foreign_";
            string countKey = prefix + "count";
            string maxKey = prefix + "max";
            if (valid)
            {
                return RequireInt(response, countKey, out error) &&
                       RequireFlag(response, countKey + "_pass", out error) &&
                       RequireDouble(response, maxKey, out error) &&
                       RequireFlag(response, maxKey + "_pass", out error);
            }
            return ValidateOptionalMetricWithPass(response, countKey, true, out error) &&
                   ValidateOptionalMetricWithPass(response, maxKey, false, out error);
        }

        private static bool TryValidatePlacementResult(VisionProtocolResponse response, out string error)
        {
            error = string.Empty;
            string[] requiredDoubles =
            {
                "x", "y", "width", "height",
                "placement_offset_x_mm", "placement_offset_y_mm", "placement_angle_deg"
            };
            for (int i = 0; i < requiredDoubles.Length; i++)
            {
                if (!RequireDouble(response, requiredDoubles[i], out error))
                    return false;
            }

            if (!ValidateOptionalStringWithPass(response, "placement_item_detect", out error))
                return false;
            string[] optionalDoubles =
            {
                "placement_item_top_gap_min", "placement_item_top_gap_max",
                "placement_item_right_max", "placement_item_right_min",
                "placement_item_bottom_gap", "placement_item_bottom_min",
                "placement_item_left_max", "placement_item_left_min",
                "placement_item_offset_x", "placement_item_offset_y", "placement_item_angle",
                "placement_item_top_gap_avg", "placement_item_bottom_gap_avg",
                "placement_item_left_gap_avg", "placement_item_right_gap_avg"
            };
            for (int i = 0; i < optionalDoubles.Length; i++)
            {
                if (!ValidateOptionalMetricWithPass(response, optionalDoubles[i], false, out error))
                    return false;
            }
            return ValidateOptionalInt(response, "algo_ms", out error);
        }

        private static bool RequireString(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            string raw = response.GetValue(key);
            return !string.IsNullOrWhiteSpace(raw) || ValidationFail("결과 필수 key가 없거나 비어 있습니다. key=" + key, out error);
        }

        private static bool RequireDouble(VisionProtocolResponse response, string key, out string error)
        {
            double ignored;
            return RequireDouble(response, key, out ignored, out error);
        }

        private static bool RequireDouble(VisionProtocolResponse response, string key, out double value, out string error)
        {
            error = string.Empty;
            if (!response.TryGetDouble(key, out value))
                return ValidationFail("결과 필수 숫자가 없거나 Invariant 유한 숫자가 아닙니다. key=" + key, out error);
            return true;
        }

        private static bool RequireInt(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            int value;
            if (!response.TryGetInt(key, out value))
                return ValidationFail("결과 필수 정수가 없거나 올바르지 않습니다. key=" + key, out error);
            return true;
        }

        private static bool RequireFlag(VisionProtocolResponse response, string key, out string error)
        {
            bool ignored;
            return RequireFlag(response, key, out ignored, out error);
        }

        private static bool RequireFlag(VisionProtocolResponse response, string key, out bool value, out string error)
        {
            error = string.Empty;
            value = false;
            string raw = response.GetValue(key);
            if (string.Equals(raw, "1", StringComparison.Ordinal) || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (string.Equals(raw, "0", StringComparison.Ordinal) || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
                return true;
            return ValidationFail("결과 필수 bool/int 값이 없거나 올바르지 않습니다. key=" + key, out error);
        }

        private static bool ValidateOptionalDouble(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            string raw = response.GetValue(key);
            if (string.IsNullOrWhiteSpace(raw))
                return true;
            double value;
            return VisionProtocolResponse.TryParseDouble(raw, out value) ||
                   ValidationFail("결과 선택 숫자가 Invariant 유한 숫자가 아닙니다. key=" + key, out error);
        }

        private static bool ValidateOptionalInt(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            string raw = response.GetValue(key);
            if (string.IsNullOrWhiteSpace(raw))
                return true;
            int value;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ||
                   ValidationFail("결과 선택 정수가 올바르지 않습니다. key=" + key, out error);
        }

        private static bool ValidateOptionalFlag(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(response.GetValue(key)))
                return true;
            return RequireFlag(response, key, out error);
        }

        private static bool ValidateOptionalMetricWithPass(
            VisionProtocolResponse response,
            string key,
            bool integer,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(response.GetValue(key)))
                return true;
            if (integer)
            {
                if (!RequireInt(response, key, out error))
                    return false;
            }
            else if (!RequireDouble(response, key, out error))
            {
                return false;
            }
            return RequireFlag(response, key + "_pass", out error);
        }

        private static bool ValidateOptionalStringWithPass(VisionProtocolResponse response, string key, out string error)
        {
            error = string.Empty;
            if (response.GetValue(key) == null)
                return true;
            return RequireString(response, key, out error) && RequireFlag(response, key + "_pass", out error);
        }

        private static bool ValidationFail(string message, out string error)
        {
            error = message ?? string.Empty;
            return false;
        }

        internal static string ResolveStatus(VisionProtocolResponse response)
        {
            if (response == null)
                return "ERR";
            if (response.IsError)
                return "ERR";

            // Canonical RESULT/MRESULT fields start after HEADER and CAMERA:
            // FINDER, HEAD, HEAD_INDEX, DIE_INDEX, CHANNEL, WAFER_ID, RECIPE_ID, LOT_ID, STATUS, META.
            // STATUS is authoritative at index 8; never scan HEAD_INDEX/CHANNEL as status candidates.
            if (response.IsInspectionResult && response.Fields != null && response.Fields.Length > 8)
            {
                string field = response.Fields[8] ?? string.Empty;
                int separator = field.IndexOf(';');
                return (separator >= 0 ? field.Substring(0, separator) : field).Trim();
            }

            string status = response.GetValueAny("status", "state", "result");
            if (!string.IsNullOrWhiteSpace(status))
                return status.Trim();
            return response.ResultToken;
        }

        public static bool IsKnownFinalStatus(string status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "PASS", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "FAIL", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "NG", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveFinalStatus(string status, VisionProtocolResponse response)
        {
            if (!string.Equals(status, "1", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(status) ? "OK" : status;
            if (response != null && response.Fields != null)
            {
                for (int i = 0; i < response.Fields.Length; i++)
                {
                    string[] tokens = (response.Fields[i] ?? string.Empty).Split(';');
                    for (int j = 0; j < tokens.Length; j++)
                    {
                        if (string.Equals(tokens[j], "PASS", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tokens[j], "FAIL", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tokens[j], "NG", StringComparison.OrdinalIgnoreCase))
                            return tokens[j];
                    }
                }
            }
            return "OK";
        }

        private static string BuildNormalizedAck(VisionProtocolResponse response, string command, string status)
        {
            var sb = new StringBuilder();
            sb.Append("ACK|");
            sb.Append(response != null ? response.Module : string.Empty);
            sb.Append('|');
            sb.Append(command ?? string.Empty);
            sb.Append('|');
            sb.Append(status ?? "OK");
            if (response != null && response.Values != null)
            {
                foreach (KeyValuePair<string, string> item in response.Values)
                {
                    sb.Append(';');
                    sb.Append(item.Key);
                    sb.Append('=');
                    sb.Append(item.Value);
                }
            }
            return sb.ToString();
        }
    }
}
