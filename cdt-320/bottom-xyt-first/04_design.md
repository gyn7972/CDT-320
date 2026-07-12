# Bottom 좌표 선응답 — 설계 (Stage 4)

## 설계 원칙
- **기존 XYT 푸시 채널을 확장**한다(신규 프로토콜/소켓 신설 없음). 이미 검증된 경로:
  `SearchDieEnd → BottomXytPushService → "XYT|…" 푸시 → VisionTcpClient → BottomXytStore`.
- 본류(BottomInspect) 수식은 손대지 않는다. 조기 통지는 **사본 선계산**(기존 RaiseSearchDieEnd 패턴)으로만 확장.
- 와이어 확장은 키-값 추가(`w=..;h=..`)라 구버전 수신부와 하위호환.

## D-1. 라이브러리 — SearchDieEnd 에 W/H(mm) 추가
**Layer:** VisionInspector (CDTInspector.cs)
**변경:**
- 이벤트 시그니처 확장: `Action<float x, float y, double t, int ix, int iy>` →
  `Action<float x, float y, double t, int ix, int iy, double wMm, double hMm>`
  (구독자는 BottomXytPushService 1곳뿐 — 동시 수정으로 안전)
- `RaiseSearchDieEnd(bip, best)` → `RaiseSearchDieEnd(bip, best, vList)` 로 확장,
  발화 지점을 646행에서 **vList/평균 계산 직후(≈656행)** 로 이동 — 여전히 CUDA 칩핑/이물 이전.
- W/H 사본 선계산(본류 773~781행 수식 그대로, 스왑 포함):
  ```csharp
  double wPx = vList.Count > 0 ? vList.Average(t => t.w) : 0;
  double hPx = vList.Count > 0 ? vList.Average(t => t.h) : 0;
  // 본류 최종값: Width=hPx×PixelSizeHeightMm/2, Height=wPx×PixelSizeWidthMm/2 (mm 변환 후 W↔H 스왑)
  double wMm = hPx * _visionConfig.BottomVision.PixelSizeHeightMm / 2;
  double hMm = wPx * _visionConfig.BottomVision.PixelSizeWidthMm / 2;
  ```
- 미검출(angle NaN) 시 기존 정책 유지: 구독자(푸시 서비스)가 valid=0 처리. W/H 도 0 송신.

## D-2. 비전 — 푸시 페이로드에 w/h 추가
**Layer:** QMC.Vision (BottomXytPushService.cs, VisionTcpServer.PushBottomXyt)
**변경:**
- `OnSearchDieEnd(..., double wMm, double hMm)` 로 수신, valid=0 이면 w/h=0.
- 페이로드: `"XYT|MODULE|fb|collet|die_index|x=..;y=..;t=..;ix=..;iy=..;valid=0|1;w=..;h=.."`
  (w/h 단위 = mm, F4 — 최종 INSPECTRESULT 의 W/H 와 동일 값)
- `PushBottomXyt` 시그니처에 wMm/hMm 파라미터 추가.

## D-3. 핸들러 — 수신/스토어에 W/H 추가
**Layer:** QMC.CDT-320 (VisionProtocol.cs 주석, VisionTcpClient.HandleBottomXytPush, BottomXytStore.BottomXytPush)
**변경:**
- `BottomXytPush` 에 `public double W { get; set; }` / `public double H { get; set; }` (mm, 0=미포함/미검출).
- `HandleBottomXytPush` 에서 `TryGetDouble("w"/"h")` — 키 없으면 0(구버전 비전 호환).
- `BottomXytReceived` 이벤트/`BottomXytStore` 는 구조 그대로(필드만 추가 전파).

## D-4. (선택 — 컨펌 필요) 핸들러 Bottom 시퀀스의 선활용
**Layer:** QMC.CDT-320 (PickerBottomInspectionSequence)
현재: RequestBottomInspection → **ApplyBottomInspectionResultAsync 가 최종 결과(INSPECTRESULT)를 통째로 대기** → 오프셋 적용/판정 → Z 어보이드 이동.
- **옵션 A (이번 범위 제안)**: D-1~D-3 까지만. 좌표+W/H 는 검사 완료 전에 핸들러 스토어에 도착해 있고,
  시퀀스 활용(어느 단계를 선행할지)은 별도 건으로 설계. 위험 0, 기존 시퀀스 무변경.
- **옵션 B (tact 단축까지)**: XYT 푸시 도착 즉시 Z 어보이드/다음 이동을 선행하고 OK/NG 는 플레이스 직전 확인.
  시퀀스 스텝 재배열이 필요해 안전(어보트/NG 경로) 재검증 필요 — 별도 설계·테스트 권장.

## 열린 질문 (사용자 컨펌)
1. 범위: 옵션 A(푸시에 W/H 추가까지) vs 옵션 B(핸들러 시퀀스 선행 동작까지)?
2. w/h 단위 mm(F4, 최종 결과와 동일 값) — 픽셀이 아니라 mm 로 보내는 것 맞는지?
3. 이벤트/페이로드에 valid=0(미검출) 시 w/h=0 송신 — 기존 x/y/t=0 정책과 동일하게 갈지?

## 타이밍 효과(참고)
현 최적화 기준 검사 1건 ≈ 215ms(단일)이며 좌표(외곽) 확정 시점은 ≈ 120ms.
조기 푸시로 좌표+W/H 가용 시점이 완료 대비 약 95ms(단일)~수백 ms(8병렬 부하) 앞당겨진다.
