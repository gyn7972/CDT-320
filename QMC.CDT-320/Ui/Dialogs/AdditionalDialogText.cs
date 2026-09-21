using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>Display-only mapping for known dialog text. Model values, commands and log text remain unchanged.</summary>
    internal static class AdditionalDialogText
    {
        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "파일 미선택", "extraDialog.remaining.viewer.noFile" },
            { "현재/최근 로컬 맵을 확인하는 중입니다...", "extraDialog.remaining.viewer.checkCache" },
            { "현재/최근 수신 맵이 없습니다. 파일을 열어 확인하세요.", "extraDialog.remaining.viewer.noCache" },
            { "초기 맵 조회", "extraDialog.remaining.viewer.initialLookup" },
            { "파일 열기", "extraDialog.remaining.viewer.openFile" },
            { "현재/최근 맵 열기", "extraDialog.remaining.viewer.openCache" },
            { "최근 수신 맵 열기", "extraDialog.remaining.viewer.openRecent" },
            { "폴더 선택", "extraDialog.remaining.viewer.selectFolder" },
            { "파일 목록 갱신", "extraDialog.remaining.viewer.refreshFiles" },
            { "목록 파일 열기", "extraDialog.remaining.viewer.openListFile" },
            { "선택한 형식으로 확인할 파일을 여세요.", "extraDialog.remaining.viewer.openChosenFormat" },
            { "현재 맵은 유지됩니다. 선택한 형식으로 다음 파일을 여세요.", "extraDialog.remaining.viewer.nextFormat" },
            { "열 파일 형식 선택", "extraDialog.remaining.viewer.selectFormat" },
            { "원본 방향 보기", "extraDialog.remaining.viewer.viewOriginal" },
            { "공정 방향 참고 보기", "extraDialog.remaining.viewer.viewProcess" },
            { "왼쪽 보기 회전", "extraDialog.remaining.viewer.rotateLeft" },
            { "오른쪽 보기 회전", "extraDialog.remaining.viewer.rotateRight" },
            { "화면 맞춤", "extraDialog.remaining.viewer.fit" },
            { "다이 주소 표시", "extraDialog.remaining.viewer.address" },
            { "직접 선택한 파일", "extraDialog.remaining.viewer.directFile" },
            { "현재 파일 작업이 끝난 후 다시 시도하세요.", "extraDialog.remaining.viewer.waitBusy" },
            { "맵 파일을 읽는 중입니다...", "extraDialog.remaining.viewer.reading" },
            { "파일 읽기 완료 · 원본 방향 0°", "extraDialog.remaining.viewer.readDone" },
            { "파일 읽기", "extraDialog.remaining.viewer.readFile" },
            { "보기 방향을 바꾸는 중입니다...", "extraDialog.remaining.viewer.rotating" },
            { "보기 회전", "extraDialog.remaining.viewer.rotate" },
            { "폴더 목록에서 선택한 파일", "extraDialog.remaining.viewer.listFile" },
            { "수신된 로컬 맵을 찾는 중입니다...", "extraDialog.remaining.viewer.findingCache" },
            { "최근 수신 파일이 로컬에 없습니다.", "extraDialog.remaining.viewer.noRecentLocal" },
            { "현재/최근 수신 파일이 로컬에 없습니다.", "extraDialog.remaining.viewer.noCurrentLocal" },
            { "웨이퍼맵 보기", "extraDialog.remaining.viewer.title" },
            { "현재 InputStage 확정 바코드의 로컬 파일", "extraDialog.remaining.viewer.currentCache" },
            { "최근 수신한 로컬 파일", "extraDialog.remaining.viewer.recentCache" },
            { "최근 수신한 로컬 파일 (현재 바코드 캐시 없음)", "extraDialog.remaining.viewer.recentCacheFallback" },
            { "폴더 파일 목록을 읽는 중입니다...", "extraDialog.remaining.viewer.readFolder" },
            { "파일 목록 · 첫 5,000개 (직접 열기 가능)", "extraDialog.remaining.viewer.firstFiles" },
            { "파일이 많아 첫 5,000개만 표시합니다. 파일 열기로 다른 파일을 선택할 수 있습니다.", "extraDialog.remaining.viewer.fileLimit" },
            { "파일을 두 번 누르거나 Enter로 여세요.", "extraDialog.remaining.viewer.openHint" },
            { "파일 목록 조회", "extraDialog.remaining.viewer.lookupFiles" },
            { "@@@ 마크", "extraDialog.remaining.viewer.mark" },
            { "빈 셀 (___)", "extraDialog.remaining.viewer.empty" },
            { "캠택 CAMTEK", "extraDialog.remaining.viewer.camtek" },
            { "삼성 RAD", "extraDialog.remaining.viewer.rad" },
            { "캠택", "extraDialog.remaining.viewer.camtekShort" },
            { "없음", "extraDialog.remaining.viewer.none" },
            { "다이를 선택하면 현재 보기의 X/Y와 BIN을 표시합니다. 보기 원점은 좌하단 0,0입니다.", "extraDialog.remaining.viewer.selectDie" },
            { "확인용", "extraDialog.remaining.viewer.reference" },
            { "파일 열기로 직접 선택할 수 있습니다.", "extraDialog.remaining.viewer.pickManual" },
            { "파일 확인용 뷰어 · 보기 방향과 형식 선택은 이 창에만 적용됩니다.", "extraDialog.remaining.viewer.initialNotice" },
            { "파일 목록", "extraDialog.remaining.viewer.fileList" },
            { "공정 방향 참고", "extraDialog.remaining.viewer.processReference" },
            { "맵을 열어 주세요.", "extraDialog.remaining.viewer.openMap" },
            { "파일 헤더와 BIN 수량\r\n파일을 열면 표시됩니다.", "extraDialog.remaining.viewer.summaryInitial" },
            { "선택한 다이 없음 · 원본 주소와 현재 보기의 좌하단 1 기준 주소를 표시합니다.", "extraDialog.remaining.viewer.selectedInitial" },
            { "파일을 열어 웨이퍼맵을 확인하세요.", "extraDialog.remaining.viewer.statusInitial" },
            { "삼성 (RAD 좌표형)", "extraDialog.remaining.viewer.radChoice" },
            { "캠택 (CAMTEK RowData)", "extraDialog.remaining.viewer.camtekChoice" },
            { "웨이퍼맵 파일 열기", "extraDialog.remaining.viewer.fileTitle" },
            { "모든 웨이퍼맵 파일 (*.*)|*.*|텍스트 파일 (*.txt)|*.txt", "extraDialog.remaining.viewer.fileFilter" },
            { "웨이퍼맵이 있는 폴더를 선택하세요.", "extraDialog.remaining.viewer.folderDescription" },
            { "90°·270°는 현재 장비에서 사용할 수 없습니다. 미리보기/설정 저장만 가능하며 FINAL APPLY는 차단됩니다.", "extraDialog.remaining.create.quarterTurn" },
            { "내부 여백", "extraDialog.remaining.create.margin" },
            { "웨이퍼 직경", "extraDialog.remaining.create.diameter" },
            { "다이 X", "extraDialog.remaining.create.dieX" },
            { "다이 Y", "extraDialog.remaining.create.dieY" },
            { "사양을 확인한 뒤 AUTO WAFER CREATE를 누르세요.", "extraDialog.remaining.create.startHint" },
            { "저장된 맵의 회전과 개수 조건을 복원했습니다.", "extraDialog.remaining.create.restoredSaved" },
            { "입력 사양과 저장된 맵 조건이 다릅니다. AUTO WAFER CREATE로 다시 생성하세요.", "extraDialog.remaining.create.mismatch" },
            { "입력 조건 미적용 · 저장할 수 없습니다. 직전 결과를 표시합니다. 개수 적용 / 재생성으로 상단 사양과 개수를 함께 적용하세요.", "extraDialog.remaining.create.unappliedCounts" },
            { "생성한 맵을 저장하는 중입니다...", "extraDialog.remaining.create.saving" },
            { "맵 저장", "extraDialog.remaining.create.saveTitle" },
            { "마지막으로 생성한 사양과 회전의 AUTO 결과로 복원했습니다.", "extraDialog.remaining.create.restoredAuto" },
            { "웨이퍼 맵을 생성하는 중입니다...", "extraDialog.remaining.create.generating" },
            { "현재 입력 사양과 끝줄·전체 개수 조건으로 다시 계산하는 중입니다...", "extraDialog.remaining.create.recalculating" },
            { "현재 조건에서 AUTO 생성된 다이가 없습니다. 개수 조건을 입력하여 미리볼 수 있습니다.", "extraDialog.remaining.create.noDies" },
            { "사양 편집 중 · 아직 미적용이므로 저장할 수 없습니다. 직전 결과와 요청 개수를 유지합니다. 개수 적용 / 재생성으로 함께 적용하세요.", "extraDialog.remaining.create.editPending" },
            { "이전식 외곽 여유 +0.200 mm입니다. 새 방식은 AUTO 또는 사양 편집 후 개수 적용 / 재생성으로 만드세요.", "extraDialog.remaining.create.legacyNotice" },
            { "현재 맵은 이전 수식의 반경 바깥쪽 여유 +0.200 mm를 사용합니다. 값을 편집하거나 AUTO 생성하면 새 내부 여백 방식으로 전환됩니다.", "extraDialog.remaining.create.legacyTip" },
            { "웨이퍼 원에서 안쪽으로 비울 반경 여백입니다. 허용 반경 = 직경/2 - 여백. DIE GAP과 다른 값입니다.", "extraDialog.remaining.create.marginTip" },
            { "90° (미리보기 전용)", "extraDialog.remaining.create.preview90" },
            { "270° (미리보기 전용)", "extraDialog.remaining.create.preview270" },
            { "BIN DIE MAP CREATE에서 GOOD·NG 각각", "extraDialog.remaining.create.outputApproval" },
            { "INPUT DIE MAP CREATE에서", "extraDialog.remaining.create.inputApproval" },
            { "Wafer Align / Die Mapping 결과를 불러오는 중입니다.", "extraDialog.remaining.review.loading" },
            { "비전 사용 종료", "extraDialog.remaining.review.visionEnd" },
            { "비전 사용 시작", "extraDialog.remaining.review.visionStart" },
            { "읽기 전용 또는 종료된 Review에서는 검증 영상을 촬영할 수 없습니다.", "extraDialog.remaining.review.captureBlocked" },
            { "표시할 Input Die Map이 없습니다.", "extraDialog.remaining.review.noMap" },
            { "Input Die Map을 불러왔습니다. 시작 Die와 픽업 경로를 확인하세요.", "extraDialog.remaining.review.loaded" },
            { "수동 얼라인 웨이퍼입니다. Jog로 정렬 후 [T 보정]을 완료해야 확정할 수 있습니다.", "extraDialog.remaining.review.manualAlign" },
            { "수동 폴백 얼라인 — T 보정 완료 전 확정 차단", "extraDialog.remaining.review.thetaPending" },
            { "T 보정 완료 — 확정 허용", "extraDialog.remaining.review.thetaDone" },
            { "T 보정 저장으로 Mapping이 무효화되어 Die Mapping 재실행을 자동 제출합니다.", "extraDialog.remaining.review.mappingResubmit" },
            { "읽기 전용 - 영상 확인/측정만 가능", "extraDialog.remaining.review.readOnlyVision" },
            { "읽기 전용 - 비전 미연결(영상 없음)", "extraDialog.remaining.review.readOnlyOffline" },
            { "안전 영역 사용 중 - 비전 미연결로 Live/Grab/측정 불가", "extraDialog.remaining.review.scopeOffline" },
            { "비전 미연결 - 영상/측정 불가(설정에서 비전 연결 확인)", "extraDialog.remaining.review.offline" },
            { "비전 안전 영역 사용 중 - Live/Grab/측정 가능", "extraDialog.remaining.review.scopeActive" },
            { "영상 확인/측정 가능 - Live/Grab은 비전 사용 시작 후 가능", "extraDialog.remaining.review.visionAvailable" },
            { "비생산 모드의 수동 Review 확인입니다.", "extraDialog.remaining.review.nonproductionOn" },
            { "비생산 모드의 수동 Review 표시를 해제했습니다.", "extraDialog.remaining.review.nonproductionOff" },
            { "현재 Stage Wafer/DieMap의 읽기 전용 화면입니다. 모션 및 데이터 변경 기능은 연결되지 않았습니다.", "extraDialog.remaining.review.readOnly" },
            { "매핑 완료 — 유지 중이던 창의 결정 잠금을 해제합니다.", "extraDialog.remaining.review.mappingDone" },
            { "Manual Review 기능이 활성화되었습니다. 확인 시 Auto 공정을 계속하고, 취소 시 센터 검출/T Align부터 다시 수행합니다.", "extraDialog.remaining.review.manualEnabled" },
            { "요청 처리에 실패했습니다. 상태를 확인한 뒤 다시 시도하세요.", "extraDialog.remaining.review.retryRequest" },
            { "확인 요청을 처리 중이므로 후보 좌표를 변경할 수 없습니다.", "extraDialog.remaining.review.offsetDecisionBlocked" },
            { "Review Draft Die Map 또는 Offset 값이 유효하지 않습니다.", "extraDialog.remaining.review.invalidOffset" },
            { "OFFSET 적용 후보에 유효하지 않은 좌표가 있습니다. Draft는 변경하지 않았습니다.", "extraDialog.remaining.review.invalidCoordinates" },
            { "OFFSET 후보를 반영했습니다. 맵과 픽업 순서를 확인한 뒤 CONFIRM을 누르세요.", "extraDialog.remaining.review.offsetApplied" },
            { "선택 Die 이동이 진행 중입니다. 완료 또는 STOP 후 다시 실행하세요.", "extraDialog.remaining.review.moveBusy" },
            { "다른 Review 수동 동작이 진행 중입니다. 완료 또는 STOP 후 다시 실행하세요.", "extraDialog.remaining.review.manualBusy" },
            { "픽업 경로 설정이 변경되었습니다. PREVIEW를 확인한 뒤 APPLY PICKUP ORDER를 실행하세요.", "extraDialog.remaining.review.pathChanged" },
            { "시작할 Die를 Wafer Map 또는 목록에서 먼저 선택하세요.", "extraDialog.remaining.review.selectStart" },
            { "SKIP/GOOD/NG Die는 시작 Die로 설정할 수 없습니다.", "extraDialog.remaining.review.badStart" },
            { "적용할 Review Draft Die Map이 없습니다.", "extraDialog.remaining.review.noDraft" },
            { "선택 시작 Die 사용이 켜져 있지만 시작 Die가 지정되지 않았습니다.", "extraDialog.remaining.review.missingStart" },
            { "Pickable Target이 0개인 빈 픽업 경로 Draft를 적용했습니다.", "extraDialog.remaining.review.emptyOrder" },
            { "상태를 변경할 Die를 선택하세요.", "extraDialog.remaining.review.selectStateDie" },
            { "Jog 정지를 요청했습니다.", "extraDialog.remaining.review.jogStopped" },
            { "Review 수동 동작 정지를 요청했습니다.", "extraDialog.remaining.review.manualStopped" },
            { "이동할 Die를 먼저 선택하세요.", "extraDialog.remaining.review.selectMoveDie" },
            { "읽기 전용 화면에서는 Live/Grab 명령을 사용할 수 없습니다.", "extraDialog.remaining.review.readOnlyCommands" },
            { "Wafer Vision 안전 제어 연결이 없습니다.", "extraDialog.remaining.review.noVisionControl" },
            { "부저 정지를 요청했습니다. 확인 또는 취소를 선택하세요.", "extraDialog.remaining.review.buzzerStopped" },
            { "진행 중인 이동/Jog를 정지(STOP/버튼 놓기)한 뒤 다시 선택하세요.", "extraDialog.remaining.review.stopBeforeDecision" },
            { "비전 사용을 종료하지 못했습니다. '비전 사용 종료'를 직접 누른 뒤 다시 선택하세요.", "extraDialog.remaining.review.endVisionFailed" },
            { "사용자 확인 요청을 처리할 장비 연결이 없습니다.", "extraDialog.remaining.review.noDecisionControl" },
            { "확인 요청을 처리하고 있습니다. Sequence 완료 응답을 기다립니다.", "extraDialog.remaining.review.confirmWaiting" },
            { "재실행/취소 요청을 처리하고 있습니다. Sequence 완료 응답을 기다립니다.", "extraDialog.remaining.review.cancelWaiting" },
            { "Auto 대기 중에는 창을 직접 닫을 수 없습니다. 확인 또는 취소/T ALIGN 재시작을 선택하세요.", "extraDialog.remaining.review.closeAutoBlocked" },
            { "동작 진행 중에는 화면을 닫을 수 없습니다. 먼저 STOP 또는 작업 완료를 확인하세요.", "extraDialog.remaining.review.closeBusyBlocked" },
            { "순서 확인 중 Die Map이 변경되었습니다. 창을 다시 열어 확인하세요.", "extraDialog.remaining.review.pickupMapChanged" },
            { "픽업 순서 확인", "extraDialog.remaining.review.pickupTitle" },
            { "현재 Review 상태에서는 픽업 순서를 적용할 수 없습니다.", "extraDialog.remaining.review.pickupBlocked" },
            { "설정으로 생성한 순서와 뷰어 순서가 다릅니다. 다시 확인하세요.", "extraDialog.remaining.review.orderMismatch" },
            { "Review에 반영된 순서가 뷰어와 다릅니다. 자동 시작 전에 다시 확인하세요.", "extraDialog.remaining.review.appliedOrderMismatch" },
            { "WAIT", "extraDialog.remaining.review.wait" },
            { "GOOD", "extraDialog.remaining.review.good" },
            { "NG", "extraDialog.remaining.review.ng" },
            { "SKIP", "extraDialog.remaining.review.skip" },
            { "NOT SET", "extraDialog.remaining.review.notSet" },
            { "Wait", "extraDialog.remaining.review.waitEnum" },
            { "Good", "extraDialog.remaining.review.goodEnum" },
            { "Ng", "extraDialog.remaining.review.ngEnum" },
            { "Skip", "extraDialog.remaining.review.skipEnum" },
            { "TopLeft", "extraDialog.remaining.review.topLeft" },
            { "TopRight", "extraDialog.remaining.review.topRight" },
            { "BottomLeft", "extraDialog.remaining.review.bottomLeft" },
            { "BottomRight", "extraDialog.remaining.review.bottomRight" },
            { "Center", "extraDialog.remaining.review.center" },
            { "Continuous", "extraDialog.remaining.review.continuous" },
            { "Step", "extraDialog.remaining.review.step" },
            { "Fine", "extraDialog.remaining.review.fine" },
            { "Coarse", "extraDialog.remaining.review.coarse" },
            { "Waiting", "extraDialog.remaining.init.waiting" },
            { "Disabled", "extraDialog.remaining.init.disabled" },
            { "Running", "extraDialog.remaining.init.running" },
            { "Done", "extraDialog.remaining.init.done" },
            { "Failed", "extraDialog.remaining.init.failed" },
            { "Reinitialize Required", "extraDialog.remaining.init.reinitialize" },
            { "PreActions", "extraDialog.remaining.init.pre" },
            { "PostActions", "extraDialog.remaining.init.post" },
            { "Home", "extraDialog.remaining.init.home" },
            { "Axis", "extraDialog.remaining.init.axis" },
            { "확인 불가", "extraDialog.remaining.init.unavailable" },
            { "현재 상태를 확인하지 못했습니다.", "extraDialog.remaining.init.unknownState" },
            { "스텝 초기화", "extraDialog.remaining.init.stepTitle" },
            { "전체 초기화", "extraDialog.remaining.init.allTitle" },
            { "초기화 시퀀스를 준비합니다.", "extraDialog.remaining.init.preparing" },
            { "초기화가 완료될 때까지 기다려 주세요.", "extraDialog.remaining.init.waitingComplete" },
            { "초기화 시퀀스를 시작합니다.", "extraDialog.remaining.init.starting" },
            { "초기화 시퀀스를 진행합니다.", "extraDialog.remaining.init.progressing" },
            { "스텝 초기화 진행 중", "extraDialog.remaining.init.steprunning" },
            { "전체 초기화 진행 중", "extraDialog.remaining.init.allrunning" },
            { "스텝 초기화 완료", "extraDialog.remaining.init.stepcompleted" },
            { "전체 초기화 완료", "extraDialog.remaining.init.allcompleted" },
            { "스텝 초기화 실패", "extraDialog.remaining.init.stepfailed" },
            { "전체 초기화 실패", "extraDialog.remaining.init.allfailed" },
            { "스텝 초기화 정지", "extraDialog.remaining.init.stepstopped" },
            { "전체 초기화 정지", "extraDialog.remaining.init.allstopped" },
            { "스텝 초기화 준비", "extraDialog.remaining.init.steppreparing" },
            { "전체 초기화 준비", "extraDialog.remaining.init.allpreparing" },
            { "스텝 초기화 가 완료되었습니다.", "extraDialog.remaining.init.stepcompletion" },
            { "전체 초기화 가 완료되었습니다.", "extraDialog.remaining.init.allcompletion" },
            { "스텝 초기화 가 실패했습니다.", "extraDialog.remaining.init.stepfailure" },
            { "전체 초기화 가 실패했습니다.", "extraDialog.remaining.init.allfailure" },
        };

        private static readonly DisplayTemplate[] Templates = new[]
        {
            new DisplayTemplate("보기 회전 {0}° · 확인용 맵", "extraDialog.remaining.viewer.rotation"),
            new DisplayTemplate("파일 목록 · {0}개", "extraDialog.remaining.viewer.filesCount"),
            new DisplayTemplate("읽기 완료 · 보기 {0}° · 확인용 맵", "extraDialog.remaining.viewer.readRotation"),
            new DisplayTemplate("{0} · {1}열 × {2}행\r\n존재 {3} · 빈 셀 {4}\r\n숫자 BIN은 원본 값으로 표시합니다.", "extraDialog.remaining.viewer.summary", 0),
            new DisplayTemplate("공정 방향 {0}° 참고", "extraDialog.remaining.viewer.processRotation"),
            new DisplayTemplate("보기 {0}° · {1}열 × {2}행", "extraDialog.remaining.viewer.mapDimensions"),
            new DisplayTemplate("출처: {0}\r\n현재 표시: {1} · 열 파일 형식: {2} · 형식 선택은 다음 파일 열기에 적용됩니다.", "extraDialog.remaining.viewer.sourceNotice", 0, 1, 2),
            new DisplayTemplate("{0} 실패 · {1}", "extraDialog.remaining.viewer.failure", 0),
            new DisplayTemplate("{0} 실패\r\n{1}\r\n\r\n원본 파일과 실제 공정은 변경되지 않았습니다.", "extraDialog.remaining.viewer.failureMessage", 0),
            new DisplayTemplate("{0}\r\n파일 열기로 직접 선택할 수 있습니다.", "extraDialog.remaining.viewer.noLocalMessage", 0),
            new DisplayTemplate("{0} 맵 {1}개의 설정을 저장했습니다. PENDING · FINAL APPLY / 장비 사용 차단", "extraDialog.remaining.create.savedPending"),
            new DisplayTemplate("{0} 맵 {1}개를 저장했습니다. 현재 승인은 PENDING입니다.\r\n{2} 확인 후 FINAL APPLY 하세요.", "extraDialog.remaining.create.saved", 2),
            new DisplayTemplate("개수 조건을 적용했습니다. 생성 결과 {0}개 · 시계 방향 {1}°.", "extraDialog.remaining.create.adjusted"),
            new DisplayTemplate("웨이퍼 맵을 생성했습니다. 생성 결과 {0}개 · 시계 방향 {1}°.", "extraDialog.remaining.create.generated"),
            new DisplayTemplate("맵 저장 실패: {0}", "extraDialog.remaining.create.saveFailed"),
            new DisplayTemplate("생성 실패: {0}", "extraDialog.remaining.create.generationFailed"),
            new DisplayTemplate("입력 조건 미적용: {0} 직전 미리보기를 유지하며 저장할 수 없습니다.", "extraDialog.remaining.create.inputFailed"),
            new DisplayTemplate("허용 원 영역 초과 {0}개: 설정 저장 가능 · 생성된 배치를 유지합니다. 현재 배치 필요 직경 {1} mm 이상.", "extraDialog.remaining.create.outsideReady"),
            new DisplayTemplate("허용 원 영역 초과 {0}개: 생성된 배치를 유지합니다. 현재 배치 필요 직경 {1} mm 이상.", "extraDialog.remaining.create.outsidePending"),
            new DisplayTemplate("{0} 초기값 {1} mm를 그대로 표시할 수 없어 입력하지 않았습니다. 0.001 mm 단위의 유효한 값을 입력하세요.", "extraDialog.remaining.create.initialValue", 0),
            new DisplayTemplate("실측 검증 아님 | {0}", "extraDialog.remaining.review.nonproductionStatus", 0),
            new DisplayTemplate("{0}개 Die가 선택되었습니다. 상태 변경 시 전체 선택 대상에 적용됩니다.", "extraDialog.remaining.review.selectedDies"),
            new DisplayTemplate("입력한 1-base 순번에 해당하는 Pickable Die가 없습니다. sequence={0}", "extraDialog.remaining.review.noSequence"),
            new DisplayTemplate("픽업 경로 Draft를 적용했습니다. Pickable Target={0}", "extraDialog.remaining.review.orderApplied"),
            new DisplayTemplate("픽업 경로 적용 요청에 실패했습니다. {0}", "extraDialog.remaining.review.orderFailed"),
            new DisplayTemplate("{0}개 Die 상태를 Review Draft에 적용했습니다: {1}", "extraDialog.remaining.review.stateApplied", 1),
            new DisplayTemplate("Die 상태는 Draft에 반영되었지만 외부 알림 처리에 실패했습니다. {0}", "extraDialog.remaining.review.stateNotifyFailed"),
            new DisplayTemplate("{0} Step Jog({1}) 요청 중입니다.", "extraDialog.remaining.review.stepJog"),
            new DisplayTemplate("{0} Jog 요청 중입니다. 버튼을 놓으면 정지 요청합니다.", "extraDialog.remaining.review.continuousJog"),
            new DisplayTemplate("사용자 확인 처리에 실패했습니다. {0}", "extraDialog.remaining.review.decisionFailed"),
            new DisplayTemplate("픽업 순서 확인/적용 실패: {0}", "extraDialog.remaining.review.pickupFailed"),
        };

        // Use only for text composed entirely from known UI notice lines, never for external error details.
        public static string DisplayOwnedLines(string originalText)
        {
            if (string.IsNullOrEmpty(originalText)) return originalText;
            string[] parts = Regex.Split(originalText, "(\r\n|\r|\n)");
            for (int i = 0; i < parts.Length; i += 2) parts[i] = Display(parts[i]);
            return string.Concat(parts);
        }

        public static string DisplayInitialization(string originalText)
        {
            if (string.IsNullOrEmpty(originalText)) return originalText;
            string key;
            if (Keys.TryGetValue(originalText, out key) &&
                (key.StartsWith("extraDialog.remaining.init.step", StringComparison.Ordinal) ||
                 key.StartsWith("extraDialog.remaining.init.all", StringComparison.Ordinal) ||
                 key == "extraDialog.remaining.init.preparing" ||
                 key == "extraDialog.remaining.init.waitingComplete" ||
                 key == "extraDialog.remaining.init.starting" ||
                 key == "extraDialog.remaining.init.progressing")) return Lang.T(key);
            return originalText;
        }

        public static void Bind(Control control, string originalText)
        {
            Lang.BindDisplay(control, originalText, Display);
        }

        public static string Display(string originalText)
        {
            if (string.IsNullOrEmpty(originalText)) return originalText;
            string key;
            if (Keys.TryGetValue(originalText, out key)) return Lang.T(key);
            foreach (DisplayTemplate template in Templates)
            {
                string translated;
                if (template.TryTranslate(originalText, out translated)) return translated;
            }
            if (originalText.IndexOf('\r') >= 0 || originalText.IndexOf('\n') >= 0)
            {
                string[] parts = Regex.Split(originalText, "(\r\n|\r|\n)");
                bool knownFirstLine = Keys.ContainsKey(parts[0]);
                if (!knownFirstLine)
                    foreach (DisplayTemplate template in Templates)
                    {
                        string ignored;
                        if (template.TryTranslate(parts[0], out ignored)) { knownFirstLine = true; break; }
                    }
                if (!knownFirstLine) return originalText;
                for (int i = 0; i < parts.Length; i += 2)
                    parts[i] = Display(parts[i]);
                return string.Concat(parts);
            }
            return Lang.Display(originalText);
        }

        private sealed class DisplayTemplate
        {
            private readonly string _key;
            private readonly string _prefix;
            private readonly Regex _pattern;
            private readonly int[] _translatedArguments;
            private readonly int _argumentCount;

            public DisplayTemplate(string originalTemplate, string key, params int[] translatedArguments)
            {
                _key = key;
                _translatedArguments = translatedArguments;
                var pattern = new StringBuilder("\\A");
                int offset = 0;
                foreach (Match placeholder in Regex.Matches(originalTemplate, @"\{(\d+)\}"))
                {
                    int index = int.Parse(placeholder.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (_prefix == null) _prefix = originalTemplate.Substring(0, placeholder.Index);
                    _argumentCount = Math.Max(_argumentCount, index + 1);
                    pattern.Append(Regex.Escape(originalTemplate.Substring(offset, placeholder.Index - offset)));
                    bool multiline = key.EndsWith("viewer.failure", StringComparison.Ordinal) ||
                        key.EndsWith("viewer.failureMessage", StringComparison.Ordinal) ||
                        key.EndsWith("create.saveFailed", StringComparison.Ordinal) ||
                        key.EndsWith("create.generationFailed", StringComparison.Ordinal) ||
                        key.EndsWith("create.inputFailed", StringComparison.Ordinal) ||
                        key.EndsWith("review.orderFailed", StringComparison.Ordinal) ||
                        key.EndsWith("review.stateNotifyFailed", StringComparison.Ordinal) ||
                        key.EndsWith("review.decisionFailed", StringComparison.Ordinal) ||
                        key.EndsWith("review.pickupFailed", StringComparison.Ordinal) ||
                        key.EndsWith("review.nonproductionStatus", StringComparison.Ordinal);
                    pattern.Append("(?<arg").Append(index).Append(multiline ? ">.*?)" : ">[^\r\n]*?)");
                    offset = placeholder.Index + placeholder.Length;
                }
                pattern.Append(Regex.Escape(originalTemplate.Substring(offset))).Append("\\z");
                _pattern = new Regex(pattern.ToString(), RegexOptions.CultureInvariant | RegexOptions.Singleline);
            }

            public bool TryTranslate(string text, out string translated)
            {
                translated = null;
                if (!string.IsNullOrEmpty(_prefix) && !text.StartsWith(_prefix, StringComparison.Ordinal)) return false;
                Match match = _pattern.Match(text);
                if (!match.Success) return false;
                object[] arguments = new object[_argumentCount];
                for (int i = 0; i < arguments.Length; i++)
                    arguments[i] = match.Groups["arg" + i].Value;
                // Only explicitly named UI arguments may be translated. Paths, counts, coordinates and SDK errors stay opaque.
                foreach (int index in _translatedArguments)
                    arguments[index] = Display((string)arguments[index]);
                translated = Lang.Format(_key, arguments);
                return true;
            }
        }
    }
}
