# Manual Sequence OUTPUT ALL

`GOOD / NG / ALL`에서 ALL을 선택한 뒤 OUTPUT LOAD 또는 OUTPUT UNLOAD를 누르면 한 번의 수동 작업으로 양쪽을 순차 처리한다. 신규 공급 및 Stage 배출 기본 순서는 Auto와 같은 **NG → GOOD**이며, Feeder에 진행 중인 자재가 있으면 허용된 잔류 이송을 먼저 처리한다.

- LOAD: 이미 정상 로딩된 Stage는 유지하고, 빈 Stage에 각 Side의 다음 Ready Bin을 공급한다. NG 미사용 설정에서는 신규 NG 공급을 제외한다.
- UNLOAD: 자재가 있는 Stage를 원본 카세트/슬롯으로 배출한다. 미완료 Stage Bin도 기존 개별 수동 배출 정책을 따른다. 재공급은 하지 않는다.
- ALL의 슬롯 선택은 자동으로 고정된다. GOOD/NG 개별 선택으로 돌아오면 해당 Side의 기존 선택을 가능한 범위에서 복원한다.
- 처리할 자재가 없으면 이동하지 않고 `처리할 대상 없음`을 표시한다. 작업 중 중복 실행 및 대상 변경을 차단한다.

## 실행과 정지

Controller는 `RunManualUnitProcessAsync`를 한 번만 사용한다. 양쪽에 같은 Context, Manual 속도와 취소 토큰을 적용하고, 기존 `OutputSequence`의 수동 이송과 물리 인터락을 재사용한다. All을 위해 Auto 모드로 바꾸거나 Auto의 언로드 후 즉시 재공급 경로를 활성화하지 않는다.

각 이송의 성공 직후 Runtime/Material을 확정 저장한다. 다음 동작 전 취소·알람·센서·Material을 다시 확인한다. 실패·정지하면 다음 Side를 시작하지 않으며, 이미 완료한 Side와 실패 원인을 화면에 함께 표시한다. 저장 경고도 기존 알람 정책에 따라 진행을 중단하고 저장 원인을 보존한다. 일반 정지 후에는 기존 READY 절차와 실제 자재 확인을 거쳐 다시 실행한다.

## 시작 차단 및 완료 확인

- Feeder와 같은 Side Stage에 자재가 함께 있으면 공유 이송을 시작하지 않는다.
- LOAD에서 완료 Feeder Bin, UNLOAD에서 미완료 Feeder Bin은 기존 수동 정책에 따라 차단한다.
- NG 미사용 상태의 NG 잔류 자재는 신규 로딩에서 무시하지 않는다. UNLOAD에서는 원본 카세트의 기존 반환 조건을 통과해야 한다.
- 실제 Ring 센서와 Material 불일치, 중복 자재, 잘못된 원본 카세트/슬롯/물리 식별자를 차단한다. Sim/DryRun은 기존 이송 센서 확인 API의 가상 상태 정책을 따른다.
- 이미 점유된 Stage를 LOAD에서 건너뛸 때 Guide Down, Clamp Lift Up, Clamp 및 설정상 필요한 바코드 확인을 검증한다. 불완전한 Stage를 임의 복구 이동하지 않는다.
- 실제 LOAD 완료는 GOOD Y/Z Process, NG Y 및 Vision X Avoid와 Feeder/Cassette 안전 자세를 확인한다. UNLOAD 완료는 Stage 축 모두 Avoid를 확인한다. 실패 시 Side별·공통 Ready 신호를 해제한다.

Feeder의 실제 반환은 끝났지만 Material 확정만 남아 Ring OFF/Material 점유 상태인 특수 복구는 ALL의 센서 일치 검사에서 차단된다. 이 경우 기존 개별 복구 절차로 자재 상태를 일치시켜야 한다. 실제 장비 상태를 확인하지 않고 저장 데이터만 변경하면 안 된다.

## 변경 진입점

- `Ui/Dialogs/ManualSequenceDialog.cs` 및 `.Designer.cs`: 선택/확인/진행 표시.
- `Equipment/MachineController.ManualOutputBatch.cs`: 단일 수동 실행과 체크포인트 저장.
- `Sequencing/ManualOutputBatchRunner.cs`: 최대 세 번의 순차 작업과 중단·부분 완료 관리.
- `Sequencing/OutputSequence.ManualBatch.cs`: 실제 상태 확인, 기존 이송 호출, 최종 자세 검증.

## 장비 없는 검증

`tests/run-batch-runner-tests.ps1`은 실제 Runner와 SequenceStopException 소스를 독립 컴파일하여 순서·동일 토큰·실패·정지·저장·잔류 자재·최종 상태 29개 시나리오를 검사한다. `tests/run-adapter-tests.ps1`은 실제 OutputSequence.ManualBatch 소스와 메모리 경계 대역으로 센서·실린더·바코드·Material·이송 호출·Ready 해제 등 19개 시나리오를 검사한다. 실제 장비 객체나 하드웨어 DLL을 사용하지 않는다.

2026-09-06 기준 위 48개 자동 검증과 실제 Dialog 소스를 사용한 독립 UI 검증 16개가 통과했다. UI 검증은 기본/상호 배타 선택, 슬롯 복원, API 연결, 실행 중 조작 차단, 지연 진행 알림, 부분 완료/대상 없음 표시와 화면 배치를 확인했다.

솔루션 컴파일은 AGENTS.md대로 별도 OutDir/중간 출력 폴더의 `/t:Build`만 사용한다. 현장에서는 저속으로 양쪽 LOAD/UNLOAD, 한쪽만 처리, NG 미사용, 잔류 Feeder, Side 사이 정지, 원본 슬롯 사용 불가, 두 번째 Side 실패 후 첫 번째 상태 보존을 확인해야 한다. 실제 축/IO 구동은 오프라인 검증에 포함하지 않는다.
