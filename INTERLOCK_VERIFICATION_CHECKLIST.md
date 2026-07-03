# INTERLOCK_VERIFICATION_CHECKLIST.md

CDT-320 인터락 / 오토 시퀀스 수정 **검증 문서**. Code가 [INTERLOCK_AUTOSEQUENCE_MASTER_FIX.md](INTERLOCK_AUTOSEQUENCE_MASTER_FIX.md)를 반영한 뒤, 이 문서로 "됐다/안 됐다"를 **객관적으로** 확인한다. "잘해주길 바라는" 대신 **검증으로 잡는다.**

판정 표기: ✅통과 / ❌실패 / ⏳미확인. 각 항목은 로그·동작으로 판정.

---

## 파트 1 — 수용 기준 체크리스트 (항목별 통과 조건)

### A. 선행조건 (빌드/무결성)
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| A-1 | VS2022 빌드(별도 OutDir) | 신규 컴파일 에러 0. 기존 warning(AxisInitializePlan/MachineController unreachable)만 | ⏳ |
| A-2 | 기존 변경 보존 | Code가 이전 작업(Output 후검사 취소, 선행검사 비침습화 등) 되돌리지 않음 | ⏳ |
| A-3 | 신규 로그/알람 한글, `catch{}` 미도입 | AGENTS.md 준수 | ⏳ |

### B. §7 현재 알람 — InputVisionX 선행검사 (최우선)
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| B-1 | 선행검사 이동이 인터락에 막혀도 **본 공정 안 죽음** | `INPUT-DIE-VISION-PREPARE-STAGE-MOVE result=-11` 로 `RearPicker/FrontPicker 자동 시퀀스 실패 result=-1` 발생 안 함 | ⏳ |
| B-2 | 협조 대기/양보로 처리 | 막히면 "선행검사를 양보/보류합니다"(신규 한글 로그) 후 본 공정 계속 | ⏳ |
| B-3 | 대기 조건 = 실제 이동 인터락 통일 | 대기(`CanMove`/dry-run)가 PickerZone Input 점유까지 포함. 대기 통과 후 이동이 다른 인터락에 -11로 막히지 않음 | ⏳ |
| B-4 | 시작 게이트에 공용 레일 경로 겹침 반영 | 상대 PickerX가 InputVisionX 경로(현재~target)+안전거리와 겹치면 선행검사 시작 안 함 | ⏳ |
| B-5 | -11 재대기 루프 | 이동 막히면 재대기 후 재시도, 타임아웃 시에도 본 공정 무해 처리 | ⏳ |

### C. §2 공통 프리미티브
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| C-1 | `MotionGuardService.CanMove(dry-run)` 존재 | 부작용 없이 allow/wait/block + reason 반환, 대기·이동 동일 소스 | ⏳ |
| C-2 | `EnsureSelfSafeAsync` | Z=Avoid→Y=Avoid 물리 후퇴+검증, 실패 시 로그+알람 | ⏳ |
| C-3 | `VerifySafeStartConfigAsync` | 양 픽커 Y·Z Avoid·X 비대면 실제 좌표 확인, 미충족 시 정규화 | ⏳ |
| C-4 | `ReconcileSafeState` | 재개 시 실제 좌표+자재로 phase/Y/Z/carrying 재구성 | ⏳ |

### D. INV-6 / INV-7 (구조 핵심)
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| D-1 | Abort/finally 물리 후퇴 선행 (INV-6) | phase 리스·신호 해제 **전** 자기 픽커 Safe 후퇴 완료. 후퇴 전 상대 게이트 개방 안 함 | ⏳ |
| D-2 | Cycle Stop 후 정규화 | 정지 시 각 픽커 Y·Z Avoid로 정규화(멈춘 자리 방치 안 함) | ⏳ |
| D-3 | Resume 전 좌표 검증 (INV-7) | 재개가 자재상태만으로 이동 안 함. `VerifySafeStartConfigAsync` 선행 | ⏳ |
| D-4 | CheckUnit 안전 배치 검증 | 첫 이동 전 실제 좌표(Y·Z·X 비대면) 확인 | ⏳ |

### E. §4 우선순위
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| E-1 | 콜드 스타트 순서 | Input→Front→Rear→Output. 두 픽커 동시 첫 전진 안 함 | ⏳ |
| E-2 | 콜드 세이프 선행 | 전 유닛 이동 전 `VerifySafeStartConfigAsync` 통과 | ⏳ |
| E-3 | 재시작 상태 기반 순서 | 완료 근접 픽커(Place>Side>Bottom>PickUp) 먼저 vacate, 나머지 Safe 대기. 한 번에 한 픽커만 전진 재개 | ⏳ |
| E-4 | Place 재개 전 Output 재무장 | OutputGood/NgStageReady 재-Set 확인 후 Place 진입 | ⏳ |

### F. INV-1~5 (마주보기/전진/Z/존)
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| F-1 | INV-1 마주보기 | 두 픽커 X 마주보기 거리 안이면 최소 한쪽 Y=Avoid. 동시 비-Avoid 절대 없음 | ⏳ |
| F-2 | INV-2 Y 전진 | Y 전진은 허용 phase(+Die 보유)에서만. 그 외 이동 전 Y=Avoid | ⏳ |
| F-3 | INV-3 X 이동 | 피치·존간·공용레일 X 이동 전 마주보기면 상대 Y=Avoid 확인 | ⏳ |
| F-4 | INV-4 Z 하강 보정 | Z 하강 중 XYT는 CorrectionWindow 내에서만(정렬보정), 창 밖 차단 | ⏳ |
| F-5 | INV-5 검사존 락 | 인풋/아웃풋 검사 중 상대 픽커 진입 대기 | ⏳ |

### G. §3 실시간 감시자
| # | 확인 | 통과 조건 | 판정 |
|---|---|---|---|
| G-1 | 상시 반응 가드 구동 | 오토/수동 무관 상시 루프. 실제 엔코더 감시 | ⏳ |
| G-2 | 임박 충돌 → 하드정지 | 거리<필요&접근 / 양쪽 Y전진+X마주보기 / 잠긴 존 침범 시 전축 Stop+알람 | ⏳ |
| G-3 | 협조 대기는 정지 아님 | 상대 busy·존 락은 wait, 알람/정지 없음 | ⏳ |

---

## 파트 2 — 로그 감시 키워드

Code 수정 후 실운전/시뮬 로그를 아래로 grep. **[정상]** 나와야 함 / **[금지]** 나오면 실패.

### 정상 확인
- `선행검사를 양보` / `선행검사 이동을 보류` — §7 협조 처리 동작
- `InputVisionX SharedRailX clearance 대기 완료`
- `InputCamera 선행검사 모드: Picker X/Y를 직접 Avoid 이동하지 않고 Input 영역 이탈만 확인`
- `PickUp 완료 후 ... 비침습 InputCamera 선행검사를 예약`
- `Place 완료 후 ... 다음 PickUp용 비침습 InputCamera 선행검사를 예약`
- `Output camera 후검사 요청 등록 완료`
- (재개) `ReconcileSafeState` / `재시작 ... 안전 위치 확인` 계열
- (정지/Abort) 자기 픽커 `Avoid 복귀 완료` 후 phase 해제

### 금지 (나오면 실패 → 원인 추적)
- `INPUT-DIE-VISION-PREPARE-STAGE-MOVE` + `result=-11` 로 `자동 시퀀스 실패 result=-1` (본 공정 죽음)
- `PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID` (허가 살아있는데 InputVisionX 이동)
- `actual=690, command=690, target=1178` 류 — Place 중 PickerX 덮어쓰기
- `InputCamera 선행검사 전 빈 Picker Avoid X축 Avoid` — 선행검사가 Picker 축 직접 이동
- `too close` / `SharedRailX real-time clearance guard stopped` 가 **정상 운전 중** 반복 (궤적 자체가 위험)
- `Interlock blocked` 가 협조 대기여야 할 상황에서 **알람/시퀀스 실패**로 승격

### 충돌/간섭 집중 관찰
- `FacingY` / `마주보기` / `PickerY 전진` + 상대 `PickerX` 접근 동시성
- `currentZone` / `targetZone` / `workArea` 불일치 (Rear가 Bottom인데 zone=Input 등)
- `Critical` / `Alarm Code` / `EStop`

---

## 파트 3 — 안전 테스트 순서 (저속·시뮬 먼저, 실장비 마지막)

> 절대 원칙: 실속(2000/12000/12000)은 아래 1~4단계를 모두 통과한 뒤에만. 1회 충돌=끝.

### 단계 0 — 기준점 확보
- [ ] 정상 base git 커밋/백업 (Code 수정 전 상태). 문제 시 복구 가능하게.
- [ ] 빌드 성공(별도 OutDir), `perl tools/verify_all.pl` 통과.

### 단계 1 — 시뮬레이터(무부하) 로직 검증
- [ ] Simulator 연결 상태로 신규 시작 → 1사이클 정상 완주(PickUp→Bottom→Side→Place→Y복귀).
- [ ] 파트1 B/D/E 항목 로그로 확인. 파트2 [금지] 키워드 0건.

### 단계 2 — 정지/재개 시나리오 (시뮬)
- [ ] Side 중 정지 → 재개: Bottom부터 재개, X/T 완료 후 PickerY 전진 순서 확인.
- [ ] Place 중 정지 → 재개: Place vacate 우선, Output 재무장 후 진입.
- [ ] 두 픽커 모두 공정 중 정지 → 재개: 한 번에 한 픽커만 전진(동시 전진 없음).
- [ ] 각 5~10회 반복, 파트2 [금지] 0건.

### 단계 3 — 병렬/경합 시나리오 (시뮬)
- [ ] Front Bottom+Side 중 Rear PickUp/Place 가능, Rear Bottom+Side 진입은 대기.
- [ ] 인풋 카메라 검사 중 픽커 픽업존 진입 대기 / 아웃풋 후검사 중 픽커 진입 대기.
- [ ] InputVisionX 선행검사 vs Rear 공용레일 점유 → 선행검사 양보(본 공정 지속). ← 이번 알람 재발 확인.

### 단계 4 — 실시간 하드정지 유도 (시뮬/저속)
- [ ] 인위적으로 마주보기 위험/거리 위반 유도 → 전축 하드정지+알람 발생 확인.
- [ ] 하드정지 후 리셋→안전 복귀→재개 정상.

### 단계 5 — 저속 실장비
- [ ] 속도 대폭 낮춰 1~3 시나리오 축소 재현, 물리 간섭 없음 눈으로 확인.

### 단계 6 — 정속 실장비
- [ ] 2000/12000/12000. 초기 수 사이클 밀착 관찰(정지 버튼 대기). 이상 로그 즉시 정지.

---

## 사용법
1. Code 수정 완료 → A-1 빌드부터.
2. 파트1 표를 위→아래로 판정(✅/❌). ❌면 파트2 키워드로 원인 grep.
3. 파트3 단계는 반드시 순서대로. 앞 단계 ❌면 다음 단계 금지.
4. Cowork(분석)에게 로그를 주면 항목별 대조·원인 분석 지원.
