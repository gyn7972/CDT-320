// INPUT CST CLEAR controller regression tests. Run with the adjacent PowerShell runner.
// The runner injects the current production methods at the marker below.
// Equipment admission, sensors, and Material are offline stubs: no application or I/O DLL is loaded.
// These tests validate admission/sensor decisions, lease cleanup, and START failure ownership.
using System;
using System.Collections.Generic;
using QMC.Common.IO;
using QMC.CDT320.Materials;
namespace QMC.Common { public static class Log { public static void Write(string a,string b,string c,string d) {} } }
namespace QMC.Common.Alarms { public static class AlarmManager { public static bool HasActive; } }
namespace QMC.Common.IO { public class BaseDigitalInput { public bool IsOn; public bool ReadOk=true; public bool Throw; } }
namespace QMC.CDT320.Ajin { public static class AjinFactory { public static bool IsRealBoardReady=true; } }
namespace QMC.CDT320.Materials { public static class MaterialStateService { public static bool Allow=true; public static bool Throw; public static bool CanCompleteInputCassetteExchange(out string reason) { reason="logical material blocked"; if(Throw) throw new Exception("material read"); return Allow; } } }
namespace QMC.CDT320 {
public enum EquipmentStatus { Stopped, Alarm }
public class Unit { public BaseDigitalInput WaferFeederRingCheckSensor=new BaseDigitalInput(); public BaseDigitalInput WaferStage8RingCheckSensor=new BaseDigitalInput(); public BaseDigitalInput WaferStage12RingCheckSensor=new BaseDigitalInput(); public BaseDigitalInput[] FlowChecks = {new BaseDigitalInput(),new BaseDigitalInput(),new BaseDigitalInput(),new BaseDigitalInput()}; }
public class Machine { public Unit InputFeederUnit=new Unit(); public Unit InputStageUnit=new Unit(); public Unit PickerFrontUnit=new Unit(); public Unit PickerRearUnit=new Unit(); }
public partial class MachineController {
private object _recipeOperationLock=new object();
private EquipmentStatus _status;
private Machine _machine=new Machine();
private bool _recipeApplyOperationActive;
private string _recipeApplyFailureReason=string.Empty;
private string _inputCassetteClearFailureReason=string.Empty;
private bool VirtualMode;
private bool GateDenied;
private int Gates,Releases,Reads;
private bool TryRegisterRecipeApplyOperation(out string reason,bool requireStoppedAxes=false) { reason="busy"; if(GateDenied) return false; if(!requireStoppedAxes) throw new Exception("must check stopped axes"); Gates++; _recipeApplyOperationActive=true; return true; }
private void EndRecipeApplyOperation() { _recipeApplyOperationActive=false; Releases++; }
private bool IsRecipeSwitchHardwareInputRequired() { return !VirtualMode; }
private static MachineController Current;
private static bool TryReadRecipePresenceSensor(BaseDigitalInput input,string name,out bool detected,out string reason) { Current.Reads++; reason=name+" read failed"; detected=false; if(input==null) return false; if(input.Throw) throw new Exception(name+" read exception"); if(!input.ReadOk) return false; detected=input.IsOn; return true; }
private class RecipeApplyOperationLease:IDisposable { private MachineController owner; public RecipeApplyOperationLease(MachineController value){owner=value;} public void Dispose(){ if(owner!=null){owner.EndRecipeApplyOperation();owner=null;} } }
// __INPUT_CASSETTE_CLEAR_METHODS__
private static int Passed;
private static MachineController New() { QMC.Common.Alarms.AlarmManager.HasActive=false; Ajin.AjinFactory.IsRealBoardReady=true; MaterialStateService.Allow=true; MaterialStateService.Throw=false; Current=new MachineController(); return Current; }
private static void Check(bool condition,string name) { if(!condition) throw new Exception("FAIL: "+name); Passed++; }
private BaseDigitalInput[] Inputs() { var values=new List<BaseDigitalInput>(); values.Add(_machine.InputFeederUnit.WaferFeederRingCheckSensor);values.Add(_machine.InputStageUnit.WaferStage8RingCheckSensor);values.Add(_machine.InputStageUnit.WaferStage12RingCheckSensor);values.AddRange(_machine.PickerFrontUnit.FlowChecks);values.AddRange(_machine.PickerRearUnit.FlowChecks);return values.ToArray(); }
private static bool Throws(Action action) { try { action(); return false; } catch(InvalidOperationException){return true;} }
public static int Main() { try { Run(); System.Console.WriteLine("PASS controller assertions="+Passed); return 0; } catch(Exception ex){System.Console.WriteLine(ex);return 1;} }
private static void Run() {
IDisposable scope; string reason;
var c=New(); Check(c.TryBeginInputCassetteClearOperation(out scope,out reason),"empty real admit"); Check(c.Reads==11 && c.Gates==1 && c.Releases==0 && c._recipeApplyOperationActive,"all real inputs read under held gate"); scope.Dispose(); Check(c.Releases==1&&!c._recipeApplyOperationActive,"success scope releases");
for(int i=0;i<11;i++){ c=New(); c.Inputs()[i].IsOn=true; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&scope==null&&reason.Contains("ON"),"sensor ON blocked "+i); Check(c.Releases==1&&!c._recipeApplyOperationActive,"sensor failure releases "+i); }
c=New(); Ajin.AjinFactory.IsRealBoardReady=false; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Reads==0&&c.Releases==1,"board unavailable blocks before reads");
c=New(); c._machine=null; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==1,"missing machine blocks");
c=New(); c._machine.InputFeederUnit=null; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==1,"missing feeder blocks");
c=New(); c._machine.PickerRearUnit.FlowChecks=new BaseDigitalInput[0]; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==1,"missing rear flow blocks");
c=New(); c.Inputs()[2].ReadOk=false; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==1,"read failure blocks and releases");
c=New(); c.Inputs()[5].Throw=true; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==1&&scope==null,"read exception blocks and releases");
c=New(); c.VirtualMode=true; Ajin.AjinFactory.IsRealBoardReady=false; Check(c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Reads==0,"virtual mode no hardware read"); scope.Dispose();
c=New(); c.VirtualMode=true; MaterialStateService.Allow=false; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Reads==0&&c.Releases==1,"virtual retains logical validation");
c=New(); MaterialStateService.Throw=true; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&scope==null&&c.Releases==1,"logical exception releases");
c=New(); c._status=EquipmentStatus.Alarm; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Gates==0,"alarm blocks before gate");
c=New(); QMC.Common.Alarms.AlarmManager.HasActive=true; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Gates==0,"active alarm blocks before gate");
c=New(); c.GateDenied=true; Check(!c.TryBeginInputCassetteClearOperation(out scope,out reason)&&c.Releases==0,"existing operation denies");
c=New(); Check(Throws(()=>c.ReportInputCassetteClearCompletion(false,"save")),"failure report requires scope"); Check(Throws(()=>c.ReportInputCassetteClearCompletion(true,"")),"success report requires scope");
c=New(); Check(c.TryBeginInputCassetteClearOperation(out scope,out reason),"latch initial scope"); c.ReportInputCassetteClearCompletion(false,"save failed"); string own=c._recipeApplyFailureReason; Check(own.Contains("INPUT CST CLEAR")&&own.Contains("save failed")&&own==c._inputCassetteClearFailureReason,"own failure blocks START"); c.ReportInputCassetteClearCompletion(false,"sync failed"); Check(c._recipeApplyFailureReason.Contains("sync failed")&&!c._recipeApplyFailureReason.Contains("save failed"),"repeat failure updates own"); scope.Dispose(); Check(c.TryBeginInputCassetteClearOperation(out scope,out reason),"own latch allows clear retry"); c.ReportInputCassetteClearCompletion(true,""); Check(c._recipeApplyFailureReason==""&&c._inputCassetteClearFailureReason=="","retry success clears own only"); scope.Dispose();
c=New(); c._recipeApplyFailureReason="unrelated recipe failure"; Check(c.TryBeginInputCassetteClearOperation(out scope,out reason),"unrelated block retained admission"); c.ReportInputCassetteClearCompletion(false,"save failed"); Check(c._recipeApplyFailureReason=="unrelated recipe failure"&&c._inputCassetteClearFailureReason=="","failure preserves unrelated existing"); c.ReportInputCassetteClearCompletion(true,""); Check(c._recipeApplyFailureReason=="unrelated recipe failure","success preserves unowned failure"); scope.Dispose();
c=New(); c.TryBeginInputCassetteClearOperation(out scope,out reason); c.ReportInputCassetteClearCompletion(false,"save failed"); c._recipeApplyFailureReason="new recipe failure"; c.ReportInputCassetteClearCompletion(false,"sync failed"); Check(c._recipeApplyFailureReason=="new recipe failure","repeat failure preserves later unrelated"); c.ReportInputCassetteClearCompletion(true,""); Check(c._recipeApplyFailureReason=="new recipe failure"&&c._inputCassetteClearFailureReason=="","success preserves later unrelated"); scope.Dispose();
}
}}
