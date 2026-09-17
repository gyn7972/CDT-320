using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320;
using QMC.CDT_320.Ui.Dialogs;

// Production Form1.WaferMapProcessing.cs is compiled unchanged. Equipment, disk and UI guard
// boundaries are replaced to inject failures. This executable cannot connect to equipment.
internal static class VerifyProcessSaveGuard
{
    internal static readonly List<string> Events = new List<string>();
    private static int checks;
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Run("save and active apply", h => { }, true, false);
            Run("recipe remote ON", h => WaferMapProcessSettingsDialog.Requested = true, true, false);
            Run("recipe remote OFF clears lot bins", h => QMC.CDT320.AppSettingsStore.Current.UseLotNetworkWaferMap = true, true, false);
            Run("prepared input rejects mode change", h => { WaferMapProcessSettingsDialog.Requested = true; QMC.CDT320.Materials.MaterialStateService.PreparedInput = true; }, false, false);
            Run("stored mode mismatch", h => RecipeStore.CorruptMode = true, false, true);
            Run("inactive recipe", h => h.ActiveRecipeName = "OTHER", false, false);
            Run("controller recipe mismatch", h => h.Controller.ActiveRecipeName = "OTHER", false, false);
            Run("UI operation in progress", h => h.UiAllowed = false, false, false);
            Run("running or material guard", h => h.Controller.AllowSave = false, false, false);
            Run("restore must precede edit", h => h.Controller.Restore = true, false, false);
            Run("invalid transform", h => RecipeMapProcessSettingsService.Invalid = true, false, false);
            Run("recipe read failure", h => RecipeStore.FailInitialLoad = true, false, false);
            Run("material changed during preparation", h => h.Controller.OnLease = () => QMC.CDT320.Materials.MaterialStateService.IsCurrent = false, false, false);
            Run("UI changed during preparation", h => h.Controller.OnLease = () => h.UiAllowed = false, false, false);
            Run("recipe file changed before save", h => h.Controller.OnLease = () => RecipeApplyFileSnapshot.Current = false, false, false);
            Run("active recipe changed before save", h => h.Controller.OnLease = () => h.ActiveRecipeName = "OTHER", false, false);
            Run("LOT changed before save", h => h.Controller.OnLease = () => Form1.Lot = "NEW", false, false);
            Run("source mode changed before save", h => h.Controller.OnLease = () => QMC.CDT320.AppSettingsStore.Current.UseLotNetworkWaferMap = true, false, false);
            Run("save returns failure", h => RecipeStore.FailSave = true, false, true);
            Run("save throws after possible write", h => RecipeStore.ThrowSave = true, false, true);
            Run("reload failure after write", h => RecipeStore.FailReload = true, false, true);
            Run("stored settings mismatch", h => RecipeStore.CorruptSavedSettings = true, false, true);
            Run("file changed after write", h => RecipeApplyFileSnapshot.ChangeAfterSave = true, false, true);
            Run("existing START failure remains", h => h.Controller.BlockReason = "previous failure", true, true);
            Console.WriteLine("PASS: " + checks + " process-map save guard assertions; equipment and disk boundaries stubbed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Run(string name, Action<Form1> configure, bool success, bool blocked)
    {
        Events.Clear();
        RecipeStore.Reset();
        RecipeMapProcessSettingsService.Invalid = false;
        RecipeApplyFileSnapshot.Current = true; RecipeApplyFileSnapshot.ChangeAfterSave = false;
        QMC.CDT320.Materials.MaterialStateService.IsCurrent = true;
        QMC.CDT320.AppSettingsStore.Current = new QMC.CDT320.AppSettings();
        Form1.Lot = "LOT";
        WaferMapProcessSettingsDialog.Requested = false;
        QMC.CDT320.Materials.MaterialStateService.PreparedInput = false;
        using (var host = new Form1())
        using (var editor = new WaferMapProcessSettingsDialog { Opacity = 0, ShowInTaskbar = false })
        {
            configure(host);
            bool expectedClearBins = QMC.CDT320.AppSettingsStore.Current.UseLotNetworkWaferMap && !editor.InputUsesRemote;
            bool result = false; string reason = null; RecipeProject saved = null;
            Exception failure = null;
            editor.Shown += (sender, args) =>
            {
                try
                {
                    result = host.TrySaveAndApplyWaferMapProcessing(
                        new RecipeProject { FileName = "TEST", OtherValue = "UNSAVED UI DRAFT" },
                        new WaferMapProcessSettings { Key = "INPUT 180 CENTER" },
                        new WaferMapProcessSettings { Key = "OUTPUT 0 BR" }, editor, out saved, out reason);
                }
                catch (Exception ex) { failure = ex; }
                finally { editor.Close(); }
            };
            editor.ShowDialog();
            if (failure != null) throw failure;
            Check(result == success, name + " result: " + reason);
            Check(!string.IsNullOrEmpty(host.Controller.BlockReason) == blocked, name + " START failure block");
            Check(!host.Controller.LeaseHeld && !host.UiProtected, name + " protections released");
            Check(host.Controller.Notified == success, name + " notification only after verification");
            if (success)
            {
                Check(ReferenceEquals(host.CurrentRecipe, saved) && ReferenceEquals(RecipeStore.Saved, saved), name + " active object immediately updated");
                Check(saved.InputMapProcessing.Key == "INPUT 180 CENTER" && saved.OutputMapProcessing.Key == "OUTPUT 0 BR", name + " independent settings applied");
                Check(saved.InputUseRemoteWaferMap == editor.InputUsesRemote, name + " requested mode verified");
                Check(saved.OtherValue == "DISK VALUE", name + " unrelated unsaved draft preserved");
                Check(Events.IndexOf("lease") < Events.IndexOf("save"), name + " lease before write");
                Check(Events.IndexOf("save") < Events.IndexOf("notify") && Events.IndexOf("notify") < Events.IndexOf("release"), name + " notify within lease after save");
                Check(string.IsNullOrEmpty(reason), name + " successful reason empty");
                Check(Events.Contains("clear-bins") == expectedClearBins, name + " temporary lot bin selection reset only for remote OFF");
                if (expectedClearBins)
                    Check(Events.IndexOf("save") < Events.IndexOf("clear-bins") && Events.IndexOf("clear-bins") < Events.IndexOf("release"), name + " bin reset inside save protection");
                Check(host.GetProjectConfigurationStatus(saved).Contains("설정 일치"), name + " applied status uses content equality");
                var changed = new RecipeProject { FileName = saved.FileName, OtherValue = "different saved field", InputUseRemoteWaferMap = saved.InputUseRemoteWaferMap,
                    InputMapProcessing = saved.InputMapProcessing, OutputMapProcessing = saved.OutputMapProcessing };
                Check(host.GetProjectConfigurationStatus(changed).Contains("설정 다름"), name + " identical name with different settings is not shown applied");
                changed.FileName = "OTHER";
                Check(host.GetProjectConfigurationStatus(changed).Contains("미적용"), name + " selected recipe is distinct from active");
            }
            else
            {
                Check(saved == null && host.CurrentRecipe == null, name + " failed setting not activated");
                Check(!string.IsNullOrEmpty(reason), name + " reason reported");
                if (!blocked) Check(!Events.Contains("save"), name + " rejected before write");
            }
        }
    }
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    internal sealed class Scope : IDisposable
    {
        private Action end;
        internal Scope(Action value) { end = value; }
        public void Dispose() { Action value = end; end = null; if (value != null) value(); }
    }
}

namespace QMC.CDT320.DieMaps
{
    public sealed class WaferMapProcessSettings { public string Key; }
    public static class WaferMapProcessService
    { public static string GetSettingsKey(WaferMapProcessSettings value) { return value == null ? "Legacy" : value.Key; } }
}
namespace QMC.CDT320
{
    public sealed class AppSettings { public bool UseLotNetworkWaferMap; }
    public static class AppSettingsStore { public static AppSettings Current; }
}
namespace QMC.CDT320.Materials
{
    public enum MaterialLocationKind { InputStage, InputFeeder, InputCassette }
    public sealed class MaterialLocation { public MaterialLocationKind Kind; }
    public sealed class WaferMaterial { public MaterialLocation CurrentLocation = new MaterialLocation(); public object InputPreparedMap; public bool HasInputStageDieMappingResult; }
    public sealed class MaterialSnapshot { public List<WaferMaterial> Wafers = new List<WaferMaterial>(); }
    public static class MaterialStateService
    {
        public static bool IsCurrent, PreparedInput;
        public static T ReadState<T>(Func<MaterialSnapshot, T> read)
        { var state = new MaterialSnapshot(); if (PreparedInput) state.Wafers.Add(new WaferMaterial { InputPreparedMap = new object() }); return read(state); }
        public static void ResetPickupBinSelectionToAll(string reason) { VerifyProcessSaveGuard.Events.Add("clear-bins"); }
        public static void CaptureRecipeResetState(out MaterialSnapshot state, out long version) { state = new MaterialSnapshot(); version = 1; }
        public static bool IsRecipeResetStateCurrent(MaterialSnapshot state, long version, out string reason)
        { reason = IsCurrent ? "" : "material changed"; return IsCurrent; }
    }
}
namespace QMC.CDT320.Recipes
{
    public sealed class RecipeProject
    { public string FileName, OtherValue; public bool? InputUseRemoteWaferMap; public WaferMapProcessSettings InputMapProcessing, OutputMapProcessing; }
    public static class RecipeMapProcessSettingsService
    {
        public static bool Invalid;
        public static RecipeProject CreateUpdatedProject(RecipeProject current, WaferMapProcessSettings input, WaferMapProcessSettings output, bool network)
        {
            if (Invalid) throw new System.IO.InvalidDataException("invalid transform");
            return new RecipeProject { FileName = current.FileName, OtherValue = current.OtherValue, InputUseRemoteWaferMap = network, InputMapProcessing = input, OutputMapProcessing = output };
        }
    }
    public static class RecipeStore
    {
        public static bool CorruptMode;
        public static bool FailInitialLoad, FailReload, FailSave, ThrowSave, CorruptSavedSettings;
        public static RecipeProject Saved;
        public static void Reset() { CorruptMode = false; FailInitialLoad = FailReload = FailSave = ThrowSave = CorruptSavedSettings = false; Saved = null; }
        public static RecipeProject Load(string name)
        { return Saved == null ? (FailInitialLoad ? null : new RecipeProject { FileName = "TEST", OtherValue = "DISK VALUE" }) : (FailReload ? null : Saved); }
        public static bool Save(RecipeProject value)
        {
            VerifyProcessSaveGuard.Events.Add("save");
            if (ThrowSave) throw new System.IO.IOException("write error");
            if (FailSave) return false;
            Saved = value;
            if (CorruptMode) Saved.InputUseRemoteWaferMap = !value.InputUseRemoteWaferMap;
            if (CorruptSavedSettings) Saved.InputMapProcessing = new WaferMapProcessSettings { Key = "CORRUPTED" };
            return true;
        }
    }
    internal sealed class RecipeApplyFileSnapshot
    {
        internal static bool Current, ChangeAfterSave;
        internal static RecipeApplyFileSnapshot Capture(string name) { return new RecipeApplyFileSnapshot(); }
        internal RecipeApplyFileSnapshot CaptureAfterProjectSave() { if (ChangeAfterSave) Current = false; return this; }
        internal bool IsCurrent(out string reason) { reason = Current ? "" : "file changed"; return Current; }
    }
}
namespace QMC.CDT_320.Ui.Dialogs { public sealed class WaferMapProcessSettingsDialog : Form { public static bool Requested; public bool InputUsesRemote => Requested; } }
namespace QMC.CDT_320.Ui.Security { public static class UserSession { public static string Name => "TEST"; } }
namespace QMC.Common { public static class Log { public static void Write(params string[] values) { } } }
namespace QMC.CDT_320
{
    public sealed class GuardController
    {
        public string ActiveRecipeName = "TEST", BlockReason;
        public bool AllowSave = true, Restore, LeaseHeld, Notified;
        public Action OnLease;
        public bool TryBeginRecipeApplyOperation(string name, bool startup, out bool restore, out IDisposable lease, out string reason)
        {
            restore = Restore; lease = null; reason = AllowSave ? "" : "equipment or material rejected";
            if (!AllowSave) return false;
            LeaseHeld = true; VerifyProcessSaveGuard.Events.Add("lease");
            lease = new VerifyProcessSaveGuard.Scope(() => { LeaseHeld = false; VerifyProcessSaveGuard.Events.Add("release"); });
            if (OnLease != null) OnLease();
            return true;
        }
        public void NotifyRecipeConfigurationApplied() { Notified = true; VerifyProcessSaveGuard.Events.Add("notify"); }
        public void BlockRecipeStartAfterFailedApply(string reason) { BlockReason = reason; }
    }
    public partial class Form1 : Form
    {
        public object Machine = new object();
        public GuardController Controller = new GuardController();
        public string ActiveRecipeName = "TEST";
        public static string Lot;
        public bool UiAllowed = true, UiProtected;
        private RecipeProject _currentRecipe;
        public RecipeProject CurrentRecipe => _currentRecipe;
        private static string NormalizeRecipeName(string value) { return (value ?? "").Trim(); }
        private static string GetActiveLotIdForRecipeReset() { return Lot; }
        private bool TryValidateRecipeApplyUiState(out string reason, Form mapEditor = null)
        { reason = UiAllowed && mapEditor != null && mapEditor.Modal ? "" : "UI state rejected"; return string.IsNullOrEmpty(reason); }
        private IDisposable BeginRecipeApplyUiProtection()
        { UiProtected = true; return new VerifyProcessSaveGuard.Scope(() => UiProtected = false); }
    }
}

namespace QMC.CDT320.Recipes
{
    public static class RecipeInputMapSource
    { public static bool UsesRemote(RecipeProject project, QMC.CDT320.AppSettings settings) { return project.InputUseRemoteWaferMap ?? settings.UseLotNetworkWaferMap; } }
}
