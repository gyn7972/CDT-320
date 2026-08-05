# 2단계 착수 보고 — Per-Wafer JSON + Manifest 전환 (문서 §0 요구 현황 보고)

작성: 2026-08-05 심야 | 기준 문서: `docs/material-state/material-per-wafer-json-manifest-implementation-prompt.txt`
(이하 "PWJ 문서") + `...-validation-checklist.txt`

## 1. Git / 작업 트리 상태

- Root: `D:\Source\CDT-320_NEW` (문서에는 `D:\Source\CDT-320_New`로 표기 — 대소문자 차이만, 동일 경로)
- Branch: `master`, HEAD: `1b90dad9` (문서 분석 기준 `ee0914e8`의 후속 — Merge 2개 뒤)
- Dirty(본 작업분): `MaterialStateService.cs`, `MaterialSnapshotStore.cs`, `RecipeStore.cs`,
  `QMC.CDT-320.csproj` 수정 + `MaterialPerfProbe.cs` 신규 (0~1단계 결과물, 미커밋)
- 기타 dirty: 루트의 무관한 프롬프트 md 2건 (건드리지 않음)

## 2. 경로/현황 대조

- 운영 데이터: `D:\CDT-320\State\` 실측(2026-08-05 00:15 기준):
  - `material_state.json` 2,279,007 B (00:15 갱신 — 앱이 0단계 빌드로 구동된 흔적)
  - `material_state.bak` 3,034,614 B (07-25)
  - `material_state.failed.*` 2건 실재 (0 B / 112 KB) — 문서가 지적한 저장 실패 이력과 일치
- 현재 저장 흐름(문서 §4와 일치 확인): 변경 → `NotifyAndSave` → 백그라운드 save worker
  (Quiet 1s / MinInterval 5s) → `_stateSync` 안 딥클론 캡처 → 락 밖 직렬화 →
  `material_state.json` 전체 교체 + `.bak` 회전
- 문서가 요구한 "저장 시간/락 시간 계측": **0단계에서 `MaterialPerfProbe`로 이미 구현됨**
  (`SaveCaptureLock` = 락 보유 캡처 시간, `StateDies`/`StateWafers` 게이지, 30초 요약)

## 3. 문서 §2 보호 대상(diff 0 요구) 준수 상태

- `Sequencing\**` / Interlock / Motion / Vision / Unit: **본 작업 diff 0 유지 중** ✅
- `NotifyAndSave` 호출 위치·순서: 0~1단계에서 **변경 없음** ✅
  (1단계는 조회 경로만 교체 — mutation/알림 경로 불변)

## 4. 변경 대상 파일 (문서 §3 허용 범위 내)

- `Equipment\Materials\Persistence\**` 신규 (core store / manifest / canonical serializer)
- `MaterialSnapshotStore.cs` — facade 유지, 신규 store 위임 분기
- `MaterialStorage.cs` — load facade
- `MaterialStateService.cs` — save worker 영역 한정
- `Form1.cs` — Recovery Required fail-closed 분기 (문서 명시 허용 최소 범위)
- `QMC.CDT-320.csproj` — 신규 Compile 등록

## 5. 문서 대비 확정 구현 계획 (승인 후 실행 순서)

1. **P1 기반**: `Persistence\` 신규 클래스 — CanonicalJson(BOM 없음/공백 없음/문화권 독립),
   Sha256 유틸, StorageKey(SHA-256 hex), Manifest 모델(§9 필드 전부), WaferDocument 모델(§10)
2. **P2 코어 store**: store instance root 주입, 고정 슬롯 ring(manifest 3 / doc revision 3),
   원자 쓰기(tmp→Replace), reparse/containment 검증, 문서 재사용(바이트 동일 시 미기록)
3. **P3 재조립/검증**: Manifest+문서 → `MaterialSnapshot` 재조립, PreviousGlobalDieId 체인,
   NormalizedSnapshotSha256 재계산 대조, 기존 `TryPrepareStateForUse` 재사용
4. **P4 migration/bootstrap**: legacy 로드 → bootstrap 디렉터리 검증 → `Directory.Move` 원자 게시
5. **P5 통합**: save worker가 신규 store 경유(활성 시), `Form1` Recovery Required 분기
6. **P6 검증**: 격리 temp 디렉터리 테스트 하네스(문서 요구), 체크리스트 전 항목

## 6. 위험/미확정 (사용자 확인 필요)

| # | 항목 | 내용 |
|---|---|---|
| 1 | **활성화 시점** | 문서 §0: "사용자가 구현 진행을 명시적으로 승인하기 전에는 코드 수정 금지". 사용자의 "2단계 계속 진행해" 지시를 구현 착수 승인으로 해석하되, **신규 형식 활성화(마이그레이션·운영 데이터 전환)는 별도 확인 전 수행하지 않음** — 신규 코드는 dormant(비활성)로 통합 |
| 2 | RetirementBarrier/EpochId 등 §9의 완전 구현은 규모가 큼 | 1차 구현은 NormalSave+Migration CommitKind 우선, RedundancyRepair/RetirementBarrier는 골격+명시적 미구현 마커(BLOCKED 보고) |
| 3 | 검증 체크리스트 1,162줄 전 항목 통과는 하룻밤 범위 초과 | 통과 가능 항목만 PASS 마킹, 나머지는 BLOCKED/미실행으로 정직 보고 |
| 4 | 밤사이 앱이 00:15에 구동됨(상태 파일 갱신) | 0단계 코드가 이미 운전 노출됨 — 아침에 `MaterialPerfProbe` 로그 확인 가치 |

## 7. 롤백 계획

- 신규 코드는 활성화 플래그/게시 디렉터리 존재 기준으로만 동작 — **게시 전에는 기존 경로 100% 유지**
- 게시 전 롤백 = 코드 원복만으로 완료 (운영 파일 무변경)
- 게시 후 롤백은 문서 §8 규칙(레거시 자동 fallback 금지)에 따름 — **그래서 게시는 사용자 입회 하에만**
