# CDT-320 운영 리스크 + 데이터 관리 통일성 점검 보고서

- 일자: 2026-07-03
- 대상: `C:\Project\CDT-320` (QMC.CDT-320 핸들러 / QMC.Vision / QMC.Common)
- 방식: 전수 패턴 검색(빈 catch, .Result/.Wait, Thread.Sleep, while(true), fire-and-forget, 비원자 저장 등) 후 소스 확인으로 오탐 제거. 높음 항목은 코드 라인 재검증 완료.

---

# PART 1. 운영(생산 가동) 리스크 — 21건 (높음 6 / 중간 11 / 낮음 4)

## 높음 — 즉시 조치 권장

### 1. Vision TCP 응답-명령 오배정 가능 (타임아웃 후 늦은 응답)
- `QMC.CDT-320\Equipment\Vision\VisionTcpClient.cs` : 148~152 (SendAsync), 475~477 (ReceiveLoop)
```csharp
if (await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, ct)).ConfigureAwait(false) != tcs.Task)
{
    lock (_pending) if (_pending.Count > 0) _pending.Dequeue();   // 어떤 TCS인지 확인 없이 제거
```
- 응답 매칭이 FIFO 큐 순서에만 의존. 타임아웃으로 대기자를 제거한 뒤 늦게 도착한 응답이 **다음 명령의 응답으로 오인**될 수 있음 → 잘못된 MATCH/INSPECT 결과로 오배치/오판정 가능.
- 권장: 명령마다 시퀀스 ID(토큰)를 부여해 응답 매칭, 타임아웃 시 해당 토큰 응답은 폐기.

### 2. 레시피 저장 비원자 + 실패 무통보
- `QMC.CDT-320\Equipment\Recipes\RecipeStore.cs` : 141~155 (Save), catch{} 120/154/208 라인
```csharp
using (var fs = File.Create(path))   // 최종 경로에 직접 쓰기 (.tmp+Replace 미사용)
{ JsonPrettySerializer.WriteObject(fs, typeof(RecipeProject), p); }
} catch { }
```
- 저장 중 전원 차단/디스크 오류 시 `.Project` 파일 반파손. 실패해도 무통보 → "저장된 줄 알았던" 레시피로 가동될 수 있음.
- 권장: `MaterialSnapshotStore.CommitSnapshot`(tmp 쓰기 + File.Replace + 재시도 + recovery 사본) 패턴으로 통일. 실패 시 메시지박스/알람.

### 3. EMG(비상정지) 처리 지연 + 축별 실패 무시
- `QMC.CDT-320\Equipment\OperationPanelMonitorService.cs` : 268~276 (HandleEmgFrontEdge)
```csharp
try { axis.Stop(); Thread.Sleep(100); axis.ServoOff(); } catch { }
```
- 전 축을 순차 처리하며 축당 100ms 대기 → 축 수 × 100ms 만큼 마지막 축 정지 지연. 특정 축 Stop/ServoOff 실패는 로그·알람 없이 삼켜져 축이 계속 움직일 수 있음(안전 경로).
- 권장: 전 축 병렬 Stop → 병렬 ServoOff, 실패 축은 알람 발생.

### 4. Vision TCP 서버 기동 실패 무통보 (포트 5100/5101/5103)
- `QMC.Vision\Form1.cs` : 502~504
```csharp
try { _svrWafer .Start(); } catch { }
try { _svrBin   .Start(); } catch { }
try { _svrBottom.Start(); } catch { }
```
- 포트 점유(중복 실행·좀비 프로세스) 시 Start() 실패해도 Vision이 정상처럼 떠 있음 → 핸들러가 영원히 접속 못 하는데 원인 파악 불가.
- 권장: 실패 시 로그 + UI 상태 표시(포트별 LISTEN/FAIL) + 재시도.

### 5. 로트(생산 실적) 저장 실패 무통보 + 비원자
- `QMC.CDT-320\Equipment\Lots\LotStorage.cs` : 73~86 (SaveJson)
```csharp
using (var fs = File.Create(path))
{ JsonPrettySerializer.WriteObject(fs, typeof(Lot), lot); }
} catch { }
```
- 디스크 풀/권한 문제 시 **생산 실적이 조용히 유실**. 손상 파일 잔존 가능.
- 권장: tmp+Replace + 실패 시 알람.

### 6. 유닛 설정/레시피 로드 실패를 전 계층에서 무시
- `QMC.Common\Machine.cs` : 106~111, 149~151 / `QMC.Common\BaseComponent.cs` : 86~91, 117~119
```csharp
public override void LoadRecipe(string recipeName)
{ try { Recipe = UnitDataStore.LoadRecipe(recipeName, StorageKey, Recipe); ... } catch { } }
```
- 로드 예외가 Composite 전 계층에서 삼켜져 일부 유닛만 **이전 레시피/기본값으로 혼합 가동** 가능 → 픽업 좌표/속도 불일치로 자재 파손 위험.
- 권장: 실패 유닛 수집 → 로드 종료 시 일괄 알람/메시지.

## 중간

### 7. AppSettings 저장 비원자 + 실패 무통보 — `AppSettings.cs:133~143` (File.Create 직접쓰기 + catch{})
### 8. 설정 파일 손상 시 조용히 기본값 대체 — `AppSettings.cs:124` `catch { Current = new AppSettings(); }`
   - UseVision/SimulationMode/포트가 현장 모르게 리셋. `EquipmentDataStore.Load`, `UnitRecipeStore.Load`의 `catch { return new T(); }`도 동일 패턴.
### 9. 픽커 phase 전환 무한 대기 — `Sequencing\Picker\PickerProcessSequence.cs:1212~1230, 1256~1272` while(true)+1ms 폴링, 타임아웃/알람 없음 → 상호 대기 데드락 시 무한 행.
### 10. Auto 리소스 획득 무한 대기 — `Sequencing\Common\UnitSequenceBase.cs:191~220` while(true)+100ms, 상한/알람 없음.
### 11. 축 초기화 Thread.Sleep — `MachineController.cs:2280~2287` 축당 1.5초 동기 블로킹(다축 동시 초기화 시 스레드풀 고갈 위험, AGENTS.md 규칙 위반).
### 12. UI 스레드 동기 TCP 촬상 — `VisionViewerSource.cs:75,120~135` `.GetAwaiter().GetResult()`로 최대 수 초 UI 프리즈.
### 13. Vision 재접속 워치독 성공 판정이 AnyConnected — `VisionReconnectWatchdog.cs:77,105,138` 6채널 중 1개만 살아도 알람 해제 → 일부 모듈 끊긴 채 자동운전 지속.
### 14. Vision TCP AcceptLoop 예외 시 조용히 종료 — `QMC.Vision\Equipment\Comm\VisionTcpServer.cs:104~117` 재시작 없음, IsRunning=true로 상태 표시 거짓.
### 15. 수동 이동 타임아웃 후 모션 계속 진행 — `OutputStageMapTransferPage.cs:1730~1767` (InputStage 동일) UI는 -1 실패인데 축 이동 Task는 미취소 → 충돌 위험.
### 16. 정지 다이얼로그 fire-and-forget — `Ui\Tabs\WorkTab.cs:489` `_ = ShowStopProgressAsync(...)` 예외 미관찰.
### 17. Ionizer 센서 감시 루프 예외 무시 — `Equipment\Sensors\IonizerSensor.cs:43~55` IO 읽기 연속 실패 시 감시 무력화 무통보(파일 내 한글 주석 인코딩 깨짐).

## 낮음

### 18. Vision 수신 루프 예외 원인 소실 — `VisionTcpClient.cs:483` catch{} → 끊김 원인 분석 불가.
### 19. Vision 모듈 초기 로드 실패 무시 — `QMC.Vision\Form1.cs:597` `try { mod.LoadSettings(); mod.LoadRecipe("default"); } catch { }`
### 20. 뷰어 수신 루프 무한 재시도 + 원인 무기록 — `VisionViewerSource.cs:92~117`
### 21. 알람 정의 파일 저장 실패 무시 — `QMC.Common\Alarms\AlarmMaster.cs:119~122`

## 양호 사례 (통일 기준으로 삼을 것)
- `MaterialSnapshotStore.CommitSnapshot` — tmp+Replace+재시도+recovery: 저장 원자성 모범.
- `EquipmentDataStore.Save` / `UnitRecipeStore.Save` — tmp+Replace 사용 중(Load 쪽만 개선 필요).
- `ImageLogSaver`, `PendingGrabStore` — 그랩/병렬검사 경로 Bitmap 누수 없음 확인.

---

# PART 2. 데이터 관리 통일성 점검

기준 원칙:
- **A**: 모든 데이터는 "레시피" 또는 "설정" 둘 중 하나로 관리 (제3 저장소·하드코딩 금지)
- **B**: 레시피 = 모듈→도구 계층, 레시피 선택 시 전 모듈/도구 공통 반영
- **C**: 설정 = 카메라(디바이스)별 구성, 디바이스별 매뉴얼 테스트 가능, 새 디바이스도 같은 패턴으로 추가

## 2-1. 데이터 인벤토리 (저장소별 현황)

| 항목 | 현재 위치 | 판정 | 근거 |
|------|---------|------|------|
| ExposureMs(ms) | 핸들러 `RecipeStore.InspectionSubset` | ❌ 비전과 이중 관리+단위 불일치 | RecipeStore.cs:328 |
| Exposure(us) | 비전 `CameraRecipe` | ❌ 핸들러 ExposureMs와 중복 | CameraData.cs:93 |
| LightIntensity(0~1) | 핸들러 `InspectionSubset` | ❌ 비전 LightSettings와 이중+스케일 다름 | RecipeStore.cs:329 |
| LightSettings(채널별) | 비전 `AlgoRecipeBase` | ❌ 위와 이중 | AlgorithmData.cs:65 |
| AutoFocusRois | `VisionSettings`(전역 설정) | ❌ 레시피별 관리 불가 | VisionConfig.cs:201 |
| AutoFocusThreshold=100 | `VisionSettings` | ❌ 하드코딩 기본값 | VisionConfig.cs:206 |
| ChippingDepthMax 등 | 핸들러 `DieSubset` + 비전 `VisionMachineRecipe` | ❌ 양쪽 중복 정의 | RecipeStore.cs:351~353 / VisionMachineData.cs:70~72 |
| LightSystemSetup(컨트롤러) | `Config\light_system.json` 고정 경로 | ⚠️ 설정으로는 적절하나 레시피 무관 고정 | LightSystemSetup.cs:103 |
| CameraId, ScaleX/Y, LightPages | 비전 `CameraConfig`/`VisionModuleSetup`(모듈별) | ✅ 적절 (단 VisionSettings에 구버전 Scale 필드 잔존) | CameraData.cs / VisionConfig.cs:185~187 |
| RecipeName | 핸들러 `.last_project` + 비전 `vision.json LastRecipeName` | ❌ 이중 관리, 재시작 시 불일치 가능 | RecipeStore.cs:199 / VisionConfigStore.cs:221 |
| 검사 활성화 | 핸들러 `Module.BottomInspectionEnable` vs 비전 `Inspector.Config.Enable` | ❌ 별개 제어, 동기 없음 | RecipeStore.cs:400 / AlgorithmData.cs:142 |

## 2-2. 원칙 B 위반 (레시피 선택 시 공통 반영 안 됨)

1. **노출값 단위/위치 불일치 (치명)** — 핸들러 ExposureMs(ms, 기본 500) vs 비전 CameraRecipe.Exposure(us, 기본 5000). TCP GRAB 경로에 변환 코드 없음 → 핸들러에서 노출을 바꿔도 비전은 자기 레시피 값 사용. *실측 재확인 완료.*
2. **조명이 레시피를 따라가지 않음** — `LightSystemSetupStore.Load()`는 항상 `Config\light_system.json`만 로드, 레시피명 미사용. 조명 채널 레벨은 비전 레시피(AlgoRecipeBase)에, 핸들러 LightIntensity는 핸들러 레시피에 → 3원 관리.
3. **"default" 폴백** — `VisionMachine.cs:20~27` 레시피 미지정 시 "default" 치환, 파일 없으면 하드코딩 기본값으로 조용히 가동.
4. **AutoFocusRois 전역 1벌** — 키가 (Camera, Target)뿐, 레시피 차원 없음 → 레시피 A/B가 같은 ROI 강제.
5. **알고리즘 파라미터 레시피 로드 전 기본값 가동** — `AlgorithmNode.LoadRecipe→ApplyToRuntime` 구조상 레시피 선택 전 매뉴얼 테스트는 AcceptThreshold=0.7 등 기본값으로 동작(레시피 값과 다른 결과).

## 2-3. 레시피↔설정 소속 오류

- 레시피로 가야 할 것: `AutoFocusRois`, `AutoFocusThreshold` (품목/다이 재질별로 다름) — 현재 전역 설정.
- 설정으로 가야 할 것 검토: 핸들러 `InspectionSubset.LightIntensity` — 조명 하드웨어 특성이면 설정, 품목별이면 비전 레시피로 일원화. 현행은 어느 쪽도 아닌 이중 관리.
- 검사 임계값(Chipping/Foreign) — 핸들러 DieSubset과 비전 VisionMachineRecipe 중 **SSOT 1곳** 지정 필요(권장: 핸들러 레시피 → RECIPE 동기로 비전 전달, 기존 RECIPEREQ/RECIPE 프로토콜 활용).

## 2-4. 원칙 C 위반 (설정=카메라별 + 매뉴얼 테스트)

| 디바이스/항목 | 카메라별 구성 | 매뉴얼 테스트 UI | 비고 |
|------|------|------|------|
| 카메라(CameraId/Scale/Delay) | ✅ CameraMappingPanel | ✅ | 기준 패턴 |
| 조명 컨트롤러 정의 | ✅ 모듈 Setup.LightPages | ❌ 설정 탭엔 컨트롤러 정의만, 채널 레벨 즉시 조정 UI 없음(레시피 에디터로만 가능) | SettingsPage.cs:82 |
| 오토포커스(CUDA on/off, Threshold) | ❌ 전역 VisionSettings | △ ROI 페이지만 | 카메라별 전환 불가 |
| 신규 디바이스 추가 패턴 | — | — | 카메라는 패턴화되어 있으나 조명/AF는 각기 다른 방식 → "아래로 디바이스 계속 추가" 구조가 성립 안 됨 |

## 2-5. 하드코딩 (레시피/설정으로 못 바꾸는 값, 주요)

- `D:\CDT-320`, `D:\CDT-320\EmguCV` 기본 경로 — VisionConfig.cs:44,68 (빈값 폴백 시 강제)
- Finder AcceptThreshold=0.7, AngleTolerance=10° — AlgorithmData.cs:97,121 (레시피 로드 전 유효)
- BottomInspector TopHatRadius=21, LinkDistance=25, Min/MaxForeignAreaFilterSize=36/100000 — AlgorithmData.cs:223 (설정 UI 없음)
- Side/Bin ChippingThreshold·PlacementTolerance=0.05 — VisionModuleData.cs:61,80,90
- TrainRoi 초기값 (320,240,100,100) — VisionMachine.cs:54 (센서 해상도 무관)
- 핸들러 기본 ExposureMs=500 / LightIntensity=0.5 / ChippingDepthMax=0.05 — RecipeStore.cs:328~351

## 2-6. 매뉴얼 테스트 vs 자동 시퀀스 경로 차이

- 매뉴얼(SettingsPage)은 `CameraConfig`/`ModuleSetup` 직접 편집, 자동은 `Recipe→LoadRecipe→ApplyToRuntime` 경로 → 레시피 미선택 상태의 매뉴얼 결과가 자동 운전과 다를 수 있음.
- 핸들러 매뉴얼은 TCP GRAB/INSPECT만 요청하고 조명/노출을 미전달 → 비전이 자기 레시피 기준으로 촬상(핸들러 UI에 보이는 값과 실제 촬상 조건 불일치).

## 2-7. 권고 (우선순위)

**P0 (기능 오류 직결)**
1. 노출 단위/소유권 통일: 노출·조명은 비전 레시피를 SSOT로 하고 핸들러 InspectionSubset의 ExposureMs/LightIntensity는 폐기(또는 표시 전용). 유지 시 TCP 경로에 ms↔us 변환 + 적용 확인 응답 추가.
2. 조명 채널 레벨을 레시피 체계(모듈→도구)로 일원화, LightSystemSetup은 "설정(하드웨어 정의)"로 명확히 분리 — 현재 구조 자체는 맞으나 문서화/UI 라벨로 구분.
3. AutoFocusRois/Threshold를 레시피(모듈별)로 이동.

**P1**
4. 검사 임계값 SSOT 1곳 지정 + RECIPE 동기 프로토콜로 전파 (핸들러↔비전 중복 정의 제거).
5. 레시피 이름 동기 단일화(RECIPEREQ/RECIPE 경로 활용, `.last_project`와 `vision.json` 각각 저장 제거).
6. 설정 탭에 디바이스 공통 패턴 확립: "디바이스 목록(카메라/조명/AF...) → 선택 → 파라미터 + [매뉴얼 테스트] 버튼" 구조로 통일. 조명 채널 레벨 즉시 테스트 UI 추가.
7. "default" 폴백 제거 — 레시피 미선택 시 가동/테스트 차단 또는 명시 경고.

**P2**
8. 하드코딩 기본값을 레시피 스키마로 승격(특히 BottomInspector 필터류 — 단, CDT-310 알고리즘 코어 값은 변경 금지 원칙 준수, 저장 위치만 이동).
9. `D:\CDT-320` 폴백 경로를 설정 필수 항목으로.
10. VisionSettings 내 구버전 Scale 필드 등 잔존 필드 정리.

---

## 검증 메모
- 높음 심각도 및 핵심 통일성 항목(VisionTcpClient 큐 매칭, RecipeStore/LotStorage catch{}, Form1 서버 Start catch{}, ExposureMs=500(ms) vs CameraRecipe.Exposure(us), AutoFocusRois 전역 저장)은 소스 라인 직접 재확인 완료.
- 라인 번호는 2026-07-03 기준. 수정 작업은 V:\Source 기준 저장소에서 진행 권장(동기화로 편집이 되돌려지는 이슈 이력 있음).
