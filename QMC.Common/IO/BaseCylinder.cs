using System;
using System.Threading.Tasks;

namespace QMC.Common.IO
{
    public delegate bool CylinderMotionGuardHandler(
        BaseCylinder cylinder,
        bool moveFwd,
        out string reason);

    // ??????????????????????????????????????????????????????????????????????????
    //  실린더 전용 데이터 클래스
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// 실린더의 기구적 설정값.<br/>
    /// 편솔(Single Solenoid) / 양솔(Double Solenoid) 구분 정보를 담는다.
    /// </summary>
    public class CylinderSetup : ISetupData
    {
        /// <summary>
        /// 편솔(Single Solenoid) 여부.<br/>
        /// <list type="bullet">
        ///   <item><description>false (기본값) : 양솔 ? OutFwd / OutBwd 두 밸브를 각각 제어한다.</description></item>
        ///   <item><description>true          : 편솔 ? OutFwd 하나만 제어한다 (OutFwd On = 전진, Off = 후진).</description></item>
        /// </list>
        /// </summary>
        public bool IsSingleSolenoid { get; set; } = false;

        public bool UseFwdSensor { get; set; } = true;

        public bool UseBwdSensor { get; set; } = true;
    }

    /// <summary>
    /// 실린더의 고정 사양 파라미터.<br/>
    /// 시뮬레이션 모드 여부를 포함한다.
    /// </summary>
    public class CylinderConfig : IConfigData
    {
        /// <summary>
        /// true이면 시뮬레이션 모드로 동작한다 (기본값: true).<br/>
        /// false이면 실제 하드웨어 I/O를 사용한다.
        /// </summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>
    /// 실린더의 공정별 파라미터.<br/>
    /// 전진/후진 타임아웃 시간을 공정마다 다르게 설정할 수 있다.
    /// </summary>
    public class CylinderRecipe : IRecipeData
    {
        /// <summary>전진 완료 대기 타임아웃 [ms] (기본값: 3000ms).</summary>
        public int FwdTimeoutMs { get; set; } = 3000;

        /// <summary>후진 완료 대기 타임아웃 [ms] (기본값: 3000ms).</summary>
        public int BwdTimeoutMs { get; set; } = 3000;
    }

    // ??????????????????????????????????????????????????????????????????????????
    //  BaseCylinder
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// 공압 실린더의 추상 베이스 클래스.<br/>
    /// 전진(Forward) / 후진(Backward) 두 방향의 솔레노이드 밸브(DO)와
    /// 위치 확인 센서(DI)를 내장하여 실린더 동작 시퀀스를 캡슐화한다.
    /// <para>
    /// 내부 구성:<br/>
    /// <list type="table">
    ///   <listheader><term>명칭</term><description>역할</description></listheader>
    ///   <item><term>OutFwd</term><description>전진 솔레노이드 밸브 출력 (양솔: 전진 ON / 편솔: ON = 전진)</description></item>
    ///   <item><term>OutBwd</term><description>후진 솔레노이드 밸브 출력 (양솔 전용, 편솔 시 미사용)</description></item>
    ///   <item><term>InFwd</term><description>전진 완료 감지 근접/리드 센서 입력</description></item>
    ///   <item><term>InBwd</term><description>후진 완료 감지 근접/리드 센서 입력</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class BaseCylinder
        : BaseComponent<CylinderSetup, CylinderConfig, CylinderRecipe>
    {
        public static CylinderMotionGuardHandler MotionGuard { get; set; }

        // ──────────────────────────────────────────────────────────────────────
        //  시뮬레이션 모드 내부 상수
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>시뮬레이션 모드에서 밸브 출력 후 센서 강제 점등까지의 대기 시간 [ms].</summary>
        private const int SimulationDelayMs = 500;

        // ──────────────────────────────────────────────────────────────────────
        //  내부 컴포넌트 ? DO (밸브)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 전진 솔레노이드 밸브 출력.<br/>
        /// <list type="bullet">
        ///   <item><description>양솔 : ON → 실린더 전진, OFF → 중립</description></item>
        ///   <item><description>편솔 : ON → 전진, OFF → 스프링 복귀(후진)</description></item>
        /// </list>
        /// </summary>
        public BaseDigitalOutput OutFwd { get; private set; }

        /// <summary>
        /// 후진 솔레노이드 밸브 출력 (양솔 전용).<br/>
        /// 편솔(<see cref="CylinderSetup.IsSingleSolenoid"/> = true) 설정 시에는 사용하지 않는다.
        /// </summary>
        public BaseDigitalOutput OutBwd { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  내부 컴포넌트 ? DI (센서)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 전진 완료 감지 센서 입력.<br/>
        /// 실린더 로드가 전진 끝단에 도달하면 ON이 된다.
        /// </summary>
        public BaseDigitalInput InFwd { get; private set; }

        /// <summary>
        /// 후진 완료 감지 센서 입력.<br/>
        /// 실린더 로드가 후진(원점) 끝단에 도달하면 ON이 된다.
        /// </summary>
        public BaseDigitalInput InBwd { get; private set; }

        // ──────────────────────────────────────────────────────────────────────
        //  생성자
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="BaseCylinder"/>를 초기화하고 하위 DO/DI 컴포넌트를 생성한다.
        /// </summary>
        /// <param name="name">
        /// 실린더 이름 (예: "Cyl_ClampA").<br/>
        /// 하위 컴포넌트 이름은 이 값을 접두어로 자동 생성된다.
        /// (예: "Cyl_ClampA_OutFwd", "Cyl_ClampA_InBwd")
        /// </param>
        protected BaseCylinder(string name) : base(name)
        {
            OutFwd = CreateDigitalOutput($"{name}_OutFwd");
            OutBwd = CreateDigitalOutput($"{name}_OutBwd");
            InFwd  = CreateDigitalInput($"{name}_InFwd");
            InBwd  = CreateDigitalInput($"{name}_InBwd");
        }

        // ──────────────────────────────────────────────────────────────────────
        //  팩토리 메서드 ? 하위 클래스에서 구체 DI/DO 구현체를 주입
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정된 이름의 <see cref="BaseDigitalOutput"/> 구현체를 생성한다.<br/>
        /// 하위 클래스에서 override하여 실보드용 DO 구현체를 반환할 수 있다.
        /// </summary>
        /// <param name="name">DO 컴포넌트 이름</param>
        protected abstract BaseDigitalOutput CreateDigitalOutput(string name);

        /// <summary>
        /// 지정된 이름의 <see cref="BaseDigitalInput"/> 구현체를 생성한다.<br/>
        /// 하위 클래스에서 override하여 실보드용 DI 구현체를 반환할 수 있다.
        /// </summary>
        /// <param name="name">DI 컴포넌트 이름</param>
        protected abstract BaseDigitalInput CreateDigitalInput(string name);

        // ──────────────────────────────────────────────────────────────────────
        //  §1. 상태 확인 프로퍼티
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 실린더가 완전히 전진한 상태인지 여부.<br/>
        /// 전진 센서 ON + 후진 센서 OFF일 때 true.
        /// </summary>
        public bool IsFwd => InFwd.IsOn && !InBwd.IsOn;

        /// <summary>
        /// 실린더가 완전히 후진(원점)한 상태인지 여부.<br/>
        /// 후진 센서 ON + 전진 센서 OFF일 때 true.
        /// </summary>
        public bool IsBwd => InBwd.IsOn && !InFwd.IsOn;

        // ──────────────────────────────────────────────────────────────────────
        //  §2. 구동 메서드
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 실린더를 전진(Forward)시키고 완료 센서 도달을 비동기로 대기한다.<br/>
        /// <list type="bullet">
        ///   <item><description>양솔 : OutFwd ON, OutBwd OFF</description></item>
        ///   <item><description>편솔 : OutFwd ON (OutBwd 불사용)</description></item>
        ///   <item><description>시뮬레이션 모드 : <see cref="SimulationDelayMs"/>ms 후 InBwd OFF, InFwd ON 강제 주입</description></item>
        /// </list>
        /// </summary>
        /// <returns>전진 완료 시 <c>true</c>, 타임아웃 시 <c>false</c></returns>
        public virtual async Task<bool> MoveFwdAsync()
        {
            if (!VerifyMotionGuard(true))
                return false;

            // ── 밸브 출력 제어 ───────────────────────────────────────────────
            OutFwd.On();

            if (!Setup.IsSingleSolenoid)
                OutBwd.Off();

            // ── 시뮬레이션 모드: 지연 후 센서 강제 주입 ────────────────────
            if (Config.IsSimulationMode)
            {
                await Task.Delay(SimulationDelayMs).ContinueWith(_ => { });
                InBwd.SimulateInput(false);
                InFwd.SimulateInput(true);
            }

            // ── 전진 완료 센서 대기 ──────────────────────────────────────────
            bool result;
            if (Setup.UseFwdSensor)
                result = await InFwd.WaitUntilStateAsync(true, Recipe.FwdTimeoutMs);
            else if (Setup.UseBwdSensor)
                result = await InBwd.WaitUntilStateAsync(false, Recipe.FwdTimeoutMs);
            else
                result = true;

            if (!result)
                Console.WriteLine($"[ALARM] '{Name}' ? 전진(Fwd) 타임아웃 ({Recipe.FwdTimeoutMs}ms 초과)");

            return result;
        }

        /// <summary>
        /// 실린더를 후진(Backward/Home)시키고 완료 센서 도달을 비동기로 대기한다.<br/>
        /// <list type="bullet">
        ///   <item><description>양솔 : OutBwd ON, OutFwd OFF</description></item>
        ///   <item><description>편솔 : OutFwd OFF (스프링 복귀)</description></item>
        ///   <item><description>시뮬레이션 모드 : <see cref="SimulationDelayMs"/>ms 후 InFwd OFF, InBwd ON 강제 주입</description></item>
        /// </list>
        /// </summary>
        /// <returns>후진 완료 시 <c>true</c>, 타임아웃 시 <c>false</c></returns>
        public virtual async Task<bool> MoveBwdAsync()
        {
            if (!VerifyMotionGuard(false))
                return false;

            // ── 밸브 출력 제어 ───────────────────────────────────────────────
            if (Setup.IsSingleSolenoid)
            {
                // 편솔: OutFwd OFF → 스프링 복귀
                OutFwd.Off();
            }
            else
            {
                // 양솔: OutBwd ON, OutFwd OFF
                OutBwd.On();
                OutFwd.Off();
            }

            // ── 시뮬레이션 모드: 지연 후 센서 강제 주입 ────────────────────
            if (Config.IsSimulationMode)
            {
                await Task.Delay(SimulationDelayMs).ContinueWith(_ => { });
                InFwd.SimulateInput(false);
                InBwd.SimulateInput(true);
            }

            // ── 후진 완료 센서 대기 ──────────────────────────────────────────
            bool result;
            if (Setup.UseBwdSensor)
                result = await InBwd.WaitUntilStateAsync(true, Recipe.BwdTimeoutMs);
            else if (Setup.UseFwdSensor)
                result = await InFwd.WaitUntilStateAsync(false, Recipe.BwdTimeoutMs);
            else
                result = true;

            if (!result)
                Console.WriteLine($"[ALARM] '{Name}' ? 후진(Bwd) 타임아웃 ({Recipe.BwdTimeoutMs}ms 초과)");

            return result;
        }

        private bool VerifyMotionGuard(bool moveFwd)
        {
            CylinderMotionGuardHandler guard = MotionGuard;
            if (guard == null)
                return true;

            string reason;
            return guard(this, moveFwd, out reason);
        }
    }
}
