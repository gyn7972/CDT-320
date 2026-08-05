# Material 성능 개선 — 2단계 체크리스트 (Per-Wafer JSON + Manifest, 야간 범위)

작성: 2026-08-05 | 선행: 1단계 체크리스트 전 항목 ✅ 필요
기준: `docs/material-state/material-per-wafer-json-manifest-implementation-prompt.txt`
야간 범위 원칙: **신규 저장 형식은 코드로 존재하되 비활성(dormant)** — 게시/마이그레이션/운영
데이터 전환은 사용자 입회 검증 전 수행하지 않는다 (stage2_report.md §6-1).

## R-2A. 기반 계층 (Persistence 신규, 런타임 영향 0)

- [ ] A1. `Equipment\Materials\Persistence\` 폴더 신설, 신규 파일 전부 csproj 등록
- [ ] A2. CanonicalJsonSerializer — UTF-8 BOM 없음, 들여쓰기/불필요 공백 없음,
      문화권·시각·프로세스 독립 (동일 의미 문서 → 동일 바이트)
- [ ] A3. Sha256 유틸 + canonical storage key (WaferInstanceId trim → invariant 정규화 →
      UTF-8 SHA-256 full hex)
- [ ] A4. Manifest 모델 — 문서 §9 필수 필드 전부 (StoreFormatVersion/EpochId/Generation/
      SnapshotRevision/CommitKind/WriterSessionId/CapturedSaveVersion/OrderedWaferInstanceIds/
      문서 entry 목록/NormalizedSnapshotSha256/ManifestPayloadSha256 등)
- [ ] A5. WaferDocument 모델 — 문서 §10 (DocumentSchemaVersion/StableId/WaferMaterial 1개/
      canonical-owner Die 목록/PreviousGlobalDieId 체인). 저장마다 바뀌는 전역 필드 미포함
- [ ] A6. 빌드 통과 + 기존 저장 경로 diff 0 (dormant 확인)

## R-2B. 코어 store (파일 계층, 아직 미배선)

- [ ] B1. store instance root 주입식 core store (production static 바인딩과 분리)
- [ ] B2. 고정 슬롯 ring — manifest 0/1/2, 문서 revision 0/1/2 (무한 증가 파일 없음)
- [ ] B3. 원자 쓰기: 같은 volume tmp → File.Replace/Move, 경로 containment +
      reparse point 거부
- [ ] B4. 문서 재사용: 이전 commit 문서와 canonical 바이트/SHA-256 동일 시 재기록 생략
- [ ] B5. Save: MaterialSnapshot → wafer 분해(입력 instance 기준 canonical owner) →
      변경 문서만 기록 → Manifest 커밋 (Generation 단조 증가)
- [ ] B6. Load: 최신 유효 Manifest 선택(§9 규칙: mtime/크기 사용 금지, Generation 순,
      hash 검증) → 문서 검증 → MaterialSnapshot 재조립 (Wafers 순서/Dies 전역 체인 복원)
- [ ] B7. 왕복 등가성: Save→Load 재조립 결과가 원본과 normalized digest 동일

## R-2C. 격리 검증 (운영 경로 무접촉)

- [ ] C1. 격리 임시 디렉터리 테스트 하네스 — 운영 `D:\CDT-320` 무접촉 (문서 §1 보호 규칙)
- [ ] C2. 시나리오: 왕복 등가 / 문서 재사용(불변 wafer 미기록) / manifest ring 회전 /
      손상 manifest 거부(높은 Generation 우선하되 hash 불일치 거부)
- [ ] C3. 빌드 통과 + 적대적 리뷰 CONFIRMED 0건

## R-2D. 명시적 야간 범위 제외 (BLOCKED — 사용자 입회 필요)

- [ ] D1. 운영 저장 경로 배선(save worker 위임) — **미수행** 사유 기록
- [ ] D2. Legacy → bootstrap → 원자 게시 마이그레이션 — **미수행**
- [ ] D3. Form1 Recovery Required 분기 — **미수행** (활성화와 동시 작업이어야 의미)
- [ ] D4. RedundancyRepair / RetirementBarrier 완전 구현 — 골격만, BLOCKED 보고
- [ ] D5. 검증 체크리스트(1,162줄) 전 항목 — 야간 통과분만 PASS, 나머지 미실행 명시

## 검증 결과 기록

검증일: 2026-08-05 심야

### R-2A 기반 계층
- ✅ A1. `Persistence\` 신설 + 3파일 csproj 등록 (`PwjFormat.cs`/`PwjStore.cs`/`PwjSelfTest.cs`)
- ✅ A2. Canonical 직렬화 — DCJS + **DateTime 라운드트립("o", RoundtripKind)**.
      ⚠ 최초 구현(기본 DCJS)은 `DateTime.MinValue` UTC 변환 예외로 **자가검증 5/5 FAIL** —
      레거시 스토어가 정규화로 우회 중인 실제 결함과 동일 계열. 실행 검증이 잡아냄 → 수정
- ✅ A3. SHA-256 유틸 + canonical storage key (trim→invariant→UTF-8 SHA-256 hex)
- ✅ A4. Manifest 모델 §9 필드 전부 (RedundancyRepair/RetirementBarrier는 필드만 — R-2D)
- ✅ A5. WaferDocument §10 (전역 순서 링크 `PreviousGlobalDieIds`, 저장마다 변하는 전역 필드 미포함,
      무소유 die는 orphan 문서)
- ✅ A6. 빌드 EXIT=0, 기존 저장 경로 diff 0 (dormant)

### R-2B 코어 store
- ✅ B1. root 생성자 주입 / B2. manifest·문서 slot ring 3 고정 / B3. tmp→Replace 원자 쓰기 +
      containment + reparse 거부 / B4. SHA 동일 시 재기록 생략 / B5. wafer 분해 저장 /
      B6. Generation 순 선택(파일 mtime/크기 불사용, payload hash 검증, Epoch 불일치 거부) /
      B7. 왕복 등가

### R-2C 격리 검증 — **실제 실행 결과**
- ✅ C1. 격리 temp(scratchpad) 사용, 운영 `D:\CDT-320` 무접촉
- ✅ C2. **5/5 PASS** (실행): 왕복 등가 / 문서 재사용(불변 wafer 미기록 + 신규 1파일만 증가) /
      ring 회전(저장 8회에도 manifest≤3·문서≤3, Generation=8 유지) / 손상 manifest 거부
      (payload hash 불일치 → 이전 세대로 복구) / 전역 die 순서 체인(교차 배치 포함) 보존
- ⚠️ C3. 적대적 리뷰 결과: **CONFIRMED 다수** (dormant라 운영 영향 없음).
      즉시 수정 5건 반영 후 재빌드 + 자가검증 5/5 PASS 재확인:
      ① DieId 공백/중복 저장 시점 거부 (체인 오염 차단) ② 재사용 전 디스크 실물
      해시 재검증 (소실 참조 전파 차단) ③ CommitKind 화이트리스트 (미구현 종류 거부)
      ④ WriteFileAtomic Flush(true) (전원 차단 창 축소) ⑤ SnapshotRevision 역행 저장 거부
      **잔여 설계 수준 결함 — 배선 전 필수 수정 목록** (이것이 해소되기 전 운영 배선 금지):
      (a) 문서 손상 시 이전 세대 fallback 부재 (Load 전면 실패 — §17)
      (b) Manifest 자기해시가 재직렬화 기반 — 스키마 진화 시 구 스토어 전량 손상 판정
          (raw bytes 구간 치환 방식으로 교체 필요)
      (c) 재사용 문서의 세대 간 단일 파일 공유 — §13 replica 교대 정책 필요
      (d) Local DateTime의 TZ offset이 canonical byte에 포함 — 시간대 독립 계약 위반
      (e) Generation/Revision 역전 검출의 슬롯 순서 의존 (전 슬롯 대조로 교체)
      (f) 레거시 NormalizeSnapshotDateTimes와의 dual-write 순서 규약 미정의
      (g) IO 일시 오류 재시도/복구 사본 부재 (레거시 5회 재시도 대비 후퇴)

### R-2D 야간 범위 제외 (계획대로 BLOCKED)
- ✅ D1~D5 전부 미수행으로 기록 — 운영 배선/마이그레이션/Form1 분기/Barrier 완전 구현은
      **사용자 입회 검증 후** 진행 (stage2_report.md §6-1, 문서 §0 승인 게이트 준수)

### 2단계 야간 판정
**조건부 완료** — 기반 계층/코어 store/격리 실행 검증은 완료(빌드 OK, 자가검증 5/5 PASS ×2회),
단 C3 잔여 목록 (a)~(g) 해소 전에는 **운영 배선 절대 금지**. 이 목록이 다음 작업의 입력이다.
