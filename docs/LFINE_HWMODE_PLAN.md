# LFINE_HWMODE_PLAN — LFine 하드웨어 모드(SM) 도입 + LightControllerMode·캐시skip 잔재 제거

브랜치 `refactor-lfine-hwmode`. (A) 잔재 제거=항상 송신 단순화 / (B) SM 모드 0~3 도입 + LightSystemSetupPage 런타임 UI.

---

## Step 0 감사 결과 (file:line)

### 1. 캐시 안전성 (skip 전용 vs 상태용)
| 필드 | 사용처 | 판정 |
|---|---|---|
| LFine `_power` | 상태 읽기 GetPowerAsync [LFineLightController.cs:109] + SetOnOff 복원(_lastOnPower) | **보존** (skip 무관) |
| LFine `_lastPageTimes` [:25] | skip 비교 [:128] + 저장 [:135] **뿐** | **필드째 제거** (skip 전용) |
| Leesos `_power` | 상태 읽기 [:106] / 단일 skip [:82] / batch 변경분-only [:140] | **필드 보존**, skip 분기 [:82]·[:140] 제거(항상 송신) |
| Sim `_power` | 상태 [:74] + TurnedOnCount [:106] / skip [:85] | **필드 보존**, skip [:85] 제거. BatchSendCount 카운터는 유지(이제 매호출 증가) |

skip 을 단언하는 테스트 = **0건**(verify_all/vision_features/스모크 전수 grep — BatchSendCount 단언 없음) → 갱신 목록 없음.

### 2. LightControllerMode 제거 영향 (전 참조)
- **Handler(QMC.CDT-320) = 안전**: 전수 grep 결과 `LightControllerMode`/`LightControllerEntry` 참조 0. LightControllerPage.cs 의 `it.Mode` [:144,171,189]는 **자체 item 클래스의 string**(무관).
- QMC.Common: `Recipes/LightControllerMode.cs`(enum 파일) 삭제 / `LightSystemSetup.cs:59-61` Entry.Mode 프로퍼티 + `:70-71` OnDeserializing 의 Mode 주입 제거(Vendor 주입은 유지). 구 JSON `Mode` 키 = DataContract 미지멤버 무시(비파괴).
- QMC.Vision: ILightController.Mode [:22-23, doc:45] / LFineLightConfig.Mode [:24] / LFineLightController.Mode [:30]+skip[:127-129] / Leesos.Mode [:30] / Sim(_mode [:18,24,32,34]+skip[:85]) / Factory [:21,33,54] / LightSystemSetupPage.cs(Mode 파싱 :270-271·:274, ApplyModeLabel :299·:321-325, Leesos Mode 고정 :307-308·:315, AddController 기본값 :329) / Designer(_colMode :24,49,138-141).

### 3. SM 명령 + enum 설계
- `LFineProtocol.SetModeCommand(int mode)` → payload `"SM0000;{mode}"` → WrapFrame = `@SM0000;{0~3}\r\n` (기존 SC/SP 패턴 `{cmd}{page:00}{ch:00};{val}` 과 동형, 프롬프트 명시 프레임).
- `LFineHardwareMode` enum — **QMC.Vision/Optics/LFine**(Common 추가 0): `PageTrigger=0, UserSequence=1, ChannelTrigger=2, SoftwareTrigger=3`.
- `ILightController.SetHardwareModeAsync(LFineHardwareMode)`: LFine=SM 프레임 송신, Sim=LastHardwareMode 기록(+true), Leesos=no-op(true). **런타임 송신 전용**(영속 config 아님, 현재모드 read 불가 → 마지막 송신값만 표시).

### 4. UI (벤더 인지) — LightSystemSetupPage
- 본문 TableLayout(RowCount 4 [Designer:110]) → **5행**: 컨트롤러 그리드(행1) 아래에 모드 행(Absolute 36) 삽입 → [라벨 "LFine HW Mode"]+[콤보]+[적용]+[마지막 송신 표시]. 채널 라벨 섹션은 아래로.
- 콤보 items = **"Page Trigger (0)" / "Software Trigger (3)" 둘만**(enum 1·2 미노출, 영어 라벨 — 기존 Mode 콤보 영어 표기와 일관).
- 토글: `OnGridCtrlSelectionChanged`(기존 핸들러 [Designer:144])에서 선택 행 Vendor 판별 — **LFine=표시/활성, Leesos=비활성+"Continuous PWM — no mode"**(죽은 no-op 컨트롤 금지, :305-311 Leesos 특수처리 패턴 재사용).
- 적용: `LightHub.Get(선택 PortName)` [LightHub.cs:62] → null/미연결 시 상태 에러("조명 연결 먼저") / 성공 시 `SetHardwareModeAsync` + 마지막 송신값 표시. (페이지에 이미 조명 연결/해제 버튼 → LightHub 흐름 존재 [:371-392])

---

## Steps
1. **잔재 제거**: 캐시-skip 분기(LFine/Leesos/Sim) + LightControllerMode 일체(위 목록). 빌드0(Common→Handler→Vision).
2. **SM + enum**: LFineProtocol.SetModeCommand/프레임, LFineHardwareMode(0~3), SetHardwareModeAsync(3구현). 빌드0.
3. **모드 UI**: 본문 5행 + 콤보(0/3만)+적용+표시, 벤더 토글. Designer 인라인+코드 분리, 디자이너 로드. 정적0. 빌드0.
4. **게이트**: 전체 빌드0/정적0/verify0/스모크(PanelLight·CamLight·ModuleLight + SM 스모크)/R2(LightSystemSetupPage 재캡처)/Handler 무수정/push 안 함.

## 정지 조건
캐시 필드 상태용 발견(분석과 상이) / Handler 빌드 깨짐 / SM 프레임 불명 / 빌드·verify 회귀 / 디자이너 로드 깨짐.
