# 아침 보고 — Material 성능 개선 0→1→2단계 야간 작업 결과 (2026-08-05)

지시: "체크리스트 만들고 검증하며 0→1→2단계 순서대로 진행" — 순서·게이트 준수 완료.
**전부 미커밋 워킹 트리 상태** (커밋은 지시 대기). 빌드 출력이 `D:\CDT-320\`(운영 폴더)로
배포되는 프로젝트 설정이므로, 현재 운영 exe에는 0~1단계 코드가 포함되어 있음.

## 단계별 결과 요약

| 단계 | 결과 | 검증 |
|---|---|---|
| **0단계** 세대 누수 수정 + 계측 | ✅ 통과 | 리뷰 CONFIRMED 2건(로그 정책 게이트) 수정, 빌드 OK |
| **1단계** 캐시/인덱스 | ✅ 통과 | 1차 리뷰 CONFIRMED 8건 → 전부 수정 → 폐쇄 검증 allClosed=true, 빌드 6회 전부 OK |
| **2단계** per-wafer 저장 | 🔶 조건부 완료 | dormant 구현 + 격리 실행 자가검증 **5/5 PASS ×2회**. 배선 전 필수 수정 목록 (a)~(g) 도출 — **운영 배선 금지 유지** |

## 변경 파일 (미커밋)

- `MaterialStateService.cs` — 세대 필터, 계측 6곳, _dieById 인덱스, InputPickContext 캐시 + 무효화 5경로
- `MaterialSnapshotStore.cs` — CloneObject 방어 (배열/사전/순환·공유 참조)
- `RecipeStore.cs` — 손상 fail-closed 유지형 검증 캐시 + LoadLastOrDefaultCached
- `InputStageMapTransferPage.cs` — 캐시 무효화 2곳 (리뷰 결함 폐쇄 목적의 유일한 UI 변경)
- `MaterialPerfProbe.cs` (신규) — 30초 요약 기준선 계측
- `Persistence\PwjFormat.cs`/`PwjStore.cs`/`PwjSelfTest.cs` (신규, dormant)
- `QMC.CDT-320.csproj` — 신규 4파일 등록

## 기대 효과 (1단계 적용분)

- 20Hz 게이트: 158만 회 문자열 비교/호출 → 사전 O(1) + 캐시 (재구축 ~80회/초 → 최악 ~1회/초, **≥98.7% 감소**)
- 락 안 레시피 디스크 I/O: 웨이퍼당 ~1초 → ~0
- 저장 캡처 락: TrimInspection 선행은 미적용(2단계 배선과 함께) — 딥클론 자체는 여전히 N 비례
  → per-wafer 배선이 최종 해소

## 아침에 확인해 주실 것

1. **계측 로그**: 어제 00:15에 앱이 구동된 흔적 있음 → 로그에서 `MaterialPerfProbe` 검색.
   `Perf summary(30s)` 라인의 `IsInputStagePickComplete`/`SaveCaptureLock`/`StateDies` 수치가
   0단계 기준선. 오늘 돌리면 1단계 효과가 같은 이름으로 비교됨
2. **1랏 시운전**: 웨이퍼 2~3장에서 체감되던 감속이 사라졌는지 + 알람/오동작 유무
   (특히 수동 die 편집·재픽업·맵 편집 후 픽업 재개가 정상인지 — 이번 캐시 무효화 경로들)
3. **커밋 여부 결정**: 이상 없으면 커밋 지시 주시면 단계별로 나눠 커밋

## 다음 결정 사항 (2단계 배선 전)

- 배선 전 필수 수정 목록 (a)~(g): `05_checklist_stage2.md` 참조 — 핵심은
  (a) 문서 손상 시 이전 세대 fallback, (b) manifest 해시를 raw bytes 기반으로,
  (c) 재사용 replica 정책, (d) DateTime 시간대 독립화
- 이 목록 수정 → 재검증 → 사용자 입회 하에 마이그레이션/배선이 정석 순서

상세 증적: `05_checklist_stage0/1/2.md`, `stage2_report.md` (모두 이 폴더)
