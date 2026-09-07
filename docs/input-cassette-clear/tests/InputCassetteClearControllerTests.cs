// INPUT CST CLEAR / Recipe Picker interlock regression tests. Run with the adjacent PowerShell runner.
// The runner injects the current production methods at the marker below.
// Equipment admission, I/O, and Material are offline stubs: no application or hardware DLL is loaded.
using System;
using System.Collections.Generic;
using QMC.Common.IO;
using QMC.CDT320.Materials;

namespace QMC.Common
{
    public static class Log
    {
        public static void Write(string a, string b, string c, string d) { }
    }
}

namespace QMC.Common.Alarms
{
    public static class AlarmManager { public static bool HasActive; }
}

namespace QMC.Common.IO
{
    public class BaseDigitalInput
    {
        public bool IsOn;
        public bool ReadOk = true;
        public bool Throw;
    }

    public class BaseDigitalOutput
    {
        public bool IsOn;
        public bool ReadOk = true;
        public bool Throw;
    }

    public static class AjinIoScanService
    {
        public static bool TryReadHardwareOutput(BaseDigitalOutput output, out int errorCode)
        {
            return QMC.CDT320.MachineController.TryReadOutputForTest(output, out errorCode);
        }
    }
}

namespace QMC.CDT320.Ajin
{
    public static class AjinFactory { public static bool IsRealBoardReady = true; }
}

namespace QMC.CDT320.Materials
{
    public static class MaterialStateService
    {
        public static bool Allow = true;
        public static bool Throw;
        public static bool PickerDataEmpty = true;
        public static bool PickerThrow;
        public static bool OutputDataPresent;
        public static int PickerDataChecks;

        public static bool CanCompleteInputCassetteExchange(out string reason)
        {
            reason = "logical material blocked";
            if (Throw) throw new Exception("material read");
            return Allow;
        }

        public static bool TryValidatePickerProductDataEmpty(out string reason)
        {
            PickerDataChecks++;
            reason = "picker product data remains";
            if (PickerThrow) throw new Exception("picker material read");
            return PickerDataEmpty;
        }
    }
}

namespace QMC.CDT320
{
    public enum EquipmentStatus { Stopped, Alarm }

    public class Unit
    {
        public BaseDigitalInput WaferFeederRingCheckSensor = new BaseDigitalInput();
        public BaseDigitalInput WaferStage8RingCheckSensor = new BaseDigitalInput();
        public BaseDigitalInput WaferStage12RingCheckSensor = new BaseDigitalInput();
        public BaseDigitalInput[] FlowChecks =
        {
            new BaseDigitalInput(), new BaseDigitalInput(), new BaseDigitalInput(), new BaseDigitalInput()
        };
        public BaseDigitalOutput[] Vacuums =
        {
            new BaseDigitalOutput(), new BaseDigitalOutput(), new BaseDigitalOutput(), new BaseDigitalOutput()
        };
    }

    public class Machine
    {
        public Unit InputFeederUnit = new Unit();
        public Unit InputStageUnit = new Unit();
        public Unit PickerFrontUnit = new Unit();
        public Unit PickerRearUnit = new Unit();
    }

    public partial class MachineController
    {
        private object _recipeOperationLock = new object();
        private EquipmentStatus _status;
        private Machine _machine = new Machine();
        private bool _recipeApplyOperationActive;
        private string _recipeApplyFailureReason = string.Empty;
        private string _inputCassetteClearFailureReason = string.Empty;
        private bool VirtualMode;
        private bool GateDenied;
        private int Gates;
        private int Releases;
        private int Reads;
        private int OutputReads;

        private bool TryRegisterRecipeApplyOperation(out string reason, bool requireStoppedAxes = false)
        {
            reason = "busy";
            if (GateDenied) return false;
            if (!requireStoppedAxes) throw new Exception("must check stopped axes");
            Gates++;
            _recipeApplyOperationActive = true;
            return true;
        }

        private void EndRecipeApplyOperation()
        {
            _recipeApplyOperationActive = false;
            Releases++;
        }

        private bool IsRecipeSwitchHardwareInputRequired() { return !VirtualMode; }

        private static MachineController Current;

        private static bool TryReadRecipePresenceSensor(
            BaseDigitalInput input, string name, out bool detected, out string reason)
        {
            Current.Reads++;
            reason = name + " read failed";
            detected = false;
            if (input == null) return false;
            if (input.Throw) throw new Exception(name + " read exception");
            if (!input.ReadOk) return false;
            detected = input.IsOn;
            return true;
        }

        internal static bool TryReadOutputForTest(BaseDigitalOutput output, out int errorCode)
        {
            Current.OutputReads++;
            errorCode = -1;
            if (output == null) return false;
            if (output.Throw) throw new Exception("output read exception");
            if (!output.ReadOk) return false;
            errorCode = 0;
            return true;
        }

        private sealed class RecipeApplyOperationLease : IDisposable
        {
            private MachineController _owner;

            public RecipeApplyOperationLease(MachineController value) { _owner = value; }

            public void Dispose()
            {
                if (_owner == null) return;
                _owner.EndRecipeApplyOperation();
                _owner = null;
            }
        }

        // __INPUT_CASSETTE_CLEAR_METHODS__

        private static int Passed;

        private static MachineController New()
        {
            QMC.Common.Alarms.AlarmManager.HasActive = false;
            Ajin.AjinFactory.IsRealBoardReady = true;
            MaterialStateService.Allow = true;
            MaterialStateService.Throw = false;
            MaterialStateService.PickerDataEmpty = true;
            MaterialStateService.PickerThrow = false;
            MaterialStateService.OutputDataPresent = false;
            MaterialStateService.PickerDataChecks = 0;
            Current = new MachineController();
            return Current;
        }

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            Passed++;
        }

        private BaseDigitalInput[] RingInputs()
        {
            return new[]
            {
                _machine.InputFeederUnit.WaferFeederRingCheckSensor,
                _machine.InputStageUnit.WaferStage8RingCheckSensor,
                _machine.InputStageUnit.WaferStage12RingCheckSensor
            };
        }

        private BaseDigitalOutput[] VacuumOutputs()
        {
            var values = new List<BaseDigitalOutput>();
            values.AddRange(_machine.PickerFrontUnit.Vacuums);
            values.AddRange(_machine.PickerRearUnit.Vacuums);
            return values.ToArray();
        }

        private static bool Throws(Action action)
        {
            try { action(); return false; }
            catch (InvalidOperationException) { return true; }
        }

        public static int Main()
        {
            try
            {
                Run();
                Console.WriteLine("PASS controller assertions=" + Passed);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static void Run()
        {
            IDisposable scope;
            string reason;

            var c = New();
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason), "empty real admit");
            Check(c.Reads == 3 && c.OutputReads == 8 && MaterialStateService.PickerDataChecks == 1 &&
                c.Gates == 1 && c.Releases == 0 && c._recipeApplyOperationActive,
                "real ring/output/material checks run under held gate");
            scope.Dispose();
            Check(c.Releases == 1 && !c._recipeApplyOperationActive, "success scope releases");

            for (int i = 0; i < 3; i++)
            {
                c = New();
                c.RingInputs()[i].IsOn = true;
                Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && scope == null && reason.Contains("ON"),
                    "ring ON blocked " + i);
                Check(c.Releases == 1 && !c._recipeApplyOperationActive, "ring failure releases " + i);
            }

            for (int i = 0; i < 8; i++)
            {
                c = New();
                c.VacuumOutputs()[i].IsOn = true;
                Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && scope == null &&
                    reason.Contains("VACUUM") && reason.Contains("ON"), "vacuum ON blocked " + i);
                Check(c.Releases == 1 && !c._recipeApplyOperationActive, "vacuum failure releases " + i);
            }

            c = New();
            foreach (BaseDigitalInput flow in c._machine.PickerFrontUnit.FlowChecks) flow.IsOn = true;
            foreach (BaseDigitalInput flow in c._machine.PickerRearUnit.FlowChecks) flow.IsOn = true;
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason), "Flow ON is not presence evidence");
            Check(c.Reads == 3 && c.OutputReads == 8, "Flow inputs are not read by clear gate");
            scope.Dispose();

            c = New();
            MaterialStateService.OutputDataPresent = true;
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason),
                "output-side die data does not block when picker data is empty");
            scope.Dispose();

            c = New();
            MaterialStateService.PickerDataEmpty = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && reason.Contains("Picker 제품 데이터"),
                "picker product data blocks clear");
            Check(c.Releases == 1 && c.OutputReads == 0, "picker data failure releases before output reads");

            c = New();
            c.VirtualMode = true;
            Ajin.AjinFactory.IsRealBoardReady = false;
            MaterialStateService.PickerDataEmpty = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Reads == 0 && c.OutputReads == 0,
                "virtual mode retains picker data validation");

            c = New();
            c.VirtualMode = true;
            Ajin.AjinFactory.IsRealBoardReady = false;
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Reads == 0 && c.OutputReads == 0,
                "virtual mode skips hardware reads");
            scope.Dispose();

            c = New();
            Ajin.AjinFactory.IsRealBoardReady = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Reads == 0 &&
                c.OutputReads == 0 && c.Releases == 1, "board unavailable blocks before reads");

            c = New();
            c._machine = null;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1,
                "missing machine blocks");

            c = New();
            c._machine.InputFeederUnit = null;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1,
                "missing feeder blocks");

            c = New();
            c._machine.PickerRearUnit.Vacuums = new BaseDigitalOutput[0];
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1,
                "missing rear vacuum outputs block");

            c = New();
            c.RingInputs()[2].ReadOk = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1,
                "ring read failure blocks and releases");

            c = New();
            c.VacuumOutputs()[2].ReadOk = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1 &&
                reason.Contains("error=-1"), "output read failure blocks and releases");

            c = New();
            c.VacuumOutputs()[5].Throw = true;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 1 && scope == null,
                "output read exception blocks and releases");

            c = New();
            MaterialStateService.Allow = false;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Reads == 0 &&
                c.OutputReads == 0 && c.Releases == 1, "input logical validation remains");

            c = New();
            MaterialStateService.Throw = true;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && scope == null && c.Releases == 1,
                "logical exception releases");

            c = New();
            MaterialStateService.PickerThrow = true;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && scope == null && c.Releases == 1,
                "picker data exception releases");

            c = New();
            c.VacuumOutputs()[0].IsOn = true;
            Check(!c.TryValidatePickerVacuumOutputsAndMaterial("Recipe 전환", true, out reason) &&
                reason.Contains("Recipe 전환") && reason.Contains("ON"), "recipe uses shared vacuum gate");

            c = New();
            MaterialStateService.PickerDataEmpty = false;
            Check(!c.TryValidatePickerVacuumOutputsAndMaterial("Recipe 전환", false, out reason) &&
                reason.Contains("Picker 제품 데이터"), "recipe uses shared picker data gate");

            c = New();
            c._status = EquipmentStatus.Alarm;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Gates == 0,
                "alarm blocks before gate");

            c = New();
            QMC.Common.Alarms.AlarmManager.HasActive = true;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Gates == 0,
                "active alarm blocks before gate");

            c = New();
            c.GateDenied = true;
            Check(!c.TryBeginInputCassetteClearOperation(out scope, out reason) && c.Releases == 0,
                "existing operation denies");

            c = New();
            Check(Throws(() => c.ReportInputCassetteClearCompletion(false, "save")),
                "failure report requires scope");
            Check(Throws(() => c.ReportInputCassetteClearCompletion(true, "")),
                "success report requires scope");

            c = New();
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason), "latch initial scope");
            c.ReportInputCassetteClearCompletion(false, "save failed");
            string own = c._recipeApplyFailureReason;
            Check(own.Contains("INPUT CST CLEAR") && own.Contains("save failed") &&
                own == c._inputCassetteClearFailureReason, "own failure blocks START");
            c.ReportInputCassetteClearCompletion(false, "sync failed");
            Check(c._recipeApplyFailureReason.Contains("sync failed") &&
                !c._recipeApplyFailureReason.Contains("save failed"), "repeat failure updates own");
            scope.Dispose();
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason), "own latch allows clear retry");
            c.ReportInputCassetteClearCompletion(true, "");
            Check(c._recipeApplyFailureReason == "" && c._inputCassetteClearFailureReason == "",
                "retry success clears own only");
            scope.Dispose();

            c = New();
            c._recipeApplyFailureReason = "unrelated recipe failure";
            Check(c.TryBeginInputCassetteClearOperation(out scope, out reason), "unrelated block retained admission");
            c.ReportInputCassetteClearCompletion(false, "save failed");
            Check(c._recipeApplyFailureReason == "unrelated recipe failure" &&
                c._inputCassetteClearFailureReason == "", "failure preserves unrelated existing");
            c.ReportInputCassetteClearCompletion(true, "");
            Check(c._recipeApplyFailureReason == "unrelated recipe failure", "success preserves unowned failure");
            scope.Dispose();

            c = New();
            c.TryBeginInputCassetteClearOperation(out scope, out reason);
            c.ReportInputCassetteClearCompletion(false, "save failed");
            c._recipeApplyFailureReason = "new recipe failure";
            c.ReportInputCassetteClearCompletion(false, "sync failed");
            Check(c._recipeApplyFailureReason == "new recipe failure", "repeat failure preserves later unrelated");
            c.ReportInputCassetteClearCompletion(true, "");
            Check(c._recipeApplyFailureReason == "new recipe failure" && c._inputCassetteClearFailureReason == "",
                "success preserves later unrelated");
            scope.Dispose();
        }
    }
}
