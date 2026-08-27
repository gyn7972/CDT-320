using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// [카메라 바코드 2026-08-27] BIN 카메라 2샷 바코드 판독 프로토콜 공용부 — 샷 요청(EPD까지)과
    /// 집계 RESULT(profile=BIN_BARCODE) 회수/정규화만 담당한다. 축 이동·재시도·복구는 호출자
    /// (자동: OutputFeederLoadToStageSequence, 수동 테스트: OutputStageRecipePage MANUAL ACTION) 책임.
    /// 와이어 규약: 비전PC_BIN바코드_2샷판독_프로토콜_수정지시_프롬프트_2026-08-26.md (리포 루트).
    /// </summary>
    public sealed class BinBarcodeCameraResult
    {
        public bool Pass;
        public string Barcode = string.Empty;
        public string DecodedShot = string.Empty;
        public string FailReason = string.Empty;
        public string Status = string.Empty;
        public string GroupId = string.Empty;
    }

    public static class BinBarcodeCameraReader
    {
        /// <summary>BIN Vision 명령 채널 연결 확인(카메라 바코드 모드 전제).</summary>
        public static bool IsVisionReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                VisionTcpClient client = VisionCommandService.ResolveInspectionClient(AutoVisionChannel.Bin);
                if (client != null && client.IsConnected)
                    return true;

                reason = "BIN 카메라 Vision 명령 채널이 연결되어 있지 않습니다(카메라 바코드 모드).";
                return false;
            }
            catch (Exception ex)
            {
                reason = "BIN 카메라 Vision 연결 확인 예외. error=" + ex.Message;
                return false;
            }
        }

        public static string NewGroupId()
        {
            return VisionCorrelationIdGenerator.NewGroupId();
        }

        /// <summary>샷 1회 요청 — 반환 시점에 EPD 수신 완료(핸들러는 이후 StageY를 움직여도 된다).
        /// null이면 요청/EPD 실패. headSide="GOOD"/"NG"(스테이지 사이드), shot=0(+오프셋)/1(−오프셋),
        /// 두 샷은 같은 groupId를 써야 비전이 한 그룹으로 누적한다.</summary>
        public static async Task<VisionRequestHandle> SendShotAsync(
            string headSide,
            int shot,
            string groupId,
            string waferId,
            int epdTimeoutMs,
            CancellationToken ct)
        {
            string recipeId = MaterialStateService.State != null
                ? (MaterialStateService.State.RecipeName ?? string.Empty)
                : string.Empty;
            string lotId = MaterialStateService.GetProductionLotId();

            var shotContext = new VisionInspectionRequestContext(
                AutoVisionChannel.Bin,
                VisionToolIds.Bin.BinBarcodeReader,
                -1,                     // fb 미사용 — HEAD는 headOverride(GOOD/NG)
                1,                      // HEAD_INDEX 고정 1 (프로토콜 §2-1)
                0, 0, 0,                // DIE_INDEX, GRID_X, GRID_Y
                shot,                   // CHANNEL = 샷 인덱스
                string.Empty,           // dieId 없음
                waferId ?? string.Empty,
                recipeId,
                lotId,
                VisionInspectionOperations.Inspect,
                VisionResultTimings.Deferred,
                groupId,
                false,                  // 판독 시점 waferId는 임시값 — 자재 문맥 강제 안 함
                headSide);
            return await AutoVisionRequestService.StartInspectionRequestAsync(
                shotContext, epdTimeoutMs, ct).ConfigureAwait(false);
        }

        /// <summary>ch1 샷 handle로 집계 RESULT 1건을 회수해 barcode를 정규화해 돌려준다.
        /// Pass = status PASS && 정규화 barcode 비어있지 않음. RESULT 미수신이면 FailReason=RESULT_TIMEOUT.</summary>
        public static async Task<BinBarcodeCameraResult> WaitAggregateResultAsync(
            VisionRequestHandle lastShotHandle,
            int timeoutMs,
            CancellationToken ct)
        {
            var outcome = new BinBarcodeCameraResult
            {
                GroupId = lastShotHandle != null && lastShotHandle.Request != null
                    ? (lastShotHandle.Request.GroupId ?? string.Empty)
                    : string.Empty
            };

            VisionInspectionResult result = await AutoVisionRequestService.WaitInspectionStageAsync(
                lastShotHandle,
                VisionInspectionCommands.Result,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (result == null)
            {
                outcome.FailReason = "RESULT_TIMEOUT";
                return outcome;
            }

            outcome.Status = result.Status ?? string.Empty;
            string rawBarcode = null;
            if (result.Values != null)
            {
                string decodedShot;
                string failReason;
                result.Values.TryGetValue("barcode", out rawBarcode);
                if (result.Values.TryGetValue("decoded_shot", out decodedShot))
                    outcome.DecodedShot = decodedShot ?? string.Empty;
                if (result.Values.TryGetValue("fail_reason", out failReason))
                    outcome.FailReason = failReason ?? string.Empty;
            }

            outcome.Barcode = Normalize(rawBarcode);
            outcome.Pass = string.Equals(outcome.Status, "PASS", StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrEmpty(outcome.Barcode);
            return outcome;
        }

        /// <summary>바코드 문자열 정규화 — OutputFeederLoadToStageSequence.NormalizeBarcode(리더 경로)와
        /// 동일 규칙: STX/ETX·공백 제거, 그 외 제어문자 발견 시 무효(빈 값).</summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var chars = new System.Collections.Generic.List<char>(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\x02' || ch == '\x03' || char.IsWhiteSpace(ch))
                    continue;
                if (char.IsControl(ch))
                    return string.Empty;
                chars.Add(ch);
            }
            return new string(chars.ToArray());
        }
    }
}
