# CDT-320 verify 베이스라인 (재정립)

갱신일: 2026-06-08. 근거: `docs/VERIFY_SUITE_INVENTORY.md`(정찰) + 본 재정립(repoint→격리→패턴수정→런타임분리).
구 베이스라인 **"117/118"은 폐기**(은퇴 백업 repo `D:/Work/CDT-320/QMC.CDT-320` 기준 거짓신호였음).

## 코어 정적 베이스라인 (CI/회귀 기준)
`perl tools/verify_all.pl` — Handler 빌드/런타임 **비의존**, 현행 flat 트리 `D:/Work/source` 대상.

| 그룹 | 스크립트 | TOTAL | PASS | FAIL | SKIP |
|---|---|---|---|---|---|
| Vision 기능 | stage2 | 10 | 10 | 0 | 0 |
| Vision 기능 | stage6 | 5 | 5 | 0 | 0 |
| Vision 모듈 | stage52 | 4 | 4 | 0 | 0 |
| 자동화 존재 | stage14 | 2 | 2 | 0 | 0 |
| 자동화 존재 | stage15 | 4 | 3 | 0 | 1 |
| 자동화 존재 | stage17 | 1 | 1 | 0 | 0 |
| 자동화 존재 | stage18 | 2 | 2 | 0 | 0 |
| Vision 정적 | vision_features | 12 | 11 | 0 | 1 |
| **합계** | **8 스크립트** | **40** | **38** | **0** | **2** |

- **FAIL 0**. SKIP 2 = `QMC.Vision.exe` 미실행 시 런타임 PING 건너뜀(정적 환경 정상 동작 — 실행 중이면 PASS 전환).
- 실행: `perl tools/verify_all.pl` (vision_features 정적 요약은 러너 말미 별도 출력).

## 런타임 그룹 (옵트인 — 코어 제외)
`perl tools/run_runtime.pl` — 실행환경(Vision.exe spawn + TCP, 프로세스 가동) 필요.
- verify_comm (TCP 5100/5101/5103 라운드트립), verify_cognex_runtime (프로세스 가동). 장비/실행 환경에서만 수동 실행.

## 격리 그룹 (`tools/retired/`, 37개 — 삭제 아님)
은퇴 백업의 구 AI Handler 스캐폴딩 기준이라 팀 권위 Handler(origin 머지본)와 불일치 → 코어에서 제외.
사유·복원법·재작성 시점은 `tools/retired/README.md`. **Handler UI 디자이너 스윕 시 팀 아키텍처 기준 재작성 후보.**

## 경로/이식성
전 스크립트 `use FindBin; $ROOT="$FindBin::Bin/.."` 상대경로화 — 트리 이동에 불변(은퇴 백업 하드코딩 제거).

## 회귀 결론
repoint·격리·패턴수정 전 과정에서 **실제 코드 회귀(iii) 0건**. repoint 직후 FAIL 80은 전부
환경(Handler 미빌드 38) + 코드진화(ii, 42: 파일부재/심볼변경/명칭변경)로 확정됨(정찰 인벤토리 참조).
