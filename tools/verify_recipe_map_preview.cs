// Offline only: copied fixtures, isolated Recipe/DataPaths, no Form1, Machine or sequence startup.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Pages.Recipe;
using QMC.Common.Data.Store;

internal static class VerifyRecipeMapPreview
{
    private static int checks;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private static void Near(double? actual, double expected, string message)
    { Check(actual.HasValue && Math.Abs(actual.Value - expected) < 0.000001, message); }
    private static object Get(object page, string field) { return typeof(MapCreatePage).GetField(field, Private).GetValue(page); }
    private static void Set(object page, string field, object value) { typeof(MapCreatePage).GetField(field, Private).SetValue(page, value); }
    private static object Call(object page, string name, params object[] args)
    { return typeof(MapCreatePage).GetMethod(name, Private).Invoke(page, args); }
    private static string Snapshot(DieMap map) { return WaferMapProcessService.ComputeMapHash(map); }
    private static void Reject(Action action, string message)
    { bool rejected = false; try { action(); } catch (InvalidDataException) { rejected = true; } Check(rejected, message); }

    private sealed class OfflinePage : MapCreatePage
    {
        public OfflinePage(string role) : base(role) { }
        protected override void OnLoad(EventArgs e) { }
        protected override void OnVisibleChanged(EventArgs e) { }
    }

    private static DieMap MakeMap(int width, int height)
    {
        var map = new DieMap { DieMapX = width, DieMapY = height, PitchX = 2.2, PitchY = 3.1,
            DieSizeX = 2, DieSizeY = 3, OriginX = -(width - 1) * 1.1, OriginY = -(height - 1) * 1.55,
            SourceFormat = "PLACE GRID TXT", SourceFileName = "wafer.02", FrameObjId = "wafer.02", Entries = new List<DieMapEntry>() };
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            if ((x == 0 && y == 0) || (x == width - 1 && y == height - 1)) continue;
            int bin = (x + y) % 3 + 1;
            map.Entries.Add(new DieMapEntry { DieMapX = x, DieMapY = y,
                OriginalMapX = 166 + x, OriginalMapY = 227 - y, DieUid = "source-" + x + "-" + y,
                EquipmentGridX = x - (width - 1) / 2.0, EquipmentGridY = y - (height - 1) / 2.0,
                PosX = map.OriginX + x * map.PitchX, PosY = map.OriginY + y * map.PitchY,
                SourceBinCode = bin, SourceToken = "token:" + x + ":" + y, BinCode = bin,
                IsTarget = (x + y) % 4 != 0, SequenceNo = 99 });
        }
        return map;
    }

    private static void VerifyCoordinates(DieMap source, RecipeMapPreview preview, int rotation, WaferMapGridOrigin origin)
    {
        Check(source.Entries.Count == preview.Map.Entries.Count, "no added or lost dies, including holes");
        foreach (var entry in preview.Map.Entries)
        {
            var original = preview.GetSource(entry);
            Check(source.Entries.Contains(original) && !ReferenceEquals(entry, original), "display entry maps to exactly its source object");
            int x = rotation == 0 ? original.DieMapX : source.DieMapX - 1 - original.DieMapX;
            int y = rotation == 0 ? original.DieMapY : source.DieMapY - 1 - original.DieMapY;
            Check(entry.DieMapX == x && entry.DieMapY == y, "rotation moves the same die");
            double gx = x, gy = y;
            switch (origin)
            {
                case WaferMapGridOrigin.TopRight: gx = source.DieMapX - 1 - x; break;
                case WaferMapGridOrigin.BottomLeft: gy = source.DieMapY - 1 - y; break;
                case WaferMapGridOrigin.BottomRight: gx = source.DieMapX - 1 - x; gy = source.DieMapY - 1 - y; break;
                case WaferMapGridOrigin.Center: gx = x - (source.DieMapX - 1) / 2.0; gy = (source.DieMapY - 1) / 2.0 - y; break;
            }
            Near(entry.LogicalGridX, gx, "applied X"); Near(entry.LogicalGridY, gy, "applied Y");
            Near(entry.PosX, original.PosX * (rotation == 0 ? 1 : -1), "physical relative X follows die");
            Near(entry.PosY, original.PosY * (rotation == 0 ? 1 : -1), "physical relative Y follows die");
            Check(entry.DieUid == original.DieUid && entry.SourceToken == original.SourceToken &&
                entry.SourceBinCode == (original.SourceBinCode ?? original.BinCode), "UID, token and source BIN stay with die");
            Check(entry.OriginalMapX == (original.OriginalMapX >= 0 ? original.OriginalMapX : original.DieMapX) &&
                entry.OriginalMapY == (original.OriginalMapY >= 0 ? original.OriginalMapY : original.DieMapY), "downloaded source grid retained");
        }
    }

    private static void VerifyOrder(DieMap map, PickupSubset pickup)
    {
        // Independent traversal of occupied rows/columns; compare UID order, not just counts.
        bool right = pickup.StartCorner == PickupStartCorner.TopRight || pickup.StartCorner == PickupStartCorner.BottomRight;
        bool bottom = pickup.StartCorner == PickupStartCorner.BottomLeft || pickup.StartCorner == PickupStartCorner.BottomRight;
        bool horizontal = pickup.Direction == PickupDirection.Horizontal;
        var rows = Enumerable.Range(0, map.DieMapY).ToList(); if (bottom) rows.Reverse();
        var cols = Enumerable.Range(0, map.DieMapX).ToList(); if (right) cols.Reverse();
        var expected = new List<string>(); bool reverse = false;
        foreach (int outer in horizontal ? rows : cols)
        {
            var line = new List<DieMapEntry>();
            foreach (int inner in horizontal ? cols : rows)
            {
                int x = horizontal ? inner : outer, y = horizontal ? outer : inner;
                var entry = map.Entries.SingleOrDefault(e => e.DieMapX == x && e.DieMapY == y && e.IsTarget);
                if (entry != null) line.Add(entry);
            }
            if (line.Count == 0) continue;
            if (reverse) line.Reverse();
            expected.AddRange(line.Select(e => e.DieUid));
            if (pickup.Pattern == PickupPattern.ZigZag) reverse = !reverse;
        }
        var ordered = map.Entries.Where(e => e.IsTarget).OrderBy(e => e.SequenceNo).ToList();
        Check(ordered.Select(e => e.DieUid).SequenceEqual(expected), "process order matches physical corner/direction/pattern after rotation and filter");
        Check(ordered.Select(e => e.SequenceNo).SequenceEqual(Enumerable.Range(1, ordered.Count)), "sequence contiguous after SKIP/filter");
        Check(map.Entries.Where(e => !e.IsTarget).All(e => e.SequenceNo == 0), "SKIP has no process number");
    }

    private static void VerifyMatrix(DieMap source, bool allRoutes)
    {
        string before = Snapshot(source);
        foreach (RecipeMapKind kind in new[] { RecipeMapKind.Input, RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
        foreach (int rotation in new[] { 0, 180 })
        foreach (WaferMapGridOrigin origin in Enum.GetValues(typeof(WaferMapGridOrigin)))
        foreach (PickupStartCorner corner in (allRoutes ? Enum.GetValues(typeof(PickupStartCorner)).Cast<PickupStartCorner>() : new[] { PickupStartCorner.TopRight }))
        foreach (PickupDirection direction in (allRoutes ? Enum.GetValues(typeof(PickupDirection)).Cast<PickupDirection>() : new[] { PickupDirection.Vertical }))
        foreach (PickupPattern pattern in (allRoutes ? Enum.GetValues(typeof(PickupPattern)).Cast<PickupPattern>() : new[] { PickupPattern.ZigZag }))
        {
            var settings = new WaferMapProcessSettings { Format = WaferMapSourceFormat.Samsung, RotationDegrees = rotation, GridOrigin = origin };
            var pickup = new PickupSubset { StartCorner = corner, Direction = direction, Pattern = pattern };
            var preview = RecipeMapPreviewService.Create(source, settings, pickup, kind, new HashSet<int> { 1, 3 });
            VerifyCoordinates(source, preview, rotation, origin);
            if (allRoutes) VerifyOrder(preview.Map, pickup);
            foreach (var e in preview.Map.Entries)
            {
                var raw = preview.GetSource(e);
                Check(e.IsTarget == (raw.IsTarget && (kind != RecipeMapKind.Input || e.BinCode == 1 || e.BinCode == 3)), "only Input uses BIN filter");
            }
        }
        Check(before == Snapshot(source), "all preview combinations leave complete source data unchanged");
    }

    private static RecipeProject CreateProject(DieMap map, string root)
    {
        string baseFile = Path.Combine(root, "base.json"), roleFile = Path.Combine(root, "input.json");
        DieMapGenerator.SaveJson(map, baseFile); DieMapGenerator.SaveJson(map, roleFile);
        var frame = new TapeFrameSubset { DieMapX = map.DieMapX, DieMapY = map.DieMapY,
            DieSizeX = map.DieSizeX, DieSizeY = map.DieSizeY,
            PitchX = map.PitchX - map.DieSizeX, PitchY = map.PitchY - map.DieSizeY, OuterDiameterMm = map.OuterDiameterMm };
        var project = new RecipeProject { FileName = "VERIFY-CREATE-PREVIEW", InputFrame = frame, OutputFrame = frame,
            Die = new DieSubset { WidthMm = map.DieSizeX, HeightMm = map.DieSizeY },
            InputBaseWaferMapFileName = baseFile, OutputBaseWaferMapFileName = baseFile,
            InputDieMapFileName = roleFile, GoodBinDieMapFileName = roleFile, NgBinDieMapFileName = roleFile,
            InputMapProcessing = new WaferMapProcessSettings { Format = WaferMapSourceFormat.Samsung, RotationDegrees = 180, GridOrigin = WaferMapGridOrigin.BottomLeft },
            OutputMapProcessing = new WaferMapProcessSettings { Format = WaferMapSourceFormat.Other, GridOrigin = WaferMapGridOrigin.TopRight },
            InputPickup = new PickupSubset { StartCorner = PickupStartCorner.TopRight, Direction = PickupDirection.Vertical, Pattern = PickupPattern.ZigZag } };
        RecipeStore.Save(project);
        Check(UnitDataStore.SaveRecipe(new InputStageRecipe(), project.FileName, "InputStageUnit"), "write isolated unit Recipe fixture");
        return project;
    }

    private static void VerifyPage(DieMap map, string output)
    {
        string root = Path.Combine(output, "data"); Directory.CreateDirectory(root); DataPaths.Root = root;
        var project = CreateProject(map, root);
        using (var page = new OfflinePage("recipe.inputMapCreate"))
        {
            Set(page, "_project", project);
            Call(page, "ApplyMap", DieMapGenerator.LoadJson(project.InputDieMapFileName), "INPUT 등록 맵 · Recipe 적용 미리보기");
            var raw = (DieMap)Get(page, "_map");
            var preview = (RecipeMapPreview)Get(page, "_preview");
            Check(preview != null, "real Create builds applied preview");
            VerifyCoordinates(raw, preview, 180, WaferMapGridOrigin.BottomLeft);
            var view = (DieMapView)Get(page, "_mapView");
            var clicked = preview.Map.Entries.First(e => e.IsTarget && e.SequenceNo == 1);
            var source = preview.GetSource(clicked);
            var rawUid = source.DieUid; int rawX = source.DieMapX, rawY = source.DieMapY, sourceBin = source.BinCode;
            view.SelectedEntry = clicked;
            Call(page, "OnMapCellClicked", clicked);
            Check(!source.IsTarget && source.DieUid == rawUid && source.DieMapX == rawX && source.DieMapY == rawY && source.BinCode == sourceBin,
                "actual rotated click edits only source mask, preserving BIN and address");
            Check(raw.ProcessTransform == null, "page retains untransformed map for FINAL APPLY");
            preview = (RecipeMapPreview)Get(page, "_preview");
            Check(view.SelectedEntry != null && view.SelectedEntry.DieUid == rawUid && !view.SelectedEntry.IsTarget, "selection stays on same die after edit");
            VerifyOrder(preview.Map, project.InputPickup);
            Check((bool)Get(page, "_maskDirty"), "edited mask remains dirty");
            string rawAfterEdit = Snapshot(raw);

            // Recipe-only changes must refresh even when the registered file path/time are unchanged.
            Set(page, "_currentMapPath", project.InputDieMapFileName);
            DateTime writeUtc = File.GetLastWriteTimeUtc(project.InputDieMapFileName);
            Set(page, "_currentMapWriteUtc", writeUtc);
            project.InputMapProcessing.GridOrigin = WaferMapGridOrigin.Center;
            project.InputMapProcessing.RotationDegrees = 0;
            project.InputPickup.StartCorner = PickupStartCorner.BottomLeft;
            RecipeStore.Save(project);
            Call(page, "ReloadRecipeMapIfChanged");
            Check(Get(page, "_previewError") == null, "settings-only refresh succeeds");
            preview = (RecipeMapPreview)Get(page, "_preview");
            Check(preview != null, "registered file reload remains compatible with Recipe frame");
            Check(Snapshot(raw) == rawAfterEdit && (bool)Get(page, "_maskDirty"), "settings reload preserves pending mask and source geometry");
            Check(File.GetLastWriteTimeUtc(project.InputDieMapFileName) == writeUtc, "preview never rewrites registered map file");
            VerifyCoordinates(raw, preview, 0, WaferMapGridOrigin.Center); VerifyOrder(preview.Map, project.InputPickup);

            var unitRecipe = new InputStageRecipe(); unitRecipe.DieMap.PickupBinFilterCsv = "1.3";
            Check(UnitDataStore.SaveRecipe(unitRecipe, project.FileName, "InputStageUnit"), "save isolated BIN filter");
            Call(page, "ReloadRecipeMapIfChanged");
            preview = (RecipeMapPreview)Get(page, "_preview");
            Check(preview.Map.Entries.Where(e => e.IsTarget).All(e => e.BinCode == 1 || e.BinCode == 3), "saved Recipe filter refreshes without map rewrite");
            VerifyOrder(preview.Map, project.InputPickup);

            unitRecipe.DieMap.PickupBinFilterCsv = "99999";
            Check(UnitDataStore.SaveRecipe(unitRecipe, project.FileName, "InputStageUnit"), "save excluding BIN filter");
            Call(page, "ReloadRecipeMapIfChanged");
            preview = (RecipeMapPreview)Get(page, "_preview");
            Check(preview != null && preview.Map.Entries.All(e => !e.IsTarget && e.SequenceNo == 0) &&
                !((Control)Get(page, "btnSave")).Enabled, "no-target filter shows all SKIP and blocks apply");
            Check(Snapshot(raw) == rawAfterEdit, "BIN filters never overwrite registered Target/Skip mask");
            unitRecipe.DieMap.PickupBinFilterCsv = "1.3";
            Check(UnitDataStore.SaveRecipe(unitRecipe, project.FileName, "InputStageUnit"), "restore BIN filter");
            Call(page, "ReloadRecipeMapIfChanged");

            var targetsBeforeInvert = raw.Entries.ToDictionary(e => e.DieUid, e => e.IsTarget);
            Call(page, "InvertMapTargets");
            Check(raw.Entries.All(e => e.IsTarget != targetsBeforeInvert[e.DieUid]), "bulk invert changes original mask by die");
            VerifyOrder(((RecipeMapPreview)Get(page, "_preview")).Map, project.InputPickup);
            Call(page, "InvertMapTargets");
            Check(raw.Entries.All(e => e.IsTarget == targetsBeforeInvert[e.DieUid]), "second invert restores original mask");

            var result = RecipeMapBuildService.SaveRoleTargetMask((RecipeProject)Get(page, "_project"), RecipeMapKind.Input, raw, RecipeStore.Save);
            Check(result.Success, "actual role-mask save succeeds in sandbox: " + result.Message);
            var saved = DieMapGenerator.LoadJson(project.InputDieMapFileName);
            Check(saved.ProcessTransform == null, "saved role map is not rotated a second time");
            var savedDie = saved.Entries.Single(e => e.DieUid == rawUid);
            Check(!savedDie.IsTarget && savedDie.DieMapX == rawX && savedDie.DieMapY == rawY && savedDie.SourceToken == source.SourceToken &&
                savedDie.SourceBinCode == source.SourceBinCode, "saved SKIP applies to the original die with its source data");
            Call(page, "ApplyMap", saved, "INPUT 등록 맵 · Recipe 적용 미리보기");
            Check(!(bool)Get(page, "_maskDirty"), "save reload clears dirty state");
            view = (DieMapView)Get(page, "_mapView");
            view.Size = new Size(1250, 820);
            view.Dock = DockStyle.None;
            var first = view.Map.Entries.First(e => e.IsTarget);
            view.SelectedEntry = first;
            typeof(DieMapView).GetField("_hover", Private).SetValue(view, first);
            Check(view.CellTextResolver(first) == first.SequenceNo.ToString(), "Create displays process number");
            Check(view.LegendItemsResolver().Length == 2, "Create legend contains plan states only");
            using (var bitmap = new Bitmap(view.Width, view.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                typeof(DieMapView).GetMethod("OnPaint", Private).Invoke(view, new object[] { new PaintEventArgs(graphics, new Rectangle(Point.Empty, view.Size)) });
                bitmap.Save(Path.Combine(output, "create-applied-preview.png"), ImageFormat.Png);
            }
            byte[] beforeZoom = RenderBytes(view);
            for (int zoom = 0; zoom < 4; zoom++)
                typeof(DieMapView).GetMethod("OnMouseWheelEvt", Private).Invoke(view,
                    new object[] { view, new MouseEventArgs(MouseButtons.None, 0, view.Width / 2, view.Height / 2, 120) });
            byte[] afterZoom = RenderBytes(view);
            CheckOverlayPixels(beforeZoom, afterZoom, view.Size);
            Check(typeof(DieMapView).GetMethod("HitTest", Private).Invoke(view, new object[] { view.Width / 2, 20 }) == null &&
                typeof(DieMapView).GetMethod("HitTest", Private).Invoke(view, new object[] { view.Width / 2, view.Height - 10 }) == null,
                "title/coordinate areas cannot edit hidden dies when zoomed");
            File.WriteAllBytes(Path.Combine(output, "create-applied-preview-zoom.png"), afterZoom);
            project.InputMapProcessing.RotationDegrees = 90; Set(page, "_project", project);
            Call(page, "RefreshSettingLabels");
            Check(view.Map == null && Get(page, "_previewError") != null && !((Control)Get(page, "btnSave")).Enabled,
                "unsupported rotation shows failure and disables apply, never stale preview");
        }
        using (var page = new OfflinePage("recipe.binMapCreate"))
        {
            Set(page, "_project", project);
            Call(page, "ApplyMap", map, "OUTPUT 등록 맵 · Recipe 적용 미리보기");
            var preview = (RecipeMapPreview)Get(page, "_preview");
            Check(preview != null && preview.Map.ProcessTransform.Settings.GridOrigin == WaferMapGridOrigin.TopRight,
                "Output uses its own origin and ignores unsupported Input rotation");
            Check(preview.Map.Entries.Count(e => e.IsTarget) == map.Entries.Count(e => e.IsTarget), "Output ignores Input BIN filter and source discriminator");
        }
        Reject(() => RecipeMapPreviewService.LoadSavedInputBins("VERIFY-NODE-MISSING"), "missing filter Recipe is reported, not replaced with all bins");
    }

    private static byte[] RenderBytes(DieMapView view)
    {
        using (var bitmap = new Bitmap(view.Width, view.Height))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var stream = new MemoryStream())
        {
            typeof(DieMapView).GetMethod("OnPaint", Private).Invoke(view, new object[] { new PaintEventArgs(graphics, new Rectangle(Point.Empty, view.Size)) });
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }

    private static void VerifyNumberRendering()
    {
        using (var view = new QMC.CDT_320.Ui.Controls.WaferMapView { Size = new Size(420, 340) })
        {
            Check(!view.FitCellTextToCell, "main/common default label style unchanged");
            Check(!view.KeepOverlaysVisible, "main/common default viewport unchanged");
            view.Map = new DieMap { DieMapX = 1, DieMapY = 1, PitchX = 1, PitchY = 1, DieSizeX = 1, DieSizeY = 1,
                Entries = new List<DieMapEntry> { new DieMapEntry { IsTarget = true, SequenceNo = 1234 } } };
            view.FitCellTextToCell = true;
            view.CellTextResolver = e => ""; byte[] blank = RenderBytes(view);
            view.CellTextResolver = e => "1234";
            Check(!blank.SequenceEqual(RenderBytes(view)), "four-digit process numbers are rendered when cell is large enough");
        }
    }

    private static void CheckOverlayPixels(byte[] before, byte[] after, Size size)
    {
        using (var firstStream = new MemoryStream(before))
        using (var secondStream = new MemoryStream(after))
        using (var first = new Bitmap(firstStream))
        using (var second = new Bitmap(secondStream))
        {
            bool equal = true;
            for (int x = 0; x < size.Width; x++)
            for (int y = 0; y < size.Height; y++)
                if ((y < 48 || y >= size.Height - 54) && first.GetPixel(x, y) != second.GetPixel(x, y)) equal = false;
            Check(equal, "zoom preserves readable title, legend and X/Y overlays pixel for pixel");
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
            Check(!AppDomain.CurrentDomain.BaseDirectory.StartsWith("D:\\CDT-320\\", StringComparison.OrdinalIgnoreCase), "test executable runs outside deployment");
            Check(!output.StartsWith("D:\\CDT-320\\", StringComparison.OrdinalIgnoreCase), "test artifacts outside deployment");
            var synthetic = MakeMap(4, 3); VerifyMatrix(synthetic, true); VerifyMatrix(MakeMap(3, 4), true);
            for (int version = 1; version <= 4; version++)
            foreach (int storedRotation in new[] { 0, 180 })
            {
                var generated = WaferMapGeneration.Generate(new WaferMapGenerationSettings(30m, 2m, 3m, 0.2m, 0.1m, 0m, version));
                generated = WaferMapGeneration.Rotate(generated, storedRotation);
                var source = GeneratedWaferMapCodec.ToDieMap(generated, "generated-" + version + "-" + storedRotation);
                source.Entries[0].IsTarget = false;
                VerifyMatrix(source, false);
                Check(source.Generation != null, "generated registered definition retained after preview");
            }
            foreach (int angle in new[] { 90, 270 }) Reject(() => RecipeMapPreviewService.Create(synthetic,
                new WaferMapProcessSettings { RotationDegrees = angle }, null, RecipeMapKind.Input, null), "90/270 remain blocked");
            var prepared = RecipeMapPreviewService.Create(synthetic, null, null, RecipeMapKind.Input, null);
            Reject(() => RecipeMapPreviewService.Create(prepared.Map, null, null, RecipeMapKind.Input, null), "prepared map cannot become registered baseline");
            Reject(() => prepared.GetSource(WaferMapProcessService.CloneMap(prepared.Map).Entries[0]), "stale preview entry cannot edit current source");
            var duplicate = WaferMapProcessService.CloneMap(synthetic);
            duplicate.Entries[1].OriginalMapX = duplicate.Entries[0].OriginalMapX;
            duplicate.Entries[1].OriginalMapY = duplicate.Entries[0].OriginalMapY;
            Reject(() => RecipeMapPreviewService.Create(duplicate, null, null, RecipeMapKind.Input, null), "ambiguous original address blocked");
            for (int i = 1; i < args.Length; i++) VerifyMatrix(DieMapGenerator.LoadJson(args[i]), false);
            VerifyPage(args.Length > 1 ? DieMapGenerator.LoadJson(args[1]) : synthetic, output);
            VerifyNumberRendering();
            Console.WriteLine("PASS: " + checks + " assertions; recipe projection, source identity, click/save/reload, filters and order. No equipment runtime.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
