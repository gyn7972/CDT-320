# Vision 단독 구동 — 실제 TCP/IP 통신 자체 테스트

작성일: 2026-06-30

## 1. 목적

**QMC.CDT-320 핸들러 없이 QMC.Vision 혼자서** 자체 시퀀스를 돌리되,
**in-process 직접 호출이 아니라 실제 TCP/IP 경로**(자기 자신의 VisionTcpServer 로 127.0.0.1 루프백 접속)로
GRAB/MATCH/INSPECT 를 주고받게 한다. → 핸들러 없이도 "통신 경로(서버 + 소켓 + 프로토콜)"까지 Vision 단독 테스트.

> 핸들러(QMC.CDT-320) 코드는 **전혀 건드리지 않았다.** 변경은 모두 QMC.Vision 내부.

## 2. 배경 — 기존 구조

Vision 은 이미 핸들러 없이 자체 순차 실행하는 시퀀서를 갖고 있다.
`SequencerTab → VisionAutoSequenceHost → AutoSequenceCoordinator → ToolSequence`,
그리고 명령 실행은 `VisionSequenceContext.Dispatch → IVisionCommandDispatcher` 로 추상화돼 있다.
기본 구현 `DirectVisionCommandDispatcher` 는 모듈 API 를 **in-process 로 직접 호출**한다(TCP 안 탐).

`IVisionCommandDispatcher` 인터페이스 주석에도 "추후 TCP 경로와 Sim 시퀀서가 동일 로직을 쓰도록 통합" 이
예정으로 적혀 있었다. 이번 작업이 바로 그 seam 을 채운 것이다.

## 3. 변경 사항 (모두 QMC.Vision, 기본 OFF → 무회귀)

| 파일 | 내용 |
|---|---|
| `Sequencing/Common/TcpLoopbackVisionCommandDispatcher.cs` | **신규.** `IVisionCommandDispatcher` 구현. 모듈명→포트 매핑 후 127.0.0.1 의 자기 VisionTcpServer 에 TCP 접속, `MODULE\|CMD\|args` 전송, `ACK\|...\|payload` 수신. payload(마지막 `\|` 세그먼트)를 반환해 `DirectVisionCommandDispatcher` 와 **동일한 결과 계약** 유지(OK;.. / PASS;.. / FAIL;.. / fail:..). EPD/ARM 등 비동기 푸시는 스킵. 포트별 지연 연결 + 락. |
| `Sequencing/Common/VisionSelfRunTcpState.cs` | **신규.** `static volatile bool Active` — 자체 TCP 구동 중 서버 RUN 게이트를 함께 여는 플래그. |
| `Sequencing/Common/VisionAutoSequenceHost.cs` | `SimSelfRunOverTcp=true` 면 코디네이터 컨텍스트에 TCP 디스패처 주입 + `Active=true`. Stop 시 `Active=false` + Dispose. |
| `Equipment/Config/VisionConfig.cs` | `SimSelfRunOverTcp` 플래그(기본 **false**). |
| `Form1.cs` | 서버 명령 게이트 `IsCommandAllowed = () => IsReady \|\| VisionSelfRunTcpState.Active` (자체 TCP 구동 시 자기 명령 허용). |
| `QMC.Vision.csproj` | 신규 파일 2개 `<Compile Include>` 등록. |

동작 스위치는 `IVisionCommandDispatcher` **한 지점**만 갈아끼운 것이라, ToolSequence/판정/로그/좌표계산 등
기존 시퀀서 로직은 그대로 재사용된다(경로만 in-process → TCP).

## 4. 사용법

### 4-1. 설정 (`vision.json`)

```json
{
  "Provider": "Sim",
  "SimAutoSequence": true,
  "SimSelfRunOverTcp": true,
  "SimSequenceIntervalMs": 500,
  "SimEmitChipUid": true
}
```

- `SimAutoSequence: true` — Vision 자체 실행 모드(핸들러 없이 RUN 버튼으로 자체 구동). 이게 켜져야 RUN 이 시퀀서를 돌린다.
- `SimSelfRunOverTcp: true` — 자체 실행을 **실제 TCP 루프백**으로 구동(끄면 기존 in-process 직접 호출).
- `SimEmitChipUid: true` — (선택) 합성 chipUid 발급 → 이미지/데이터 로그까지 실제 흐름과 동일하게 기록.

### 4-2. 실행

1. `QMC.Vision.exe` 실행 (핸들러/Simulator 불필요). 기동 시 6채널 TCP 서버가 listen.
2. 작업 화면에서 **RUN** 버튼 → 자체 시퀀서가 시작되며, 이번엔 명령이 **127.0.0.1 소켓**으로 오감.
3. **STOP** → 시퀀서 정지 + 루프백 연결 해제 + 게이트 닫힘.

## 5. 검증 방법

- **통신 로그**(통신 페이지 / `VisionCommLog`)에 자기 자신과의 TX/RX 가 보이면 실제 TCP 통신 확인:
  - 서버측 `[WaferVision] RX: WaferVision|MATCH|...`, `[WaferVision] TX: ACK|WaferVision|MATCH|...`
  - `[WaferVision:5100] client connected: 127.0.0.1:xxxxx`
- **SEQ 로그**: `[SEQ] 자체 실행 경로 = 실제 TCP 루프백(127.0.0.1). 서버 게이트 개방.`
- **netstat**: `netstat -ano | findstr "5100 5101 5103 5105 5106"` → LISTENING + 127.0.0.1 간 ESTABLISHED.
- **프로토콜 왕복 검증(완료)**: 디스패처 요청 → 서버 wrapping(echo 규칙) → 푸시 스킵 리더 → payload 추출이
  `DirectVisionCommandDispatcher` 반환과 **문자열 단위로 동일**함을 파서 포팅으로 확인
  (GRAB/MATCH/INSPECT/FAIL/ERR 전부 PASS). 즉 판정/로그는 경로와 무관하게 동일.

## 6. 동작 원리 요약

```
ToolSequence.RunCycleAsync
  └ Context.Dispatch(module, "GRAB"/"MATCH"/"INSPECT", args)
      └ IVisionCommandDispatcher = TcpLoopbackVisionCommandDispatcher   ← 이번에 주입
          └ TcpClient → 127.0.0.1:모듈포트 (자기 VisionTcpServer)
              └ "MODULE|CMD|args"  ─────────────►  VisionTcpServer.ProcessLine
                                                     └ VisionCommandCore.(Grab/Match/Inspect)
              └ payload ◄── "ACK|MODULE|CMD|[echo|]payload"
          └ payload 반환(Direct 와 동일 계약) → Judge → 로그/메트릭
```

## 7. 한계 / 후속

- 게이트: 자체 TCP 구동 중에는 `VisionSelfRunTcpState.Active=true` 로 서버가 명령을 허용한다.
  실제 핸들러 모드에선 항상 false → 기존 READY 게이트 동작 불변.
- 뷰어 이미지 스트림(5200~)은 이 경로와 무관(자체 시퀀서는 명령 채널만 사용).
- 결과값/판정은 실제 Sim 알고리즘(OpenCV/Sim 백엔드)이 만든다 — 모의 고정값이 아님.
  (즉 알고리즘까지 실제로 태우며 통신 경로만 TCP 로 강제하는 진짜 end-to-end 자체 테스트.)
- UI 토글은 추가하지 않았다(Designer 변경 리스크 회피). 현재는 `vision.json` 으로 제어.
  원하면 GENERAL 페이지의 'Sim 자동 실행' 토글 옆에 체크박스를 추가할 수 있다.

## 8. ⚠️ 빌드/동기화 주의

- 변경은 마운트된 `C:\Project\CDT-320` 에 적용. **실제 빌드 저장소가 `V:\Source` 라면** 빌드 전에
  이 변경(QMC.Vision 5개 파일)이 반영됐는지 확인할 것(동기화가 편집을 되돌릴 수 있음).
- 이 환경(Linux)에서는 .NET/WinForms 빌드가 불가해 **컴파일 검증은 미수행**. 프로토콜 왕복은 파서 포팅으로 검증함.
  Windows 에서 아래 빌드 1회 확인 권장.

```powershell
$MSB = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $MSB "QMC.Vision\QMC.Vision.csproj" /t:Build /p:Configuration=Debug
```
