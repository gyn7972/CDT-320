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
            {  0, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WAFER LIFTER_Z
            {  1, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WAFER FEEDER_Y
            {  2, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WAFER STAGE_Y
            {  3, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WAFER STAGE_T
            {  4, new Params { DefaultVelocity=  50, Acceleration=   500, Deceleration=   500, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // WAFER EXPANDING_Z
            {  5, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // ALIGN VISION_X
            {  6, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NEEDLE_X
            {  7, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NEEDLE_Z
            {  8, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // EJECT PIN_Z
            {  9, new Params { DefaultVelocity=2000, Acceleration= 20000, Deceleration= 20000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_X
            { 10, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_Y
            { 11, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_T0
            { 12, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_Z0
            { 13, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_T1
            { 14, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_Z1
            { 15, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_T2
            { 16, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_Z2
            { 17, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_T3
            { 18, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT PICKER_Z3
            { 19, new Params { DefaultVelocity= 300, Acceleration=  3000, Deceleration=  3000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // FRONT SIDE VISION_Y0
            { 20, new Params { DefaultVelocity= 300, Acceleration=  3000, Deceleration=  3000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR SIDE VISION_Y0
            { 21, new Params { DefaultVelocity=2000, Acceleration= 20000, Deceleration= 20000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_X
            { 22, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_Y
            { 23, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_T0
            { 24, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_Z0
            { 25, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_T1
            { 26, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_Z1
            { 27, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_T2
            { 28, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_Z2
            { 29, new Params { DefaultVelocity=36000,Acceleration=360000, Deceleration=360000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_T3
            { 30, new Params { DefaultVelocity=1000, Acceleration= 10000, Deceleration= 10000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // REAR PICKER_Z3
            { 31, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NG BIN_Y
            { 32, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // NG BIN_Z
            { 33, new Params { DefaultVelocity= 500, Acceleration=  5000, Deceleration=  5000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // GOOD BIN_Y
            { 34, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // INSPECTION VISION_X
            { 35, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // BIN FEEDER_Y
            { 36, new Params { DefaultVelocity= 100, Acceleration=  1000, Deceleration=  1000, HomeVelocity= 200, JogCoarse= 50, JogFine= 5 } }, // BIN LIFTER_Z
        };

        /// <summary>축 번호로 파라미터 조회. 없으면 default Params 반환.</summary>
        public static Params Get(int axisNo)
        {
            if (Table.TryGetValue(axisNo, out var p)) return p;
            return new Params { DefaultVelocity = 1000, Acceleration = 10000, Deceleration = 10000,
                                HomeVelocity = 200, JogCoarse = 50, JogFine = 5 };
        }

        /// <summary>축 번호로 AxisRecipe 인스턴스 생성. BaseAxis 초기화에서 사용 가능.</summary>
        public static AxisRecipe BuildRecipe(int axisNo)
        {
            var p = Get(axisNo);
            return new AxisRecipe
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
