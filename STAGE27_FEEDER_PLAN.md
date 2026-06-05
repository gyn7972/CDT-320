# Stage 27 — Feeder 시퀀스 통합 작업 계획

## 1. 배경 (Phase 1 조사 결과)

### 현황 — 3개 층의 통합 미흡

| 컴포넌트 | 단위층 구현 | 사이클 호출 | UI 상태 |
|---|---|---|---|
| InputLoader Feeder | ✅ Stage 26 통합됨 | ✅ LoadNextWafer | ✅ InputCassettePage 완료 |
| **OutputUnloader Feeder** | ✅ 4개 메서드 구현 | ❌ **호출 0건** | ❌ 정적 placeholder |
| **InputStage Handoff** | ❌ ReceiveFromFeeder 없음 | ❌ | — |
| **InputFeederPage** | — | — | ❌ 정적 더미 (Click 0건) |
| **OutputFeederPage** | — | — | ❌ 2 InfoRow 만 |

### 발견된 빈 부분

1. **OutputUnloader 가 NullObject 로 OutputStage 에 주입됨** — `CDT320Machine.cs` 에서 실 인스턴스 미연결
2. **DoOneDieAsync Place 단계 후 OutputUnloader 호출 없음** — Bin 적재 시퀀스 0건
3. **InputFeederPage / OutputFeederPage Click 0건** — 모든 버튼 비활성
4. **InputStage 가 InputLoader Feeder 의 Exchange 위치(150mm) 에서 웨이퍼를 받는 메서드 없음** — Handoff Gap
5. **SimCassetteDriver 가 OutputUnloader 센서(NG/Good1/Good2 + Protrusion + Clamp) 미초기화**

---

## 2. 작업 계획 (6개 항목)

### Step 1. MachineController — Feeder 시퀀스 헬퍼 추가
- `StoreCompletedWaferAsync(DieGrade grade)` — Place 완료 후 Good/NG 카세트에 웨이퍼 적재
- `ScanOutputCassettesAsync()` — Output 3 카세트 매핑 (UI Map 버튼)
- `OutputCurrentSlotByGrade` 추적 (NG/Good1/Good2 별 현재 슬롯)
- 옵션 `WafersPerOutputBatch = 8` (몇 다이당 적재)

### Step 2. DoOneDieAsync 통합
- 매 `WafersPerOutputBatch` 다이 완료 후 Place 결과(`inspPass`)에 따라 `StoreCompletedWaferAsync(Good|Ng)` 호출
- 카세트 가득 시 알람 + 사이클 종료
- `CycleRunAsync` 종료 시 마지막 배치 강제 적재

### Step 3. CDT320Machine.cs — NullOutputUnloader → 실 어댑터
- 새 어댑터 클래스 `OutputUnloaderAdapter : IOutputUnloaderUnit` 추가
- 실제 OutputUnloader 인스턴스를 OutputStage 에 주입하여 RequestWaferChangeAsync 가 진짜 호출되도록

### Step 4. InputFeederPage 라이브 바인딩 + Click
- 200ms Timer — FeederY ActualPosition / FeederUpDownCyl.IsFwd / FeederClampCyl.IsFwd / 센서들 LED
- 액션 버튼: Feeder Init / Fwd Cyl / Bwd Cyl / Clamp / Unclamp

### Step 5. OutputFeederPage 신규 (placeholder → 실 페이지)
- OutputUnloader.FeederY / 실린더 / 3카세트 안착 LED + Protrusion/Detect/Clamped 센서 LED
- 액션 버튼: Map Cassettes / Pickup at Stage / Place at Cassette

### Step 6. SimCassetteDriver 확장
- OutputUnloader 의 ExistSensor_NG / Good1 / Good2 모두 ON 으로 시뮬
- ProtrusionSensor OFF, WaferDetectSensor 위치 기반 토글
- FeederClampCyl.InFwd ↔ WaferClampedSensor 동기화

---

## 3. 체크리스트 (각 항목 검증 가능 단위로 분해)

### A. MachineController
- [ ] A1. `StoreCompletedWaferAsync(DieGrade)` 메서드 추가 (50줄)
- [ ] A2. `ScanOutputCassettesAsync()` 추가
- [ ] A3. `OutputSlotState` 클래스 (NG/Good1/Good2 슬롯 인덱스 추적)
- [ ] A4. `WafersPerOutputBatch` 속성 (기본 8)
- [ ] A5. DoOneDieAsync — 매 8 다이마다 StoreCompleted 호출
- [ ] A6. CycleRunAsync — 종료 시 잔여 배치 처리

### B. CDT320Machine
- [ ] B1. `OutputUnloaderAdapter` 어댑터 클래스 신규 (Equipment/Sim/ 또는 Equipment/)
- [ ] B2. CDT320_Machine 생성자에서 NullOutputUnloaderUnit → OutputUnloaderAdapter
- [ ] B3. csproj 등록

### C. InputFeederPage
- [ ] C1. 200ms Timer — FeederY 위치 라이브 표시
- [ ] C2. FeederClampCyl / FeederUpDownCyl 상태 LED
- [ ] C3. Action 버튼 5개 (Init/FwdCyl/BwdCyl/Clamp/Unclamp) + Click 핸들러

### D. OutputFeederPage 재작성
- [ ] D1. 페이지 신규 작성 (정적 → 라이브)
- [ ] D2. OutputUnloader.FeederY/실린더/센서 200ms 갱신
- [ ] D3. Action 버튼 (Map / Pickup / Place) + Click

### E. SimCassetteDriver 확장
- [ ] E1. OutputUnloader 의 3 카세트 ExistSensor 모두 ON 초기화
- [ ] E2. OutputUnloader.ProtrusionSensor OFF
- [ ] E3. OutputUnloader.ElevatorZ.MoveCompleted → 슬롯별 WaferDetect 토글
- [ ] E4. OutputUnloader.FeederClampCyl.InFwd ↔ WaferClampedSensor 동기화
- [ ] E5. OutputSlotsState 배열 3개 (Ng/Good1/Good2)

### F. 빌드/검증
- [ ] F1. Build clean (warning 0)
- [ ] F2. verify_all.pl → 117/118 PASS 회귀 무결성
- [ ] F3. Auto-cycle 런타임 테스트 — Output 카세트 적재 로그 확인
- [ ] F4. 핸들러 UI 캡처 (InputFeederPage / OutputFeederPage)
- [ ] F5. 체크리스트 정합성 재검증
- [ ] F6. PPT 보고서 생성

---

## 4. 보안/안전성 고려

- 인터락: ProtrusionSensor 감시는 OutputUnloader 에 이미 구현됨 (`MoveElevatorWithProtrusionGuardAsync`) — 그대로 활용
- 카세트 안착 사전 점검 (`IsCassettePresent`) 호출 — 미안착 시 시퀀스 중단
- 사이클 중 Output 카세트 가득 시 → AlarmManager.Raise + `CycleStopAsync()` 자동 호출

## 5. 위험 요소

| 위험 | 영향 | 대응 |
|---|---|---|
| OutputUnloaderAdapter 변경이 InputStageUnit 의 다른 호출 경로 깨뜨림 | 빌드 fail | 기존 NullOutputUnloaderUnit 유지 + 어댑터를 OutputStage 에만 주입 |
| 매 8 다이마다 Output 시퀀스 → 사이클 시간 증가 | UX | `WafersPerOutputBatch` 옵션화 (기본 8, --auto-cycle 시 조정) |
| 시뮬 카세트 가득 → 카세트 모두 채워지면 사이클 정지 | 운영 | 시뮬에서 Output 카세트 비어있는 상태로 시작 (StoreFullWafer 받기 전) |

## 6. 산출물

- 수정 파일: `MachineController.cs`, `CDT320Machine.cs`, `InputFeederPage.cs`, `OutputPages.cs`, `SimCassetteDriver.cs`
- 신규 파일: `Equipment/Sim/OutputUnloaderAdapter.cs`
- csproj 업데이트
- PPT 보고서: `D:\Work\CDT-320\문서\04_Stage27_Feeder_상세보고서.pptx`
