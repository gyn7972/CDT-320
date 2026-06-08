# tools/retired/ — 격리된 verify 스크립트 (삭제 아님, 복원 가능)

생성: 2026-06-08 (verify 스위트 재정립 Stage 2).

## 격리 사유
여기 36개 스크립트(`verify_handler_features.pl` + Handler 계열 `verify_stageN.pl`)는
**은퇴된 백업 repo(`D:/Work/CDT-320/QMC.CDT-320`)의 구 AI Handler 스캐폴딩** 기준으로 작성됐다.
현행 flat 트리 `D:/Work/source/QMC.CDT-320` 의 Handler 는 origin(팀) 대규모 머지로 교체된
**권위 코드베이스**라, 이 스크립트들이 검사하는 파일/심볼 체계와 불일치한다.

정찰(`docs/VERIFY_SUITE_INVENTORY.md`)·repoint 실행 결과 이들의 FAIL 은 전부
**(ii) 정당한 코드 진화**(파일 제거/이름변경/아키텍처 교체) 또는 **환경**(Handler exe 미빌드)였고,
**실제 회귀(iii)는 0건**으로 확정됐다. 따라서 코어 베이스라인에서 제외(격리)한다.

- 예: `StandardInterlocks`/`ExtendedInterlocks1-3`/`AlarmMaster`/`AlarmManager`/
  `InputStageUnit`/`TransferPickerUnit`/`OutputStageUnit`/`OperationPanelUnit`/
  `ResourceSensorsUnit`/`IonizerUnit`/`PostPnpTransferUnit`/`SubsetPageBase` 등은 현 트리에 부재.
- `MachineController.cs`(팀 4761줄)·`CDT320Machine.cs` 는 존재하나 구 AI 심볼명(`MaxRetries`/
  `JobQueue.Enqueue`/`BinCodeMap.ConvertToBinCode` 등)과 불일치.

## 상태
- **삭제가 아니라 이동**이다. 이력 보존(`git mv`). 복원하려면 `git mv tools/retired/verify_X.pl tools/verify_X.pl`.
- `verify_all.pl` 러너의 `@stages` 에서 제외됨(코어 베이스라인 비의존).

## 재작성 후보 시점
**Handler UI 디자이너 스윕**(QMC.CDT-320, 현재 범위 밖, 30+ 파일) 진행 시,
팀 권위 Handler 아키텍처 기준으로 이 체크들을 재작성/되살리는 것을 고려한다.

## 격리 목록 (36)
handler_features, stage3, stage5, stage7, stage8, stage9, stage10, stage11, stage12, stage13,
stage16, stage19, stage20, stage21, stage22, stage23, stage24, stage25, stage26, stage27,
stage28, stage29, stage30, stage31, stage32, stage43, stage44, stage45, stage46, stage47,
stage48, stage49, stage50, stage51, stage53, stage54
