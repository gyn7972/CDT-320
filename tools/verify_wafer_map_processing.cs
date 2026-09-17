using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

internal static class VerifyProcessMaps
{
    private static int checks;
    private static string root;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name) { bool rejected = false; try { action(); } catch (InvalidDataException) { rejected = true; } Check(rejected, name); }
    private static bool Near(double a, double b) { return Math.Abs(a - b) < 1e-9; }
    private static WaferMapProcessSettings Settings(int angle, WaferMapGridOrigin origin, WaferMapSourceFormat format = WaferMapSourceFormat.Samsung)
    { return new WaferMapProcessSettings { Format = format, RotationDegrees = angle, GridOrigin = origin }; }
    private static T Copy<T>(T value)
    {
        using (var stream = new MemoryStream()) { var ser = new DataContractJsonSerializer(typeof(T)); QMC.Common.Data.Store.JsonPrettySerializer.WriteObject(stream, typeof(T), value); stream.Position = 0; return (T)ser.ReadObject(stream); }
    }
    private static PickupSubset Order() { return new PickupSubset { StartCorner = PickupStartCorner.TopLeft, Direction = PickupDirection.Horizontal, Pattern = PickupPattern.Straight }; }
    private static RecipeProject Project(string name)
    {
        Func<TapeFrameSubset> frame = () => new TapeFrameSubset { FrameSpecName = "TEST", DieMapX = 3, DieMapY = 2,
            DieSizeX = 1, DieSizeY = 2, PitchX = 0, PitchY = 0, OuterDiameterMm = 100, EdgeSkipMode = "ExternalMap", Rotate = "None" };
        return new RecipeProject { FileName = name, Die = new DieSubset { WidthMm = 1, HeightMm = 2 },
            Frame = frame(), InputFrame = frame(), OutputFrame = frame(), InputPickup = Order(), OutputPickup = Order() };
    }
    private static DieMap Rad()
    {
        string file = Path.Combine(root, "1234.02");
        string content = "[TEST/01000/02000/%6/&6/]\r\n";
        for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++)
            content += "X=" + (10 + x) + " Y=" + (20 + y) + " B=" + (x + 3 * y) + "\r\n";
        File.WriteAllText(file, content);
        return WaferMapParserRegistry.Load(file, WaferMapSourceFormat.Samsung);
    }
    private static void VerifyRotation(DieMap raw)
    {
        string before = WaferMapProcessService.ComputeMapHash(raw);
        foreach (int angle in new[] { 0, 180 }) foreach (WaferMapGridOrigin origin in Enum.GetValues(typeof(WaferMapGridOrigin)))
        {
            var settings = Settings(angle, origin);
            var map = WaferMapProcessService.Prepare(raw, settings, "Input");
            Check(map.Entries.Count == raw.Entries.Count, "entry count preserved");
            string compatibilityReason;
            Check(RecipeDieMapResolver.IsMappedInputCompatibleWithRecipe(map, raw, out compatibilityReason, settings), "runtime map matches approved baseline through recipe transformation: " + compatibilityReason);
            foreach (var source in raw.Entries)
            {
                var target = map.Entries.Single(e => e.OriginalMapX == source.OriginalMapX && e.OriginalMapY == source.OriginalMapY);
                int x = angle == 0 ? source.DieMapX : 2 - source.DieMapX;
                int y = angle == 0 ? source.DieMapY : 1 - source.DieMapY;
                Check(target.DieMapX == x && target.DieMapY == y, "rotated array address");
                Check(target.DieUid == source.DieUid && target.SourceBinCode == source.SourceBinCode && target.SourceToken == source.SourceToken &&
                    target.BinCode == source.BinCode && target.IsTarget == source.IsTarget, "same original die/bin/token/target");
                Check(Near(target.PosX, angle == 0 ? source.PosX : -source.PosX) && Near(target.PosY, angle == 0 ? source.PosY : -source.PosY), "physical rotation");
                Check(Near(target.PosX - map.OriginX, x * map.PitchX) && Near(target.PosY - map.OriginY, y * map.PitchY), "input alignment offset uses transformed corner");
                double gridX = origin == WaferMapGridOrigin.Center ? x - 1.0 : (origin == WaferMapGridOrigin.TopRight || origin == WaferMapGridOrigin.BottomRight) ? 2 - x : x;
                double gridY = origin == WaferMapGridOrigin.Center ? .5 - y : (origin == WaferMapGridOrigin.BottomLeft || origin == WaferMapGridOrigin.BottomRight) ? 1 - y : y;
                Check(target.LogicalGridX == gridX && target.LogicalGridY == gridY && target.SequenceNo == 0, "logical grid and deferred sequence");
            }
            PickupSequenceGenerator.ApplySequenceNumbers(map, Order());
            var first = map.Entries.Where(e => e.SequenceNo > 0).OrderBy(e => e.SequenceNo).First();
            Check(first.DieMapY == 0 && first.DieMapX == 0, "order assigned after rotation");
            var repeat = WaferMapProcessService.Prepare(map, settings, "Input");
            if (WaferMapProcessService.ComputeMapHash(map) != WaferMapProcessService.ComputeMapHash(repeat))
            {
                using (var f = File.Create(Path.Combine(root, "repeat-before.json"))) new DataContractJsonSerializer(typeof(DieMap)).WriteObject(f, map);
                using (var f = File.Create(Path.Combine(root, "repeat-after.json"))) new DataContractJsonSerializer(typeof(DieMap)).WriteObject(f, repeat);
            }
            Check(WaferMapProcessService.ComputeMapHash(map) == WaferMapProcessService.ComputeMapHash(repeat), "resume does not rotate twice or reset sequence");
            Check(WaferMapProcessService.ComputeMapHash(Copy(map)) == WaferMapProcessService.ComputeMapHash(map), "prepared map JSON roundtrip");
            string csvPath = Path.Combine(root, "prepared-" + angle + "-" + origin + ".csv");
            DieMapGenerator.SaveCsv(map, csvPath);
            var csv = DieMapGenerator.LoadCsv(csvPath);
            Check(csv != null && csv.ProcessTransform.SettingsKey == map.ProcessTransform.SettingsKey && csv.SourceContentHash == map.SourceContentHash, "CSV preserves prepared map provenance");
            Check(csv.Entries.All(e => { var original = map.Entries.Single(item => item.DieUid == e.DieUid); return e.SourceBinCode == original.SourceBinCode && e.SourceToken == original.SourceToken && e.LogicalGridX == original.LogicalGridX && e.LogicalGridY == original.LogicalGridY; }), "CSV preserves original data and logical grid");
            Reject(() => WaferMapProcessService.Prepare(map, Settings(angle == 0 ? 180 : 0, origin), "Input"), "changed settings rejected on prepared map");
            Reject(() => WaferMapProcessService.Prepare(map, settings, "Good"), "cross role replay rejected");
            var absolute = Copy(map); absolute.ProcessTransform.IsAbsolutePosition = true;
            Reject(() => WaferMapProcessService.Prepare(absolute, settings, "Input"), "absolute map cannot be rotated again");
        }
        Check(before == WaferMapProcessService.ComputeMapHash(raw), "source immutable across all preparations");
        foreach (int angle in new[] { 90, 270, -180, 360 }) Reject(() => WaferMapProcessService.Prepare(raw, Settings(angle, WaferMapGridOrigin.TopLeft), "Input"), "unsupported rotation blocked");
        Check(WaferMapProcessService.Prepare(raw, Settings(0, WaferMapGridOrigin.TopLeft, WaferMapSourceFormat.Camtek), "Input").Entries.Count == raw.Entries.Count, "registered map preparation ignores remote discriminator");
        Reject(() => WaferMapProcessService.ValidateSourceFormat(raw, Settings(0, WaferMapGridOrigin.TopLeft, WaferMapSourceFormat.Other)), "unregistered remote parser format blocked");
        var legacy = DieMapGenerator.GenerateRect(3, 2, 1, 2, 0, 0, "LEGACY");
        var legacyRotated = WaferMapProcessService.Prepare(legacy, Settings(180, WaferMapGridOrigin.Center, WaferMapSourceFormat.Legacy), "Input");
        Check(legacyRotated.GetCell(2, 1).OriginalMapX == 0 && legacyRotated.GetCell(2, 1).OriginalMapY == 0, "legacy missing original coordinates use pre-rotation address");
        var invalid = Copy(raw); invalid.Entries[0].DieMapX = -1;
        Reject(() => WaferMapProcessService.Prepare(invalid, null, "Input"), "negative local index blocked before normalization");
        invalid = Copy(raw); invalid.Entries[1].OriginalMapX = invalid.Entries[0].OriginalMapX; invalid.Entries[1].OriginalMapY = invalid.Entries[0].OriginalMapY;
        Reject(() => WaferMapProcessService.Prepare(invalid, null, "Input"), "duplicate source identity blocked");
    }
    private static void VerifyFormatsAndRecipe(DieMap raw)
    {
        string path = Path.Combine(root, "1234.02");
        Check(raw.FrameObjId == "1234.02" && raw.SourceFileName == "1234.02", "entire barcode identity retained");
        Check(raw.Entries.Select(e => e.DieUid).SequenceEqual(WaferMapParserRegistry.Load(path, WaferMapSourceFormat.Samsung).Entries.Select(e => e.DieUid)), "stable identity across fresh parses");
        Check(raw.SourceContentHash == WaferMapProcessService.ComputeHash(File.ReadAllBytes(path)), "source hash uses parsed bytes");
        Check(raw.Entries.Single(e => e.SourceBinCode == 0).BinCode == 255, "raw zero survives runtime BIN normalization");
        var project = Project("PROCESS-TEST");
        project.InputMapProcessing = Settings(180, WaferMapGridOrigin.BottomLeft);
        var imported = RecipeMapBuildService.ImportBaseAndBuildRole(project, path, false, p => true);
        Check(imported.Success, "extensionless numeric barcode import: " + imported.Message);
        string noExtension = Path.Combine(root, "1234"); File.Copy(path, noExtension);
        Check(WaferMapParserRegistry.LoadRegistration(noExtension, null).Entries.Count == 6, "legacy extensionless registration dispatch");
        var approval = RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.Input, DieMapGenerator.LoadJson(imported.InputMapPath), p => true);
        Check(approval.Success, "approval fixture: " + approval.Message);
        string approvedPath = RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input);
        byte[] mapBytes = File.ReadAllBytes(approvedPath);
        string originalApproval = project.InputMapApprovalHash;
        var updated = RecipeMapProcessSettingsService.CreateUpdatedProject(project, Settings(0, WaferMapGridOrigin.Center), Settings(180, WaferMapGridOrigin.TopRight, WaferMapSourceFormat.Legacy), false);
        Check(project.InputMapApprovalHash == originalApproval && project.InputMapProcessing.RotationDegrees == 180, "editing does not mutate current recipe");
        Check(updated.InputUseRemoteWaferMap == false && project.InputUseRemoteWaferMap == null, "registered mode saved explicitly without changing source recipe");
        string reason, resolved;
        var approved = RecipeDieMapResolver.LoadCompatibleMap(updated, RecipeMapKind.Input, out resolved, out reason);
        Check(approved != null, "profile-only save retains approved target mask: " + reason);
        var approvedCsv = DieMapGenerator.LoadCsv(Path.ChangeExtension(approvedPath, ".csv"));
        Check(RecipeMapPaths.IsMapApproved(updated, RecipeMapKind.Input, approvedCsv, out reason), "CSV fallback retains profile approval and original metadata");
        var tampered = Copy(approved); tampered.Entries[0].SourceBinCode = 777;
        Check(!RecipeMapPaths.IsMapApproved(updated, RecipeMapKind.Input, tampered, out reason), "original BIN mutation invalidates approval");
        Check(mapBytes.SequenceEqual(File.ReadAllBytes(approvedPath)), "profile-only save leaves registered role map bytes unchanged");
        var remoteUpdated = RecipeMapProcessSettingsService.CreateUpdatedProject(project, Settings(0, WaferMapGridOrigin.Center), project.OutputMapProcessing, true);
        Check(RecipeDieMapResolver.LoadCompatibleMap(remoteUpdated, RecipeMapKind.Input, out resolved, out reason) != null, "remote mode profile save preserves compatible approved registration for later registered mode");
        Check(remoteUpdated.InputUseRemoteWaferMap == true, "remote mode saved in recipe");
        var differentRemote = RecipeMapProcessSettingsService.CreateUpdatedProject(project, Settings(0, WaferMapGridOrigin.Center, WaferMapSourceFormat.Camtek), project.OutputMapProcessing, true);
        Check(RecipeDieMapResolver.LoadCompatibleMap(differentRemote, RecipeMapKind.Input, out resolved, out reason) != null, "remote discriminator change preserves registered map approval");
        var roundtrip = Copy(updated);
        Check(roundtrip.InputMapProcessing.RotationDegrees == 0 && roundtrip.OutputMapProcessing.RotationDegrees == 180 &&
            roundtrip.InputMapProcessing.GridOrigin == WaferMapGridOrigin.Center && roundtrip.OutputMapProcessing.GridOrigin == WaferMapGridOrigin.TopRight, "independent Input Output recipe settings persist");
        updated.InputMapProcessing.RotationDegrees = 180;
        Check(!RecipeMapPaths.IsMapApproved(updated, RecipeMapKind.Input, approved, out reason), "direct profile edit invalidates old approval");
        string camtek = Path.Combine(root, "CT.05");
        File.WriteAllText(camtek, "ROWCT: 2\r\nCOLCT: 3\r\nXDIES: 1\r\nYDIES: 2\r\nRowData: 001 000 @@@\r\nRowData: ___ 255 002\r\n");
        DieMap ct = WaferMapParserRegistry.Load(camtek, WaferMapSourceFormat.Camtek);
        Check(ct.Entries.Count == 5 && ct.GetCell(0,1) == null, "CAMTEK blank cells remain absent");
        Check(ct.GetCell(1,0).SourceBinCode == 0 && ct.GetCell(2,0).SourceToken == "@@@" && ct.GetCell(1,1).SourceToken == "255", "CAMTEK zero, mark and numeric255 distinguished");
        Reject(() => WaferMapParserRegistry.Load(camtek, WaferMapSourceFormat.Samsung), "CAMTEK as Samsung fails without fallback");
        Reject(() => WaferMapParserRegistry.Load(path, WaferMapSourceFormat.Camtek), "Samsung as CAMTEK fails without fallback");
        var ctProject = Project("CAMTEK-TEST"); ctProject.InputMapProcessing = Settings(0, WaferMapGridOrigin.TopLeft, WaferMapSourceFormat.Camtek);
        var ctImport = RecipeMapBuildService.ImportBaseAndBuildRole(ctProject, camtek, false, p => true);
        Check(ctImport.Success, "CAMTEK registration works: " + ctImport.Message);
        Check(DieMapGenerator.LoadJson(ctImport.InputMapPath).Entries.Single(e => e.OriginalMapX == 0 && e.OriginalMapY == 0).DieMapY == 0, "CAMTEK registered and remote orientation match");
        GeneratedWaferMap generated = WaferMapGeneration.Generate(new WaferMapGenerationSettings(25m, 3m, 2m, .2m, .3m));
        var legacyCircleProject = Project("LEGACY-CIRCLE");
        var legacyCircleResult = RecipeMapBuildService.CreateGridBaseAndBuildRole(legacyCircleProject, false, p => true);
        Check(legacyCircleResult.Success, "existing Recipe Grid circle creation remains supported");
        Check(WaferMapProcessService.Prepare(legacyCircleResult.RoleMap, Settings(180, WaferMapGridOrigin.Center, WaferMapSourceFormat.Circle), "Input").Entries.Count > 0, "Circle discriminator accepts existing registered Recipe Grid circle");
        foreach (int angle in new[] { 0, 180 })
        {
            var circle = GeneratedWaferMapCodec.ToDieMap(WaferMapGeneration.Rotate(generated, angle), "CIRCLE");
            var prepared = WaferMapProcessService.Prepare(circle, Settings(180, WaferMapGridOrigin.Center, WaferMapSourceFormat.Circle), "Good");
            Check(prepared.Generation == null && prepared.ProcessTransform.SourceGeneration.RotationDegrees == angle && prepared.Entries.Count == circle.Entries.Count, "generated circle retains baseline generation separately");
            var circleInputSettings = Settings(180, WaferMapGridOrigin.BottomLeft, WaferMapSourceFormat.Circle);
            var circleInput = WaferMapProcessService.Prepare(circle, circleInputSettings, "Input");
            Check(RecipeDieMapResolver.IsMappedInputCompatibleWithRecipe(circleInput, circle, out reason, circleInputSettings), "generated input still passes runtime approval comparison after rotation: " + reason);
        }
        foreach (int angle in new[] { 90, 270 })
        {
            var quarter = GeneratedWaferMapCodec.ToDieMap(WaferMapGeneration.Rotate(generated, angle), "QUARTER");
            Reject(() => WaferMapProcessService.Prepare(quarter, null, "Input"), "saved 90/270 maps blocked");
        }
        AppSettingsStore.Current.NetworkWaferMapFolder = root;
        RecipeStore.Current = Project("FETCH"); RecipeStore.Current.InputMapProcessing = Settings(0, WaferMapGridOrigin.TopLeft);
        LotWaferMapSlotInfo info;
        Check(LotWaferMapFetchService.TryFetchWaferMapByBarcode("1234.02", out info, out reason) && Path.GetFileName(info.LocalPath) == "1234.02", "remote file exact barcode: " + reason);
        RecipeStore.Current.InputMapProcessing.Format = WaferMapSourceFormat.Camtek;
        Check(!LotWaferMapFetchService.TryFetchWaferMapByBarcode("1234.02", out info, out reason), "format change invalidates cached parse and blocks mismatch");
    }
    private static void VerifyRegisteredAndRemoteModeIsolation(DieMap raw)
    {
        string placeSourcePath = Path.Combine(root, "RKE-existing-place.txt");
        File.WriteAllText(placeSourcePath, "PLACE_WAFER_ROW\tPLACE_WAFER_COL\r\n1\t1\r\n1\t2\r\n1\t3\r\n2\t1\r\n2\t2\r\n2\t3\r\n");
        DieMap place = DieMapGenerator.Load(placeSourcePath);
        Check(place.SourceFormat == "PLACE GRID TXT", "actual PLACE TXT fixture reproduces reported format");
        // Registered PLACE maps use Recipe pitch because this format does not contain a physical pitch.
        place.PitchY = 2; place.OriginY *= 2;
        foreach (DieMapEntry entry in place.Entries) entry.PosY *= 2;
        string placePath = Path.ChangeExtension(placeSourcePath, ".json");
        DieMapGenerator.SaveJson(place, placePath);
        byte[] placeBytes = File.ReadAllBytes(placePath);
        string resolved, reason;
        foreach (int version in new[] { 0, 1 })
        foreach (bool network in new[] { false, true })
        foreach (var format in new[] { WaferMapSourceFormat.Legacy, WaferMapSourceFormat.Samsung, WaferMapSourceFormat.Camtek, WaferMapSourceFormat.Circle, WaferMapSourceFormat.Other })
        {
            string tag = "V" + version + " network=" + network + " format=" + format;
            RecipeProject current = Project("MODE-ISOLATION-" + version + "-" + network + "-" + format);
            current.InputDieMapFileName = current.GoodBinDieMapFileName = current.NgBinDieMapFileName = placePath;
            RecipeMapPaths.SetConfiguredBaseFileName(current, RecipeMapKind.Input, placePath);
            if (version == 1)
                foreach (var kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
                    RecipeMapPaths.ApproveMap(current, kind, place);
            Check(RecipeDieMapResolver.LoadCompatibleMap(current, RecipeMapKind.Input, out resolved, out reason) != null, tag + " old registered map usable before format edit: " + reason);
            var settings = Settings(180, WaferMapGridOrigin.BottomLeft, format);
            RecipeProject updated = Copy(RecipeMapProcessSettingsService.CreateUpdatedProject(current, settings, null, network));
            Check(updated.InputMapProcessing.Format == format && updated.InputMapProcessing.RotationDegrees == 180, tag + " independent setting saved");
            Check(current.InputMapProcessing == null && placeBytes.SequenceEqual(File.ReadAllBytes(placePath)), tag + " format selection does not mutate prior project or map bytes");
            Check(RecipeMapPaths.IsMapApproved(updated, RecipeMapKind.Input, place, out reason), tag + " registered PLACE map retains approval for every remote discriminator");
            DieMap registered = RecipeDieMapResolver.LoadCompatibleMap(updated, RecipeMapKind.Input, out resolved, out reason);
            Check(registered != null && registered.SourceFormat == "PLACE GRID TXT", tag + " registered source is used without replacement");
            DieMap rotated = WaferMapProcessService.Prepare(registered, settings, "Input");
            Check(rotated.Entries.All(e => place.Entries.Any(original => original.OriginalMapX == e.OriginalMapX && original.OriginalMapY == e.OriginalMapY && Near(e.PosX, -original.PosX) && Near(e.PosY, -original.PosY))), tag + " registered map rotates with source identity preserved");
            Check(RecipeDieMapResolver.LoadCompatibleMap(updated, RecipeMapKind.GoodBin, out resolved, out reason) != null, tag + " independent output remains usable");
            var imported = RecipeMapBuildService.ImportBaseAndBuildRole(updated, Path.Combine(root, "1234.02"), false, p => true);
            Check(imported.Success, tag + " registration import ignores remote discriminator: " + imported.Message);
            Check(RecipeDieMapResolver.LoadCompatibleMap(updated, RecipeMapKind.Input, out resolved, out reason) == null, tag + " imported map still requires FINAL APPLY");
            var approved = RecipeMapBuildService.SaveRoleTargetMask(updated, RecipeMapKind.Input, DieMapGenerator.LoadJson(imported.InputMapPath), p => true);
            Check(approved.Success && RecipeDieMapResolver.LoadCompatibleMap(updated, RecipeMapKind.Input, out resolved, out reason) != null, tag + " registered replacement still follows FINAL APPLY: " + approved.Message);
            RecipeProject outputChanged = RecipeMapProcessSettingsService.CreateUpdatedProject(current, null, settings, network);
            Check(RecipeMapPaths.IsMapApproved(outputChanged, RecipeMapKind.GoodBin, place, out reason) && RecipeMapPaths.IsMapApproved(outputChanged, RecipeMapKind.NgBin, place, out reason), tag + " Output approval ignores stored discriminator");
            Check(RecipeDieMapResolver.LoadCompatibleMap(outputChanged, RecipeMapKind.GoodBin, out resolved, out reason) != null &&
                RecipeDieMapResolver.LoadCompatibleMap(outputChanged, RecipeMapKind.NgBin, out resolved, out reason) != null, tag + " Good and NG keep registered map");
            Check(RecipeDieMapResolver.LoadCompatibleMap(outputChanged, RecipeMapKind.Input, out resolved, out reason) != null, tag + " output setting does not invalidate Input");
        }
        var noMap = Project("MODE-ISOLATION-EMPTY");
        noMap.InputFrame.EdgeSkipMode = noMap.OutputFrame.EdgeSkipMode = noMap.Frame.EdgeSkipMode = "None";
        var emptyUpdated = RecipeMapProcessSettingsService.CreateUpdatedProject(noMap, Settings(0, WaferMapGridOrigin.TopLeft), null, false);
        Check(emptyUpdated.InputMapProcessing.Format == WaferMapSourceFormat.Samsung, "new recipe can save discriminator without registered map");
        Check(!RecipeMapPaths.IsMapApproved(emptyUpdated, RecipeMapKind.Input, null, out reason), "saved discriminator does not approve missing map");
        Reject(() => RecipeMapProcessSettingsService.CreateUpdatedProject(noMap, Settings(90, WaferMapGridOrigin.TopLeft), null, false), "format first does not permit unsupported process rotation");
        var unsupported = RecipeMapProcessSettingsService.CreateUpdatedProject(noMap, Settings(0, WaferMapGridOrigin.TopLeft, WaferMapSourceFormat.Other), null, false);
        Check(RecipeMapPaths.IsMapApproved(unsupported, RecipeMapKind.Input, raw, out reason), "unsupported remote discriminator does not block an existing registered map");
        RecipeStore.Current = unsupported;
        LotWaferMapSlotInfo info;
        Check(!LotWaferMapFetchService.TryFetchWaferMapByBarcode("1234.02", out info, out reason), "unsupported discriminator still blocks actual remote fetch");
        foreach (var format in new[] { WaferMapSourceFormat.Samsung, WaferMapSourceFormat.Camtek, WaferMapSourceFormat.Other })
        {
            var settings = Settings(0, WaferMapGridOrigin.TopLeft, format);
            Check(WaferMapParserRegistry.LoadRegistration(placeSourcePath, settings).SourceFormat == "PLACE GRID TXT", "registered PLACE import is independent of " + format);
            Check(WaferMapParserRegistry.LoadRegistration(Path.Combine(root, "CT.05"), settings).SourceFormat == "CAMTEK RowData", "registered CAMTEK import is independent of " + format);
        }
        RecipeStore.Current = Project("REMOTE-WITH-PLACE-REGISTRATION");
        RecipeStore.Current.InputDieMapFileName = placePath;
        RecipeStore.Current.InputMapProcessing = Settings(0, WaferMapGridOrigin.TopLeft);
        Check(LotWaferMapFetchService.TryFetchWaferMapByBarcode("1234.02", out info, out reason), "remote Samsung file succeeds with PLACE registration");
        Check(!LotWaferMapFetchService.TryFetchWaferMapByBarcode("CT.05", out info, out reason), "remote wrong format cannot fall back to registered PLACE map");
        Check(!LotWaferMapFetchService.TryFetchWaferMapByBarcode("ABSENT-REMOTE-MAP", out info, out reason), "missing remote source cannot fall back to registered map");
    }

    private static void VerifyPinnedState(DieMap raw)
    {
        var settings = Settings(180, WaferMapGridOrigin.BottomLeft);
        var map = WaferMapProcessService.Prepare(raw, settings, "Input");
        var wafer = new WaferMaterial { WaferId = "1234.02", WaferInstanceId = "PHYSICAL-1", CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage } };
        MaterialStateService.TestState = new MaterialSnapshot(); MaterialStateService.TestState.Wafers.Add(wafer);
        MaterialStateService.PinPreparedInputMap(wafer, "1234.02", true, map);
        wafer.HasInputStageDieMappingResult = true;
        wafer.DieIds.Add("PROCESSED-DIE");
        map.Entries[0].PosX += 10;
        Check(MaterialStateService.GetPreparedInputMap(wafer, "1234.02", true).Entries[0].PosX != map.Entries[0].PosX, "pin stores isolated snapshot");
        var restored = Copy(wafer);
        MaterialStateService.TestState.Wafers[0] = restored;
        var pinned = MaterialStateService.GetPreparedInputMap(restored, "1234.02", true);
        var draft = new QMC.CDT_320.Ui.Common.WaferMaps.PickupOrderDraft(pinned, Order(), null, PickupSequenceGenerator.Build(pinned, Order()).Select(e => e.DieUid), false);
        Check(draft.Map.ProcessTransform.SettingsKey == pinned.ProcessTransform.SettingsKey && draft.Map.Entries[0].LogicalGridX == pinned.Entries[0].LogicalGridX && draft.Map.Entries[0].SourceToken == pinned.Entries[0].SourceToken, "pickup-order editor preserves map transformation and source metadata");
        var copiedState = ActualSnapshotClone.Copy(MaterialStateService.TestState);
        Check(copiedState.Wafers[0].InputPreparedMap != null && copiedState.Wafers[0].InputPreparedMapBarcode == "1234.02" && copiedState.Wafers[0].InputPreparedMapUsesNetwork, "actual storage clone retains input pinned state");
        Check(!object.ReferenceEquals(copiedState.Wafers[0].InputPreparedMap, restored.InputPreparedMap), "actual storage clone isolates prepared map graph");
        MaterialStateService.PinPreparedInputMap(restored, "1234.02", true, pinned);
        Check(restored.HasInputStageDieMappingResult && restored.DieIds.Single() == "PROCESSED-DIE", "resume preserves completed input state");
        Reject(() => MaterialStateService.GetPreparedInputMap(restored, "DIFFERENT", true), "barcode mismatch blocks reuse");
        Reject(() => MaterialStateService.GetPreparedInputMap(restored, "1234.02", false), "source mode mismatch blocks reuse");
        var changed = WaferMapProcessService.Prepare(raw, Settings(0, WaferMapGridOrigin.BottomLeft), "Input");
        Reject(() => MaterialStateService.PinPreparedInputMap(restored, "1234.02", true, changed), "settings replacement rejected for same wafer");
        changed = Copy(pinned); changed.ProcessTransform.SourceMapHash = "replacement";
        Reject(() => MaterialStateService.PinPreparedInputMap(restored, "1234.02", true, changed), "source replacement rejected for same wafer");
        var outputMap = WaferMapProcessService.Prepare(raw, Settings(180, WaferMapGridOrigin.TopRight), "Good");
        PickupSequenceGenerator.ApplySequenceNumbers(outputMap, Order());
        var ordered = outputMap.Entries.Where(e => e.SequenceNo > 0).OrderBy(e => e.SequenceNo).ToList();
        var output = new WaferMaterial { WaferInstanceId = "OUTPUT-1", OutputReceivePreparedMapInstanceId = "OUTPUT-1", OutputReceivePreparedMap = outputMap,
            OutputReceiveNextIndex = 1, OutputReceiveTotalCount = ordered.Count - 1 };
        output.OutputReceiveSlots = ordered.Select((e,i) => new OutputReceiveSlotMaterial { OrderIndex = i, DieMapX = e.DieMapX, DieMapY = e.DieMapY,
            OriginalMapX = e.OriginalMapX, OriginalMapY = e.OriginalMapY, PosX = e.PosX, PosY = e.PosY, LogicalGridX = e.LogicalGridX, LogicalGridY = e.LogicalGridY,
            DieUid = i == 0 ? "ALREADY-PLACED" : "", IsTarget = i < ordered.Count - 1 }).ToList();
        output = Copy(output);
        var state = new MaterialSnapshot(); state.Wafers.Add(output);
        state.Dies.Add(new DieMaterial { DieId = "PROCESSED-DIE", InputSourceBinCode = 0, InputSourceToken = "000", InputLogicalGridX = -.5, InputLogicalGridY = 2, InputMapGridOrigin = WaferMapGridOrigin.Center });
        var snapshotClone = ActualSnapshotClone.Copy(state);
        Check(snapshotClone.Dies[0].InputMapGridOrigin == WaferMapGridOrigin.Center && Copy(snapshotClone).Dies[0].InputMapGridOrigin == WaferMapGridOrigin.Center, "applied input origin survives actual typed clone and JSON snapshot");
        Check(snapshotClone.Wafers[0].OutputReceivePreparedMap != null && snapshotClone.Wafers[0].OutputReceivePreparedMapInstanceId == "OUTPUT-1", "actual storage clone retains output prepared map");
        Check(snapshotClone.Wafers[0].OutputReceiveSlots[0].LogicalGridX == output.OutputReceiveSlots[0].LogicalGridX && snapshotClone.Dies[0].InputSourceBinCode == 0 && snapshotClone.Dies[0].InputSourceToken == "000" && snapshotClone.Dies[0].InputLogicalGridX == -.5, "actual storage clone preserves logical coordinates and original BIN");
        output = Copy(snapshotClone).Wafers[0];
        var resumed = MaterialStateService.TestOutputRestore(output);
        Check(resumed.Select(e => e.OriginalMapX + "," + e.OriginalMapY).SequenceEqual(ordered.Select(e => e.OriginalMapX + "," + e.OriginalMapY)), "output restore uses pinned order without recipe reload");
        Check(output.OutputReceiveSlots[0].DieUid == "ALREADY-PLACED" && output.OutputReceiveNextIndex == 1 && !output.OutputReceiveSlots.Last().IsTarget, "output occupancy/progress/receive limit survive restart");
        var corrupt = Copy(output); corrupt.OutputReceiveSlots[0].PosX += 1;
        Reject(() => MaterialStateService.TestOutputRestore(corrupt), "output slot geometry mismatch blocked");
        corrupt = Copy(output); corrupt.OutputReceiveSlots[0].LogicalGridX += 1;
        Reject(() => MaterialStateService.TestOutputRestore(corrupt), "output slot logical coordinate mismatch blocked");
        corrupt = Copy(output); corrupt.WaferInstanceId = "OUTPUT-2";
        Reject(() => MaterialStateService.TestOutputRestore(corrupt), "output instance mismatch blocked");
        output.OutputReceivePreparedMap = null;
        resumed = MaterialStateService.TestOutputRestore(output);
        Check(resumed.Count == ordered.Count && output.OutputReceiveSlots[0].DieUid == "ALREADY-PLACED", "legacy output slot restore preserves progress without current recipe");
        corrupt = Copy(output); corrupt.OutputReceiveSlots[0].PosX = double.PositiveInfinity;
        Reject(() => MaterialStateService.TestOutputRestore(corrupt), "legacy output infinite slot position blocked");
        var replaced = Copy(restored); replaced.OutputReceivePreparedMap = outputMap;
        MaterialStateService.TestNewPhysicalWafer(replaced);
        Check(replaced.InputPreparedMap == null && replaced.OutputReceivePreparedMap == null && replaced.InputPreparedMapBarcode == null, "new physical wafer does not inherit previous prepared map");
    }
    private static int Main()
    {
        try { root = AppDomain.CurrentDomain.BaseDirectory; var raw = Rad(); VerifyRotation(raw); VerifyFormatsAndRecipe(raw); VerifyRegisteredAndRemoteModeIsolation(raw); VerifyPinnedState(raw); Console.WriteLine("PASS: " + checks + " process-map assertions; no equipment runtime executed."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

// Only hardware/store/session boundaries are substituted; material POCOs, transforms, file parsers,
// recipe role services, input pinning and the extracted output cache implementation are production source.
namespace QMC.CDT320
{
    public enum BinSide { Good, Ng }
    public class AppSettings { public bool UseLotNetworkWaferMap = true; public string NetworkWaferMapFolder = ""; public string NetworkWaferMapFormat = "Rad"; }
    public static class AppSettingsStore { public static AppSettings Current = new AppSettings(); }
}
namespace QMC.CDT320.Recipes
{
    public static class RecipeStore { public static RecipeProject Current; public static string GetLastProjectName() { return Current == null ? null : Current.FileName; } public static RecipeProject LoadLastOrDefaultCached() { return Current; } }
}
namespace QMC.CDT320.Materials
{
    [DataContract] public class InputStageReviewSavedVerification { public InputStageReviewSavedVerification Clone() { return new InputStageReviewSavedVerification(); } }
    public static partial class MaterialStateService
    {
        private static readonly object _stateSync = new object();
        public static MaterialSnapshot TestState;
        private static MaterialSnapshot State { get { return TestState; } }
        private static void NotifyAndSave(string reason) { }
        private static string EnsureWaferInstanceIdNoLock(WaferMaterial wafer) { return wafer.WaferInstanceId; }
        private static DieMap LoadRecipeBinMap(BinSide side) { throw new Exception("Unexpected recipe reload during resume"); }
        private static PickupSubset ResolveOutputPickup(RecipeProject project) { return project.OutputPickup; }
        private static List<DieMapEntry> BuildOutputReceiveOrder(DieMap map, PickupSubset pickup) { return PickupSequenceGenerator.Build(map, pickup); }
        public static List<DieMapEntry> TestOutputRestore(WaferMaterial wafer) { _outputReceiveOrderCache.Clear(); return ResolveOutputReceiveOrderCached(BinSide.Good, wafer); }
        public static void TestNewPhysicalWafer(WaferMaterial wafer) { ClearPreparedWaferMapsForNewInstanceNoLock(wafer); }
    }
}
