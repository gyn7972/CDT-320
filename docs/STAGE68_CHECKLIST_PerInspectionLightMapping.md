# STAGE 68 — CHECKLIST: 검사별 조명 매핑

- **연계 SPEC**: `docs/STAGE68_SPEC_PerInspectionLightMapping.md`
- **작성일**: 2026-05-29
- **상태**: SPEC + CHECKLIST 작성 단계 (구현 미진입 — 사용자 컨펌 대기)
- **사전 조건**: SPEC §12 확인 필요 #1~#8 해소. 의존: Stage 67(LFine) + Stage 64(검사 트리).

> ⚠ = SPEC §12 옵션 결정 의존. 구현 = Stage 69.

## A. 데이터 모델 (QMC.Common)
- [ ] `LightControllerEntry` / `LightChannelLabel` / `LightSystemSetup` 추가 — **PortName 자연키(Id 없음)**
- [ ] `AlgorithmLightWiring` 추가 — 알고리즘 ↔ (ControllerPort, Channels[], Page)
- [ ] Setup Save 검증 — PortName 중복 거부 + 모든 AlgorithmWiring.ControllerPort 가 Controllers 에 존재
- [ ] `InspectionLightSetting` / `InspectionLightOverride` 추가 — Recipe 측 (Channel, Level, On, StrobeTimeUs) 만
- [ ] `AlgorithmCameraMapping.InspectionLights` 추가 ⚠ (#2 옵션 A) — `EmitDefaultValue=false`, null=미사용
- [ ] `LightSystemSetupStore` (Load/Save, `Config\light_system.json`) + 구버전 호환(없으면 빈 리스트)

## B. LightHub (QMC.Vision)
- [ ] `Comm\LightHub.cs` — `Dictionary<PortName, ILightController>`
- [ ] `Form1.Load` 에서 Setup.Controllers 순회 → `LightControllerFactory.Create` → 등록 (Sim 모드 분기)
- [ ] `LightHub.Get(port)` 라우팅 + 미등록 시 null
- [ ] ⚠ `ILightController.SwitchPageAsync(int)` 추가 (PageCount>1, #3) — PageCount==1 이면 no-op

## C. 마이그레이션
- [ ] `Config\LightSystemMigrator.cs` — io_set.lightSource.json PORT 별 그룹핑 → Controllers 생성 + 채널 라벨
- [ ] 채널 이름 휴리스틱 → AlgorithmWirings 자동 채움 ⚠ (#5) — 모호(RING/ALIGN) 미할당 + 사용자 검토 안내
- [ ] io_set 3포트 → 2 컨트롤러 재매핑 ⚠ (#1)
- [ ] 원본 백업 ⚠ (#7, `.bak.YYYYMMDD`) + 첫 기동 1회 보고

## D. UI — Setup 페이지
- [ ] `Ui\Pages\LightSystemSetupPage.cs` — 섹션1(컨트롤러 인벤토리+채널 라벨) + 섹션2(알고리즘 결선 표 5행)
- [ ] SettingsPage 트리 "■ 시스템 설정 / 조명 시스템" 노드 (prefix `sys:light`)
- [ ] 결선 표: 알고리즘 5행 × (Controller 콤보, Channels 다중체크, Page)
- [ ] **포트 일괄 변경 버튼** ⚠ (#8) — 옛→새 PortName 원자적 동시 갱신 (Entry + 모든 Wiring)
- [ ] Validation: Port 중복 / ChannelCount>0 / 채널 겹침 경고(비차단)

## E. UI — InspectionLightPanel
- [ ] `Ui\Pages\InspectionLightPanel.cs` — 결선 헤더(읽기전용) + 값 편집 표
- [ ] 검사 노드(`cam:<alg>:<insp>`) 우측 디테일 결합 ⚠ (#6 배치: 탭/좌우/아래)
- [ ] Channel 콤보 = **풀(AlgorithmLightWiring.Channels) 내 채널만** 노출
- [ ] 행 추가/삭제 (풀 잔여 시만) + On/Off All
- [ ] Save / Apply / Reset / Cancel
- [ ] 슬라이더 디바운스 ⚠ (#4, 50ms) → `LightHub.Get(port).SetPowerAsync` 라이브
- [ ] 결선 미설정/풀 빈 경우 비활성 + 안내

## F. 알람
- [ ] `QMC.Common\Alarms\AlarmMaster.cs` 신규 3 — `LIGHT-WIRING-MISS` / `LIGHT-MAP-INVALID` / `LIGHT-CHANNEL-OUT-OF-POOL` (Warning, ko/en)
- [ ] Apply 시점 raise (Stage 67 LIGHT-* 6 재사용)

## G. 검증
- [ ] `dotnet build` warning 0 / error 0
- [ ] verify_all 회귀 없음 (현 65 PASS)
- [ ] `tools\verify_stage69.ps1` 신규:
  - "LightSystemSetup 로드 시 컨트롤러 2개 정합"
  - "InspectionLightOverride 직렬화 라운드트립 (빈→Load→Save→빈 유지)"
  - "io_set 마이그레이션 후 컨트롤러 N + 8 채널 라벨 일치"
- [ ] **실행/스크린샷은 보조(2번) 모니터** — 구현 Stage 강제

## H. 문서
- [ ] `MISMATCH_RESOLUTION_LOG.md` M-68-1~5 추가
- [ ] `ARCHITECTURE_EXPORT.md` 재생성 권장
- [ ] `STAGE69_RESULT_*.md` (구현 Stage)

## I. Git
- [ ] 브랜치 `stage-68-per-insp-light-mapping-spec` (완료)
- [ ] SPEC + CHECKLIST + MISMATCH 첫 커밋 (← 본 단계 종료점)

---

## 진행 순서
1. SPEC §12 #1~#8 컨펌
2. 구현 Stage(69): A → B → C → D → E → F → G

## 예상 작업 시간
구현 Stage 69 기준 **8 ~ 12 시간**
