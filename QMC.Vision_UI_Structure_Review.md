# QMC.Vision UI 구조 검토 리포트

작성일: 2026-06-26
대상: `C:\Project\CDT-320\QMC.Vision`
목적: Designer/`.cs` 분리 규칙 준수 여부 점검 + 디버깅 단순화를 위한 구조 정리 지점 도출
규칙 근거: `AGENTS.md` — "컨트롤 선언/배치는 `.Designer.cs`에 인라인, 비즈니스 로직은 일반 `*.cs`"

> ⚠️ 본 문서는 **리포트 전용**입니다. 코드는 아직 수정하지 않았습니다.
> ⚠️ 편집 기준 저장소 주의: 과거 작업상 이 프로젝트의 편집 기준은 `V:\Source`이며 `C:\Project`를 직접 고치면 동기화가 되돌릴 수 있음. 실제 수정 시 경로를 먼저 확정할 것.

---

## 1. 총평

| 항목 | 상태 |
|---|---|
| 전체 `.cs` 수 | 212개 (Designer 34개) |
| Designer → `.cs` 분리 | **대체로 양호** |
| Designer에 로직 침범(역위반) | 거의 없음 (Dispose 정리 호출 수준, 허용) |
| `.cs`에 정적 컨트롤 생성(정위반) | **2개 페이지에 집중** |
| 동적 UI(코드 생성이 정상) | 다수, 문제 아님 |

결론: 구조 자체는 정석에 가깝다. 정리할 부분은 **레시피 타깃 페이지 2종에 집중**되어 있고, 이 둘만 손보면 "디버깅 시 컨트롤이 Designer에 있나 코드에 있나 헷갈리는" 문제의 대부분이 사라진다.

---

## 2. 수정 필요 — 우선순위 높음

### 2-1. VisionTargetPage / InspectorTargetPage : Designer + 코드 혼재

| 파일 | `.cs` | Designer | Designer 컨트롤 필드 |
|---|---|---|---|
| `Ui/Pages/Settings/Recipe/VisionTargetPage.cs` | 898줄 | 517줄 | 21개 |
| `Ui/Pages/Settings/Recipe/InspectorTargetPage.cs` | 875줄 | 432줄 | 20개 |

**문제:** Designer 파일이 멀쩡히 있는데도 `.cs`에서 컨트롤을 추가로 생성·배치한다. 한 화면의 UI가 두 파일에 쪼개져 있어 디버깅 시 추적이 어렵다.

`.cs`에 들어가 있는 UI 생성 메서드 (Designer로 이관 대상):

VisionTargetPage.cs
- `BuildChildPanels()` (L132) — 조명 패널 host에 `_lightPanel` 부착
- `BuildParams()` (L188) — 파라미터 그리드 구성
- `BuildRoiControls()` (L513) — ROI 방향 패드(▲◀●▶▼), 리사이즈 패드(W±/H±/Full), Step 입력칸, info 라벨을 코드로 생성·배치 (약 50줄)
- `RoiCell()` (L566) / `RoiFlow()` (L571) — 버튼 팩토리

InspectorTargetPage.cs
- `BuildRoiControls()` (L142) — **위와 거의 동일한 코드 복붙**
- `RoiCell()` (L190) — 동일
- `BuildChildPanels()` (L275), `BuildParams()` (L331) — 동일 패턴

**핵심 악취 — 중복:** 두 파일의 ROI 패드 생성 블록이 사실상 같다.
```
pad.Controls.Add(RoiCell("▲", (s,e)=>Nudge(0,-1)), 1, 0);
pad.Controls.Add(RoiCell("◀", (s,e)=>Nudge(-1,0)), 0, 1);
... (양쪽 동일)
```
한쪽만 수정하면 다른 페이지와 어긋난다.

**권장 정리:**
1. ROI 패드(방향/리사이즈 버튼 그리드)를 **공용 UserControl** `RoiNudgePad`로 추출
   - Designer 보유, `Nudge/Resize/Recenter/FullSize` 이벤트만 외부로 노출
   - 두 페이지는 이 컨트롤 하나만 Designer에 배치
   - → 복붙 제거 + Designer 일원화 동시 달성
2. 또는 더 가벼운 방법: 기존 `Helper/RecipeLayout.cs`(정적 팩토리, 이미 존재)에 ROI 패드 생성 헬퍼를 추가해 양쪽이 **한 줄 호출**로 동일하게 생성. (단 이 경우 컨트롤은 여전히 코드 생성이므로 "Designer 일원화"는 절충)
   - 권장: **1번(UserControl 추출)** 이 디버깅·일관성 모두 유리.
3. `BuildParams()`의 파라미터 그리드는 항목이 알고리즘별로 가변이므로 **코드 생성이 타당** → 그대로 두되, host 패널만 Designer에 선언(이미 그러함).

> 참고: `RecipeLayout.cs`는 Handler 측과 미러링되는 공용 레이아웃 팩토리(정적)다. 의도된 공유 패턴이므로 유지. 다만 ROI 패드가 이 헬퍼를 안 쓰고 페이지마다 직접 만든 게 중복의 원인.

---

## 3. 경미 — 선택 정리

### 3-1. 인라인 입력 팝업 폼
- `Ui/Pages/Settings/Recipe/RecipePage.cs:667` — TextBox 한 개짜리 입력 폼을 코드로 즉석 조립
- `Ui/Pages/Settings/LightSystemSetupPage.cs:467` — Label+TextBox+OK/취소 입력 폼을 코드로 즉석 조립

→ 이미 있는 `Ui/Controls/NumericKeypadDialog`(Designer 보유) 또는 공용 `TextInputDialog` 하나를 만들어 대체하면 페이지 코드가 단순해지고 입력 UX도 통일된다. 우선순위 낮음.

### 3-2. Designer 없이 코드로만 만든 자체 컨트롤 (규칙 위반이나 허용 가능)
- `Ui/Controls/PaginationBar.cs` — 컨트롤 필드를 `.cs` 상단에 선언·구성
- `Ui/Controls/PickerView.cs` — 채널 셀을 코드로 생성
- `Ui/Controls/SpcTrendChart.cs` — 차트 자체 그리기

→ 재사용 위젯이고 자체 완결적이라 디버깅 부담이 적다. **그대로 둬도 무방.** 다만 신규 컨트롤은 Designer 보유 원칙을 따를 것.

---

## 4. 정상 — 위반 아님 (수정 불필요)

런타임에 개수가 정해지는 동적 UI는 코드 생성이 정석이다:
- `Ui/Pages/Work/OperationPage.cs` — 모듈 수만큼 상태 카드 반복 생성
- `Ui/Pages/Settings/SettingsPage.cs` — 동적 네비게이션 버튼 + 사전 선언 패널 부착
- `Ui/Pages/Settings/Recipe/RecipePage.cs` — 프로젝트/세트 버튼 가변 생성
- `Ui/Controls/PaginationBar.cs` — 페이지 버튼 가변

또한 Designer 파일에서 검출된 "로직" 3건(`InspectorPage`/`FinderPage`/`CameraMappingPanel`의 Designer)은 모두 `Dispose()` 안의 정리 호출(`StopLive()`, `Disconnect()`)로, Designer의 표준 Dispose 패턴 범위 → **허용**.

`CameraMappingPanel.cs`(1002줄, 최대 파일)와 `Form1.cs`(903줄)는 컨트롤 선언이 Designer에, `.cs`는 로직만 → **정석 구조, 모범 사례**.

---

## 5. 권장 작업 순서 (실제 수정 착수 시)

1. `RoiNudgePad` 공용 UserControl 신설 (Designer 보유) — 방향/리사이즈 버튼 + 이벤트 노출
2. `VisionTargetPage` / `InspectorTargetPage`에서 `BuildRoiControls`/`RoiCell`/`RoiFlow` 제거 → `RoiNudgePad` 한 개로 교체, Designer에 배치
3. 두 페이지의 ROI 핸들러(`Nudge/Resize/Recenter/FullSizeRoi`)를 `RoiNudgePad` 이벤트 구독으로 연결
4. 빌드: `QMC.Vision.csproj` Debug 컴파일 확인
5. (선택) 인라인 입력 팝업 → 공용 `TextInputDialog`로 대체
6. 회귀: `perl tools/verify_vision_features.pl` (Vision exe 실행 시)

각 단계는 독립 커밋 권장 — 한 번에 두 페이지를 동시에 바꾸지 말고 한 페이지씩 검증.

---

## 6. 한 줄 요약

> 구조는 양호하다. **`VisionTargetPage`·`InspectorTargetPage` 두 곳의 ROI/패널 인라인 생성 코드를 공용 UserControl로 빼내 Designer로 일원화**하면, 중복 제거와 디버깅 단순화가 동시에 해결된다. 나머지는 대부분 정상이거나 경미하다.
