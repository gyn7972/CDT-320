# 바텀 검사 2채널(조명별 촬영) 설계 / 범위

작성: 2026-06-29 · 상태: **설계(미구현)** · 결정 대기

## 1. 배경 / 현황

- 사용자 관찰: "바텀 카메라가 조명을 바꿔 2번 촬영(채널1=조명셋팅1, 채널2=조명셋팅2)하는 느낌인데, 채널 CSV 데이터가 안 남는다."
- 조사 결과(CDT-310/300/320 핸들러/Vision):
  - **CDT-310 / CDT-300 에는 바텀을 조명 바꿔 N회 촬영하는 채널/조명 분기가 없음.** 바텀은 단일 촬영.
  - **현재 QMC.Vision 바텀도 단일 촬영**: `ToolSequence.IsBottomInspect()` 분기는 채널 컨텍스트를 `-1`(미지정)로 두고 1회 디스패치(`SetInspectContext(picker, -1, ix, iy)`).
  - **측면(SideVision)만 2채널 구조 보유**: `ToolSequence.IsSideInspect()` 가 `for (chOff = 0..1)` 루프로 ch1(0°)→ch2(90°) 2회 촬영, `SetInspectContext(picker, baseCh+chOff, ix, iy)`. 채널 0~3 = Front ch1/ch2(0/1), Back ch1/ch2(2/3).
  - 4-맵의 **"1 Channel / 2 Channel ChippingSize" 는 조명 채널이 아니라** 상·하 에지(=1ch) vs 좌·우 에지(=2ch) 표시용 별칭(`InspectionViewerControl.BuildPositionMaps`: c1=max(Chipping Top,Bottom), c2=max(Chipping Left,Right)). `BottomInspector` 는 Chipping Top/Right/Bottom/Left 만 산출.

→ 따라서 "바텀 조명 2채널 촬영 + 채널별 데이터 저장" 은 **기존 어디에도 없던 신규 동작**이며, **측면의 2채널 패턴을 바텀에 미러링**하면 구현 가능하다.

## 2. 목표

1. 바텀 한 다이를 **조명 셋팅을 바꿔 2회 촬영**한다(채널0=조명1, 채널1=조명2). N채널 일반화 여지 둠.
2. 각 채널 이미지로 **채널별 측정값**을 산출한다(무엇을 채널별로 둘지는 §6 미결정).
3. **검사 결과 스토어/CSV 에 채널 데이터를 기록**한다.
4. 4-맵의 "1/2 Channel" 을 실제 채널(조명1/조명2) 데이터로 의미를 맞춘다(또는 채널×에지 재정의).

## 3. 변경 지점(레이어별)

### 3-1. 시퀀서 — `Sequencing/Common/ToolSequence.cs`
- `IsBottomInspect()` 분기를 측면(`IsSideInspect`)처럼 **채널 루프**로 변경:
  - `for (chOff = 0; chOff <= 1; chOff++)` 안에서 `SetInspectContext(picker, chOff, ix, iy)` 후 채널별 조명 적용 + 디스패치.
  - chipUid 는 다이 기준(이미 적용)이라 채널이 달라도 같은 다이로 집계됨.
- 채널 수는 레시피 설정(조명 셋팅 개수)로 일반화 가능.

### 3-2. 조명 — 채널별 조명 셋팅 적용
- 현재 조명은 모듈 단위 1:1(`VisionModule.MigrateLightPages`, `LightHub`, `LightSettings`=Port/Page). 측면도 채널이 같은 조명 페이지 공유(채널은 시뮬 이미지 경로만 분기, 실조명은 핸들러가 사전 설정).
- 바텀 2채널은 **채널별 조명 셋팅 2개**가 필요:
  - 옵션 A: 바텀 검사 노드 레시피에 `LightSettings[ch]` (채널 인덱스별 Port/Page) 추가.
  - 옵션 B: 조명 적용은 핸들러가 채널 신호에 맞춰 수행하고, Vision 은 채널 인덱스만 부여(측면과 동일 철학). 실장비는 B, Sim 은 채널별 시뮬 이미지 경로로 모사.
- Sim: `GrabForTool` 가 채널별 시뮬 이미지(예 `...Ch1`, `...Ch2`)를 반환하도록(측면 `VisionModule` 채널 분기 패턴 재사용).

### 3-3. 검사기 — `Equipment/Core/BottomInspector.cs`
- 현재 채널 무시. **채널 컨텍스트(`VisionCommandCore.CurrentInspectChannel`) 인식** 필요.
- 채널별 결과를 어떻게 낼지(§6 미결정)에 따라:
  - (a) 같은 항목을 채널별로 측정 → 결과 키에 채널 태그(예 `Width@ch1`) 또는 채널별 Item 으로 저장.
  - (b) 채널별로 다른 항목만(예 ch1=치수, ch2=칩핑/이물) 측정.

### 3-4. 저장 — `InspectionResultStore` / `MaterialTracker` / `DataLogSaver`
- `InspectionResultStore.Item.Channel` 필드는 **이미 존재**(측면용). 바텀도 channel 0/1 부여하면 per-channel Item 누적됨.
- `MaterialTracker.DieRecord` 에 바텀 채널별 필드 추가(또는 기존 Back_Chipping 을 채널별로 분리).
- `DataLogSaver.Headers`(31컬럼)에 **채널 컬럼 추가**(예 `Back_Chipping_*_Ch1/Ch2` 또는 `..._Light1/Light2`). 스키마 변경 → §5 호환성.
- 수동 익스포터 `InspectionResultCsv` 는 Item.Values 동적 컬럼이라 채널 항목이 자동 포함됨(Channel 컬럼 이미 출력).

### 3-5. UI — 4-맵 / 결과 그리드
- `InspectionViewerControl.BuildPositionMaps` 의 바텀 "1/2 Channel ChippingSize" 를 **실제 채널 데이터**(조명1/조명2)로 매핑하도록 변경(현재 상·하/좌·우 에지 별칭).
- 결과 그리드/패널에 채널 표시(측면 4채널 뷰 패턴 참고).

## 4. 단계(제안)

- **Phase A** — 시퀀서 채널 루프 + Sim 채널별 이미지(조명 모사). (구동/순서만, 측정은 동일)
- **Phase B** — 검사기 채널 인식 + 채널별 결과 산출(§6 결정 반영).
- **Phase C** — 저장(MaterialTracker/DataLogSaver) 채널 컬럼 + CSV.
- **Phase D** — 4-맵/그리드 채널 표시 정리.

## 5. 호환성 / 마이그레이션

- `DataLogSaver` 헤더 변경 시 기존 `vision_YYYYMMDD.csv` 와 컬럼 불일치 → 새 파일부터 적용(일자별 파일이라 자연 분리). 다운스트림(분석) 합의 필요.
- 단일채널 기존 레시피: 채널 1개(조명 셋팅 1개)면 기존과 동일 동작하도록 기본값 보존.
- 측면 채널(0~3)과 **채널 인덱스 의미 충돌 주의**: 바텀 채널 0/1 은 모드(Bottom) 스코프 내에서만 유효(스토어가 Mode 별 분리라 충돌 없음).

## 6. 미결정 / 확인 필요(결정 후 구현)

1. **채널별로 무엇이 다른가?** 조명만 다르고 측정 항목은 동일(채널별 같은 항목 2세트)인가, 아니면 채널마다 측정 목적이 다른가(ch1=치수/배경, ch2=칩핑/이물)? — 이게 검사기·저장 스키마를 좌우.
2. **CSV 채널 컬럼 명명**: `_Ch1/_Ch2` vs `_Light1/_Light2` vs 조명 페이지명 기반.
3. **4-맵 의미**: "1/2 Channel" 을 조명 채널로 바꿀지, 아니면 채널×에지(2×2)로 확장할지.
4. **조명 적용 주체**: 실장비에서 조명 전환을 Vision 이 직접(Strobe 포트 제어)인지 핸들러가 신호로 하는지(측면은 핸들러). 이게 Phase A 조명 처리 방식 결정.
5. **채널 수**: 2 고정인지 N 일반화인지.

---
참고 코드: `ToolSequence.cs`(IsSideInspect/IsBottomInspect), `BottomInspector.cs`, `VisionCommandCore.cs`(CurrentInspectChannel/SetInspectContext), `InspectionResultStore.cs`(Item.Channel), `MaterialTracker.cs`/`DataLogSaver.cs`(저장/CSV), `InspectionViewerControl.cs`(BuildPositionMaps), `VisionModule.cs`(조명/채널 시뮬 이미지).
