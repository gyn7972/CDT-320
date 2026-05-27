using System.Runtime.InteropServices;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AJINEXTEK AXL.dll P/Invoke 래퍼 (32/64bit 공용).
    /// 실보드 미연결/미설치 환경에서는 호출 시 런타임 예외가 날 수 있으므로,
    /// <see cref="AjinSystem.Open"/> 에서 try/catch 로 감싸서 열기.
    /// </summary>
    internal static class Axl
    {
        private const string Dll = "AXL.dll";

        // ─── Library lifecycle ──────────────────────────
        [DllImport(Dll)] public static extern uint AxlOpen(int irqNo);
        [DllImport(Dll)] public static extern uint AxlClose();
        [DllImport(Dll)] public static extern uint AxlIsOpened();

        // ─── Motion info ────────────────────────────────
        [DllImport(Dll)] public static extern uint AxmInfoGetAxisCount(ref int count);
        [DllImport(Dll)] public static extern uint AxmInfoIsInvalidAxisNo(int axisNo);

        // ─── Servo / Alarm ──────────────────────────────
        [DllImport(Dll)] public static extern uint AxmSignalServoOn(int axisNo, int useSign);
        [DllImport(Dll)] public static extern uint AxmSignalIsServoOn(int axisNo, ref int status);
        [DllImport(Dll)] public static extern uint AxmSignalServoAlarmReset(int axisNo, int reset);
        [DllImport(Dll)] public static extern uint AxmStatusReadAlarm(int axisNo, ref int alarm);
        [DllImport(Dll)] public static extern uint AxmStatusReadAlarmCode(int axisNo, ref uint alarmCode);

        // ─── Motion control ─────────────────────────────
        [DllImport(Dll)] public static extern uint AxmMoveStartPos(int axisNo, double pos, double vel, double accel, double decel);
        [DllImport(Dll)] public static extern uint AxmMoveVel(int axisNo, double vel, double accel, double decel);
        [DllImport(Dll)] public static extern uint AxmMoveStop(int axisNo, double decel);
        [DllImport(Dll)] public static extern uint AxmMoveEStop(int axisNo);
        [DllImport(Dll)] public static extern uint AxmMoveSStop(int axisNo);

        // ─── Home search ────────────────────────────────
        [DllImport(Dll)] public static extern uint AxmHomeSetStart(int axisNo);
        [DllImport(Dll)] public static extern uint AxmHomeGetResult(int axisNo, ref int result);
        [DllImport(Dll)] public static extern uint AxmHomeGetRate(int axisNo, ref uint homeMainStepNumber, ref uint homeStepNumber);

        // ─── Status read ────────────────────────────────
        [DllImport(Dll)] public static extern uint AxmStatusGetCmdPos(int axisNo, ref double pos);
        [DllImport(Dll)] public static extern uint AxmStatusGetActPos(int axisNo, ref double pos);
        [DllImport(Dll)] public static extern uint AxmStatusSetActPos(int axisNo, double pos);
        [DllImport(Dll)] public static extern uint AxmStatusSetCmdPos(int axisNo, double pos);
        [DllImport(Dll)] public static extern uint AxmStatusReadInMotion(int axisNo, ref int motion);
        [DllImport(Dll)] public static extern uint AxmStatusReadInPos(int axisNo, ref int inpos);
        [DllImport(Dll)] public static extern uint AxmStatusReadVel(int axisNo, ref double vel);

        // ─── Sensors / Limits ───────────────────────────
        [DllImport(Dll)] public static extern uint AxmSignalReadOriginLevel(int axisNo, ref int level);
        [DllImport(Dll)] public static extern uint AxmSignalReadLimit      (int axisNo, ref int posLimit, ref int negLimit);

        // ─── Parameters (load/save) ─────────────────────
        [DllImport(Dll, CharSet = CharSet.Ansi)] public static extern uint AxmMotLoadParaAll(string path);
        [DllImport(Dll, CharSet = CharSet.Ansi)] public static extern uint AxmMotSaveParaAll(string path);

        // ─── DIO ────────────────────────────────────────
        [DllImport(Dll)] public static extern uint AxdInfoGetModuleCount(ref int count);
        [DllImport(Dll)] public static extern uint AxdInfoGetInputCount (int moduleNo, ref int count);
        [DllImport(Dll)] public static extern uint AxdInfoGetOutputCount(int moduleNo, ref int count);
        [DllImport(Dll)] public static extern uint AxdiReadInportBit    (int moduleNo, int offset, ref int value);
        [DllImport(Dll)] public static extern uint AxdoWriteOutportBit  (int moduleNo, int offset, int value);
        [DllImport(Dll)] public static extern uint AxdoReadOutportBit   (int moduleNo, int offset, ref int value);
    }
}
