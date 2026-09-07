using System;
using System.Threading;

namespace QMC.CDT320.Materials
{
    public static partial class MaterialStateService
    {
        /// <summary>
        /// 상단 표시 전용 문자열을 복사한다. 저장/변경 중이면 UI를 기다리게 하지 않고
        /// 다음 표시 주기에 다시 읽는다. 장비 제어와 자재 유무 판단에는 사용하지 않는다.
        /// </summary>
        internal static bool TryGetProcessingDisplayIds(out string input, out string good, out string ng)
        {
            input = good = ng = "-";
            if (!Monitor.TryEnter(_stateSync))
                return false;

            try
            {
                if (State == null || State.Wafers == null)
                    return false;

                bool hasInput = false;
                bool hasGood = false;
                bool hasNg = false;
                foreach (WaferMaterial wafer in State.Wafers)
                {
                    if (wafer == null || wafer.CurrentLocation == null ||
                        WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                        continue;

                    string display = !string.IsNullOrWhiteSpace(wafer.BarcodeId)
                        ? wafer.BarcodeId
                        : (!string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "-");
                    switch (wafer.CurrentLocation.Kind)
                    {
                        case MaterialLocationKind.InputStage:
                            if (!hasInput) { input = display; hasInput = true; }
                            break;
                        case MaterialLocationKind.OutputStageGood:
                            if (!hasGood) { good = display; hasGood = true; }
                            break;
                        case MaterialLocationKind.OutputStageNg:
                            if (!hasNg) { ng = display; hasNg = true; }
                            break;
                    }
                    if (hasInput && hasGood && hasNg)
                        break;
                }
                return true;
            }
            finally
            {
                Monitor.Exit(_stateSync);
            }
        }
    }
}
