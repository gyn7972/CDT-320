# CAM_RIGHTLAYOUT_PLAN — CameraMappingPanel 우측 컬럼 반응형

브랜치 `ui-cameramapping-right-layout`. 버튼=상단 가로 플로우(줄바꿈), 미리보기=남는 공간 채움. 좌측·동작·텍스트 불변.

## 현 상태 (실측 — 직전 2열 재정렬에서 배치한 구조)
- 우측 = 절대좌표 @ x700 (Designer): 버튼 8개 3줄(`_btnSave`(700,40)…`_btnLiveStop`(916,116)) + `_lblStatus`(700,158,430×24) + `_picPreview`(700,188,430×242 고정, 앵커 없음).
- `_body` = Panel(AutoScroll, Dock=Fill, Padding 10/12/10/10, 1140×595). 패널은 _detailHost 에 Fill → 창 리사이즈 전파됨.

## 방식 비교 → docking 스택 택1 (권장안 채택)
- **(A) docking 스택(채택)**: `_rightPanel`(Anchor 4방향) 안에 Dock=Top flow + Dock=Top status + Dock=Fill preview. 줄바꿈으로 flow 높이가 변하면 **아래 컨트롤이 자동으로 밀림** — 레이아웃 엔진이 처리.
- (B) 절대좌표 + 고정높이 flow + 앵커: flow 가 2줄로 접힐 때 status/preview Y 를 코드로 재계산해야 함(SizeChanged 핸들러 필요) — 복잡, 기각.

## 확정 구조 (Designer 인라인 — 람다·객체초기화자·지역변수 산술 금지)
```
_body
 ├─ (좌측 불변: 카메라 필드·DCF·조명 그리드 — x20~670 절대좌표 유지)
 └─ _rightPanel : Panel
      Location (700, 36) / Size (430, 549) — 1140-700-10(右패딩), 595-36-10(下패딩)
      Anchor = Top|Bottom|Left|Right   → 창 리사이즈 시 우/하로 확장
      Controls.Add 순서 = _picPreview → _lblStatus → _flowButtons  (Dock 처리는 역순: flow Top → status Top → preview Fill)
      ├─ _flowButtons : FlowLayoutPanel — Dock=Top, FlowDirection=LeftToRight, WrapContents=true,
      │     AutoSize=true, AutoSizeMode=GrowAndShrink. 버튼 8개 이동(순서 Save·Cancel·Reset·Apply·TestGrab·Connect·LiveStart·LiveStop).
      │     각 버튼: Size/Text/스타일/Click 핸들러 전부 그대로, Location 제거 → Margin(0,0,4,4).
      ├─ _lblStatus : Dock=Top, Height 24 (텍스트·폰트·색 불변)
      └─ _picPreview : Dock=Fill, SizeMode=Zoom, BackColor 검정 — 남는 공간 전부
```
- 효과: 폭 좁아짐 → flow 자동 줄바꿈(2~3줄) → status/preview 가 자동 하향 + preview 축소. 폭 넓어짐 → 버튼 한 줄 + preview 확대.
- 좌측 컬럼·`_lblAlgorithm` 헤더·SaveAll/Connect/Live 코드 일절 불변(컨트롤 참조 이름 동일 — 부모만 변경).

## 영향점 (게이트에 포함)
- **CamLightSmoke 의 `_picPreview.Location==(700,188)` 단언** → 구조 단언으로 교체(`_rightPanel` 존재 + preview.Dock==Fill + flow.WrapContents). 나머지 단언(조명 그리드) 불변.
- 디자이너 로드: 표준 컨트롤(Panel/FlowLayoutPanel)·인라인 선언만 사용 → 가능.

## 게이트
빌드0(Common→Vision)/정적0/verify 코어 FAIL0/run_smoke.ps1 전체 PASS(샌드박스)/실행 스샷(넓힘=한 줄+큰 미리보기, 좁힘=줄바꿈+미리보기 조정, 좌측 불변)/디자이너 로드/타 페이지 R2 보존/push 안 함.
