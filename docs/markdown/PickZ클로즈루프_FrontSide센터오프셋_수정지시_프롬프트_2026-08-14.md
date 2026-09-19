# [수정 지시] PickerZ 클로즈드 루프 — FrontSide 비전 center_offset_mm로 Z축 보상 (Pick·Place·Bottom검사·Side검사 전부, 감산 적용)

작업 대상: `D:\Source\CDT-320_New`
지시자: 팀장님 (2026-08-14 지시 — **편집은 변경 목록 고지·승인 후**)
전제: **비전 PC 수정은 팀장님 담당** — FrontSide 결과의 `chN_side_item_center_offset_mm`이
실측값을 반환하도록 비전 쪽에서 수정 예정. 이 프롬프트는 핸들러 쪽만 다룬다.
선행 분석: 2026-08-14 세션 — 설비로그(Event/2026-08-14.csv, 08-13.csv) 및
Side 결과 수신·Z 산식 구조 분석 완료

---

## 0. 이 작업의 규칙 (반드시 준수)

1. **시킨 것만 한다.** 아래 "변경 범위"에 없는 것은 손대지 말 것. 걸림돌이 생기면
   임의로 해결하지 말고 **"문제 있다, 어떻게 할까요?"로 팀장님께 물어볼 것**.
2. **시퀀스/좌표 산식 코드는 무조건 승인 후 진행.** 변경할 파일·함수·라인 목록을
   팀장님께 전부 고지하고 승인받은 뒤 편집한다. **Z축은 충돌 리스크가 있는 축이다 —
   적용 지점 4곳의 정확한 산식 위치를 반드시 사전 고지할 것.**
3. **빌드 주의**: 기본 빌드 출력이 실장비 실행 폴더(`D:\CDT-320`)로 직결된다.
   장비 실행 중 기본 빌드 금지. 검증 빌드는 `/p:OutDir=<임시경로>` 우회.
4. **모션 속도 스케일 규칙**: 이번 작업은 **기존 Z 이동의 목표값 보정만** 한다.
   신규 이동 명령/속도 코드를 작성하지 말 것(기존 이동 함수·가드 경로 그대로 사용).
5. **계측 필수**: 샘플 유입·필터 갱신·적용 4곳·스킵마다 수치를 로그로 남겨 실장비
   1회 실행으로 "어떤 값이 어디에 얼마나 반영됐는지" 확정 가능하게. (§4)
6. `ActualPosition`이 지령값을 반환하는 것은 의도된 설계다. 교정하지 말 것.
7. 시뮬레이션 모드 확인 후 실장비. **실장비 최초 가동은 클램프 한계를 줄여(§5-3)
   소량으로 보정 방향부터 검증한다.**

---

## 1. 배경 (로그 분석 확정 사실)

### 1-1. FrontSide 리턴 현황 (2026-08-14 설비로그 확인)

Side 검사 집계 수신(`AUTO-VISION-SIDE-INSPECTRESULT`)의 FRONTSIDE raw
(profile=SIDE_JUDGE, ver=1) 키 전체:
- 메타: `group_id`, `request_id`, `profile`, `ver`, `measure_valid`, `algo_ms`
- 채널별(ch0/ch1): `side_item_center_offset_mm`, `side_item_foreign_count(+_pass)`,
  `side_item_foreign_max(+_pass)`, `valid`
- 로그상 ch0/ch1은 같은 다이의 2회 촬영(Side 0°/90°)에 대응한다.

현재 `side_item_center_offset_mm`은 **08-13·08-14 이틀 전체에서 전부 0.0000**
(비전이 실측을 채우지 않음)이고, 핸들러 코드는 이 키를 **파싱하지 않는다**(참조 0건).
→ 비전 PC 수정(팀장님 담당)으로 실값이 오기 시작하면 이 값을 Z 폐루프 소스로 쓴다.

### 1-2. 팀장님 확정 (2026-08-14)

1. 소스: **FrontSide 비전의 `center_offset_mm`, 0도 촬영(ch0)만 사용.**
   **RearSide 값과 90도(ch1) 측정 데이터는 사용하지 않는다.**
2. 적용 대상: **Pick Z, Place Z, Bottom 검사 Z, Side 검사 Z — 4곳 전부**.
3. 부호: **감산(−)** — `적용 Z = 기존 산식 − 필터값`. (필터는 비전 raw 부호 그대로
   저장, 적용 지점에서 감산 — Place XY 런타임과 동일 관례)

### 1-3. 기존 Z 산식 구조 (적용 지점의 앵커)

| 공정 | 현재 Z 산식 | 앵커 |
|---|---|---|
| Pick | 레시피 `PickPosition` (AF 확정: `bestZ + BottomToPickMm`) 기반 하강 | 확정식 PickerSequenceBase.cs:386~418(무변경), 하강 목표 산출은 PickerPickUpSequence.PickZMotion.cs |
| Place | 레시피 `PlacePosition` (AF 확정: `bestZ + BottomToPlaceMm`) + OverDrive | 확정식 동일(무변경), 하강 목표는 PickerPlaceSequence.PlaceDown.cs / PlaceTargets.cs:160 부근 |
| Bottom 검사 | Bottom 포커스 Z(AF BestPosition 계열) | PickerBottomInspectionSequence 검사 Z 목표 산출부 |
| Side 검사 | Bottom AF BestPosition + `BottomToSideZOffsetMm` | PickerSequenceBase.cs:3975~4074 (ResolveSideInspectionZ 계열) |

**중요**: 런타임 Z 보정은 **사이클마다 이동 목표 계산 시 1회 감산**한다.
레시피 공정값(PickPosition/PlacePosition)·AF 확정식에 스며들게 하면 **이중 적용**이
되므로 절대 금지(§3).

---

## 2. 변경 범위 (이것만)

### 2-1. 신규 `PickerZRuntimeOffsetService` + `PickerZRuntimeOffsetStore`

`PlaceRuntimeOffsetService`/`Store` 패턴을 그대로 복제한 **Z 단일 채널** 서비스:
- 필터: side(Front/Rear picker) × pickerNo(1~4) = **8세트 × Z 1채널** LowPassFilter(EMA).
- 이상치 거부 / 필터 상태 클램프 / 컷오프 / Enable / 지연 저장(RequestDeferredSave) /
  스레드 lock — 전부 Place 서비스와 동일 구조.
- 저장 파일: `Config\pickerz_runtime_offset.json`
  (`UsePickerZRuntimeOffset` / `CutoffFrequency` / 한계값 / 8행 필터 상태).
- **기본값(§6-2 확인 후 확정)**: fc 0.1, 이상치 한계 0.5mm,
  **클램프 한계 ±0.3mm** (Z는 충돌 리스크가 있어 XY(±0.5)보다 타이트하게),
  **Enable 기본 OFF** (실장비 방향 검증 후 팀장님이 ON).
- 클램프 도달 시 Warning 래치(Place와 동일 정책).

### 2-2. 샘플 유입 — Side 집계 결과 수신부

"Side 양쪽 카메라 집계 결과 수신 완료"(AUTO-VISION-SIDE-INSPECTRESULT)를 만드는
집계 지점(또는 그 결과를 소비하는 시퀀스 매칭부 — PickerBottomAndSideInspectionSequence
:3044~3161 부근)에서:

- **FRONTSIDE raw의 `ch0_side_item_center_offset_mm`만** 파싱한다.
  **RearSide raw와 ch1(90도) 키는 읽지 않는다** (확정 1).
- 샘플 자격 게이트(모두 만족 시에만 필터 갱신):
  1. FRONTSIDE 판정 PASS + `measure_valid=1`
  2. `ch0_valid=1` — ch0 무효면 그 다이는 스킵+로그 (ch1로 대체하지 않는다)
  3. 대상 (side, pickerNo) 컨텍스트 확정(검사 중인 픽커/헤드) — 불명이면 스킵
- **ch0=0도 촬영 매핑 확인**: 로그 정황상 ch=0 요청이 0도 촬영에 대응하나, 구현 착수 시
  Side 검사 시퀀스의 ch 파라미터 발행 코드로 확정해 팀장님께 고지할 것(§6-1).
- 필터에는 **비전 raw 부호 그대로** 저장. NG(FAIL) 다이 측정은 미반영
  (Place XY와 동일 원칙).
- **키 부재/파싱 실패 시 스킵+사유 로그** — 비전 ver 변동(ver=1 → 상향) 시에도
  키만 있으면 동작하도록 키 기반 파싱(ver 하드체크 금지, ver은 로그에만 기록).

### 2-3. 적용 4곳 — 전부 `기존 목표 Z − 필터값(해당 side·pickerNo)`

Enable ON일 때만, 각 지점에서 `GetOffset` 1회 캡처 후 감산:

| # | 적용 지점 | 비고 |
|---|---|---|
| A | Pick Z 하강 목표 (PickZMotion 산출부) | OverDrive 등 기존 항 뒤에 −보정 |
| B | Place Z 하강 목표 (PlaceDown/PlaceTargets 산출부) | 〃 |
| C | Bottom 검사 Z 목표 | 검사 높이 보정 |
| D | Side 검사 Z 목표 (ResolveSideInspectionZ 계열) | 검사 높이 보정 — 폐루프의 측정 높이 자체도 보정됨 |

- 기존 이동 함수·MotionGuard·소프트리밋 경로 그대로(보정 후 목표가 가드를 통과해야
  이동 — 가드 우회 금지).
- 착수 시 4곳의 **정확한 함수·라인**을 분석해 팀장님께 고지 후 편집(§0-2).

### 2-4. Enable 스위치 UI

- GeneralPage에 "PICKER Z 런타임 오프셋" 콤보 추가 — 기존 PICK/PLACE 런타임 콤보
  (:102~103)와 동일 패턴(저장은 서비스 스토어 JSON, AppSettings 아님).

### 2-5. 모니터 표시

- `RuntimeOffsetMonitorDialog`에 Z 필터 상태 표시 추가(8세트 Z값 + 최종 갱신 —
  표시 전용). **기구 이관 대상 아님**(Z의 영구 보정은 BottomToPickMm 레시피 영역 —
  이번 범위 밖, 버튼 만들지 말 것).
- 필터 설정 UI 건(`플레이스런타임필터_설정UI_수정지시_프롬프트_2026-08-14.md`) 구현 시
  Z 열(한계·컷오프)도 함께 포함할 것 — 연계 메모.

---

## 3. 절대 건드리지 말 것

| 대상 | 이유 |
|---|---|
| AF 절대산식·레시피 공정값 갱신식 (`PickPosition = bestZ + BottomToPickMm` 등, PickerSequenceBase.cs:386~418) | 런타임 보정이 스며들면 **이중 적용**. 확정식 무변경 — 보정은 사이클별 이동 목표에만 |
| ColletAfZOffset 구( 舊) 체계 | 2026-07-29 삭제 확정 — 복원 금지 |
| 기존 Pick/Place XY 런타임 서비스·적용식 | 무변경 — Z는 별도 신규 서비스 |
| Side/Bottom 검사 시퀀스 흐름·판정 | Z 목표값 감산 외 무변경 |
| MotionGuard/소프트리밋/충돌 감시 | 우회·완화 금지 — 보정 후 목표로 기존 가드 통과 |
| RearSide raw | 파싱하지 않음(소스는 FrontSide만 — 확정 1) |
| FIFO·픽업캡·필터설정UI 건 파일 | 별개 프롬프트 — 접근 금지(연계 메모 §2-5만) |

---

## 4. 필수 로그 계측

1. **필터 갱신**: side/pickerNo/die + ch0/ch1 원값·유효 여부·병합값 + accepted(이상치
   판정) + filtered 결과. (Place XY 갱신 로그와 동일 형식, 태그 예: `PICKERZ-RUNTIME-OFFSET`)
2. **적용 4곳 각각**: 지점명(Pick/Place/Bottom/Side) + baseZ + filteredZ + correctedZ
   1줄 — 기존 Z 목표 로그가 있으면 거기에 항 추가.
3. **스킵 사유**: FAIL/measure_valid≠1/채널 무효/키 부재/컨텍스트 불명 — 종류별 수치 포함.
4. **클램프 Warning**(래치), **Enable 전환**, **이상치 폐기** — Place 서비스와 동일 수준.

---

## 5. 검증 (실장비 전에 반드시)

### 5-1. 정적
- `/p:OutDir` 빌드, 신규 경고 0건. 적용 4곳이 레시피 확정식이 아닌 **이동 목표 계산부**인지
  육안 확인(이중 적용 없음). Enable OFF 시 4곳 모두 기존 산식과 완전 동일.

### 5-2. 시뮬/오프라인
1. 비전 수정 전(0 샘플): 필터 0 유지, 적용해도 목표 불변 — 무해 확인.
2. 가짜 샘플 주입(테스트 경로/수동 스토어 편집): 필터 학습 → 4곳 적용 로그에
   filteredZ 감산 확인.
3. 이상치/클램프/컷오프/재기동 영속/Enable OFF 완전 동일 — Place 검증 항목 준용.
4. ch0 유효/무효 2케이스: 유효 → 반영, 무효 → 스킵+사유 로그(ch1로 대체 안 함) 확인.

### 5-3. 실장비 (비전 수정 후, 팀장님 입회)
1. **방향 검증 먼저**: 클램프 한계를 0.1mm로 임시 축소 + Enable ON → 수 사이클 후
   center_offset 측정값이 **0으로 수렴하는 방향**인지 확인. 커지면 즉시 Enable OFF
   하고 **보고**(부호 반전은 팀장님 결정 — 임의 수정 금지).
2. 수렴 확인 후 클램프 정상값 복원, 연속 생산에서 4곳 correctedZ 로그·Pick/Place
   품질(후검사 결과) 관찰.
3. Z 보정 적용 상태에서 소프트리밋/가드 차단 미발생 확인(경계 티칭 위치에서).

---

## 6. 팀장님께 보고/확인이 필요한 사항 (임의 처리 금지)

1. ~~채널 병합 규칙~~ → **답변 완료(2026-08-14): Front 0도(ch0)만 사용, Rear·90도 미사용.**
   구현 시 ch0=0도 매핑을 코드로 확정해 고지하는 것만 남음(§2-2).
2. **기본값**: fc 0.1 / 이상치 0.5mm / **클램프 ±0.3mm** / **Enable 기본 OFF** — 승인 여부.
3. **비전 부호 정의 확인**: `center_offset_mm`의 +방향이 물리적으로 어느 쪽(다이가
   위/아래)인지 비전 쪽 정의를 받아둘 것 — 감산(−) 적용은 확정이나, 방향 검증(§5-3-1)
   결과 해석에 필요하다.
4. Bottom/Side **검사 Z에도 보정을 거는 것의 상호작용**: Side 검사 높이가 보정되면
   측정 기준면 자체가 움직인다(폐루프 안정성엔 유리하나 측정 절대기준은 이동).
   4곳 전부 적용은 팀장님 확정 사항이므로 그대로 구현하되, 시험 중 진동/발산 징후가
   보이면 검사 Z 2곳(C·D)만 제외하는 옵션을 보고할 것.

---

## 7. 작업 체크리스트

### 7-1. 코드 편집
- [ ] `PickerZRuntimeOffsetStore.cs` 신설 (문서: Use/fc/한계/8행 Z 상태, Normalize 폴백)
- [ ] `PickerZRuntimeOffsetService.cs` 신설 (8세트 Z EMA, 이상치·클램프·래치·지연 저장,
      Get/SetEnabled·GetOffset·GetSnapshot·Reset류 — Place 패턴 대칭)
- [ ] Side 집계 수신부 — FRONTSIDE **ch0(0도)** center_offset만 파싱 + 자격 게이트
      (PASS·measure_valid·ch0_valid) + `OnInspectionOffset(side, pickerNo, z)` 호출
      (RearSide·ch1 미파싱, ch0=0도 매핑 코드 확정·고지)
- [ ] 적용 A: Pick Z 하강 목표 감산 (+로그)
- [ ] 적용 B: Place Z 하강 목표 감산 (+로그)
- [ ] 적용 C: Bottom 검사 Z 목표 감산 (+로그)
- [ ] 적용 D: Side 검사 Z 목표 감산 (+로그)
- [ ] GeneralPage — PICKER Z 런타임 콤보(기존 패턴)
- [ ] RuntimeOffsetMonitorDialog — Z 상태 표시(표시 전용, 이관 버튼 없음)

### 7-2. 무변경 확인 (§3)
- [ ] AF 확정식·레시피 공정값 갱신식 무변경 (이중 적용 없음 육안 확인)
- [ ] 기존 XY 런타임 서비스·적용식 무변경
- [ ] 검사 시퀀스 흐름·판정 무변경 (Z 목표 감산만)
- [ ] MotionGuard/소프트리밋 우회 없음
- [ ] RearSide 미파싱 / 신규 이동·속도 코드 없음

### 7-3. 정적/빌드
- [ ] `/p:OutDir` 빌드, 신규 경고 0건
- [ ] Enable OFF 시 전 경로 기존과 완전 동일 육안 확인
- [ ] 적용 지점 4곳 함수·라인 사전 고지·승인 완료

### 7-4. 검증
- [ ] §5-2 시뮬 4건 / [ ] §5-3 실장비 3건 (방향 검증 → 정상 운전 → 가드 확인)

### 7-5. 착수 전 팀장님 확인
- [ ] §6 확인 사항 4건 답변 수령
- [ ] 비전 PC 수정 완료(center_offset 실값 반환) 확인 — §5-3은 그 이후에만
- [ ] 변경 파일·함수·라인 목록 최종 고지 및 편집 승인
