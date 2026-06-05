# Stage 89 Fix — vision.json LightUseSim 이 항상 true 로 되돌아가는 문제

## 증상
사용자가 `vision.json` 의 `"LightUseSim":false` 로 수정해도 앱 실행 후 다시 `true` 로 변환됨.

## 원인 — OnDeserializing 강제 true + Load 끝 자동 Save
`VisionSettings`(`QMC.Vision\Config\VisionConfig.cs`):
- `[OnDeserializing]`(라인 40)이 **매 로드 시 `LightUseSim = true` 강제**.
- DataContract 는 프로퍼티 이니셜라이저를 실행하지 않으므로 구버전 키 누락 대비로 OnDeserializing 에 기본값을 심는데, LightUseSim 까지 무조건 true 로 덮음.
- `VisionConfigStore.Load`(라인 152)가 끝에서 `Save()`(마이그레이션 정규화) → 메모리 true 가 디스크에 다시 기록 → 사용자가 적은 false 소멸.

## 수정 (`VisionConfig.cs`, 2줄)
1. 라인 20: `= true` → `= false` (실장비 기본, Sim 은 명시 opt-in).
2. OnDeserializing 의 `LightUseSim = true;` 라인 **제거** (나머지 viewer 기본값 라인은 유지).

### 효과
| JSON | 결과 |
|------|------|
| `"LightUseSim":false` | ✅ false 유지 |
| `"LightUseSim":true` | ✅ true 유지 |
| 키 없음 / 빈 파일 | ✅ false (default(bool), OnDeserializing 강제 제거) |

"안전 기본 = Sim" → "실장비 기본" 정책 변경. 단 `LightHub.ConnectAllAsync` 가 시리얼 Open 실패해도 `LIGHT-OPEN-FAIL` 알람만 raise 하고 앱은 진행하므로 위험 없음. Sim 사용 시 `"LightUseSim":true` 명시.

## 검증 — 실 Store 라운드트립 (Load + 자동 Save 포함)
하니스 `cdt-320\lightusesim-test\LightUseSimRoundtripTest.cs` — `VisionConfigStore.Load`/`Save`/`Path_`/`Current` 직접 사용. **ALL PASS**:

| 시나리오 | 결과 |
|---------|------|
| 1. false: 파일 false → Load → 메모리 false → 자동 Save 후 파일에 `"LightUseSim":false` 유지 | ✅ |
| 2. true: 라운드트립 유지 | ✅ |
| 3. 키 누락 → default false | ✅ |
| 4. vision.json 없음 → 새 객체 false + 파일 생성 | ✅ |
| 빌드 (MSBuild QMC.Vision) | ✅ 오류 0 / 신규 경고 0 |

> 하니스가 사용자 버그 경로(파일 편집 → Load → 끝의 자동 Save → 파일 재기록)를 그대로 재현. 이전 코드면 S1 에서 파일이 true 로 뒤집혔을 것. 실 LFine 점등(시나리오 5)은 HW 필요로 미실측 — LightHub 실 컨트롤러 초기화는 기존 검증됨.

## 규칙 준수
- 수정 범위 `VisionConfig.cs` 만. `OnDeserialized`/`VisionConfigStore.Load` 자동 Save 흐름 미수정.

## 커밋
로컬 커밋 + master 머지 + 브랜치 삭제. **remote push 안 함 — 사용자 컨펌 대기.**
