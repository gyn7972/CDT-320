# STAGE 80 — SPEC: 다중 컨트롤러 결선 + Setup/Recipe UI 책임 분리

- **작성일**: 2026-06-02
- **단계**: SPEC (구현 전, 사용자 컨펌 대기) — **SPEC + CHECKLIST 까지만, 코드 변경 없음**
- **Stage 번호 근거**: git/docs 최신 = Stage 79 → **NN = 80**
- **브랜치**: `stage-80-multi-ctrl-ui-separation-spec`

---

## 0. Baseline 확인 (코드 직접 sanity check — 정지조건)
| baseline | 결과 | 근거 |
|---|---|---|
| LFine batch (`SetChannelBatchAsync` SP 1프레임) | ✅ | `LFineLightController.cs` |
| `LightControllerMode` enum + 캐시 정책 | ✅ | `LightControllerMode.cs` |
| `LightControllerEntry.Vendor` + 팩토리 분기 | ✅ | `LightControllerFactory.cs` `case "Leesos"` |
| `LeesosLightController` (LC/LH/LS, 12-bit, 응답 검증) | ✅ | `LeesosLightController.cs` |
| `InspectionLightPanel.Apply` 가 batch 사용 | ✅ | `SetChannelBatchAsync` 호출 |

> ⚠ 차이: 프롬프트는 "Leesos TimeoutMs 기본 **500ms**" 라 했으나 실제 `LeesosLightConfig.TimeoutMs = **1000**`. 본 Stage 범위 밖(미차단). 필요 시 별도 조정.

**→ baseline 5건 전부 존재 → 진행 가능.**

---

# Part A — 다중 컨트롤러 결선

## A.1 배경
한 알고리즘이 여러 조명 컨트롤러를 동시 사용(예: BottomInspection = COM1 ch3~6 + COM2 ch1,2). 현 모델은 "1 알고리즘 = 1 컨트롤러".

## A.2 데이터 모델 — Setup (`LightSystemSetup.cs:119`)
현재:
```csharp
public class AlgorithmLightWiring {
    string Algorithm;
    string ControllerPort;          // 단일
    List<int> Channels;
    int LegacyPage;                 // Stage 70 — Page→Recipe 이전 잔존 키(읽기전용)
}
```
변경:
```csharp
public class AlgorithmLightWiring {
    string Algorithm;
    List<ControllerChannels> ControllerSets = new();
    int LegacyPage;                                  // 유지 (Stage 70 마이그레이션과 병존)
    // 마이그레이션용 임시 보존 프로퍼티 (아래 A.4)
}
public class ControllerChannels {
    string ControllerPort;          // LightControllerEntry.PortName FK
    List<int> Channels = new();
}
```
> Page 는 Recipe(`InspectionLightSetting.Page`)에 이미 있으므로 ControllerChannels 에 미포함.

## A.3 데이터 모델 — Recipe (`InspectionLightSubset.cs:11`)
현재 `InspectionLightSetting { Channel, Level, On, StrobeTimeUs, StabilizeDelayMs, Page }` → **`ControllerPort` 추가**.
```csharp
[DataMember(EmitDefaultValue=false)] public string ControllerPort { get; set; }   // ← 신규
```
> 같은 채널 번호(ch3)가 두 컨트롤러에 모두 있을 수 있어 Channel 만으론 모호. **StabilizeDelayMs 등 기존 필드 보존**(프롬프트 모델은 누락 — 정정). Clone 에도 ControllerPort 추가.

## A.4 마이그레이션 (`OnDeserialized` / `[OnDeserializing]`)
### Setup — 단일 → ControllerSets[0]
구 키 `ControllerPort`/`Channels` 를 임시 프로퍼티(`[DataMember(Name="ControllerPort"/"Channels", EmitDefaultValue=false)]`)로 받고, `OnDeserialized` 에서 ControllerSets 비어있고 구 ControllerPort 있으면 단일항목 List 로 변환. 소비 후 임시 프로퍼티 비워 재저장 시 구 키 소멸 (LegacyPage 패턴과 동일).
### Recipe — ControllerPort 자동 채움
옛 `InspectionLightSetting`(ControllerPort 없음) 로드 시:
- 소속 알고리즘 `ControllerSets` 가 **1개** → 그 PortName 자동.
- **2개 이상** → `ControllerSets[0].ControllerPort` 로 채우고 **status/알람 보고**: "다중 컨트롤러 결선 — Recipe ControllerPort 임의 선택됨. Setup 검토 필요." (확인 필요 #1)
- ※ Recipe 측 마이그레이션은 Setup 을 참조하므로 **Form1.Load 의 1회 보정 단계**(AlgorithmCameraSubset.MigrateWiringPageToSettings 와 유사)에서 수행 — DataContract 콜백만으론 Setup 참조 불가.

## A.5 SetupUI 결선 → TreeView (`LightSystemSetupPage.cs`)
현재 `_gridWiring`(평면 DataGridView: Algorithm/ControllerPort 콤보/ChannelsCsv, `:133-143`) → **TreeView + 우측 디테일**.
- 알고리즘 5노드 고정(`VisionAlgorithm.All`), expand.
- 노드 클릭 → 우측에 그 알고리즘 `ControllerSets` 목록 + 각 set 의 (Controller 콤보, Channels 체크박스).
- "+ 컨트롤러 추가": 인벤토리 중 그 알고리즘에 **아직 없는** PortName 만 콤보 → 새 set.
- "삭제": 그 set 만 제거.
- (Controller,Channel) 두 알고리즘 겹침 → 경고만(차단 X, 확인 필요 #2).

## A.6 Cascade — 인벤토리 삭제 시 결선 정리 (`DeleteController` `:286-294`)
현재 평면 grid refRows 기반. → ControllerSets 기준으로: 삭제 포트를 가진 모든 `ControllerSets` 항목 제거 + 다이얼로그("결선 N건 함께 비움") + Recipe 측 매칭 `InspectionLightSetting`(ControllerPort 일치) 처리(확인 필요 #3).

## A.7 Recipe UI — Controller 컬럼 (`InspectionLightPanel.cs`)
현재 표 = Channel(RO)/Name(RO)/Level/Page (Stage 72). → **Controller 컬럼 추가**.
- Controller 콤보 = `wiring.ControllerSets` PortName 들 (표시 `PortName (Name)`, 저장값 PortName).
- Channel 콤보 = 선택 Controller 의 Channels 풀만.
- Controller 변경 → Channel 콤보 재구성 + 기존 값이 풀 밖이면 첫 채널 reset.
- **단일 결선** → Controller 자동 채움 + readonly(확인 필요 #4).
- **0개 결선** → 패널 비활성 + "Setup 에서 결선 설정" 안내.

## A.8 Apply — 컨트롤러별 group + Task.WhenAll (`InspectionLightPanel.cs:234-259`)
현재 단일 `await ctrl.SetChannelBatchAsync(grp.Key, times)`(page 그룹). → **ControllerPort 로 group → 각 컨트롤러 batch Task → `Task.WhenAll`**(독립 시리얼 포트라 병렬 안전, tact = 최저속 1 RTT).
- ctrl Hub 미등록 → `LIGHT-MAP-INVALID` + 그 ctrl skip, 나머지 계속.
- wiring 누락 → `LIGHT-WIRING-MISS` + 그 group skip.
- 풀 밖 채널 → `LIGHT-CHANNEL-OUT-OF-POOL`.
- 같은 컨트롤러 내 Page 혼재 시 sub-group(옵션).
- ※ Stage 75 적용후 ReceiveResponseAsync(수신 검증)는 컨트롤러별로 유지/통합 — 구현 시 결정.

## A.9 알람 — 신규 0 (`LIGHT-WIRING-MISS`/`LIGHT-MAP-INVALID`/`LIGHT-CHANNEL-OUT-OF-POOL` 재사용)

### A.x 영향받는 호출처 (모델 변경 — 전수)
- `LightSystemSetup.cs`: `RenamePort`(:46-47), `Validate`(:67-68) → ControllerSets 순회
- `LightSystemMigrator.cs`(:77-83): io_set→wiring → ControllerSets 빌드
- `LightSystemSetupPage.cs`: BindAllCore(:182-184), CollectFromUi(:419-421), Delete cascade(:286-294), TrimWiringPool(:368-375) → TreeView 기반 재작성
- `InspectionLightPanel.cs`: Wiring()(:108), BindFields(:114-129), Apply(:218-259) → 다중 결선
- `AlgorithmCameraSubset.MigrateWiringPageToSettings`: LegacyPage 소비(ControllerPort 미사용) → 영향 적음, 확인

---

# Part B — Setup ↔ Recipe UI 책임 분리

## B.1 원칙 (책임 매트릭스)
| 항목 | 편집 페이지 |
|---|---|
| 인벤토리(Port/Baud/Vendor/Mode/ChannelCount/PageCount/MaxPower/MaxOnTimeUs) | **Setup 만** |
| 채널 라벨(이름/색) | **Setup 만** |
| 알고리즘 결선(ControllerSets/Channels) | **Setup 만** |
| **Level/On/StrobeTimeUs/Page** (검사값) | **Recipe 만** |

## B.2 Setup — 현황 (이미 분리됨)
- **grep 결과 Setup 페이지에 Level/Brightness/TrackBar/DefaultLevel 컨트롤 0건** → 값 편집 UI 없음(이미 깔끔). 채널 라벨 그리드 = Channel/Name/Color(DefaultLevel 컬럼 없음).
- 남은 작업(경량): 헤더 텍스트 "조명 시스템 설정 — 기구적 결선(한 번 설정 후 거의 변경 없음)", 안내 라벨 "검사별 밝기/On-Off 는 [레시피] 에서 설정", 인벤토리에 MaxOnTime 컬럼(현 미표시) 추가 검토.

## B.3 Recipe — 결선 헤더(다중) + 프리셋 표시
- 현재 `_lblWiring` 단일 `결선: {ControllerPort} / 풀:[...]` (`:124-129`) → **ControllerSets 다중 표시**(읽기전용): `COM1 (Main) / 풀:[3,4,5,6]` + `COM2 (Aux) / 풀:[1,2]`.
- "변경은 Setup 에서" 링크 라벨.
- **현재 프리셋 표시** — ⚠ **중요 발견**: Vision 은 `AlgorithmCameraMapStore.Path_` = 고정 단일 `Config\algorithm_camera.json` 만 사용, **명명된 RecipeProject/프리셋 개념 없음**. 따라서 "현재 프리셋: {RecipeName}" 은 **단일 파일명/경로**만 표시 가능. (다중 명명 프리셋은 미래 기능 — 본 Stage 범위 밖.) 확인 필요 #5.

## B.4 동기화 (Store 단일 source-of-truth)
- InspectionLightPanel 진입 시 `LightSystemSetupStore` + `AlgorithmCameraMapStore` 모두 Load → 결선 헤더/콤보 items(Setup) + 값 표(Recipe).
- Setup 결선 변경 → Recipe 다음 진입 시 콤보 재구성.
- 풀 밖 채널 → `LIGHT-CHANNEL-OUT-OF-POOL` + 그 행 빨강.

## B.5 권한 분리 — 범위 밖(미래). UI 분리가 기반.

---

# 공통

## 5. 영향 범위 (신규 파일 0 — 모두 확장)
| 파일 | 변경 |
|---|---|
| `LightSystemSetup.cs` | AlgorithmLightWiring.ControllerSets + ControllerChannels + 마이그레이션 + RenamePort/Validate 갱신 |
| `InspectionLightSubset.cs` | InspectionLightSetting.ControllerPort + Clone |
| `LightSystemMigrator.cs` | io_set→ControllerSets |
| `AlgorithmCameraSubset.cs` | Recipe ControllerPort 자동채움(1회 보정, 다중 안내) |
| `LightSystemSetupPage.cs` | 결선 TreeView 화 + cascade + 헤더/안내 텍스트 |
| `InspectionLightPanel.cs` | Controller 컬럼 + 다중 결선 헤더 + 프리셋 표시 + Apply 병렬(Task.WhenAll) |

## 6. 시퀀스 문서 04~08 영향 — 없음 (Vision UI/모델).

## 7. Mismatch
| # | 위치 | 내용 |
|---|---|---|
| M-80-1 | AlgorithmLightWiring 단일 컨트롤러 | ControllerSets List 확장 + 1:1 마이그레이션 |
| M-80-2 | InspectionLightSetting ControllerPort 누락 | 신규 필드 + 자동 마이그레이션(단일=자동/다중=안내). StabilizeDelayMs 등 기존 필드 보존 |
| M-80-3 | Setup/Recipe 책임 | Setup 엔 이미 Level 류 0건(분리 거의 완료) — 헤더/안내만 보강 |
| M-80-4 | "프리셋" 용어 | Vision 은 단일 전역 algorithm_camera.json — 명명 프리셋 개념 없음. 헤더는 파일명만 표시(확인 #5) |
| M-80-5 | baseline Leesos TimeoutMs | 프롬프트 500ms ↔ 실제 1000ms — 본 Stage 미차단 |

## 8. 확인 필요 항목
1. 다중 결선 시 Recipe ControllerPort 자동채움 — 첫 set 선택 OK? vs 빈 값+사용자 입력 강제?
2. 같은 (Controller,Channel) 중복 결선 — 경고만(기본) vs 차단?
3. Cascade 삭제 시 Recipe `InspectionLightSetting` 처리 — 자동 비움 vs 빨강 표시만?
4. 단일 결선일 때 Controller 컬럼 — 자동+readonly(기본) vs 항상 편집?
5. "현재 프리셋" 표시 — Vision 은 단일 파일뿐. 파일명만 표시 OK? (다중 명명 프리셋 도입은 별도 Stage?)

## 9. 다음 단계
SPEC+CHECKLIST 컨펌 → 구현. 순서: 모델 → 마이그레이션 → Setup TreeView → Recipe Controller 컬럼 → Apply 병렬 → cascade → 동기화 → verify. (데이터 모델 변경은 마이그레이션 라운드트립 통과 없이 머지 금지.)
