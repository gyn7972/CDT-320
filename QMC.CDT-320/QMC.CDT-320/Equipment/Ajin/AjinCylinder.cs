using QMC.Common.IO;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// 실보드 Ajin DIO 를 내부 DO/DI 로 사용하는 실린더.
    /// 생성 시 OutFwd/OutBwd/InFwd/InBwd 각 4개의 (module,bit) 를 주입.
    /// </summary>
    public class AjinCylinder : BaseCylinder
    {
        private readonly (int mod, int bit) _outFwd, _outBwd, _inFwd, _inBwd;

        public AjinCylinder(string name,
            DioMap outFwd, DioMap outBwd,
            DioMap inFwd, DioMap inBwd,
            bool singleSolenoid = false)
            : base(name)
        {
            Setup.IsSingleSolenoid = singleSolenoid;
            Setup.UseFwdSensor = inFwd != null;
            Setup.UseBwdSensor = inBwd != null;
            Config.IsSimulationMode = false;
            ReplaceInternalIo(name, outFwd, outBwd, inFwd, inBwd);
        }

        public AjinCylinder(string name,
            (int mod, int bit) outFwd, (int mod, int bit) outBwd,
            (int mod, int bit) inFwd,  (int mod, int bit) inBwd,
            bool singleSolenoid = false)
            : this(name, Map(outFwd), Map(outBwd), Map(inFwd), Map(inBwd), singleSolenoid)
        {
        }

        private AjinCylinder(string name,
            (int mod, int bit) outFwd, (int mod, int bit) outBwd,
            (int mod, int bit) inFwd,  (int mod, int bit) inBwd,
            bool singleSolenoid, bool initialized)
            : base(name)
        {
            // base 생성자가 CreateDigitalOutput/CreateDigitalInput 호출 시점에는
            // 아래 필드가 아직 세팅 전이라 호출 시 상수 fallback이 필요.
            // → 대신 private 필드는 base 생성 이후 override 로 다시 설정하는 패턴으로 간다.
            // (간단화: 본 생성자는 base가 NullCtor 로 sim 구성요소를 만든 뒤 즉시 교체)
            _outFwd = outFwd; _outBwd = outBwd; _inFwd = inFwd; _inBwd = inBwd;
            Setup.IsSingleSolenoid = singleSolenoid;
            Config.IsSimulationMode = false;

            // base 가 만든 Sim DIO 대신 실 DIO 로 교체
            ReplaceInternalIo(name, Map(outFwd), Map(outBwd), Map(inFwd), Map(inBwd));
        }

        private void ReplaceInternalIo(string name,
            DioMap outFwd, DioMap outBwd,
            DioMap inFwd, DioMap inBwd)
        {
            // 옛 IO 인스턴스를 ScanService에서 먼저 등록 해제한다.
            AjinIoScanService scan = AjinIoScanService.Current;
            if (scan != null)
            {
                scan.UnregisterOutput(OutFwd as AjinDigitalOutput);
                scan.UnregisterOutput(OutBwd as AjinDigitalOutput);
                scan.UnregisterInput(InFwd as AjinDigitalInput);
                scan.UnregisterInput(InBwd as AjinDigitalInput);
            }

            // BaseCylinder 의 OutFwd/OutBwd/InFwd/InBwd 는 private set 이라
            // 리플렉션으로 교체. 한 번만 수행되므로 비용 무시.
            var t = typeof(BaseCylinder);
            var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            t.GetProperty("OutFwd", bf).SetValue(this, CreateOutput(name + "_OutFwd", outFwd));
            t.GetProperty("OutBwd", bf).SetValue(this, CreateOutput(name + "_OutBwd", outBwd));
            t.GetProperty("InFwd",  bf).SetValue(this, CreateInput(name + "_InFwd", inFwd));
            t.GetProperty("InBwd",  bf).SetValue(this, CreateInput(name + "_InBwd", inBwd));

            // 새 IO 인스턴스를 ScanService에 등록하여 폴링 대상으로 편입한다.
            if (scan != null)
            {
                scan.RegisterOutput(OutFwd as AjinDigitalOutput);
                scan.RegisterOutput(OutBwd as AjinDigitalOutput);
                scan.RegisterInput(InFwd as AjinDigitalInput);
                scan.RegisterInput(InBwd as AjinDigitalInput);
            }
        }

        public void Rebind(DioMap outFwd, DioMap outBwd, DioMap inFwd, DioMap inBwd)
        {
            Setup.UseFwdSensor = inFwd != null;
            Setup.UseBwdSensor = inBwd != null;
            ReplaceInternalIo(Name, outFwd, outBwd, inFwd, inBwd);
        }

        private static BaseDigitalOutput CreateOutput(string name, DioMap map)
        {
            if (map == null) return new SimDigitalOutput(name);
            return new AjinDigitalOutput(name, map.Module, map.Bit);
        }

        private static BaseDigitalInput CreateInput(string name, DioMap map)
        {
            if (map == null) return new SimDigitalInput(name);
            return new AjinDigitalInput(name, map.Module, map.Bit);
        }

        private static DioMap Map((int mod, int bit) value)
        {
            return new DioMap { Module = value.mod, Bit = value.bit };
        }

        // base 가 호출하는 DI/DO 팩토리 — 생성 중에는 sim 이 만들어지고,
        // 생성자 끝에서 ReplaceInternalIo 로 실 보드 DIO 로 교체됨.
        protected override BaseDigitalOutput CreateDigitalOutput(string name)
            => new SimDigitalOutput(name);

        protected override BaseDigitalInput CreateDigitalInput(string name)
            => new SimDigitalInput(name);
    }
}
