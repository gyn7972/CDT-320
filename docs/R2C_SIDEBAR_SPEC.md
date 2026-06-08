# R2c (C) — Handler 사이드바 비주얼 스펙 + Vision 현황 대조

> 생성: 2026-06-08. R2c Step 0(읽기 전용). 출처: `QMC.CDT-320/Ui/Controls/SidebarButton.cs`, `Ui/Tabs/TabBase.Designer.cs`, `Ui/UiTheme.cs`.

## Handler 사이드바 비주얼 스펙 (미러 기준)
### 컨테이너 (TabBase)
- PnlSidebar: **Dock=Right, Width=UiTheme.SidebarWidth(210)**, BackColor=SidebarBg(#595959).
- LblSidebarHeader: Dock=Top, **Height=50**, BackColor=SidebarHeaderBg(#F0F0F0), ForeColor=SidebarHeaderFg(Black), Font=**SectionFont**(맑은고딕 11 Bold), **TextAlign=MiddleRight, Padding(0,0,16,0)**.
- PnlSidebarButtons: Dock=Fill, FlowDirection=TopDown, WrapContents=false, **AutoScroll=true**, BackColor=SidebarBg, **Padding(4,6,4,6)**.

### SidebarButton (Control, 커스텀 페인트)
- 기본 Size **180×46**(TabBase 가 Width=SidebarWidth-8=**202** 로 설정), Margin(0,0,0,2), Cursor=Hand, Font=ButtonFont(맑은고딕 11).
- 색: 선택=SidebarBtnSelBg(White)/SidebarBtnSelFg(#222) · hover=#707070 · 기본=SidebarBtnBg(#595959)/SidebarBtnFg(White).
- 텍스트 **중앙 정렬**(H/V center). 하단 **1px 구분선 #404040**.
- 상태점 **내장 없음**(Handler SidebarButton 엔 점 없음 — (A) 는 신규 추가, 동일 톤으로 좌측 색바/점).

## Vision 현황(R2b) vs Handler — 차이
| 항목 | Vision(R2b 현재) | Handler | 조치 |
|---|---|---|---|
| 사이드바 폭 | body TLP col 240 | PnlSidebar Dock=Right 210 | 210 으로 + (현 TLP col 유지 가능, 폭만 210) |
| 헤더 | H28, MiddleLeft, ButtonFont, "검사 알고리즘" | H50, **MiddleRight**, SectionFont, Pad 우16 | 미러 |
| 버튼 | 기본 Button 224×34, 좌정렬, flat | **SidebarButton** Control 202×46, **중앙**, 커스텀페인트, hover#707070, 하단구분선 | **Vision SidebarButton 신규**(비주얼 1:1) |
| 상태점 | setting-selector 버튼 내부 자식 Panel → **렌더 안 됨**(Button 은 컨테이너 아님) | (없음) | **커스텀 페인트로 점 그림**(렌더누락 해소) — 알고리즘 버튼에 표시(A) |
| 결합 | 없음 | Security/UserLevel/i18n/UiClickAuditor | Vision 은 결합 **제외**(비주얼만) |

## 구현안 (C)+(A)
- **Vision `Ui/Controls/SidebarButton.cs` 신규**: Handler 페인트 1:1(선택/hover/기본 색, 중앙텍스트, 하단 구분선) + **상태점**(좌측 작은 원/색바: 미설정 회색 / 설정완료 녹 / 변경됨 주황) 페인트. `Selected` bool + `Status` enum(Off/Done/Dirty) 속성. Designer/Code 분리 불요(Control, IC 없음) 또는 단순 partial.
- RecipePage 사이드바를 SidebarButton 으로 교체(폭 210, 헤더 50/MiddleRight). 알고리즘 버튼 Status=그 알고리즘 세팅들의 집계(any dirty→Dirty / any 저장→Done / 없음→Off).
- ⚠ 상태점 렌더누락 근본원인 = **Button 에 자식 Panel(dot) 추가** → Button 비컨테이너라 미표시. 커스텀 페인트(OnPaint 에서 직접 점 그림)로 해소.
- TLP 베이스/IC 선언적 유지. 기존 동작(알고리즘 선택→스왑) 보존.
