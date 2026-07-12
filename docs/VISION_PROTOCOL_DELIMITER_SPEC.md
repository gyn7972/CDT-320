# Vision Protocol Delimiter Spec

작성일: 2026-07-11

이 문서는 Handler(`QMC.CDT-320`)와 Vision PC(`QMC.Vision`) 사이 TCP 통신에서 검사 종류와 구분자가 어떻게 전달되고 처리되는지 정리한다. 판단 기준은 Vision PC 수신부 코드이다.

## 결론

Vision PC는 Handler가 보내는 구분자를 동일하게 받아 처리한다.

- 1차 구분자: `|`
- grid 인덱스 구분자: `;`
- 결과 payload key/value 구분자: `=`
- 라인 종료: `\n`
- 문자열 인코딩: UTF-8

검사 종류는 `CMD`만으로 결정되지 않는다. `MODULE`, `CMD`, `tool` 조합으로 결정된다.

```text
MODULE|CMD|tool|fb|collet|die_index|channel|gridX;gridY
```

`tool` 위치에는 `SurfaceInspector`, `PlacementInspector`, `DieFinder`, `ReticleFinder` 같은 Vision 프로젝트의 Finder/Inspector ID가 들어간다.

## 기본 라인 포맷

Handler 송신:

```text
MODULE|CMD|arg1|arg2|...
```

Vision 응답:

```text
ACK|MODULE|CMD|result
ERR|MODULE|CMD|msg
```

Vision 비동기 푸시:

```text
EPD|MODULE
XYT|MODULE|fb|collet|die_index|x=...;y=...;t=...;ix=...;iy=...;valid=0|1;w=...;h=...
```

Vision TCP 서버의 주석과 실제 파싱이 이 포맷을 기준으로 한다.

- `QMC.Vision/Equipment/Comm/VisionTcpServer.cs`: 요청/응답 포맷 주석
- `QMC.Vision/Equipment/Comm/VisionTcpServer.cs`: `line.Split('|')`
- `QMC.Vision/Equipment/Comm/VisionCommandRouter.cs`: `line.Split('|')`

## 신형 8파트 검사/매칭 포맷

현재 자동 검사에서 기준으로 봐야 할 포맷은 고정 8파트이다.

```text
MODULE|INSPECTASYNC|inspector|fb|collet|die_index|channel|gridX;gridY
MODULE|MATCHASYNC  |finder   |fb|collet|die_index|channel|gridX;gridY
```

필드 의미:

| 위치 | 이름 | 의미 |
|---:|---|---|
| 0 | `MODULE` | Vision 모듈 이름 |
| 1 | `CMD` | 명령 |
| 2 | `tool` | Finder 또는 Inspector ID |
| 3 | `fb` | `0` = Front, `1` = Rear/Back |
| 4 | `collet` | `1`~`4` |
| 5 | `die_index` | 픽업 순서. 결과 매칭 키 |
| 6 | `channel` | Side는 `0`/`1`, Bottom/Bin은 보통 `0` |
| 7 | `gridX;gridY` | 웨이퍼맵 인덱스 |

Vision 쪽 `ColletAddress.TryParseWire()`가 위 8파트를 그대로 파싱한다. 마지막 필드는 `ParseGrid()`에서 `;`로 나눠 `gridX`, `gridY`로 만든다.

## Vision PC 수신 흐름

### TCP 서버 경로

`VisionTcpServer.ProcessLine()`:

1. 수신 라인을 `|`로 분리한다.
2. `parts[0]`을 `MODULE`로 본다.
3. `parts[1]`을 `CMD`로 본다.
4. `MATCHASYNC`/`INSPECTASYNC`는 먼저 `ACK|MODULE|CMD|STARTED`를 보낸다.
5. 실제 Grab/검사/매칭은 백그라운드로 실행한다.

`STARTED`는 Grab 전에 오는 접수 응답이다. Handler 자동운전은 이 응답으로 다음 모션을 시작하지 않는다.
명령 전 해당 TCP 연결 모듈의 EPD 대기를 먼저 등록하고, `EPD|MODULE`을 수신한 뒤에만 다음 모션으로 진행한다.
다른 모듈의 EPD는 인정하지 않는다. `FPD|MODULE`은 이전 버전 호환 수신만 유지한다.

최종 데이터는 EPD에 포함되지 않는다.

```text
MATCHASYNC   -> EPD로 모션 진행 -> MATCHRESULT로 최종 매칭값 회수
INSPECTASYNC -> EPD로 모션 진행 -> INSPECTRESULT로 최종 검사값 회수
```

DryRun/Simulation에서는 Vision 연결이 있을 때만 별도 `GRAB`을 보내고 결과는 시뮬레이션 값을 사용한다.
정상 자동운전에서는 `GRAB`을 먼저 보내지 않고 `MATCHASYNC`/`INSPECTASYNC` 자체가 1회 촬상한다.

`INSPECTASYNC`는 `DoInspectAsync()`에서 `ColletAddress.TryParseWire(parts, ...)`를 호출한다.

`MATCHASYNC`는 `DoMatchAsync()`에서 `parts[2]`를 finder로 쓰고, 신형 8파트이면 `die_index`를 결과 키로 쓴다.

### 내부 Direct Dispatcher 경로

Vision 내부 시퀀서가 TCP 없이 직접 실행하는 경로도 같은 기준을 쓴다.

`DirectVisionCommandDispatcher.Execute()`의 `INSPECTASYNC`는 이미 분리된 `args` 배열을 받아 `ColletAddress.TryParseArgs()`로 파싱한다.

즉 TCP 경로는 `TryParseWire()`, 내부 경로는 `TryParseArgs()`를 쓰지만, 둘 다 같은 자리 규칙을 공유한다.

## 검사 종류 ID 정리

Vision 프로젝트 기준 등록 ID:

| Vision 모듈 | 등록된 Finder/Inspector |
|---|---|
| `WaferVision` | `EjectPinFinder`, `ReticleFinder`, `AlignDieFinder`, `FirstReferenceFinder`, `SecondReferenceFinder`, `DieFinder`, `ScaleFinder` |
| `BottomInspection` | `ReticleFinder`, `ColletFinder`, `DieFinder`, `SurfaceInspector`, `FocusFinder`, `ScaleFinder`, `DistortionCompensation` |
| `BinVision` | `ReticleFinder`, `DieFinder`, `PlacementInspector`, `ScaleFinder` |
| `FrontSideVision` | `DieEdgeFinder`, `FrontSurfaceInspector`, `FrontChippingInspector`, `FocusFinder` |
| `RearSideVision` | `DieEdgeFinder`, `RearSurfaceInspector`, `RearChippingInspector`, `FocusFinder` |

Handler 자동 시퀀스에서 현재 보내도록 맞춘 주요 ID:

| 용도 | Handler 채널 | tool ID |
|---|---|---|
| Wafer Die 찾기 | `Wafer` | `DieFinder` |
| Wafer Align | `Wafer` | `AlignDieFinder`, `FirstReferenceFinder`, `SecondReferenceFinder`, `ReticleFinder` |
| Bottom 검사 | `BottomInspection` | `SurfaceInspector` |
| Front Side 검사 | `FrontSide` | `FrontSurfaceInspector` |
| Rear Side 검사 | `RearSide` | `RearSurfaceInspector` |
| Bin/Output 위치 검사 | `Bin` | `DieFinder` MATCH 기반 |

## Side Vision ID 기준

Vision 프로젝트 등록 이름:

```text
FrontSideVision -> DieEdgeFinder, FrontSurfaceInspector, FrontChippingInspector
RearSideVision  -> DieEdgeFinder, RearSurfaceInspector, RearChippingInspector
```

Handler Side 표면 검사 요청은 Vision 등록명에 맞춰 아래 이름을 사용한다.

```text
FrontSide -> FrontSurfaceInspector
RearSide  -> RearSurfaceInspector
```

`DieEdgeFinder`는 Finder이므로 Side edge 위치를 찾는 MATCH 계열에 사용해야 한다. 표면/외관 검사는 Inspector ID인 `FrontSurfaceInspector`/`RearSurfaceInspector`가 기준이다.

Vision 수신부는 `m.Inspectors.ContainsKey(insp)`로 정확한 문자열을 찾는다. 등록되지 않은 `SurfaceInspector`, `TopSurfaceInspector`, `BottomSurfaceInspector`를 Side 모듈로 보내면 `fail:inspector not found`가 발생할 수 있다.

## 예시

Bottom 검사 시작:

```text
BottomInspection|INSPECTASYNC|SurfaceInspector|0|1|123|0|190;225
```

Bottom 검사 결과 폴링:

```text
BottomInspection|INSPECTRESULT|SurfaceInspector|123
```

Front Side 검사 시작:

```text
FrontSideVision|INSPECTASYNC|FrontSurfaceInspector|0|1|123|0|190;225
```

Rear Side 검사 시작:

```text
RearSideVision|INSPECTASYNC|RearSurfaceInspector|1|1|123|0|190;225
```

Wafer Die 매칭 시작:

```text
WaferVision|MATCHASYNC|DieFinder|0|1|123|0|190;225
```

매칭 결과 폴링:

```text
WaferVision|MATCHRESULT|DieFinder|123
```

Bottom XYT push:

```text
XYT|MODULE|fb|collet|die_index|x=...;y=...;t=...;ix=...;iy=...;valid=1;w=...;h=...
```

Bottom 외곽 검출이 끝나면 Vision은 최종 `INSPECTRESULT`보다 먼저 XYT/W/H를 푸시할 수 있다.
Handler `VisionTcpClient`는 이 푸시를 응답 큐와 분리해 파싱하고 `BottomXytStore`에
`(fb,collet)` 및 `die_index` 기준 최신값으로 저장한다. Side 공정은 이 저장값 도착을 확인한 뒤 진행한다.

## 정리 기준

앞으로 검사 ID는 Vision 프로젝트 등록 이름을 기준으로 통일하는 것이 맞다.

권장 기준:

| 영역 | 권장 tool ID |
|---|---|
| Bottom | `SurfaceInspector` |
| Front Side | `FrontSurfaceInspector` |
| Rear Side | `RearSurfaceInspector` |
| Output/Bin placement | `PlacementInspector` 또는 현재 사용 중인 `DieFinder` MATCH 중 하나로 명확히 고정 |

이름 통일 전에는 Handler 송신 로그의 `channel=... inspector=...`와 Vision 수신 로그의 `RX: MODULE|CMD|...`를 같이 봐야 한다.
