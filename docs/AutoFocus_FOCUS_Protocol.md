# 오토포커스 FOCUS 프로토콜 (Handler ↔ Vision)

대상: CDT-320 오토포커스 — **Bottom / Front / Back 카메라**.
실행 위치: **QMC.Vision** (프레임당 Score 계산 + 세션 누적).
모터 Z: **핸들러가 TCP/IP로 송신** → 그래프 X축. Score → 그래프 Y축.

라인 포맷은 기존과 동일: `MODULE|CMD|args...` (구분자 `|`).
응답: `ACK|MODULE|CMD|result` / `ERR|MODULE|CMD|msg`.

## 명령

### FOCUS_START — 세션 시작(리셋)

```
MODULE|FOCUS_START|<camera>|<target>
```

- `camera` : `BOTTOM` | `FRONT` | `BACK`
- `target` : `COLLET` | `DIE` | `SIDE`
- 동작: 해당 (camera,target) 세션의 모든 시리즈(Pickup1~4 또는 측면 1개) 샘플을 비운다.
- 응답: `OK;camera=Bottom;target=Collet`

스캔 1회 시작 전에 호출. (콜렛/다이 각각, 측면 각각 별도 세션)

### FOCUS_VAL — 한 위치 Score 측정 + 누적

```
MODULE|FOCUS_VAL|<motorZ>|<camera>|<target>|[pickupNo]|[init]
```

- `motorZ` : 현재 Z 모터 위치(소수점 `.`, InvariantCulture). 그래프 X.
- `camera` / `target` : 위와 동일.
- `pickupNo` : **Picker 번호 `1`~`4`**. 측면은 `0`(→ 내부 단일 시리즈 1로 매핑).
- `init` : `1`/`INIT`/`TRUE` 면 이 샘플을 **최초값(점 표시)** 으로 지정. 생략 시 `0`.
- **그랩-ACK + 백그라운드 채점**: Vision 은 **grab 만 동기로** 끝내고(이미지 확보) **즉시 ACK**("그랩 완료", 점수 아님)를 보낸다.
  핸들러는 이 ACK 를 받고 다음 Z 로 이동한다(backpressure — grab 이 올바른 Z 에서 찍히고 Vision 이 밀리지 않음).
  실제 채점(ROI 점수 계산 + 누적 + 이미지 폐기)은 **백그라운드**(`AutoFocusProcessor`)에서 모아서 처리한다.
- 백그라운드 동작: [설정 > 오토 포커스]에서 지정한 ROI 들을 `AutoFocusCore.Score(image, roi, AutoFocusThreshold)` 로 채점 →
  각 ROI 시리즈(1~4)에 `(motorZ, score)` **누적**(append). ROI 미설정이면 전체 프레임 1점(측면=시리즈1). 스텝별 택타임은 `AutoFocusTactLog`.
- 응답(그랩 완료, score 미포함 — 채점은 백그라운드): `OK;z=12.3400;pickup=2;init=0;queued=1`
- 테스트 버튼(설정 > 오토 포커스)도 동일: FOCUS_VAL/FOCUS_BEST 모두 응답 수신.

핸들러 스캔 루프: `FOCUS_START` → (Z 이동 → `FOCUS_VAL`) 반복 → 응답의 score로 best 판단(또는 Vision UI BEST표 참조).

> 하위호환: 인자 없는 `MODULE|FOCUS_VAL` 단독 호출은 기존 4-ROI(Left/Right top·bottom) 측정으로 동작.

### FOCUS_BEST — 결과 조회

```
MODULE|FOCUS_BEST|<camera>|<target>|[pickupNo]
```

- 핸들러가 스캔 종료 후 best 위치/점수를 TCP 로 회수. **이 명령은 응답을 기다린다**(FOCUS_VAL 과 달리).
- 동작: 진행 중인 백그라운드 채점이 **모두 끝날 때까지 대기**(`AutoFocusProcessor.WaitForDrain`) 후, 누적 전체에서 **최고 점수 Z** 를 회수.
- 응답(인덱스 키): `OK;p1z=19.9500;p1s=214.00;p1n=21;p2z=20.1000;p2s=198.00;p2n=21;...`
  (`p<n>z`=bestZ, `p<n>s`=bestScore, `p<n>n`=샘플수). `pickupNo` 지정 시 그 픽업만(측면 `0`→시리즈 `1`).

## 카메라 ↔ 모듈 ↔ 포트

| camera | 와이어 모듈명(`MODULE`) | 기본 포트 | 타깃 |
|---|---|---|---|
| `BOTTOM` | `BottomInspection` | 5101 | `COLLET`, `DIE` (Pickup1~4) |
| `FRONT`  | `FrontSideVision`    | 5105 | `SIDE` (Pickup1~4) |
| `BACK`   | `RearSideVision` | 5106 | `SIDE` (Pickup1~4) |

> Vision 은 명령이 도착한 **모듈의 카메라로 현재 보이는 위치에서 1장 grab** 해 채점한다(모션 없음). 모터 이동·다음 스텝은 핸들러가 수행.

## 게이트

`FOCUS_START` / `FOCUS_VAL` 은 **RUN 게이트 면제**(PING/EXPOSE/GRAB과 동일). 셋업·캘리브레이션 용도로 RUN 아닐 때도 허용. 단, **Z 모션 안전은 핸들러 책임**.

## 색상 / ROI

시리즈는 Picker1~4(=ROI1~4) = **빨(Red) · 노(Gold) · 파(RoyalBlue) · 녹(ForestGreen)**. 모든 카메라/타깃 공통.
각 Picker 의 ROI 영역은 [설정 > 오토 포커스]에서 이미지에 드래그로 지정하며 (camera,target)별로 `VisionConfig.AutoFocusRois`(vision.json)에 저장.
UI 라벨은 "ROI1~4"(구 "Pickup1~4"). 신호(`pickupNo`)는 Picker, FOCUS_BEST 응답의 `p{n}*` 키는 Picker{n}(=ROI{n}) 의 best.

## Vision 측 구성요소

| 파일 | 역할 |
|---|---|
| `Equipment\Core\AutoFocusCore.cs` | 310 ScoreFocus 이식 (Bitmap→grayscale→라플라시안→상위200 평균) |
| `Equipment\Core\AutoFocusSession.cs` | 카메라×타깃×Pickup 시리즈, best/최초값, 4색, BEST표 스냅샷 |
| `Equipment\Core\AutoFocusStore.cs` | 프로세스 전역 세션 스토어 + 인자 파서 |
| `Equipment\Core\VisionCommandCore.cs` | `FocusStart()` / `FocusValue()` 공유 처리 |
| `Equipment\Comm\VisionCommandRouter.cs` / `VisionTcpServer.cs` | FOCUS_START / FOCUS_VAL 디스패치 |

UI(BEST표 + 4색 그래프)는 `AutoFocusStore` 를 읽어 표시.

| 파일 | 역할 |
|---|---|
| `Ui\Pages\Settings\AutoFocusPanel.cs` | 설정 > **오토 포커스** 페이지. 카메라(바텀/앞측면/뒤측면) 선택, Bottom 은 콜렛/다이, Pickup1~4 BEST 표 + 4색 그래프(초기값 점). 400ms 타이머로 `AutoFocusStore` 실시간 반영 |
| `Ui\Tabs\SettingsTab.cs` | 설정 사이드바에 "오토 포커스" 버튼 등록(GENERAL/카메라/조명/통신 다음) |

## 빠른 테스트 (핸들러 없이)

`tools/autofocus_tcp_test.py` 로 START→VAL 스윕→BEST 를 직접 송신:

```
python tools/autofocus_tcp_test.py --camera bottom --target collet --pickups 1,2,3,4
python tools/autofocus_tcp_test.py --camera front  --target side
```

QMC.Vision.exe 실행 후, 설정 > 오토 포커스에서 표/그래프가 차오르는지 확인.
Sim 모드는 프레임이 고정이라 곡선이 평탄할 수 있음(통신·누적 파이프라인 검증용). 실제 곡선은 핸들러가 실제 Z 를 움직일 때 형성.
