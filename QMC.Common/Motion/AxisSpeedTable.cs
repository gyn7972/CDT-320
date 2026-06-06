using System.Collections.Generic;

namespace QMC.Common.Motion
{
    /// <summary>
    /// 축별 속도/가속도 파라미터 테이블. 사용자가 AxisMotionParams.xlsx 로 정의한 값.
    /// BaseAxis 가 초기화 시 본 테이블에서 자기 축 번호로 lookup 하여 Recipe 에 반영 가능.
    /// </summary>
    public static class AxisSpeedTable
    {
        public struct Params
        {
            public double DefaultVelocity;
            public double Acceleration;
            public double Deceleration;
            public double HomeVelocity;
            public double JogCoarse;
            public double JogFine;
        }

        public static readonly IReadOnlyDictionary<int, Params> Table = new Dictionary<int, Params>
        {
            //  No                              DefVel   Accel    Decel    HomeVel  JogCoarse JogFine
            {  0, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // InputLifterZ
            {  1, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // InputFeederY
            {  2, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WaferStageY
            {  3, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WaferStageT
            {  4, new Params { DefaultVelocity=  50, Acceleration=   500, Deceleration=   500, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WaferExpandingZ
            {  5, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // InputVisionX
            {  6, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NeedleX
            {  7, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NeedleZ
            {  8, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // EjectPinZ
            {  9, new Params { DefaultVelocity=2000, Acceleration= 20000, Deceleration= 20000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerX
            { 10, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerY
            { 11, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerT0
            { 12, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerZ0
            { 13, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerT1
            { 14, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerZ1
            { 15, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerT2
            { 16, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerZ2
            { 17, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerT3
            { 18, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontPickerZ3
            { 19, new Params { DefaultVelocity= 300, Acceleration=  3000, Deceleration=  3000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FrontSideVisionY0
            { 20, new Params { DefaultVelocity= 300, Acceleration=  3000, Deceleration=  3000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearSideVisionY0
            { 21, new Params { DefaultVelocity=2000, Acceleration= 20000, Deceleration= 20000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerX
            { 22, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerY
            { 23, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerT0
            { 24, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerZ0
            { 25, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerT1
            { 26, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerZ1
            { 27, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerT2
            { 28, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerZ2
            { 29, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerT3
            { 30, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // RearPickerZ3
            { 31, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputGoodStageY
            { 32, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputGoodStageZ
            { 33, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputNGStageY
            { 34, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputVisionX
            { 35, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputFeederY
            { 36, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // OutputLifterZ
        };

        /// <summary>축 번호로 파라미터 조회. 없으면 default Params 반환.</summary>
        public static Params Get(int axisNo)
        {
            if (Table.TryGetValue(axisNo, out var p)) return p;
            return new Params { DefaultVelocity = 1000, Acceleration = 10000, Deceleration = 10000,
                                HomeVelocity = 200, JogCoarse = 50, JogFine = 5 };
        }

        /// <summary>축 번호로 AxisConfig 인스턴스 생성. BaseAxis 초기화에서 사용 가능.</summary>
        public static AxisConfig BuildConfig(int axisNo)
        {
            var p = Get(axisNo);
            return new AxisConfig
            {
                DefaultVelocity   = p.DefaultVelocity,
                Acceleration      = p.Acceleration,
                Deceleration      = p.Deceleration,
                HomeVelocity      = p.HomeVelocity,
                JogCoarseVelocity = p.JogCoarse,
                JogFineVelocity   = p.JogFine
            };
        }
    }
}
