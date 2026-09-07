# Input Wafer 바코드 LOT 검사 검증 기록

최종 기록일: 2026-09-08. 장비 공통 Config의 LOT 접두어 검사와 바코드·맵 오류의 얼라인 전 복구를 검증했다. 격리 회귀에서 정책·Config 34건, 복구창 8건, 맵 사전검사 28건, 바코드 흐름 48건이 모두 통과했다. 별도 기존 RAD 파서 회귀 78건과 전체 솔루션 빌드도 통과했다. 실제 장비·원격 통신은 미수행이다.

## 정책 및 공통 Config

| 항목 | 기대 결과 | 상태 | 근거 |
|---|---|---|---|
| 검사 OFF | N·LOT·바코드 사용 상태와 관계없이 기존 동작 유지 | 통과 | `disabled-preserves-legacy` |
| 접두어 일치 | 앞 N글자 OrdinalIgnoreCase 비교, N=1·3·5·128 허용 | 통과 | 일치·대소문자·길이 경계 회귀 |
| 접두어 불일치 | 실패와 원인 반환 | 통과 | `different-prefix-is-rejected` |
| 바코드 OFF와 검사 ON | 실패 | 통과 | `barcode-off-is-rejected` |
| N 범위 오류 | 0·음수·129·정수 최솟값/최댓값 거부 | 통과 | `invalid-length-*` |
| LOT·바코드 없음/길이 부족 | 실패 | 통과 | null·빈값·공백·짧은 문자열 회귀 |
| 정규화 경계 | 앞뒤 Trim, LOT 내부 공백을 임의 삭제하지 않음 | 통과 | Trim 및 내부 공백 회귀; scanner 정규화는 기존 호출부 소유 |
| 같은 LOT의 다른 웨이퍼 | N 이후 `.02`/`.03` 차이를 추가 검사하지 않음 | 통과 | `same-lot-other-wafer-is-allowed` |
| 새 Config 기본값 | false / 5 | 통과 | `new-config-defaults` |
| 구 키 누락 | false / 5, 한쪽 키만 없어도 해당 기본값 복원 | 통과 | 실제 Config 데이터 선언부 DataContractJsonSerializer 회귀 |
| 저장된 잘못된 N | 0·음수·129 보존, 활성 검사에서 거부 | 통과 | explicit 값 복원 및 정책 검사 |
| 공통 Config 저장 및 레시피 독립성 | Config true/5·false/3 저장 복원, Recipe에는 검사 설정 없음 | 통과 | `common-config-roundtrip`, `recipe-switch-does-not-own-common-policy` |
| 실제 InputStage Config | 새 키 누락 기본값 적용, 기존 설정 로드, 원본 SHA256 동일 | 통과 | `D:\Source\DATA_LOG\Log\EquipmentData\Config\InputStageUnit.json` 읽기 전용 회귀 |
| UI 입력 범위 | 사용 여부·N 모두 공통 Config 저장, N=1~128 검증 후 변경 | 정적 확인 | `AddBarcodeLotPrefixSettingItems`, 기존 ParameterGrid 오류 표시·저장 경로 사용 |

## 바코드·맵 복구 통합

| 항목 | 기대 결과 | 상태 및 근거 |
|---|---|---|
| 바코드 적용 시점 | 정규화·접두어·맵 검사 및 문맥 재확인 후 Material 반영 | 통과: 실제 후보 검증 메서드 회귀, 오독 후보 commit 0·최종 정상 후보 commit 1 |
| 바코드 수동 확인 | 반복 오입력도 같은 정책으로 검사하고 성공 후 진행 | 통과: 최초 Working 상태 정정, 오입력→오입력→정상값 회귀 |
| 맵 확보 오류 | 재시도·수동 확인·취소를 얼라인 전 단계에서 처리 | 통과: 동일 후보 Retry 재검사, 수정된 키 맵 조회, 취소/미완료 응답 commit 0 |
| 재시작 경로 | 확정값도 현재 LOT·Config·맵 조건 재검사 | 통과: 동일 확정값 commit 0, LOT·Config·레시피·BIN·물리 세대 변경 시 차단 |
| 얼라인 선행 차단 | 바코드·맵 확인 전 이동 Step 미진입, 수동/STEP은 확정값 검사만 수행 | 정적 확인 및 실제 메서드 회귀 통과: Auto 게이트, Align CheckUnit 유지, `allowRecovery: false`의 창/commit 0 |
| 후단 정정 차단 | 얼라인 진행 스텝·결과·맵/리뷰 결과·픽업 예약/이력 보유 시 정정 금지 | 통과: 결과 플래그 없는 얼라인 모션 재개 포함; Working 및 CheckUnit/Idle은 최초 입력 허용 |
| 사용 설정 조합 | prefix OFF에도 네트워크 맵 검사, 네트워크 OFF에도 prefix 검사 | 통과: 두 검사 OFF의 바코드 단독 모드, 바코드 OFF+필수 검사 ON 차단 포함 |
| 오류 복구 대화상자 | 편집·재시도·취소 결과와 실패 표시 동작 | 통과: 실제 Dialog 소스 격리 회귀 8/8, 장비 연결 없음 |
| 맵 사전검증 | 원격 조회·파싱·피치·BIN 오류 처리 및 활성 데이터 비변경 | 통과: 실제 서비스 소스와 경계 대체 구현을 사용한 28/28 |
| 기존 RAD 파서 | 기존 레코드/수량 정책 유지 | 통과: 합성 55+실파일 23=78/78, 실파일 SHA256 보존 |
| 전체 솔루션 빌드 | 별도 OutDir `/t:Build`, 운영 출력 보호 | 통과: Debug / Any CPU, exit 0, 오류 0·기존 경고 41, 이번 변경 파일 경고 없음 |

## 재현 및 제한

[run-policy-tests.ps1](tests/run-policy-tests.ps1)은 production policy와 `InputStageUnit.cs`의 실제 Config 및 레시피 데이터 선언부를 추출·격리 컴파일한다. 하드웨어를 가진 `InputStageUnit` 클래스는 생성하거나 실행하지 않는다. `IRecipeData`, Vision 식별자 상수는 실제 production 선언을 사용한다.

[run-dialog-tests.ps1](tests/run-dialog-tests.ps1), [run-map-preflight-tests.ps1](tests/run-map-preflight-tests.ps1), [run-barcode-flow-tests.ps1](tests/run-barcode-flow-tests.ps1)은 각각 실제 Dialog, 맵 사전검증 서비스, 후보 검증·복구 메서드를 격리 컴파일한다. 후보 회귀는 `InputFeederLoadToStageSequence.cs`의 검증·문맥·공정 시작 판정·정규화 메서드를 실행할 때마다 추출하고 실제 순수 정책과 함께 실행한다. Material 저장·맵 조회·복구 응답·재개 상태는 메모리 대체 구현이므로 장비 모션 전체를 실행한 검증은 아니다.

맵 사전검사 회귀의 다운로드·파일 파싱 경계도 대체 구현을 사용한다. 실제 파서 동작은 별도 [RAD 파서 회귀](../wafer-map-count-validation/tests/run-tests.ps1) 78건으로 보완했다. 따라서 신규 118건은 실제 원격 통신이나 장비 전체 시뮬레이션 통과를 뜻하지 않는다.

```powershell
& .\docs\input-wafer-barcode-validation\tests\run-policy-tests.ps1
& .\docs\input-wafer-barcode-validation\tests\run-dialog-tests.ps1
& .\docs\input-wafer-barcode-validation\tests\run-map-preflight-tests.ps1
& .\docs\input-wafer-barcode-validation\tests\run-barcode-flow-tests.ps1
```

최종 신규 회귀 결과: 성공 118(34+8+28+48), 실패 0, 각 실행 exit 0. 실제 InputStage 공통 Config 1개 원본 SHA256 전후 동일이다. 후보 회귀 대상 전체 시퀀스 소스 SHA256은 `4B3F587B70615D0CFD6DE68C582A85AD275FD1D3C9FFA9949AA02A54FE6E9427`, 정책 소스는 `C74DA7A7F8A356037B7775606E614A38C58E5C23ABCA07AEB98901739505B9F4`이며 테스트 전후 동일했다.

검증 산출물 정리는 부분 완료다. 테스트 GUID별 임시 폴더는 정리되어 잔여 0개다. 별도 빌드 출력 `_build_check_handler/input-wafer-barcode-validation-handler`는 절대 경로·재분석 지점 0개·출력 파일 7개를 확인한 뒤 정리를 시도했으나 도구의 자동 승인 정책이 삭제를 차단하여 폴더와 로그를 보존했다. 운영 출력이나 실제 데이터는 정리하지 않았다.

| 현장 항목 | 상태 |
|---|---|
| 실제 바코드 리더·카메라 판독 | 미수행 |
| 실제 원격 맵 다운로드 및 장애 복구 | 미수행 |
| 실제 웨이퍼 얼라인·축 이동·픽업 | 미수행 |

운영 Config와 실제 레시피 파일은 변경하지 않았다. 새 키가 없는 기존 Config의 검사는 OFF이며, 사용 여부와 N은 Input Stage 설정 화면의 공통 Config 항목에서 설정한다. 저장 위치는 EquipmentData/Config/InputStageUnit.json이며 레시피 전환 시 값이 바뀌지 않는다.
