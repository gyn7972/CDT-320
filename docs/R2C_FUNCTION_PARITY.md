# R2c (B) — RecipePage 기능 패리티 (옛 트리 vs 새 사이드바)

> 생성: 2026-06-08. R2c Step 0(읽기 전용). 옛 RecipePage(R2 이전, git) 기능 전수 → 새 설계 도달 가능/누락.

## 옛 RecipePage(R2 이전) 노출 기능 전수
| # | 기능 | 진입(옛) | 동작 | 새 설계 도달 | 비고 |
|---|---|---|---|---|---|
| 1 | 모듈→Finder 편집 | 좌측 트리 노드(5모듈×Finder) | `_content`=FinderPage(mod,finder) | ✅ 사이드바(알고리즘)→세팅선택기(finder)→VisionTargetPage | 페이지 교체(FinderPage→VisionTargetPage, 기능 동일) |
| 2 | 모듈→Inspector 편집 | 좌측 트리 노드(Inspector) | `_content`=InspectorPage(mod,insp) | ✅ 세팅선택기(inspector)→InspectorPage | 동일 |
| 3 | **SPC X-bar Chart** | 상단바 `_btnSpc` → ShowSpc() | `_content`=SpcChartPage | ❌ **누락** | R2b 사이드바에서 제거됨. ShowSpc() 메서드는 보존(미연결) |
| 4 | **검사 파라미터(에디터)** | 상단바 `_btnParams` → ShowParameterEditors() | `_content`=ParameterEditorHost(5에디터 콤보) | ❌ **누락** | R2b 제거. ShowParameterEditors() 메서드 보존(미연결) |
| 5 | 헤더(모듈명) | `_hdr` "Recipe — Module" | 표시 | ✅ 브레드크럼 "Recipe — 알고리즘/세팅" | 개선 |
- 옛 RecipePage 에 컨텍스트메뉴/기타 버튼/숨은 진입점 **없음**(Designer 확인: _hdr + _bar[_btnSpc,_btnParams] + _table[_tree,_content] 뿐).

## 누락 = SPC X-bar(#3) + 검사 파라미터 에디터(#4) — 2건
- 둘 다 `_content` 호스트로 페이지 스왑하던 **글로벌**(알고리즘/세팅 무관) 기능. 옛 동작·페이지(SpcChartPage / ParameterEditorHost) 그대로 보존하며 진입점만 새 설계에 복원 필요.

## ★ 배치 결정 필요(보고 후 컨펌 — 추측 금지)
SPC / 검사파라미터 진입점을 어디에:
- **(가) 사이드바 하단 "일반" 그룹**: 검사 알고리즘 5개 아래 구분선 + [SPC X-bar][검사 파라미터] 버튼(Handler TabBase 의 보조메뉴 영역 패턴). 선택 시 `_content`=Spc/ParameterEditorHost(세팅선택기는 비움/숨김).
- **(나) 상단바 버튼**: 헤더 우측 SAVE 옆에 [SPC][파라미터] 버튼(옛 위치 유사).
- **(다) 별도 상단 탭**: 본문 위 탭(타깃 편집 / SPC / 파라미터).
- 권고: **(가)** — Handler 사이드바 일관성 + 알고리즘과 시각 통합. 단 SPC/파라미터는 알고리즘 비종속이라 선택 시 세팅선택기 바를 숨기고 `_content` 전체 사용.
