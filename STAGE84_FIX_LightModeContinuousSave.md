# Stage 84 Fix — LightControllerMode.Continuous JSON 저장 누락

## 증상
LightSystemSetupPage 에서 Mode 를 **Continuous(상시 모드)** 로 변경·저장해도 `light_system.json` 에 안 들어가고, 다음 로드 시 기본값(StrobeOnCommand)으로 떨어짐. StrobeExternal/StrobeOnCommand 는 정상.

## 원인 — `EmitDefaultValue=false` + `Continuous=0` 조합

```csharp
// LightControllerMode.cs : Continuous = 0  (= default(enum))
// LightSystemSetup.cs:85 (수정 전)
[DataMember(EmitDefaultValue = false)] public LightControllerMode Mode { get; set; } = LightControllerMode.StrobeOnCommand;
```

`DataContractJsonSerializer` 는 `EmitDefaultValue=false` value-type 멤버를 **`default(T)` 값일 때 직렬화에서 제외**.
- `default(LightControllerMode)` = 0 = **Continuous** → Continuous 선택 시 `"Mode"` 키 누락.
- 로드 시: DataContract 는 프로퍼티 초기화값(StrobeOnCommand)을 실행하지 않고, `[OnDeserializing]`(라인 95)이 `Mode = StrobeOnCommand` 를 주입 → 키 없으니 StrobeOnCommand 로 복원.
- 결과: **Continuous 만** 저장 안 되는 것처럼 보임.

(다른 두 모드 StrobeExternal=1 / StrobeOnCommand=2 는 0 이 아니라 정상 기록되던 것.)

## 수정 (1줄)
`QMC.Common\Recipes\LightSystemSetup.cs:85`

```csharp
// AS-IS
[DataMember(EmitDefaultValue = false)] public LightControllerMode Mode { get; set; } = LightControllerMode.StrobeOnCommand;
// TO-BE
[DataMember] public LightControllerMode Mode { get; set; } = LightControllerMode.StrobeOnCommand;
```

`EmitDefaultValue=false` 제거 → enum 값과 무관하게 항상 기록. enum 재배치는 기존 JSON 호환 깨짐 때문에 미채택. diff: **+2 / -1** (주석 1줄 포함).

## 검증 — 라운드트립 (Store 와 동일 `DataContractJsonSerializer(typeof(LightSystemSetup))`)

하니스 `cdt-320\light-mode-save-test\ModeRoundtripTest.cs` (QMC.Common.dll 강타입 참조). 결과 **ALL PASS**:

| 시나리오 | 결과 |
|---------|------|
| 1+3. Save+Load Mode=Continuous | ✅ JSON 키 존재(`key=True`) + 복원 Continuous |
| 1+3. Save+Load Mode=StrobeExternal | ✅ 키 존재 + 복원 StrobeExternal |
| 1+3. Save+Load Mode=StrobeOnCommand | ✅ 키 존재 + 복원 StrobeOnCommand |
| 4. Legacy JSON (Mode 키 제거) → 로드 | ✅ default StrobeOnCommand (OnDeserializing) |
| MSBuild (QMC.Vision target, QMC.Common 재빌드) | ✅ 오류 0 / 신규 경고 0 (선재 System.IO.Ports MSB3245만) |

> 이 하니스는 UI Save→`Store.Save`→파일→재시작→`Store.Load` 와 **동일한 직렬화 경로**(`LightSystemSetup.cs:188/207`)를 그대로 타므로 GUI 클릭 없이 시나리오 1~4 를 실질 검증. 버그는 순수 직렬화 결함이고 UI 의 Mode 수집(`CollectFromUi`)은 이미 정상이었음(타 모드는 저장되던 사실로 확인).

### JSON 샘플 (수정 후, Continuous)
DataContractJsonSerializer 는 멤버를 알파벳순 출력 → `..."MaxPower":240,"Mode":0,"Name":"L1",...` 처럼 **`"Mode":0` 키가 항상 포함**됨(이전엔 누락).

## 부수 확인 — 동일 패턴 전수 조사 (`EmitDefaultValue=false`)
QMC.Common 전체 grep 결과, value-type 0-default 트랩은 **Mode 하나뿐**:
- `AlgorithmCameraSubset.cs` 다수 — 전부 `int?`/`double?`/`string`(nullable). default=null 이라 0 은 누락 안 됨(설계상 "채워진 필드만 저장"). 안전.
- `LightSystemSetup.cs:83 Vendor` — string default "LFine"(≠null) → 항상 기록, null 이어도 OnDeserialized 복원. 안전.
- `InspectionLightSubset.cs:22 Page` (int=0) / `LightSystemSetup.cs:130 LegacyPage` (int=0) — **버그 아님**: 프로퍼티 기본값(0) == `default(int)`(0) 이고 OnDeserializing 오버라이드 없음 → 0 누락돼도 0 으로 복원(라운드트립 일치). Mode 만 초기값(2)≠default(0)+OnDeserializing 강제라 유일하게 깨졌던 것.

## 빌드 대상 메모
수정 파일은 sln 빌드 대상 top-level `QMC.Common\Recipes\LightSystemSetup.cs`. `QMC.CDT-320\QMC.Common\...` 사본은 sln 미참조 → 미수정.

## 커밋
로컬 커밋만. **remote push 안 함 — 사용자 컨펌 대기.**
