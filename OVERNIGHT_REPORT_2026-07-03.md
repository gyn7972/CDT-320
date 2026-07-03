# 야간 작업 보고서 — 2026-07-03 아침 확인용

요청 3건: ① 모든 칩핑 실측 검출 ② 시퀀서 느림 수정 ③ 시퀀서 화면 통신로그 반반.
모두 C:\Project\CDT-320 에 적용 완료. **솔루션 리빌드(QMc.Vision.Inspector → QMC.Vision) 후 확인 바랍니다.**

## ① 칩핑 실측 검출 — 전 모듈 완료

칩핑 마커를 그리는 곳은 코드 전체에서 3곳뿐임을 전수 확인(그 외 Foreign/Cognex 마커는 원래 실측):

| 경로 | 기존 | 변경 |
|---|---|---|
| Side lib (SideChippingInspector) | 고정 48x48, 1개 | 스펙 초과 연속구간 → 실측 bbox 다중(C1,C2..) + Chipping Count 항목 |
| Side 레거시 (SideChippingCore) | 고정 48x48, 1개 | 동일 구현(폴백 일관성) |
| Bottom 레거시 (BottomInspector.MaxInwardDev) | 최대점 10x10, 변당 1개 | 4변 각각 스펙 초과 구간 → 실측 bbox 다중 |
| Bottom lib (MapLibResult) | Contour bbox(원래 실측) | 변경 없음 |

- 판정값(Top/Bottom/Max 등) 계산은 어디도 변경하지 않음 — 마커/카운트만 추가.
- 구간 규칙: 깊이 > 스펙(Upper/ChippingDepth), x-간격 ≤5px 병합. PASS 수준 결손은 마커 생략(수치는 표에 그대로).
- 더블 노치 검증: 노치 2개(55/70px) → 마커 2개, bbox 실제와 일치.

## ② 시퀀서 느림 — 원인 분석 + 수정

Timing 로그(Event csv) 실측: INSPECT 1회당 grab 0.9~6.0초 + algo 1.3~4.7초. 원인 3가지:

1. **[수정됨] 저장이미지 그랩이 매회 12000² 32bpp 재변환** — 캐시가 있어도 `new Bitmap()` 복제가
   그랩마다 576MB 할당+포맷변환(~1초). → 24bppRgb 마스터 1회 변환 + 동일포맷 Clone(memcpy)으로 변경.
   부수효과: 검사 ToGray 의 LockBits(24bpp) 포맷변환도 소멸. (VisionModule.LoadImageAsGrab)
2. **[수정됨] Bottom lib 시도 낭비** — BottomInspect 가 항상 null(레거시 폴백)인데 매회
   그레이변환+파라미터 구성(~1초)을 반복. → 연속 2회 null 이면 이후 lib 시도 생략(성공 시 자동 복귀).
   (BottomInspector._libNullStreak)
3. **[원인 확정 — 장비 PC에서 빌드 1회 필요] Foreign Backend = Cpu** — 배포된
   `QMC.Vision\NativeDeps\MakePixelShiftImage.dll` 이 **2026-05-21자 구버전**으로,
   PE 익스포트 테이블을 직접 확인한 결과 CudaInterop 이 요구하는 8개 익스포트
   (AllocateDeviceMemory, FreeDeviceMemory, CopyHostToDevice, CopyDeviceToHost,
   FindTopBottomLineCandidates, cf_cuda_device_name, cf_morph_box_u8, cf_find_die_edges)가
   **하나도 없음** (있는 것은 구식 ApplySobelFilter/FindChipping/FindBlobsWithCuda 뿐).
   ColletFinderCuda.dll(6/26자)도 stddev 계열만 있는 부분 빌드라 통합본이 아님.
   → **해결: 장비 PC의 "x64 Native Tools Command Prompt for VS 2022"에서
   `native\build_all.bat` 1회 실행** (nvcc 필요, BUILD.md 참조). 통합 QmcVisionCuda.dll 이
   3개 이름으로 NativeDeps 에 배포되고 csproj 가 bin 으로 자동 복사.
   이후 Foreign Backend=Cuda 로 바뀌며 측면 라인검출/Bottom 다이에지/이물 모폴로지가 GPU 로 감.

### 오늘 Timing 실측(Event csv, INSPECT 12,430회)

| 항목 | 평균 | 비고 |
|---|---|---|
| total | 2533ms | 최대 169s 스파이크 존재(GC/디스크 추정) |
| grab | 1087ms | → 수정①로 대부분 제거 기대 |
| algo | 1446ms | → CUDA 빌드(위 3번) 후 대폭 단축 기대 |

기대 효과: INSPECT 1회 6~10초 → **약 1.5~2.5초** (grab ~0.2초 + algo CPU 1.3초). CUDA 복구 시 추가 단축.

## ③ 시퀀서 화면 — 시퀀서로그 | 통신로그 반반

SequencerPage 하단 로그를 좌우 50:50 분할: 왼쪽 = 기존 시퀀서 로그(초록), 오른쪽 = 통신 로그(파랑).
통신 로그는 CommLink 페이지와 동일 소스(VisionCommLog: 핸들러↔비전 TCP RX/TX, 폴링 응답 홍수 억제 내장),
Revision 변화시에만 갱신 + 자동 스크롤. '로그 Clear'는 시퀀서 로그만 지움(통신 로그는 CommLink 페이지에서 Clear).

## 아침 확인 절차

1. 리빌드 → Vision 실행
2. 시퀀서 페이지: 로그 반반 표시 + 통신 RX/TX 흐르는지
3. 바텀/측면 Auto 실행 → 사이클 시간 체감(Event 로그 Timing 으로 전후 비교 가능)
4. 바텀 검사(칩핑 NG 이미지) → 4변 결손마다 실측 박스
5. 남은 확인: Foreign Backend 가 여전히 Cpu 면 CUDA dll 익스포트 점검 필요(보고서 ② -3)

## 오늘 전체 수정 파일

- VisionInspector\SideChippingInspector.cs — F1 마진, F2 임계, 다중영역(ChippingRegions)
- QMC.Vision\Equipment\Core\SideAppearanceInspector.cs — F3 이물 에지필터, 실측 마커, Chipping Count
- QMC.Vision\Equipment\Core\SideChippingCore.cs — 다중영역(RegionMark)
- QMC.Vision\Equipment\Core\BottomInspector.cs — 4변 실측 마커, lib null 래치
- QMC.Vision\Equipment\Unit\VisionModule.cs — 저장이미지 그랩 최적화
- QMC.Vision\Equipment\Unit\AlgorithmNode.cs — F4 가드
- QMC.Vision\Ui\Pages\Settings\Recipe\InspectorTargetPage.cs — F5 입력검증
- QMC.Vision\Ui\Pages\Work\SequencerPage(.Designer).cs — 통신로그 분할

상세 근거: SIDE_INSPECTION_REVIEW_2026-07-02.md 참조.
