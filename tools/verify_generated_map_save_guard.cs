using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320;
using QMC.CDT_320.Ui.Dialogs;

// 실제 Form1 저장 coordinator를 컴파일하며, 장비/디스크 경계만 대체한다.
// Handler를 시작하거나 장비 객체를 생성하지 않는다.
internal static class VerifyGeneratedMapSaveGuard
{
    internal static readonly List<string> Events = new List<string>();
    private static int _checks;

    [STAThread]
    private static int Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            RunCase("success input", host => { }, true, false, false);
            RunCase("success output", host => { }, true, false, true);
            RunCase("boundary overflow input draft", host => GeneratedWaferMap.TestOutOfBounds = 21, true, false, false);
            RunCase("boundary overflow output draft", host => GeneratedWaferMap.TestOutOfBounds = 21, true, false, true);
            RunCase("boundary draft equipment guard", host => { GeneratedWaferMap.TestOutOfBounds = 21; host.Controller.AllowSave = false; }, false, false, false);
            RunCase("boundary draft UI guard", host => { GeneratedWaferMap.TestOutOfBounds = 21; host.UiAllowed = false; }, false, false, false);
            RunCase("boundary draft storage failed", host => { GeneratedWaferMap.TestOutOfBounds = 21; RecipeMapBuildService.FailStorage = true; }, false, true, false);
            RunCase("active mismatch", host => host.ActiveRecipeName = "OTHER", false, false, false);
            RunCase("controller mismatch", host => host.Controller.ActiveRecipeName = "OTHER", false, false, false);
            RunCase("UI operation", host => host.UiAllowed = false, false, false, false);
            RunCase("equipment/material guard", host => host.Controller.AllowSave = false, false, false, false);
            RunCase("material state changed", host => QMC.CDT320.Materials.MaterialStateService.IsCurrent = false, false, false, false);
            RunCase("recipe file changed", host => RecipeApplyFileSnapshot.Current = false, false, false, false);
            RunCase("generation validation", host => RecipeMapBuildService.InvalidGeneration = true, false, false, false);
            RunCase("storage failed", host => RecipeMapBuildService.FailStorage = true, false, true, false);
            RunCase("project save failed", host => RecipeStore.FailSave = true, false, true, false);
            RunCase("project reload failed", host => RecipeStore.FailLoad = true, false, true, false);
            Console.WriteLine("PASS generated-map save coordinator: " + _checks + " assertions; equipment and storage boundaries stubbed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void RunCase(string name, Action<Form1> configure, bool success, bool blocked, bool output)
    {
        Events.Clear();
        GeneratedWaferMap.TestOutOfBounds = 0;
        RecipeMapBuildService.InvalidGeneration = RecipeMapBuildService.FailStorage = false;
        RecipeMapBuildService.Received = null;
        RecipeStore.FailSave = RecipeStore.FailLoad = false;
        RecipeStore.Saved = null;
        RecipeStore.Existing = new RecipeProject { FileName = "TEST", OtherRoleDraft = "saved value" };
        RecipeApplyFileSnapshot.Current = true;
        QMC.CDT320.Materials.MaterialStateService.IsCurrent = true;
        using (var host = new Form1())
        using (var editor = new WaferMapCreateDialog { Opacity = 0, ShowInTaskbar = false })
        {
            configure(host);
            bool actual = false;
            string reason = null;
            Exception failure = null;
            var preview = new GeneratedWaferMap();
            editor.Shown += (sender, args) =>
            {
                try
                {
                    actual = host.TrySaveGeneratedWaferMap(new RecipeProject { FileName = "TEST", OtherRoleDraft = "unsaved other role" },
                        preview, output, editor, out reason);
                }
                catch (Exception ex) { failure = ex; }
                finally { editor.Close(); }
            };
            editor.ShowDialog();
            if (failure != null) throw failure;
            Check(actual == success, name + ": result " + reason);
            Check(!string.IsNullOrEmpty(host.Controller.BlockReason) == blocked, name + ": failure block");
            Check(!host.Controller.LeaseHeld && !host.UiProtected, name + ": protections released");
            Check(host.Controller.Notified == success, name + ": readiness notification after success only");
            if (success)
            {
                Check(RecipeStore.Saved != null && RecipeStore.Saved.PendingRole == (output ? "OUTPUT" : "INPUT"), name + ": selected role");
                Check(Events.IndexOf("lease") < Events.IndexOf("maps"), name + ": protection before map write");
                Check(Events.IndexOf("project") < Events.IndexOf("notify"), name + ": project before notification");
                Check(Events.IndexOf("notify") < Events.IndexOf("release"), name + ": notification inside lease");
                Check(ReferenceEquals(host.CurrentRecipe, RecipeStore.Saved), name + ": current project refreshed");
                Check(RecipeStore.Saved.OtherRoleDraft == "saved value", name + ": other role draft not persisted");
                Check(ReferenceEquals(RecipeMapBuildService.Received, preview), name + ": complete preview reaches draft storage unchanged");
            }
            else if (!blocked)
            {
                Check(!Events.Contains("maps") && !Events.Contains("project"), name + ": rejected before writes");
            }
            Check(!actual || string.IsNullOrEmpty(reason), name + ": success reason");
        }
    }

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception("FAIL " + message);
    }

    internal sealed class Scope : IDisposable
    {
        private Action _end;
        internal Scope(Action end) { _end = end; }
        public void Dispose() { Action end = _end; _end = null; if (end != null) end(); }
    }
}

namespace QMC.CDT320.DieMaps
{
    public sealed class GeneratedWaferMap
    {
        public static int TestOutOfBounds;
        public WaferMapGenerationSettings Settings { get; } = new WaferMapGenerationSettings();
        public int OutOfBoundsCount => TestOutOfBounds;
        public int Count => 684;
        public int UsedColumns => 26;
        public int UsedRows => 34;
        public int RotationDegrees => 90;
    }
    public sealed class WaferMapGenerationSettings
    {
        public int GenerationVersion => 2;
    }
}

namespace QMC.CDT320.Materials
{
    public sealed class MaterialSnapshot { }
    public static class MaterialStateService
    {
        public static bool IsCurrent;
        public static void CaptureRecipeResetState(out MaterialSnapshot state, out long version)
        { state = new MaterialSnapshot(); version = 1; }
        public static bool IsRecipeResetStateCurrent(MaterialSnapshot state, long version, out string reason)
        { reason = IsCurrent ? "" : "state changed"; return IsCurrent; }
    }
}

namespace QMC.CDT320.Recipes
{
    public sealed class RecipeProject
    {
        public string FileName;
        public string PendingRole;
        public string OtherRoleDraft;
    }
    public sealed class RecipeMapBuildResult
    {
        public bool Success;
        public bool PersistenceStarted;
        public string Message;
    }
    public static class RecipeMapBuildService
    {
        public static bool InvalidGeneration, FailStorage;
        public static GeneratedWaferMap Received;
        public static RecipeMapBuildResult CreateGeneratedBaseAndBuildRole(RecipeProject project,
            GeneratedWaferMap generated, bool output, Func<RecipeProject, bool> save)
        {
            Received = generated;
            if (InvalidGeneration) return new RecipeMapBuildResult { Message = "invalid settings" };
            VerifyGeneratedMapSaveGuard.Events.Add("maps");
            project.PendingRole = output ? "OUTPUT" : "INPUT";
            bool saved = !FailStorage && save(project);
            return new RecipeMapBuildResult { Success = saved, PersistenceStarted = true, Message = saved ? "" : "storage failed" };
        }
    }
    public static class RecipeStore
    {
        public static bool FailSave, FailLoad;
        public static RecipeProject Saved;
        public static RecipeProject Existing;
        public static bool Save(RecipeProject project)
        {
            VerifyGeneratedMapSaveGuard.Events.Add("project");
            if (FailSave) return false;
            Saved = project;
            return true;
        }
        public static RecipeProject Load(string name) { return Saved != null ? (FailLoad ? null : Saved) : Existing; }
    }
    internal sealed class RecipeApplyFileSnapshot
    {
        internal static bool Current;
        internal static RecipeApplyFileSnapshot Capture(string name) { return new RecipeApplyFileSnapshot(); }
        internal bool IsCurrent(out string reason) { reason = Current ? "" : "files changed"; return Current; }
    }
}

namespace QMC.CDT_320.Ui.Dialogs { public sealed class WaferMapCreateDialog : Form { } }
namespace QMC.CDT_320.Ui.Security { public static class UserSession { public static string Name => "TEST"; } }
namespace QMC.Common { public static class Log { public static void Write(params string[] args) { } } }

namespace QMC.CDT_320
{
    public sealed class GuardController
    {
        public string ActiveRecipeName = "TEST";
        public string BlockReason;
        public bool AllowSave = true, LeaseHeld, Notified;
        public bool TryBeginRecipeApplyOperation(string name, bool startup, out bool restore, out IDisposable lease, out string reason)
        {
            restore = false; lease = null; reason = AllowSave ? "" : "equipment guard rejected";
            if (!AllowSave) return false;
            LeaseHeld = true;
            VerifyGeneratedMapSaveGuard.Events.Add("lease");
            lease = new VerifyGeneratedMapSaveGuard.Scope(() => { LeaseHeld = false; VerifyGeneratedMapSaveGuard.Events.Add("release"); });
            return true;
        }
        public void NotifyRecipeConfigurationApplied()
        { Notified = true; VerifyGeneratedMapSaveGuard.Events.Add("notify"); }
        public void BlockRecipeStartAfterFailedApply(string reason) { BlockReason = reason; }
    }
    public partial class Form1 : Form
    {
        public object Machine = new object();
        public GuardController Controller = new GuardController();
        public string ActiveRecipeName = "TEST";
        public bool UiAllowed = true, UiProtected;
        private RecipeProject _currentRecipe;
        public RecipeProject CurrentRecipe => _currentRecipe;
        private static string NormalizeRecipeName(string value) { return (value ?? "").Trim(); }
        private static string GetActiveLotIdForRecipeReset() { return "LOT"; }
        private bool TryValidateRecipeApplyUiState(out string reason, Form mapEditor = null)
        {
            reason = UiAllowed && mapEditor != null && mapEditor.Modal ? "" : "UI state rejected";
            return string.IsNullOrEmpty(reason);
        }
        private IDisposable BeginRecipeApplyUiProtection()
        {
            UiProtected = true;
            return new VerifyGeneratedMapSaveGuard.Scope(() => UiProtected = false);
        }
    }
}
