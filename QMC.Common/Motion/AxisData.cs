using QMC.Common;

namespace QMC.Common.Motion
{
    /// <summary>
    /// 모션 축의 프로파일 형식.
    /// </summary>
    public enum AxisProfileMode
    {
        /// <summary>사다리꼴 가감속 프로파일.</summary>
        Trapezoid = 0,

        /// <summary>S-Curve 가감속 프로파일.</summary>
        SCurve = 1
    }

    /// <summary>
    /// 축 위치/속도 단위 변환 헬퍼. 내부 저장값은 축의 현재 <see cref="AxisSetup.Unit"/> 기준 값이다.
    /// </summary>
    public static class AxisUnitConverter
    {
        public const string Millimeter = "mm";
        public const string Micrometer = "um";
        public const string Degree = "deg";

        public static readonly string[] SupportedUnits = { Millimeter, Micrometer, Degree };

        public static bool IsSupported(string unit)
        {
            try
            {
                return IsLength(unit) || IsDegree(unit);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static bool IsLength(string unit)
        {
            try
            {
                string normalized = Normalize(unit);
                return normalized == Millimeter || normalized == Micrometer;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static bool IsDegree(string unit)
        {
            try
            {
                return Normalize(unit) == Degree;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static string Normalize(string unit)
        {
            try
            {
                string text = (unit ?? string.Empty).Trim();
                if (string.Equals(text, Micrometer, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "µm", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "μm", System.StringComparison.OrdinalIgnoreCase))
                    return Micrometer;
                if (string.Equals(text, Degree, System.StringComparison.OrdinalIgnoreCase))
                    return Degree;
                return Millimeter;
            }
            catch
            {
                return Millimeter;
            }
            finally
            {
            }
        }

        public static double ConvertValue(double value, string fromUnit, string toUnit)
        {
            try
            {
                string from = Normalize(fromUnit);
                string to = Normalize(toUnit);
                if (from == to) return value;
                if (!IsLength(from) || !IsLength(to)) return value;
                if (from == Millimeter && to == Micrometer) return value * 1000.0;
                if (from == Micrometer && to == Millimeter) return value / 1000.0;
                return value;
            }
            catch
            {
                return value;
            }
            finally
            {
            }
        }

        public static string NativeUnitFor(string displayUnit)
        {
            try
            {
                string normalized = Normalize(displayUnit);
                return IsDegree(normalized) ? Degree : Millimeter;
            }
            catch
            {
                return Millimeter;
            }
            finally
            {
            }
        }

        public static string DisplayUnitFor(BaseAxis axis)
        {
            try
            {
                if (axis == null || axis.Setup == null)
                    return Millimeter;
                return Normalize(axis.Setup.Unit);
            }
            catch
            {
                return Millimeter;
            }
            finally
            {
            }
        }

        public static double ToDisplay(double nativeValue, string displayUnit)
        {
            try
            {
                string unit = Normalize(displayUnit);
                return ConvertValue(nativeValue, NativeUnitFor(unit), unit);
            }
            catch
            {
                return nativeValue;
            }
            finally
            {
            }
        }

        public static double ToDisplay(double nativeValue, BaseAxis axis)
        {
            try
            {
                return ToDisplay(nativeValue, DisplayUnitFor(axis));
            }
            catch
            {
                return nativeValue;
            }
            finally
            {
            }
        }

        public static double FromDisplay(double displayValue, string displayUnit)
        {
            try
            {
                string unit = Normalize(displayUnit);
                return ConvertValue(displayValue, unit, NativeUnitFor(unit));
            }
            catch
            {
                return displayValue;
            }
            finally
            {
            }
        }

        public static double FromDisplay(double displayValue, BaseAxis axis)
        {
            try
            {
                return FromDisplay(displayValue, DisplayUnitFor(axis));
            }
            catch
            {
                return displayValue;
            }
            finally
            {
            }
        }

        public static string Format(double value, string unit)
        {
            try
            {
                string normalized = Normalize(unit);
                if (normalized == Micrometer)
                    return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        public static string FormatDisplay(double nativeValue, BaseAxis axis, string format, bool includeUnit)
        {
            try
            {
                string unit = DisplayUnitFor(axis);
                double displayValue = ToDisplay(nativeValue, unit);
                string text = displayValue.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
                return includeUnit ? text + " " + unit : text;
            }
            catch
            {
                return nativeValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }
    }

    /// <summary>
    /// 축의 기구/배선 설정값.<br/>
    /// 전원 OFF 후에도 유지되어야 하는 축 번호, 단위, 리밋, 홈 방향 같은 값을 둔다.
    /// <list type="bullet">
    ///   <item><description>장비 구성이나 배선에 가까운 값은 Setup 에 둔다.</description></item>
    ///   <item><description>운전 속도처럼 공정마다 바뀔 수 있는 값은 <see cref="AxisConfig"/> 에 둔다.</description></item>
    /// </list>
    /// </summary>
    public class AxisSetup : ISetupData
    {
        /// <summary>축이 속한 유닛 이름.</summary>
        public string UnitName { get; set; } = string.Empty;

        /// <summary>화면 표시용 축 이름. 비어 있으면 BaseAxis.Name 을 사용한다.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>AXL 보드 번호. 현재 AXM 호출은 전역 축 번호를 사용하므로 매핑 정보로 보관한다.</summary>
        public int BoardNo { get; set; } = 0;

        /// <summary>AXL 라이브러리의 축 번호 (0-based).</summary>
        public int AxisNo { get; set; } = 0;

        /// <summary>논리 단위 1당 펄스 수.</summary>
        public double PulsesPerUnit { get; set; } = 1000.0;

        /// <summary>보드 설정용 축 스케일.</summary>
        public int AxisScale { get; set; } = 1000;

        /// <summary>축 위치 단위. 예: mm, um, deg.</summary>
        public string Unit { get; set; } = AxisUnitConverter.Millimeter;

        /// <summary>축 사용 여부. false 이면 매니저에서 생성하지 않을 수 있다.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>플러스 방향 소프트웨어 리밋 위치 [Unit].</summary>
        public double SoftLimitPlus { get; set; } = 200.0;

        /// <summary>마이너스 방향 소프트웨어 리밋 위치 [Unit].</summary>
        public double SoftLimitMinus { get; set; } = -5.0;

        /// <summary>소프트웨어 리밋 사용 여부.</summary>
        public bool SoftLimitEnabled { get; set; } = true;

        /// <summary>원점 완료 후 적용할 원점 오프셋 [Unit].</summary>
        public double HomeOffset { get; set; } = 0.0;

        /// <summary>원점 복귀 방향.</summary>
        public HomeDirection HomeDirection { get; set; } = HomeDirection.Ccw;

        /// <summary>원점 검출 신호.</summary>
        public HomeSignal HomeSignal { get; set; } = HomeSignal.HomeSensor;

        /// <summary>원점 복귀 제한 시간 [ms].</summary>
        public int HomeTimeoutMs { get; set; } = 60000;

        /// <summary>일반 이동 제한 시간 [ms].</summary>
        public int MoveTimeoutMs { get; set; } = 60000;

        /// <summary>펄스 출력 방식.</summary>
        public PulseOutput PulseOutput { get; set; } = PulseOutput.TwoPulse_High_CCW_CW;

        /// <summary>엔코더 입력 방식.</summary>
        public EncoderInput EncoderInput { get; set; } = EncoderInput.Reverse_SQR4;
        /// <summary>위치 피드백 입력 소스.</summary>
        public InputSource InputSource { get; set; } = InputSource.Encoder;

        /// <summary>서보 ON 출력 레벨.</summary>
        public ActiveLevel ServoOnLevel { get; set; } = ActiveLevel.High;

        /// <summary>알람 입력 레벨.</summary>
        public ActiveLevel AlarmLevel { get; set; } = ActiveLevel.Low;

        /// <summary>알람 리셋 출력 레벨.</summary>
        public ActiveLevel AlarmResetLevel { get; set; } = ActiveLevel.High;

        /// <summary>비상 정지 입력 레벨.</summary>
        public ActiveLevel EmergencyLevel { get; set; } = ActiveLevel.High;

        /// <summary>정지 모드.</summary>
        public StopMode StopMode { get; set; } = StopMode.Emergency;

        /// <summary>인포지션 신호 사용/레벨 설정.</summary>
        public InPosition InPosition { get; set; } = InPosition.High;

        /// <summary>플러스 리밋 입력 레벨.</summary>
        public ActiveLevel PositiveLimitLevel { get; set; } = ActiveLevel.Low;

        /// <summary>마이너스 리밋 입력 레벨.</summary>
        public ActiveLevel NegativeLimitLevel { get; set; } = ActiveLevel.Low;

        /// <summary>모션 프로파일 형식.</summary>
        public AxisProfileMode ProfileMode { get; set; } = AxisProfileMode.SCurve;

        /// <summary>가속 Jerk 비율 [%].</summary>
        public int AccJerkPercent { get; set; } = 50;

        /// <summary>감속 Jerk 비율 [%].</summary>
        public int DecJerkPercent { get; set; } = 50;

        /// <summary>축의 기구 Stroke [Unit].</summary>
        public double Stroke { get; set; } = 0.0;

        /// <summary>브레이크 장착 여부.</summary>
        public bool Brake { get; set; } = false;

       
    }

    /// <summary>
    /// 축의 고정 사양/보드 신호 설정값.<br/>
    /// 모터, 드라이버, 보드 설정처럼 장비 모델에 묶이는 값을 둔다.
    /// </summary>
    public class AxisConfig : IConfigData
    {
        /// <summary>
        /// true 이면 실제 보드 호출 없이 시뮬레이션 엔진으로 동작한다.<br/>
        /// false 여도 보드가 열려 있지 않거나 축 번호가 유효하지 않으면 자동으로 true 로 폴백한다.
        /// </summary>
        public bool IsSimulationMode { get; set; } = true;
        /// <summary>일반 이동 기본 속도 [Unit/s].</summary>
        public double DefaultVelocity { get; set; } = 100.0;

        /// <summary>축의 최대 허용 속도 [Unit/s].</summary>
        public double MaxVelocity { get; set; } = 0.0;

        /// <summary>일반 이동 가속도 [Unit/s^2].</summary>
        public double Acceleration { get; set; } = 1000.0;

        /// <summary>일반 이동 감속도 [Unit/s^2].</summary>
        public double Deceleration { get; set; } = 1000.0;

        /// <summary>원점 복귀 1차 속도 [Unit/s].</summary>
        public double HomeFirstVelocity { get; set; } = 50.0;

        /// <summary>원점 복귀 2차 속도 [Unit/s].</summary>
        public double HomeSecondVelocity { get; set; } = 20.0;

        /// <summary>원점 복귀 3차 속도 [Unit/s].</summary>
        public double HomeThirdVelocity { get; set; } = 5.0;

        /// <summary>원점 복귀 마지막 접근 속도 [Unit/s].</summary>
        public double HomeLastVelocity { get; set; } = 1.0;
        public double HomeIndexSearchVelocity { get; set; } = 5;

        /// <summary>원점 복귀 대표 속도 [Unit/s]. 기존 코드 호환용.</summary>
        public double HomeVelocity { get; set; } = 200.0;

        /// <summary>원점 복귀 1차 가속도 [Unit/s^2].</summary>
        public double HomeFirstAcceleration { get; set; } = 500.0;

        /// <summary>원점 복귀 1차 감속도 [Unit/s^2].</summary>
        public double HomeFirstDeceleration { get; set; } = 500.0;

        /// <summary>원점 복귀 2차 가속도 [Unit/s^2].</summary>
        public double HomeSecondAcceleration { get; set; } = 200.0;

        /// <summary>원점 복귀 2차 감속도 [Unit/s^2].</summary>
        public double HomeSecondDeceleration { get; set; } = 200.0;

        /// <summary>Jog 빠른 속도 [Unit/s].</summary>
        public double JogCoarseVelocity { get; set; } = 10.0;

        /// <summary>Jog 미세 속도 [Unit/s].</summary>
        public double JogFineVelocity { get; set; } = 5.0;

        /// <summary>Jog 가속도 [Unit/s^2].</summary>
        public double JogAcceleration { get; set; } = 100.0;

        /// <summary>Jog 감속도 [Unit/s^2].</summary>
        public double JogDeceleration { get; set; } = 100.0;
        /// <summary>인포지션 허용 오차 [Unit].</summary>
        public double InPositionTolerance { get; set; } = 0.01;
    }

    /// <summary>
    /// 축의 공정별 운전 파라미터.<br/>
    /// 속도, 가속도, 감속도처럼 레시피나 공정 조건에 따라 바뀔 수 있는 값을 둔다.
    /// </summary>
    public class AxisRecipe : IRecipeData
    {
        
    }
}
