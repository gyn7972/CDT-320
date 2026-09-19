# 레시피 변경 시 생산 상태 초기화 — 구현 프롬프트

작성일: 2026-09-06
상태: 전환·기동 초기화 및 승인된 시퀀스·인터락 연결 구현. Review 겹침 범위도 추가 승인받아 연결·검증한다. 실제 완료/검증 결과는 02 체크리스트와 04 인계 기록을 기준으로 한다.
연결 문서: [구현·검증 체크리스트](D:/Source/CDT-320_New/docs/recipe-change-material-reset/02-validation-checklist.md)
[시퀀스·인터락 변경 승인 범위](D:/Source/CDT-320_New/docs/recipe-change-material-reset/03-sequence-interlock-approval.md)

## 1. 작업 기준과 사용자 확정 사항

기준 저장소는 D:\Source\CDT-320_New이며 현재 master의 미커밋 변경까지 포함한 로컬 코드다.
최초 재분석 시 HEAD는 7961f2ba772ea90fc6b08035b0a1604784e3a4c8, 구현 착수 시 다시 확인한 HEAD는 478ffef077ff0b95bfa64e796e3970e4810b093d이다. 구현 시 파일과 diff를 다시 읽었으며 이전 분석 때의 코드를 덮어쓰지 않았다.
사용자는 작업 브랜치를 만들지 말고 현재 로컬 코드에서 진행하라고 명시했다. 브랜치 생성·전환, 다른 복사본 덮어쓰기, commit/push는 하지 않는다.
사용자가 03 문서의 전환 보호·다음 START 상태·실물 입력 3개 범위를 승인했다. 이어 다른 작업과 겹치는 InputStage Review 정리도 먼저 수정하도록 명시 승인했다. 이 범위를 넘는 시퀀스·인터락 또는 다른 작업 변경은 별도 승인 대상이다.
저장소 상시 규칙은 [AGENTS.md](D:/Source/CDT-320_New/AGENTS.md)를 따른다. 이 문서는 해당 기능의 구현 입력이며 상시 규칙을 대체하지 않는다.

사용자가 확정한 결과:
1. 다른 레시피로 실제 적용할 때 프로그램 재시작 없이 기존 생산 상태를 비운다.
2. 장비 동작 중이거나 실제 제품이 남아 있으면 변경을 차단한다.
3. 새 레시피를 먼저 읽고 검증한다. 사전검증 실패 시 기존 레시피와 Material을 유지한다.
4. 모든 위치의 Wafer/Die Material, 이전 공정 재개 단계, 맵·얼라인·임시 검사 정보를 초기화한다.
5. 새 레시피의 카세트 구성으로 빈 Material을 생성하고 Front/Rear FLOW 강제 ON 승인을 모두 해제한다.
6. 저장 완료를 확인한 뒤 적용 성공을 표시한다. 다음 START는 새 Material 기준의 준비·매핑부터 시작한다.
7. 진행 중 LOT, Teaching·보정값, 원점 완료 상태는 유지한다.
8. 같은 레시피의 설정 저장·재적용과 시작 시 '기존 Material 사용 → 예'에는 이 전체 초기화를 적용하지 않는다.
9. 시작 시 '아니오'와 레시피 변경이 동일한 생산 상태 초기화 핵심 처리를 사용하도록 한다.

여기서 '재시작과 같은 환경'은 생산 데이터와 휘발성 공정 상태의 초기화를 의미한다.
Form1/Controller/드라이버를 재생성하거나, 프로세스를 재실행하거나, 축 원점복귀·IO 출력·자동 START를 수행하라는 요구가 아니다.

## 2. 구현 전 로컬 코드 재분석 근거

아래 위치는 작성 시점 기준이며, 구현 시에는 메서드명으로 다시 찾는다.

| 근거 | 현재 동작 | 구현에 반영할 점 |
|---|---|---|
| [Form1.PromptMaterialRecoveryOnStartup](D:/Source/CDT-320_New/QMC.CDT-320/Form1.cs:1278) | '예'는 Snapshot 복원, '아니오'는 InitializeForRecipe(1,1,25,25) | 복원 경로와 새 상태 시작 경로를 명시적으로 구분 |
| [Form1의 LOT 복원 순서](D:/Source/CDT-320_New/QMC.CDT-320/Form1.cs:1019) | Material 선택 이후 LOT 복원. 초기화 전 LOT ID도 보관 | Material을 비워도 현재 LOT은 유지 |
| [InitializeMaterialStateFromRecipe](D:/Source/CDT-320_New/QMC.CDT-320/Form1.cs:1472) | 새 Snapshot, 레시피의 Input/Good 레벨 구성, 활성 LOT 연결 | 공통 초기화 핵심으로 옮길 기존 동작 |
| [MaterialStorage.CreateDefaultState](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStorage.cs:23) | Input1/2, Good1/2, Ng1의 빈 카세트 구조 생성 | 기존 구조를 부분 삭제하는 대신 새 구조 생성 |
| [ProjectPage.OnApplyCurrent](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Pages/Recipe/ProjectPage.cs:729) | Material 잔재 확인창 이후 ForceClear 실행, 그 뒤 Project 저장·적용 | 삭제를 대상 레시피 검증보다 먼저 수행하지 않도록 순서 변경 |
| [LoadMachineRecipe / ApplyMachineRecipe](D:/Source/CDT-320_New/QMC.CDT-320/Form1.cs:279) | LoadMachineRecipe 종료 시 적용 lease 해제. Material 문맥·LOT·마커 저장은 상위에서 계속 수행 | 초기화 및 저장 확정까지 전환 전체를 독점 보호 |
| [Machine.LoadRecipe / ValidateRecipe](D:/Source/CDT-320_New/QMC.Common/Machine.cs:140) | 전체 노드 ValidateRecipe 후 로드. 필수 노드 누락·손상은 실패 | 이미 있는 전체 검증을 앞에서 재사용. Project 파일 존재만 확인하지 않음 |
| [CompleteRecipeApplyContext](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/MachineController.Recipe.cs:569) | 빈 장비이면 Material.RecipeName만 갱신 | 정상 A→B에서도 생산 초기화가 필요 |
| [ClearAllMaterialForRecipeChange](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.cs:963) | Wafer/Die 제거, BIN All, 슬롯 점유·매핑 제거. 카세트 구성 자체는 보존 | 새 레시피 기준의 전체 초기화와는 다름 |
| [SequenceResumeStore](D:/Source/CDT-320_New/QMC.CDT-320/Sequencing/Common/SequenceResumeStore.cs:224) | 프로세스 내 static Dictionary. ClearAll 제공 | 레시피 전환 시 이전 재개 단계 제거 |
| [START의 시퀀스 생성](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/MachineController.SequenceControl.cs:124) | 다음 START에서 SignalBus/Context/Coordinator 새로 생성 | 실행 중 컨텍스트나 물리 안전 lease를 임의 해제할 필요 없음 |
| [TryFlushPendingSave](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.Save.cs:55) | 목표 상태 세대가 디스크에 저장됐는지 확인 | NotifyAndSave 요청 성공만으로 적용 완료 처리하지 않음 |
| [RecipeStore.SaveLastProjectName](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs:289) | 저장 예외를 catch로 삼킴 | 마커 저장 실패를 이번 적용 결과로 확인할 수 있도록 보완 필요 |
| [AppSettingsStore.Save](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/AppSettings.cs:469) | 저장 실패를 호출자에게 전달하지 않음 | LastProject 저장 실패를 성공으로 처리하지 않음 |

현재 강제 변경의 실물 확인은 Ring·돌출 센서 중심이며 Front/Rear Picker FLOW를 읽지 않는다.
[TryReadRecipePresenceSensors](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/MachineController.Recipe.cs:918)의 확인 범위를 보완 대상으로 검토한다.
실제 FLOW OFF만으로 픽커가 비었다고 단정할 수 없다. 사용자가 보고한 '실제 다이 보유 + FLOW OFF' 사례가 있으므로, Material 잔재 정리는 실제 제품 제거를 확인한 경우에만 허용한다.
FLOW 강제 ON은 생산 판정에만 쓰며, 레시피 전환의 실물 확인 근거를 대신하지 않는다.

## 3. 적용 이유를 구분하는 계약

명칭은 제안이다. 기존 구조에 맞춰 구현하되 아래 구분은 유지한다.

| 상황 | 전체 생산 초기화 | 처리 |
|---|---|---|
| RecipeSwitch: 활성 A에서 다른 B로 정상 전환 | 수행 | 사전검증·실물 확인 후 새 B 기준 상태로 전환 |
| StartupFresh: 시작 시 '아니오' 또는 기존 새 상태 시작 경로 | 수행 | 레시피 결정 및 LOT 복원 순서를 고려해 공통 핵심 사용 |
| StartupRestore: 시작 시 '예' | 수행 안 함 | 저장 Material/LOT 복원 및 기존 정합성 검사 유지 |
| ReapplyCurrent: 같은 레시피 저장·재적용 | 수행 안 함 | 기존 동작 중/실물/Alarm 검사 유지. 전체 삭제로 통과시키지 않음 |
| 목록 선택·Open·Reload·Save As 등 편집만 한 경우 | 수행 안 함 | 활성 Recipe 전환이 확정되지 않은 동작에는 연결하지 않음 |
| 시작 복원에서 저장 Material의 원래 Recipe를 적용하는 특례 | 수행 안 함 | StartupRestore 문맥 안에서 보존 |

같은 이름 판정에는 기존 NormalizeRecipeName과 대소문자 비교 정책을 재사용한다.
기동 시 ActiveRecipeName이 빈 문자열에서 마지막 Recipe로 설정되는 것도 단순 A→B 조건으로 처리하지 않는다.
일반 사용자 RecipeSwitch는 Material.RecipeName이 대상 이름과 같다는 이유만으로 기존 materialRecipeRestore 특례에 흘러 들어가 초기화를 건너뛰지 않는다. 이 전환은 실물 제거 확인 후 전체 초기화하며, '예'로 복원하는 기동 문맥만 별도로 보존한다.
현재 같은 레시피 적용에서도 materialOnlyBlock이면 강제 정리 분기로 들어갈 수 있으므로, 새 요구의 '같은 레시피에는 전체 초기화하지 않음'과 충돌하지 않게 분기를 명확히 한다.

## 4. 권장 구현 구조와 실행 순서

UI는 입력 수집·확인창·결과 표시를 맡고, 전환 순서는 Controller/Service의 공통 진입점에서 수행한다.
MaterialStateService에는 '새 상태 생성/교체'를, Controller 쪽에는 '공정 임시 상태 정리 및 전환 조정'을 둔다.
기존 startup 함수를 UI 이벤트에서 그대로 재호출하거나 기존 ForceClear에 초기화 코드를 여기저기 덧붙이지 않는다.

1. 요청 목적과 이전/대상 Recipe를 고정한다. 같은 레시피인지 먼저 분류하되 기존 운전 차단 조건은 유지한다.
2. 대상 Project와 Machine/Unit/Component 필수 Recipe 전체를 검증한다. 저장이 필요한 Project 편집값도 후보로 먼저 검증하되, 실제 파일 저장은 작업자 확인 및 재검증 뒤 적용 보호 안에서 수행한다. 취소 시 Project·Material·활성 Recipe·마지막 Recipe 포인터를 바꾸지 않는다.
3. 기존 레시피 적용 작업 보호를 사용해 START 및 다른 적용과의 경합을 차단한다. Material 수동 편집/Review/수동 모션이 이 보호 밖으로 들어올 수 있는지도 확인한다.
4. 완전 정지와 실물 제거를 확인한다. Material 잔재 정리에 대한 기존 작업자 확인 절차를 활용한다. 대화상자 이후 상태·Recipe·Material 세대를 다시 확인한다. '아니오'/닫기에서는 변경하지 않는다.
5. 기존 Recipe 이름과 Material 참조·세대를 고정하고 대상 Recipe의 빈 Material 후보를 먼저 생성한다. 후보 작성 중 활성 State를 바꾸지 않는다. Project/Unit 파일 해시를 확인 전후와 적용 완료 시 비교한다. Project 저장 후에는 의도적으로 쓴 Project만 새 기준으로 받고 Unit 파일 변경은 거절한다.
6. 대상 Recipe 적용과 공정 상태 초기화를 수행한다. 적용 레시피/새 Material/활성 LOT이 일치하는 상태로 교체하고 파생 캐시를 무효화한다. 변경되지 않은 옛 Snapshot에 새 Recipe 이름만 붙여 끝내지 않는다.
7. StateChanged를 통해 UI를 갱신하되, 세대 만료·승인 해제는 지연 UI 이벤트에 의존하지 않고 동기적으로 확정한다.
8. 새 Material Snapshot의 저장 완료와 마지막 Recipe 포인터·필요 설정의 저장 결과를 확인한다. 성공 조건은 ActiveRecipeName, Material.RecipeName, .last_project, AppSettings.LastProject가 대상 Recipe로 일치하고 새 Material이 저장된 것이다.
9. 새 Recipe에 대한 운전 준비 확인을 무효화하고 기존 Output GOOD/NG 전체 준비 요청을 유지한다. 다음 START는 기존 INIT/안전/Recipe/Vision ACK 검사 후 새 Material에 맞는 준비·매핑 경로로 진입한다.
10. 필요한 갱신과 저장이 모두 끝난 뒤 성공을 표시하고 작업 보호를 해제한다. 실제 축 이동이나 자동 START를 이 전환 함수에서 호출하지 않는다.

Machine.ValidateRecipe와 이후 Load 사이에도 파일이 바뀔 수 있다. 적용 도중 오류를 별도로 처리한다.
기존 Material을 먼저 지운 뒤 대상 파일 오류를 발견하는 현재 UI 순서를 남겨두지 않는다.
새로운 bool/enum/결과형의 구체 명칭은 구현 선택이며, 결과에 실패 단계와 전환 확정 여부를 표현할 수 있어야 한다.

## 5. 초기화 범위와 보존 범위

| 영역 | 초기화할 상태 | 구현 근거/주의 |
|---|---|---|
| Material 전체 | 모든 위치의 Wafer·Die, 예약/보유·검사·매핑/Review 정보, 슬롯 점유, 카세트 매핑·존재 기록, Pickup BIN 선택 | 새 Snapshot으로 교체. CSV 등 파일 이력을 삭제한다는 의미가 아님 |
| 카세트 구성 | 새 Recipe의 Input/Good 레벨 활성화와 빈 슬롯 구조 | 현재 startup은 Input/Good 각각 1~2레벨 및 25/25 슬롯 사용. 별도 슬롯 설정 계약이 있으면 동일 해석을 양쪽에서 공유 |
| Material 파생 상태 | DieId 인덱스, InputPick 캐시, UI 조회 캐시 | _stateSync 및 기존 Snapshot/세대 확인 정책 활용 |
| InputStage | CurrentWaferMaterial, CurrentWaferMap, 현재 wafer origin/pitch/얼라인 결과 | ClearCurrentWaferMaterial / ClearCurrentWaferMap 재사용. 영속 Teaching/보정값과 구분 |
| Controller 입력 맵 | _inputDieMap, 픽업 순서, LotStorage.ActiveInputDieMap | [ClearInputDieMap](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/MachineController.cs:334) 재사용 |
| 시퀀스 재개 | SequenceResumeStore, SequenceFailureStore, UI에 남은 공정 진행 표시 | 완전히 종료된 공정에 대해서만 정리. 실제 Alarm 원인을 삭제하는 것으로 대체하지 않음 |
| 임시 검사 문맥 | InputStageHybridResultSession, 양쪽 InputCamera 선행검사/픽업 허가, VisionDieAddressStore, 검사 재시도 상태 | 각 소유자의 Clear API를 사용. 실행 중 비전 작업이 남으면 적용을 차단하거나 정상 종료 완료 후 진행 |
| InputStage Review | 새로 추가된 세션/요청 세대, geometry 검증 증거, 지연된 UI/검사 callback | Form1.InputStageRunReview*.cs와 MaterialStateService.InputStageReviewGeometry.cs의 현행 세대 검증 보존. 옛 요청이 새 다이에 승인/결과를 쓰지 못하게 함 |
| 픽커 FLOW 복구 | 양쪽 P1~P4 승인 및 확인 중인 요청 세대 | ClearAllPickerFlowRecoveries(reason)로 명시적 해제. StateChanged 지연이나 화면 Refresh에 의존하지 않음 |
| Unit/UI 투영 | 카세트 슬롯·Stage·Picker 표시, 이전 wafer 지도 참조 | 새 Material 기준 재조회/동기화. 실제 센서/실린더 상태를 빈 값으로 덮어쓰지 않음 |

보존할 것:
- LotSessionService의 활성 LOT과 LOT 이력·통계. 활성 LOT이 없으면 새 Material.LotId도 비우고 Recipe에 저장된 과거 LotId를 부활시키지 않는다.
- 새 레시피에서 정상 로드하는 Teaching/파라미터와 장비 Calibration·영속 학습 보정값. 초기화 목적으로 0/default를 쓰지 않는다.
- 실제 원점 완료 상태, 장비 초기화 완료 상태, 축/IO/실린더 물리 상태, 기존 안전 조건.
- 생산 CSV/Log/검사 파일 이력 및 Recipe 파일 자체. 이번 기능은 운영 폴더 일괄 삭제를 포함하지 않는다.
- 사용자가 보존을 요청한 PickerPickUpSequence.MotionResolvers.cs의 VerifyPickerHasDieDataAndFlowAfterPick 내부 flowOn = true; Test 코드.
- Sim/DryRun의 기존 판정과 모션 계약. 가상 모드에서도 레시피 전환의 논리 초기화 범위는 동일하게 검증한다.

'Runtime'이 이름에 있다고 모두 지우지 않는다. PickRuntimeOffsetService/PlaceRuntimeOffsetService 등은 시작 때 저장값을 다시 읽는 영속 보정도 포함한다.

## 6. 시작 시 '아니오'와 공유할 때의 순서

현재 순서는 Material 복원 선택 → LOT 복원 → 마지막 Recipe 적용 → 필요 시 레시피 기준 Material 재생성이다.
초기화 전에 보관하는 _materialSnapshotLotIdBeforeInitialization을 유지하고, 활성 LOT 복원이 끝나면 새 Material에 그 LOT을 연결한다.
레시피가 아직 결정되지 않은 초기 단계와 대상 Recipe가 검증된 최종 단계를 구분한다. 필요 시 빈 기본 상태 생성과 최종 Recipe 기준 초기화를 나누되 핵심 생성·정리 정책을 중복 구현하지 않는다.
StartupRestore('예')에서는 공통 전체 초기화를 호출하지 않는다.
ApplyStartupMachineRuntimeState를 레시피 변경 처리에서 다시 실행하지 않는다. 사용자가 합의한 원점/INIT 상태 보존과 충돌한다.

## 7. 실패·저장·동시성 처리

- 사전검증 실패/사용자 취소: 기존 Recipe, Material, 진행 상태, 마지막 Recipe 기록 유지.
- 적용 중 실패: 실패 단계와 초기화 대상 이름을 기록하고 START를 차단한다. 이전 레시피를 임의로 다시 로드하거나 로컬 코드를 롤백하지 않는다. 실패한 대상 이름으로 재시도할 때도 전체 초기화·저장을 완료해야 차단을 해제한다. 부분 적용을 완료로 표시하지 않는다.
- 새 Snapshot 저장 실패: NotifyAndSave 호출만으로 성공 판정하지 않는다. TryFlushPendingSave 결과를 확인한다.
- .last_project 또는 LastProject 설정 저장 실패: 현재 void 저장 메서드가 예외를 삼키므로 이번 경로에서 결과를 확인할 수 있는 작은 변경을 설계한다. 다른 설정 전체의 저장 체계를 무관하게 재작성하지 않는다.
- 복수 파일 저장은 한 파일의 File.Replace만으로 전체 원자성이 보장되지 않는다. 전환 중 종료되어 Recipe 포인터와 Material이 불일치하면 기존 정합성 게이트로 자동 운전을 막고 복구 경로를 남긴다.
- 이전 Snapshot의 지연 저장이 새 상태를 덮지 않도록 기존 상태 세대/Revision/저장 watermark를 유지한다. 새 Snapshot의 기본 Revision 값을 이유로 프로세스 전역 저장 세대를 0으로 초기화하지 않는다.
- _stateSync 안에서는 상태 생성/교체·캐시 무효화만 짧게 처리한다. UI 확인창, 파일 flush 대기, 비전 통신을 이 락 안에서 실행하지 않는다.
- 이전 Review/검사/승인 callback은 무효화된 세대 또는 옛 State 참조임을 확인하고 중단해야 한다. 공유 존·phase lease를 강제로 풀어 정지한 것처럼 꾸미지 않는다.
- 로그에는 호출 목적, 이전/대상 Recipe, LOT, 초기화한 Wafer/Die 수, 수행 단계, 저장 성공/실패 및 차단 사유를 기록한다.

## 8. 예상 변경 지점과 회귀 보호

주요 대상: Form1.cs, Ui/Pages/Recipe/ProjectPage.cs, Equipment/MachineController.Recipe.cs, Equipment/Materials/MaterialStateService 관련 partial 및 필요 시 새 공통 초기화 파일.
저장 결과 전달 보완: Equipment/Recipes/RecipeStore.cs, Equipment/AppSettings.cs의 요청과 직접 관련된 경로.
연동 검토: InputStage Unit/Controller 맵, InputStage Review 세대, SequenceResumeStore/FailureStore, 임시 검사/픽업 허가·비전 주소, PickerFlowRecovery.
시작 시 새 상태 경로와 레시피 전환 경로를 같은 핵심으로 묶되, 모든 Save/Load 진입점에 무조건 초기화를 삽입하지 않는다.
새 .cs 파일은 현재 csproj의 명시적 Compile 목록에 등록한다.

최초 분석 때 미커밋이던 FLOW 복구·InputStage Review geometry·이력 UI 코드는 구현 착수 시 현재 HEAD에 포함되어 있었다. 이후 다른 작업이 InputStage Review/InputPick/픽업 순서 UI를 수정하고 있다. 겹침을 발견해 승인받았으며, Review 저장 작업 추적을 위한 최소 연결 외에는 다른 작업의 구현을 덮어쓰거나 되돌리지 않는다.
AppSettings.cs, MachineController.cs, Material 관련 partial, InputStage Unit, Form1.InputStageRunReview 관련 파일, csproj는 현재 구현을 읽고 필요한 부분만 수정한다.

## 9. 완료 기준

연결된 체크리스트의 구현 및 로컬 검증을 실제 결과로 갱신한다.
현재 로컬 솔루션의 별도 OutDir /t:Build와 관련 기능 검증을 수행한다. 원본 Clean/Rebuild 및 운영 폴더로 배포하지 않는다.
하드웨어 없는 테스트에서 상태 생성·목적 분기·실패 경계·저장 세대·늦은 callback을 검증한다. 실제 Form1/장비 생성자 또는 운영 Snapshot 경로를 사용하지 않는다.
기존 FLOW 복구의 이전 183개 검증 결과는 이번 초기화 기능의 통과 증거가 아니다. 이번 변경에 맞는 검증을 별도로 수행한다.
현장 운전 검증은 별도 미실시 상태로 남기며, 자동으로 완료 처리하지 않는다.
