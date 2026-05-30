using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.Common.Alarms;

namespace QMC.CDT320
{
    // ==========================================================================
    //  OutputStageUnit ?꾩슜 ?몃? ?곕룞 ?명꽣?섏씠??    // ==========================================================================

    /// <summary>
    /// ?ㅼ씠 遺꾨쪟 寃곌낵瑜??앸퀎?섎뒗 ?닿굅??
    /// </summary>
    public enum DieGrade
    {
        /// <summary>?묓뭹 ?ㅼ씠. GoodStage濡??댁넚?쒕떎.</summary>
        Good,

        /// <summary>遺덈웾 ?ㅼ씠. NgStage濡??댁넚?쒕떎.</summary>
        Ng
    }

    /// <summary>
    /// OutputStageUnit?쇰줈 ?꾨떖?섎뒗 ?ㅼ씠 1媛쒖쓽 ?섏떊 ?붿껌 ?뚮씪誘명꽣.<br/>
    /// TPU ?쇱빱 湲곌뎄 ?ㅽ봽?뗪낵 Bottom Vision 蹂댁젙媛믪쓣 ?⑹궛?섏뿬 理쒖쥌 StageY 醫뚰몴瑜?怨꾩궛?쒕떎.
    /// </summary>
    public class ReceiveDieRequest
    {
        /// <summary>???ㅼ씠瑜?諛곗텧???寃??ㅽ뀒?댁? ?깃툒.</summary>
        public DieGrade Grade { get; set; }

        /// <summary>
        /// TPU ?쇱빱 湲곌뎄 ?ㅽ봽??X [mm].<br/>
        /// ?쎌빱 ?몄쫹??湲곌퀎???몄떖?됱쑝濡? 罹섎━釉뚮젅?댁뀡?쇰줈 痢≪젙??怨좎젙媛?
        /// </summary>
        public double TpuOffsetX { get; set; }

        /// <summary>
        /// TPU ?쇱빱 湲곌뎄 ?ㅽ봽??Y [mm].<br/>
        /// StageY 理쒖쥌 ?대룞 ?꾩튂??媛?곕맂??
        /// </summary>
        public double TpuOffsetY { get; set; }

        /// <summary>
        /// Bottom Vision 蹂댁젙 ?ㅽ봽??X [mm].<br/>
        /// 鍮꾩쟾 珥ъ긽?쇰줈 怨꾩궛???ㅼ씠 以묒떖??X ?몄감.
        /// </summary>
        public double VisionOffsetX { get; set; }

        /// <summary>
        /// Bottom Vision 蹂댁젙 ?ㅽ봽??Y [mm].<br/>
        /// StageY 理쒖쥌 ?대룞 ?꾩튂??媛?곕맂??
        /// </summary>
        public double VisionOffsetY { get; set; }
    }

    // --------------------------------------------------------------------------

    /// <summary>
    /// OutputStageUnit怨??곕룞?섎뒗 TPU(Transfer Picker Unit) ?명꽣?섏씠??<br/>
    /// OutputStageUnit? ??怨꾩빟???듯빐 TPU??"Place 以鍮??꾨즺" 諛?    /// "?ㅼ쓬 ?ㅼ씠 ?섏떊 媛?? ?곹깭瑜??뚮┛??
    /// </summary>
    public interface ITpuUnit
    {
        /// <summary>
        /// OutputStage媛 TPU Place ?꾩튂濡??대룞 ?꾨즺?덉쓬???뚮┛??<br/>
        /// TPU?????좏샇瑜?諛쏆? ??PickerZ瑜??섍컯?쒖폒 ?ㅼ씠瑜?Place?쒕떎.
        /// </summary>
        void NotifyPlaceReady();

        /// <summary>
        /// TPU媛 Place瑜??꾨즺?섍퀬 ?쎌빱瑜??곸듅쨌?꾪눜???뚭퉴吏 鍮꾨룞湲??湲고븳??<br/>
        /// ??硫붿꽌?쒓? 諛섑솚????BinCamera 寃?щ? ?섑뻾?대룄 ?덉쟾?섎떎.
        /// </summary>
        /// <param name="timeoutMs">?湲???꾩븘??[ms]</param>
        /// <returns>TPU ?꾪눜 ?꾨즺 ??true, ??꾩븘????false.</returns>
        Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000);

        /// <summary>
        /// OutputStageUnit???ㅼ쓬 ?ㅼ씠瑜??섏떊??以鍮꾧? ?먯쓬??TPU???뚮┛??<br/>
        /// TPU?????좏샇瑜?諛쏆? ???ㅼ쓬 ?쇱빱??Place ?쒗?ㅻ? ?쒖옉?쒕떎.
        /// </summary>
        void NotifyReadyForNextDie();

        /// <summary>
        /// TPU??肄쒕젢 ?щ━??Collet Cleaning) ?숈옉???붿껌?섍퀬 ?꾨즺???뚭퉴吏 ?湲고븳??<br/>
        /// NgStage???붾? ?곸뿭?먯꽌 ?ㅽ뻾?섎ŉ, ?щ━???꾨즺 ??諛섑솚?쒕떎.
        /// </summary>
        /// <param name="timeoutMs">?щ━???꾨즺 ?湲???꾩븘??[ms]</param>
        /// <returns>?щ━???꾨즺 ??true, ??꾩븘????false.</returns>
        Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000);
    }

    /// <summary>
    /// OutputStageUnit怨??곕룞?섎뒗 OutputUnloaderUnit ?명꽣?섏씠??<br/>
    /// 鍮?Bin)??媛??李쇱쓣 ???⑥씠??援먯껜(Wafer Change)瑜??붿껌?쒕떎.
    /// </summary>
    public interface IOutputUnloaderUnit
    {
        /// <summary>
        /// 吏???깃툒??鍮?Bin)??媛??李쇱쓬??Unloader???뚮━怨?        /// ?⑥씠??援먯껜媛 ?꾨즺???뚭퉴吏 鍮꾨룞湲??湲?Suspend)?쒕떎.<br/>
        /// Unloader媛 援먯껜瑜??꾨즺?섎㈃ 諛섑솚?쒕떎.
        /// </summary>
        /// <param name="grade">援먯껜 ?붿껌 ????깃툒 (Good / NG)</param>
        /// <param name="timeoutMs">援먯껜 ?꾨즺 ?湲???꾩븘??[ms]. 0 = 臾댄븳 ?湲?</param>
        /// <returns>援먯껜 ?꾨즺 ??true, ??꾩븘????false.</returns>
        Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0);
    }

    // ==========================================================================
    //  OutputStageUnit ?꾩슜 ?곗씠???대옒??    // ==========================================================================

    /// <summary>
    /// StageModule??湲곌뎄???ㅼ젙媛?<br/>
    /// ?묒뾽 ?믪씠, 異⑸룎 ?뚰뵾 ?믪씠, ?붾?(?щ━?? ?꾩튂 ???섎뱶?⑥뼱 援먯껜 ?꾧퉴吏 ?좎??섎뒗 媛?
    /// </summary>
    public class StageModuleSetup : ISetupData
    {
        /// <summary>
        /// StageZ ?묒뾽 ?믪씠 [mm].<br/>
        /// TPU ?쎌빱媛 ?ㅼ씠瑜?Place?섎뒗 ?숈븞 ?ㅽ뀒?댁?媛 ?좎??댁빞 ?섎뒗 Z ?꾩튂.
        /// </summary>
        public double WorkPositionZ   { get; set; } = 10.0;

        /// <summary>
        /// StageZ 異⑸룎 ?뚰뵾 ?섍컯 ?꾩튂 [mm].<br/>
        /// 諛섎?履??ㅽ뀒?댁?媛 X 怨듭쑀 援ш컙???듦낵????諛섎뱶?????꾩튂 ?댄븯濡??섍컯?댁빞 ?쒕떎.
        /// </summary>
        public double AvoidPositionZ  { get; set; } = 0.0;

        /// <summary>
        /// StageY ?⑥씠??援먯껜(Unload) ?꾩튂 [mm].<br/>
        /// Unloader媛 鍮??⑥씠?쇰? 爰쇰궡???꾩튂.
        /// </summary>
        public double UnloadPositionY { get; set; } = -50.0;

        /// <summary>
        /// StageY ?湲??꾩튂 [mm].<br/>
        /// ?묒뾽???놁쓣 ???ㅽ뀒?댁?媛 癒몃Т?????꾩튂.
        /// </summary>
        public double HomePositionY   { get; set; } = 0.0;

        /// <summary>
        /// StageY 肄쒕젢 ?щ━?앹슜 ?붾? ?곸뿭 ?꾩튂 [mm].<br/>
        /// NG ?⑥씠?쇱쓽 ?ъ슜?섏? ?딅뒗 ?멸낸 醫뚰몴濡? TPU 肄쒕젢????뒗 ?⑸룄.
        /// </summary>
        public double CleaningPositionY { get; set; } = 80.0;

        /// <summary>
        /// StageZ ?꾩튂 鍮꾧탳 ???덉슜 ?ㅼ감 [mm].<br/>
        /// ActualPosition??AvoidPositionZ 짹 Tolerance 踰붿쐞 ?댁뿉 ?덉쑝硫??뚰뵾 ?꾨즺濡??먯젙.
        /// </summary>
        public double PositionTolerance { get; set; } = 0.05;
    }

    /// <summary>StageModule 怨좎젙 ?ъ뼇 ?뚮씪誘명꽣.</summary>
    public class StageModuleConfig : IConfigData
    {
        /// <summary>?쒕??덉씠??紐⑤뱶 ?щ?.</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>StageZ ?뚰뵾 ?꾨즺 ?뺤씤 ?대쭅 媛꾧꺽 [ms].</summary>
        public int AvoidCheckIntervalMs { get; set; } = 10;

        /// <summary>StageZ ?뚰뵾 ?꾨즺 理쒕? ?湲??쒓컙 [ms].</summary>
        public int AvoidTimeoutMs { get; set; } = 3000;
    }

    /// <summary>StageModule 怨듭젙蹂??묒뾽 ?뚮씪誘명꽣.</summary>
    public class StageModuleRecipe : IRecipeData
    {
        /// <summary>StageY ?대룞 ?띾룄 [mm/s].</summary>
        public double YVelocity { get; set; } = 100.0;

        /// <summary>StageZ ?대룞 ?띾룄 [mm/s].</summary>
        public double ZVelocity { get; set; } = 50.0;
    }

    // --------------------------------------------------------------------------

    /// <summary>OutputStageUnit 湲곌뎄???ㅼ젙媛?</summary>
    public class OutputStageSetup : ISetupData
    {
        /// <summary>
        /// BinCameraX 寃??吏꾩엯 ?꾩튂 [mm].<br/>
        /// ?ㅽ뀒?댁? ?묒뾽 X 醫뚰몴 ?꾩뿉???덉갑 ?곹깭瑜?寃?ы븯???꾩튂.
        /// </summary>
        public double BinCameraWorkPositionX { get; set; } = 150.0;

        /// <summary>
        /// BinCameraX ?湲??뚰뵾) ?꾩튂 [mm].<br/>
        /// ?ㅽ뀒?댁? ?대룞 諛?TPU 吏꾩엯 ??異⑸룎???쇳빐 ?꾪눜?섎뒗 ?꾩튂.
        /// </summary>
        public double BinCameraRetractPositionX { get; set; } = 0.0;

        /// <summary>
        /// StageY 湲곗? ?꾩튂 [mm].<br/>
        /// TPU媛 Place?????ㅽ뀒?댁???湲곕낯 Y ?꾩튂. Offset????媛믪뿉 媛?곕맂??
        /// </summary>
        public double StageBasePositionY { get; set; } = 50.0;
    }

    /// <summary>OutputStageUnit 怨좎젙 ?ъ뼇 ?뚮씪誘명꽣.</summary>
    public class OutputStageConfig : IConfigData
    {
        /// <summary>?쒕??덉씠??紐⑤뱶 ?щ?.</summary>
        public bool IsSimulationMode { get; set; } = true;

        /// <summary>TPU Place ?꾨즺 ?湲???꾩븘??[ms].</summary>
        public int TpuPlaceDoneTimeoutMs { get; set; } = 3000;

        /// <summary>?⑥씠??援먯껜 ?꾨즺 ?湲???꾩븘??[ms]. 0 = 臾댄븳 ?湲?</summary>
        public int WaferChangeTimeoutMs { get; set; } = 0;

        /// <summary>肄쒕젢 ?щ━???꾨즺 ?湲???꾩븘??[ms].</summary>
        public int ColletCleaningTimeoutMs { get; set; } = 10000;
    }

    /// <summary>OutputStageUnit 怨듭젙蹂??묒뾽 ?뚮씪誘명꽣.</summary>
    public class OutputStageRecipe : IRecipeData
    {
        /// <summary>BinCameraX ?대룞 ?띾룄 [mm/s].</summary>
        public double BinCameraVelocity { get; set; } = 200.0;
    }

    // ==========================================================================
    //  A. StageModule - 媛쒕퀎 ?ㅽ뀒?댁? ?쒖뼱 ?쒕툕 紐⑤뱢 (Good / NG 媛?1媛?
    // ==========================================================================

    /// <summary>
    /// 媛쒕퀎 異쒕젰 ?ㅽ뀒?댁?(Good ?먮뒗 NG) ?쒖뼱 ?쒕툕 紐⑤뱢.<br/>
    /// StageY(?꾪썑 ?대룞)? StageZ(?곹븯 ?대룞)瑜?蹂댁쑀?섎ŉ, Z異뺤쑝濡?    /// 異⑸룎 ?뚰뵾 諛??묒뾽 ?믪씠 議곗젅???대떦?쒕떎.
    /// <para>
    /// <b>異⑸룎 ?뚰뵾 ?먯튃:</b><br/>
    /// Good Stage? NG Stage???숈씪??X ?묒뾽 醫뚰몴瑜?怨듭쑀?쒕떎.
    /// ?곕씪???쒖そ ?ㅽ뀒?댁?媛 Y 諛⑺뼢?쇰줈 ?대룞?섍린 ?꾩뿉 諛섎뱶??諛섎?履??ㅽ뀒?댁???    /// StageZ媛 <see cref="StageModuleSetup.AvoidPositionZ"/> ?댄븯???덉뼱???쒕떎.
    /// </para>
    /// <para>
    /// 怨꾩링 ?꾩튂: <c>OutputStageUnit ??StageModule</c>
    /// </para>
    /// </summary>
    public class StageModule : BaseUnit<StageModuleSetup, StageModuleConfig, StageModuleRecipe>
    {
        // ----------------------------------------------------------------------
        //  A-1. ?섎뱶?⑥뼱 硫ㅻ쾭
        // ----------------------------------------------------------------------

        /// <summary>
        /// ?ㅽ뀒?댁? ?꾪썑 ?대룞 異?(Y異?.<br/>
        /// ?ㅼ씠 ?섏떊 ?꾩튂, Unload ?꾩튂, ?щ━???꾩튂 媛??대룞???ъ슜?쒕떎.
        /// </summary>
        public BaseAxis StageY { get; private set; }

        /// <summary>
        /// ?ㅽ뀒?댁? ?곹븯 ?대룞 異?(Z異?.<br/>
        /// <see cref="StageModuleSetup.WorkPositionZ"/> : TPU Place ?묒뾽 ?믪씠.<br/>
        /// <see cref="StageModuleSetup.AvoidPositionZ"/> : 異⑸룎 ?뚰뵾 ?섍컯 ?꾩튂.
        /// </summary>
        public BaseAxis StageZ { get; private set; }

        // ----------------------------------------------------------------------
        //  A-2. ?앹꽦??        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="StageModule"/>??珥덇린?뷀븳??
        /// </summary>
        /// <param name="moduleName">紐⑤뱢 ?대쫫 (?? "GoodStage", "NgStage")</param>
        public StageModule(string moduleName) : base(moduleName)
        {
            StageY = AjinFactory.CreateAxis(moduleName + "_StageY");
            StageZ = AjinFactory.CreateAxis(moduleName + "_StageZ");

            // Stage 30 ??StageModule SoftLimit ?뺤옣 (default 200 too strict)
            StageY.Setup.SoftLimitPlus = 350.0;
            StageZ.Setup.SoftLimitPlus = 250.0;

            // Composite ?몃━ ?깅줉
            Components.Add(StageY);
            Components.Add(StageZ);
        }

        // ----------------------------------------------------------------------
        //  A-3. Z異?異⑸룎 ?뚰뵾 ?ы띁 硫붿꽌??        // ----------------------------------------------------------------------

        /// <summary>
        /// StageZ媛 ?꾩옱 異⑸룎 ?뚰뵾 ?꾩튂(AvoidPositionZ)???덈뒗吏 ?뺤씤?쒕떎.<br/>
        /// ?ㅼ젣 ?꾩튂(ActualPosition)媛 AvoidPositionZ 짹 PositionTolerance 踰붿쐞 ?댁뿉
        /// ?덉쑝硫??뚰뵾 ?꾨즺濡??먯젙?쒕떎.
        /// </summary>
        /// <returns>?뚰뵾 ?꾩튂???덉쑝硫?true.</returns>
        public bool IsAtAvoidPosition()
        {
            double diff = Math.Abs(StageZ.ActualPosition - Setup.AvoidPositionZ);
            return diff <= Setup.PositionTolerance;
        }

        /// <summary>
        /// StageZ瑜?異⑸룎 ?뚰뵾 ?꾩튂(<see cref="StageModuleSetup.AvoidPositionZ"/>)濡??섍컯?쒗궓??<br/>
        /// ?대? ?뚰뵾 ?꾩튂???덉쑝硫??대룞 ?놁씠 利됱떆 諛섑솚?쒕떎.
        /// </summary>
        /// <returns>?섍컯 ?꾨즺 ??true, 異??뚮엺 諛쒖깮 ??false.</returns>
        public async Task<bool> MoveToAvoidPositionAsync()
        {
            // ?대? ?뚰뵾 ?꾩튂???덉쑝硫?遺덊븘?뷀븳 ?대룞 ?앸왂
            if (IsAtAvoidPosition())
                return true;

            await StageZ.MoveAbsoluteAsync(Setup.AvoidPositionZ, Recipe.ZVelocity);

            if (StageZ.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveToAvoidPosition: StageZ ?섍컯 ?ㅽ뙣.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// StageZ瑜??묒뾽 ?믪씠(<see cref="StageModuleSetup.WorkPositionZ"/>)濡??곸듅?쒗궓??<br/>
        /// TPU Place 吏곸쟾???몄텧?쒕떎.
        /// </summary>
        /// <returns>?곸듅 ?꾨즺 ??true, 異??뚮엺 諛쒖깮 ??false.</returns>
        public async Task<bool> MoveToWorkPositionAsync()
        {
            await StageZ.MoveAbsoluteAsync(Setup.WorkPositionZ, Recipe.ZVelocity);

            if (StageZ.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveToWorkPosition: StageZ ?곸듅 ?ㅽ뙣.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-WORKZ",
                    source: Name + ".MoveToWorkPositionAsync",
                    message: "StageZ ?묒뾽 ?꾩튂 ?대룞 ?ㅽ뙣 (axis code=" + StageZ.AlarmCode + ")");
                return false;
            }

            return true;
        }

        /// <summary>
        /// StageY瑜?吏?뺥븳 ?덈? ?꾩튂濡??대룞?쒕떎.
        /// </summary>
        /// <param name="targetY">紐⑺몴 Y ?꾩튂 [mm]</param>
        /// <returns>?대룞 ?꾨즺 ??true, 異??뚮엺 諛쒖깮 ??false.</returns>
        public async Task<bool> MoveYAsync(double targetY)
        {
            await StageY.MoveAbsoluteAsync(targetY, Recipe.YVelocity);

            if (StageY.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> MoveY: StageY ?대룞 ?ㅽ뙣. " +
                    "Target=" + targetY + "mm");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-MOVEY",
                    source: Name + ".MoveYAsync",
                    message: "StageY ?대룞 ?ㅽ뙣 (Target=" + targetY.ToString("F3") +
                             "mm, axis code=" + StageY.AlarmCode + ")");
                return false;
            }

            return true;
        }

        /// <summary>
        /// StageY瑜??湲??? ?꾩튂濡?蹂듦??쒗궓??<br/>
        /// ?щ━???꾨즺 ?? ?먮뒗 Unload ?꾨즺 ???먯쐞移?蹂듦????ъ슜?쒕떎.
        /// </summary>
        /// <returns>蹂듦? ?꾨즺 ??true, 異??뚮엺 諛쒖깮 ??false.</returns>
        public async Task<bool> MoveToHomeAsync()
        {
            return await MoveYAsync(Setup.HomePositionY);
        }
    }

    // ==========================================================================
    //  B. OutputStageUnit - 理쒖긽??Output Stage 硫붿씤 ?좊떅
    // ==========================================================================

    /// <summary>
    /// 寃???꾨즺???ㅼ씠瑜??묓뭹(Good) / 遺덈웾(NG)?쇰줈 遺꾨쪟?섏뿬 ?곸옱?섎뒗 Output Stage ?좊떅.<br/>
    /// GoodStage? NgStage ??媛쒖쓽 <see cref="StageModule"/>??蹂댁쑀?섎ŉ,
    /// BinCameraX濡??덉갑 ?곹깭瑜?寃?ы븳??
    /// <para>
    /// <b>?듭떖 ?명꽣???ㅺ퀎 - 異⑸룎 ?뚰뵾(Collision Avoidance):</b><br/>
    /// GoodStage? NgStage???숈씪??X ?묒뾽 醫뚰몴瑜?怨듭쑀?쒕떎. ?곕씪???寃??ㅽ뀒?댁???    /// StageY瑜??吏곸씠湲??? 諛섎뱶??諛섎?履??ㅽ뀒?댁???StageZ瑜?    /// <see cref="StageModuleSetup.AvoidPositionZ"/>濡??섍컯?쒗궓 ???대룞?댁빞 ?쒕떎.
    /// </para>
    /// <para>
    /// 怨꾩링 援ъ“:<br/>
    /// <c>OutputStageUnit</c><br/>
    /// ?쒋? <c>GoodStage (StageModule)</c><br/>
    /// ??  ?쒋? <c>GoodStage_StageY (SimAxis)</c><br/>
    /// ??  ?붴? <c>GoodStage_StageZ (SimAxis)</c><br/>
    /// ?쒋? <c>NgStage   (StageModule)</c><br/>
    /// ??  ?쒋? <c>NgStage_StageY   (SimAxis)</c><br/>
    /// ??  ?붴? <c>NgStage_StageZ   (SimAxis)</c><br/>
    /// ?붴? <c>BinCameraX            (SimAxis)</c>
    /// </para>
    /// </summary>
    public class OutputStageUnit : BaseUnit<OutputStageSetup, OutputStageConfig, OutputStageRecipe>
    {
        // ----------------------------------------------------------------------
        //  B-1. ?섎뱶?⑥뼱 而댄룷?뚰듃
        // ----------------------------------------------------------------------

        /// <summary>
        /// ?묓뭹(Good) ?ㅼ씠瑜??곸옱?섎뒗 ?ㅽ뀒?댁? 紐⑤뱢.
        /// </summary>
        public StageModule GoodStage { get; private set; }

        /// <summary>
        /// 遺덈웾(NG) ?ㅼ씠瑜??곸옱?섍퀬 肄쒕젢 ?щ━???붾? ?곸뿭???쒓났?섎뒗 ?ㅽ뀒?댁? 紐⑤뱢.
        /// </summary>
        public StageModule NgStage { get; private set; }

        /// <summary>
        /// Bin(?덉갑) ?꾩튂瑜?寃?ы븯??移대찓?쇱쓽 X ?대룞 異?<br/>
        /// Place ?꾨즺 ???묒뾽 ?꾩튂濡?吏꾩엯?섏뿬 ?덉갑 ?щ?瑜?珥ъ긽?섍퀬, 利됱떆 ?꾪눜?쒕떎.
        /// </summary>
        public BaseAxis BinCameraX { get; private set; }

        // ----------------------------------------------------------------------
        //  B-2. ?몃? ?곕룞 ?명꽣?섏씠??        // ----------------------------------------------------------------------

        /// <summary>
        /// TPU ?곕룞 ?명꽣?섏씠??<br/>
        /// "Place 以鍮??꾨즺" ?좏샇 ?꾩넚 諛?"Place ?꾨즺 / ?ㅼ쓬 ?섏떊 媛?? ?곹깭 ?듬낫???ъ슜.
        /// </summary>
        public ITpuUnit Tpu { get; private set; }

        /// <summary>
        /// OutputUnloader ?곕룞 ?명꽣?섏씠??<br/>
        /// 鍮?Bin)??媛??李쇱쓣 ???⑥씠??援먯껜 ?붿껌???ъ슜.
        /// </summary>
        public IOutputUnloaderUnit Unloader { get; private set; }

        // ----------------------------------------------------------------------
        //  B-3. ?앹꽦??        // ----------------------------------------------------------------------

        /// <summary>
        /// <see cref="OutputStageUnit"/>??珥덇린?뷀븯怨??섏쐞 紐⑤뱢??Composite ?몃━???깅줉?쒕떎.
        /// </summary>
        /// <param name="tpu">TPU ?곕룞 ?명꽣?섏씠??/param>
        /// <param name="unloader">OutputUnloader ?곕룞 ?명꽣?섏씠??/param>
        public OutputStageUnit(ITpuUnit tpu, IOutputUnloaderUnit unloader)
            : base("OutputStageUnit")
        {
            if (tpu      == null) throw new ArgumentNullException("tpu");
            if (unloader == null) throw new ArgumentNullException("unloader");

            Tpu      = tpu;
            Unloader = unloader;

            GoodStage  = new StageModule("GoodStage");
            NgStage    = new StageModule("NgStage");
            BinCameraX = AjinFactory.CreateAxis("OutputStage_BinCameraX");
            // Stage 30 ??BinCameraX SoftLimit ?뺤옣
            BinCameraX.Setup.SoftLimitPlus = 350.0;

            // Composite ?몃━ ?깅줉 - Save() ??怨듯넻 ?숈옉???섏쐞 ?몃━ ?꾩껜???꾪뙆??            Components.Add(GoodStage);
            Components.Add(NgStage);
            Components.Add(BinCameraX);
        }

        // ======================================================================
        //  B-4. ?대? ?명꽣???ы띁 硫붿꽌??        // ======================================================================

        /// <summary>
        /// 吏?뺥븳 ?寃??ㅽ뀒?댁???諛섎?履??ㅽ뀒?댁?瑜?諛섑솚?쒕떎.
        /// </summary>
        /// <param name="target">?寃??ㅽ뀒?댁? 紐⑤뱢</param>
        /// <returns>諛섎?履??ㅽ뀒?댁? 紐⑤뱢.</returns>
        private StageModule GetOppositeStage(StageModule target)
        {
            return ReferenceEquals(target, GoodStage) ? NgStage : GoodStage;
        }

        /// <summary>
        /// <b>[異⑸룎 ?뚰뵾 ?명꽣???듭떖 硫붿꽌??</b><br/>
        /// ?寃??ㅽ뀒?댁???StageY瑜??대룞?섍린 ?꾩뿉 諛섎?履??ㅽ뀒?댁???StageZ媛
        /// <see cref="StageModuleSetup.AvoidPositionZ"/>???덈뒗吏 ?뺤씤?섍퀬,
        /// ?꾨땲?쇰㈃ 癒쇱? ?섍컯?쒗궓??
        /// <para>
        /// ?명꽣???쒕굹由ъ삤:<br/>
        /// ?? Good Stage瑜??묒뾽 ?꾩튂濡?蹂대궪 ?? NG Stage媛 ?꾩쭅 WorkPositionZ???덈떎硫?        /// NG Stage瑜?AvoidPositionZ濡?癒쇱? ?대┛ ??Good Stage Y瑜??대룞?쒕떎.
        /// </para>
        /// </summary>
        /// <param name="targetStage">Y異뺤쓣 ?대룞?쒗궗 ?寃??ㅽ뀒?댁?</param>
        /// <returns>?명꽣??泥섎━ ?꾨즺(?뚰뵾 ?뺤씤 諛??꾩슂 ???섍컯) ??true, ?뚮엺 ??false.</returns>
        private async Task<bool> EnsureOppositeStageAvoidedAsync(StageModule targetStage)
        {
            StageModule opposite = GetOppositeStage(targetStage);

            // 諛섎?履??ㅽ뀒?댁?媛 ?대? ?뚰뵾 ?꾩튂???덉쑝硫?利됱떆 諛섑솚
            if (opposite.IsAtAvoidPosition())
            {
                Console.WriteLine(
                    "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' ?대? ?뚰뵾 ?꾩튂. 異붽? ?숈옉 遺덊븘??");
                return true;
            }

            // ?? 異⑸룎 寃쎄퀬 諛?媛뺤젣 ?뚰뵾 ?섍컯 ?????????????????????????????????
            // 諛섎?履??ㅽ뀒?댁?媛 WorkPositionZ 遺洹쇱뿉 ?덈뒗 ?곹깭?먯꽌 ?寃??ㅽ뀒?댁???            // StageY瑜??대룞?섎㈃ X 怨듭쑀 援ш컙?먯꽌 ???ㅽ뀒?댁?媛 異⑸룎?쒕떎.
            // 諛섎뱶??諛섎?履쎌쓣 AvoidPositionZ濡??섍컯?쒗궓 ???대룞?댁빞 ?쒕떎.
            Console.WriteLine(
                "[WARN]  '" + Name + "' -> Interlock: '" + opposite.Name +
                "' Z=" + opposite.StageZ.ActualPosition.ToString("F3") +
                "mm, ?뚰뵾 ?꾩튂 ?꾨떂. 媛뺤젣 ?섍컯 ?쒖옉.");

            bool avoidOk = await opposite.MoveToAvoidPositionAsync();

            if (!avoidOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> Interlock: '" + opposite.Name +
                    "' ?뚰뵾 ?섍컯 ?ㅽ뙣. '" + targetStage.Name + "' StageY ?대룞 以묐떒.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-AVOID",
                    source: Name + ".EnsureOppositeStageAvoidedAsync",
                    message: "諛섎?履??ㅽ뀒?댁?(" + opposite.Name + ") ?뚰뵾 ?섍컯 ?ㅽ뙣. '" +
                             targetStage.Name + "' StageY ?대룞 以묐떒");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> Interlock: '" + opposite.Name +
                "' ?뚰뵾 ?꾨즺. '" + targetStage.Name + "' StageY ?대룞 ?덇?.");
            return true;
        }

        // ======================================================================
        //  B-5. Step 1: ?ㅼ씠 ?섏떊 諛?異⑸룎 ?뚰뵾 ?대룞
        // ======================================================================

        /// <summary>
        /// TPU濡쒕????ㅼ씠瑜??섏떊?섍린 ?꾪빐 ?寃??ㅽ뀒?댁?瑜?Place ?꾩튂濡??대룞?쒕떎.
        /// <para>
        /// <b>?ㅽ뻾 ?쒖꽌:</b><br/>
        /// 1. [異⑸룎 ?뚰뵾 ?명꽣?? 諛섎?履??ㅽ뀒?댁? StageZ ??AvoidPositionZ ?섍컯 ?뺤씤.<br/>
        /// 2. ?寃??ㅽ뀒?댁? StageZ ??WorkPositionZ ?곸듅.<br/>
        /// 3. TpuOffset + VisionOffset???⑹궛??理쒖쥌 Y ?꾩튂濡?StageY ?대룞.<br/>
        /// 4. ?대룞 ?꾨즺 ??TPU??"Place 以鍮??꾨즺" ?좏샇 ?꾩넚.
        /// </para>
        /// </summary>
        /// <param name="request">?ㅼ씠 ?섏떊 ?붿껌 ?뚮씪誘명꽣 (?깃툒, ?ㅽ봽????</param>
        /// <returns>?대룞 諛??좏샇 ?꾩넚 ?꾨즺 ??true, ?명꽣???뚮엺 諛쒖깮 ??false.</returns>
        public async Task<bool> ReceiveDieAsync(ReceiveDieRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            StageModule target = request.Grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ReceiveDie: Grade=" + request.Grade +
                ", TpuOffsetY=" + request.TpuOffsetY.ToString("F3") +
                ", VisionOffsetY=" + request.VisionOffsetY.ToString("F3"));

            // ?? Step 1-1. [異⑸룎 ?뚰뵾 ?명꽣?? ?????????????????????????????????
            // ?寃??ㅽ뀒?댁? StageY瑜??대룞?섍린 ?? 諛섎?履??ㅽ뀒?댁? StageZ媛
            // AvoidPositionZ???덈뒗吏 ?뺤씤?섍퀬 ?꾩슂?섎㈃ 媛뺤젣 ?섍컯?쒕떎.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // ?? Step 1-2. ?寃?StageZ ??WorkPositionZ ?곸듅 ??????????????????
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ ?묒뾽 ?꾩튂濡??곸듅 以?..");

            bool workZOk = await target.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            // ?? Step 1-3. ?ㅽ봽???⑹궛 ??StageY ?대룞 ????????????????????????
            // Final Y = base position + TPU offset + bottom vision offset.
            double finalY = Setup.StageBasePositionY
                            + request.TpuOffsetY
                            + request.VisionOffsetY;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageY ?대룞. FinalY=" + finalY.ToString("F3") + "mm " +
                "(Base=" + Setup.StageBasePositionY.ToString("F3") +
                " + TpuY=" + request.TpuOffsetY.ToString("F3") +
                " + VisionY=" + request.VisionOffsetY.ToString("F3") + ")");

            bool moveYOk = await target.MoveYAsync(finalY);
            if (!moveYOk)
                return false;

            // ?? Step 1-4. TPU??Place 以鍮??꾨즺 ?좏샇 ?꾩넚 ????????????????????
            // TPU?????좏샇瑜?諛쏆? ??PickerZ瑜??섍컯?쒖폒 ?ㅼ씠瑜?Place?쒕떎.
            Tpu.NotifyPlaceReady();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU??Place 以鍮??꾨즺 ?좏샇 ?꾩넚 ?꾨즺.");

            return true;
        }

        // ======================================================================
        //  B-6. Step 2: Bin 鍮꾩쟾 寃??(Place ?꾨즺 ???덉갑 ?곹깭 ?뺤씤)
        // ======================================================================

        /// <summary>
        /// TPU媛 ?ㅼ씠瑜??대젮?볤퀬 ?꾪눜????BinCamera濡??덉갑 ?곹깭瑜?寃?ы븳??
        /// <para>
        /// <b>?ㅽ뻾 ?쒖꽌:</b><br/>
        /// 1. TPU Place ?꾨즺 + ?쎌빱 ?꾪눜源뚯? ?湲?<see cref="ITpuUnit.WaitPlaceDoneAsync"/>).<br/>
        /// 2. BinCameraX ??寃???꾩튂(WorkPositionX)濡?吏꾩엯.<br/>
        /// 3. 鍮꾩쟾 珥ъ긽 諛??덉갑 寃???섑뻾(?쒕??덉씠?? ??긽 OK).<br/>
        /// 4. BinCameraX ???湲?Retract) ?꾩튂濡?利됱떆 ?꾩쭊.<br/>
        /// 5. TPU??"?ㅼ쓬 ?ㅼ씠 ?섏떊 媛?? ?곹깭 ?듬낫.
        /// </para>
        /// </summary>
        /// <returns>寃???꾨즺 ??true, TPU ?湲???꾩븘???먮뒗 ?뚮엺 ??false.</returns>
        public async Task<bool> InspectBinPositionAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> Bin 寃???쒖옉. TPU Place ?꾨즺 ?湲?以?..");

            // ?? Step 2-1. TPU Place ?꾨즺 + ?쎌빱 ?꾪눜 ?湲??????????????????????
            // TPU ?쎌빱媛 ?꾩쟾???꾪눜???ㅼ뿉??BinCamera瑜?吏꾩엯?쒖폒 異⑸룎??諛⑹??쒕떎.
            bool placeDone = await Tpu.WaitPlaceDoneAsync(Config.TpuPlaceDoneTimeoutMs);
            if (!placeDone)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: TPU Place ?꾨즺 ?湲???꾩븘??");
                AlarmManager.Raise(
                    AlarmSeverity.Warning,
                    "OS-PLACEDONE",
                    source: Name + ".InspectBinPositionAsync",
                    message: "TPU Place ?꾨즺 ?湲???꾩븘??(timeout=" +
                             Config.TpuPlaceDoneTimeoutMs + "ms)");
                return false;
            }

            Console.WriteLine("[INFO]  '" + Name + "' -> TPU ?꾪눜 ?뺤씤. BinCamera 吏꾩엯 以?..");

            // ?? Step 2-2. BinCameraX ??寃???꾩튂 吏꾩엯 ????????????????????????
            await BinCameraX.MoveAbsoluteAsync(
                Setup.BinCameraWorkPositionX, Recipe.BinCameraVelocity);

            if (BinCameraX.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX 吏꾩엯 ?ㅽ뙣.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX 寃???꾩튂 吏꾩엯 ?ㅽ뙣 (axis code=" +
                             BinCameraX.AlarmCode + ")");
                return false;
            }

            // ?? Step 2-3. 鍮꾩쟾 珥ъ긽 諛??덉갑 寃???????????????????????????????
            // [?ㅼ젣 援ы쁽 ?? ?ш린??鍮꾩쟾 TCP ?대씪?댁뼵?몃줈 Trigger瑜?蹂대궡怨?寃곌낵瑜??섏떊?쒕떎.
            // ?쒕??덉씠?섏뿉?쒕뒗 利됱떆 OK濡?泥섎━?쒕떎.
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera ?덉갑 寃???섑뻾 以?..");
            SimulatorBridge.Instance?.CameraExposeFlash("BIN");
            await Task.Delay(20).ContinueWith(_ => { }); // 珥ъ긽 ?뚯슂 ?쒓컙 ?쒕??덉씠??
            Console.WriteLine("[INFO]  '" + Name + "' -> BinCamera 寃???꾨즺. 利됱떆 ?꾪눜.");

            // ?? Step 2-4. BinCameraX ???湲?Retract) ?꾩튂 利됱떆 ?꾩쭊 ??????????
            // ?ㅼ쓬 TPU 吏꾩엯 諛??ㅽ뀒?댁? ?대룞怨쇱쓽 媛꾩꽠??理쒖냼?뷀븯湲??꾪빐 利됱떆 ?꾪눜?쒕떎.
            await BinCameraX.MoveAbsoluteAsync(
                Setup.BinCameraRetractPositionX, Recipe.BinCameraVelocity);

            if (BinCameraX.IsAlarm)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> InspectBin: BinCameraX ?꾪눜 ?ㅽ뙣.");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "OS-BINCAM",
                    source: Name + ".InspectBinPositionAsync",
                    message: "BinCameraX ?湲??꾩튂 ?꾪눜 ?ㅽ뙣 (axis code=" +
                             BinCameraX.AlarmCode + ")");
                return false;
            }

            // ?? Step 2-5. TPU???ㅼ쓬 ?ㅼ씠 ?섏떊 媛???듬낫 ?????????????????????
            // ???좏샇 ?댄썑 TPU???ㅼ쓬 ?쇱빱??Place ?쒗?ㅻ? ?쒖옉?????덈떎.
            Tpu.NotifyReadyForNextDie();
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> TPU??'?ㅼ쓬 ?ㅼ씠 ?섏떊 媛?? ?듬낫 ?꾨즺.");

            return true;
        }

        // ======================================================================
        //  B-7. Step 3: ?⑥씠??援먯껜 ?湲?(Bin Full)
        // ======================================================================

        /// <summary>
        /// 吏???깃툒??鍮?Bin)??媛??李쇱쓣 ???ㅽ뀒?댁?瑜?援먯껜 ?꾩튂濡??대룞?섍퀬
        /// Unloader???⑥씠??援먯껜瑜??붿껌????援먯껜 ?꾨즺源뚯? Suspend?쒕떎.
        /// <para>
        /// <b>?ㅽ뻾 ?쒖꽌:</b><br/>
        /// 1. [異⑸룎 ?뚰뵾 ?명꽣?? 諛섎?履??ㅽ뀒?댁? StageZ ?뚰뵾 ?뺤씤.<br/>
        /// 2. ?寃??ㅽ뀒?댁? StageZ ??AvoidPositionZ ?섍컯(?대룞 以??덉쟾 ?믪씠 ?뺣낫).<br/>
        /// 3. ?寃??ㅽ뀒?댁? StageY ??UnloadPositionY ?대룞.<br/>
        /// 4. Unloader??泥댁씤吏 ?붿껌 ?꾩넚 ??援먯껜 ?꾨즺源뚯? 鍮꾨룞湲??湲?Suspend).
        /// </para>
        /// </summary>
        /// <param name="grade">媛??李?鍮덉쓽 ?깃툒 (Good / NG)</param>
        /// <returns>援먯껜 ?꾨즺 ??true, ?명꽣???뚮엺/??꾩븘????false.</returns>
        public async Task<bool> RequestWaferChangeAsync(DieGrade grade)
        {
            StageModule target = grade == DieGrade.Good ? GoodStage : NgStage;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: Grade=" + grade +
                " Bin Full. 援먯껜 ?꾩튂 ?대룞 ?쒖옉.");

            // ?? Step 3-1. [異⑸룎 ?뚰뵾 ?명꽣?? ?????????????????????????????????
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(target);
            if (!interlockOk)
                return false;

            // ?? Step 3-2. ?寃?StageZ ??AvoidPositionZ ?섍컯 (?대룞 以??덉쟾) ???
            // 援먯껜 ?꾩튂濡??대룞 ?쒖뿉??StageZ瑜??뚰뵾 ?믪씠濡??좎??댁빞
            // 寃쎈줈??媛꾩꽠 援ъ“臾쇨낵??異⑸룎??諛⑹??쒕떎.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' StageZ ?뚰뵾 ?꾩튂 ?섍컯 以?..");

            bool avoidOk = await target.MoveToAvoidPositionAsync();
            if (!avoidOk)
                return false;

            // ?? Step 3-3. ?寃?StageY ??UnloadPositionY ?대룞 ????????????????
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' 援먯껜 ?꾩튂(Y=" + target.Setup.UnloadPositionY.ToString("F1") +
                "mm)濡??대룞 以?..");

            bool moveOk = await target.MoveYAsync(target.Setup.UnloadPositionY);
            if (!moveOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> '" + target.Name +
                "' 援먯껜 ?꾩튂 ?꾨떖. Unloader??泥댁씤吏 ?붿껌 ?꾩넚 (Suspend ?쒖옉).");

            // ?? Step 3-4. Unloader??泥댁씤吏 ?붿껌 ??援먯껜 ?꾨즺源뚯? Suspend ??????
            // WaferChangeTimeoutMs == 0?대㈃ 臾댄븳 ?湲?Unloader ?묒뾽 ?꾨즺源뚯?).
            bool changeOk = await Unloader.RequestWaferChangeAsync(grade, Config.WaferChangeTimeoutMs);

            if (!changeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> WaferChange: Unloader 援먯껜 ??꾩븘?? Grade=" + grade);
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> WaferChange: 援먯껜 ?꾨즺. Grade=" + grade);
            return true;
        }

        // ======================================================================
        //  B-8. Step 4: 肄쒕젢 ?щ━??(??Good ?⑥씠??濡쒕뵫 ?꾨즺 吏곹썑 1???ㅽ뻾)
        // ======================================================================

        /// <summary>
        /// TPU ?쎌빱??肄쒕젢(?몄쫹)??臾살? ?대Ъ吏덉쓣 ?쒓굅?섍린 ?꾪빐 ?щ━?앹쓣 ?섑뻾?쒕떎.
        /// <para>
        /// <b>?몃━嫄?議곌굔:</b> ?덈줈??Good ?⑥씠?쇨? 濡쒕뵫 ?꾨즺??吏곹썑 1???ㅽ뻾?쒕떎.
        /// </para>
        /// <para>
        /// <b>?ㅽ뻾 ?쒖꽌:</b><br/>
        /// 1. [異⑸룎 ?뚰뵾 ?명꽣?? GoodStage StageZ ?뚰뵾 ?뺤씤
        ///    (NgStage瑜??대룞?쒗궎誘濡?諛섎?履쎌? GoodStage).<br/>
        /// 2. NgStage StageY ??CleaningPositionY(NG ?⑥씠???붾? ?멸낸) ?대룞.<br/>
        /// 3. NgStage StageZ ??WorkPositionZ ?곸듅(TPU 肄쒕젢???붾? ?곸뿭???우쓣 ?믪씠).<br/>
        /// 4. TPU??肄쒕젢 ?щ━???붿껌 ?꾩넚 ???꾨즺 ?湲?<br/>
        /// 5. NgStage StageZ ??AvoidPositionZ ?섍컯.<br/>
        /// 6. NgStage StageY ??HomePositionY 蹂듦?(?먯쐞移?.
        /// </para>
        /// </summary>
        /// <returns>?щ━???꾨즺 ??true, ?명꽣???뚮엺/??꾩븘????false.</returns>
        public async Task<bool> PerformColletCleaningAsync()
        {
            Console.WriteLine("[INFO]  '" + Name + "' -> 肄쒕젢 ?щ━???쒖옉. NgStage ?붾? ?곸뿭?쇰줈 ?대룞.");

            // ?? Step 4-1. [異⑸룎 ?뚰뵾 ?명꽣?? ?????????????????????????????????
            // NgStage瑜??대룞?쒗궎誘濡?諛섎?履쎌씤 GoodStage??StageZ ?뚰뵾瑜?癒쇱? ?뺤씤?쒕떎.
            bool interlockOk = await EnsureOppositeStageAvoidedAsync(NgStage);
            if (!interlockOk)
                return false;

            // ?? Step 4-2. NgStage StageY ??CleaningPositionY ?대룞 ????????????
            // NG ?⑥씠?쇱쓽 ?ъ슜?섏? ?딅뒗 ?멸낸(?붾?) ?곸뿭?쇰줈 ?대룞?쒕떎.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY ??CleaningPositionY=" +
                NgStage.Setup.CleaningPositionY.ToString("F1") + "mm ?대룞 以?..");

            bool cleaningMoveOk = await NgStage.MoveYAsync(NgStage.Setup.CleaningPositionY);
            if (!cleaningMoveOk)
                return false;

            // ?? Step 4-3. NgStage StageZ ??WorkPositionZ ?곸듅 ????????????????
            // TPU 肄쒕젢???붾? ?곸뿭 ?쒕㈃???우쓣 ???덈룄濡??묒뾽 ?믪씠濡??곸듅?쒕떎.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageZ ??WorkPositionZ ?곸듅 以?..");

            bool workZOk = await NgStage.MoveToWorkPositionAsync();
            if (!workZOk)
                return false;

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> ?щ━??以鍮??꾨즺. TPU??肄쒕젢 ?щ━???붿껌 ?꾩넚.");

            // ?? Step 4-4. TPU??肄쒕젢 ?щ━???붿껌 ???꾨즺 ?湲?????????????????
            // TPU?????붿껌??諛쏆쑝硫??쎌빱瑜??붾? ?곸뿭 ?꾩뿉????뒗 ?숈옉???섑뻾?쒕떎.
            // ?щ━?앹씠 ?꾨즺???뚭퉴吏 鍮꾨룞湲??湲고븳??
            bool cleaningOk = await Tpu.RequestColletCleaningAsync(Config.ColletCleaningTimeoutMs);
            if (!cleaningOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: TPU ?щ━????꾩븘??");
                // ??꾩븘?껋씠?붾씪??NgStage???덉쟾 ?꾩튂濡?蹂듦??쒗궓??
            }
            else
            {
                Console.WriteLine("[INFO]  '" + Name + "' -> TPU 肄쒕젢 ?щ━???꾨즺.");
            }

            // ?? Step 4-5. NgStage StageZ ??AvoidPositionZ ?섍컯 ??????????????
            // ?대룞 ???덉쟾 ?믪씠 ?뺣낫. ?ㅽ뙣?대룄 怨꾩냽 吏꾪뻾?섏뿬 蹂듦?瑜??쒕룄?쒕떎.
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageZ ?뚰뵾 ?꾩튂 ?섍컯 以?..");

            bool avoidOk = await NgStage.MoveToAvoidPositionAsync();
            if (!avoidOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage StageZ ?섍컯 ?ㅽ뙣.");
                return false;
            }

            // ?? Step 4-6. NgStage StageY ??HomePositionY 蹂듦? ????????????????
            Console.WriteLine(
                "[INFO]  '" + Name + "' -> NgStage StageY ???꾩튂 蹂듦? 以?..");

            bool homeOk = await NgStage.MoveToHomeAsync();
            if (!homeOk)
            {
                Console.WriteLine(
                    "[ALARM] '" + Name + "' -> ColletCleaning: NgStage ??蹂듦? ?ㅽ뙣.");
                return false;
            }

            Console.WriteLine(
                "[INFO]  '" + Name + "' -> 肄쒕젢 ?щ━???쒗???꾨즺. NgStage ?먯쐞移?");

            // ??꾩븘?껋씠 ?덉뿀?ㅻ㈃ 理쒖쥌 false 諛섑솚
            return cleaningOk;
        }
    }
}

