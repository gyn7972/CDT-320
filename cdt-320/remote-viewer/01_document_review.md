# CDT-320 Document Review — 원격 뷰어 (그랩 영상 송출)

## Source documents
- 사용자 구두 요구 (2026-05-29 세션): "핸들러 프로그램에서 비전 프로그램의 그랩 영상을 볼 수 있도록 원격 뷰어 시스템을 만든다. 검사 알고리즘마다 카메라 화면 1개씩 전송."
- 사용자 설계 확정 답변 (AskUserQuestion, 2026-05-29):
  - 이미지 소스 = **1번+2번 혼합** (실제 그랩 Bitmap + 화면영역 캡처 둘 다 지원)
  - 채널 단위 = **모듈(스테이션)별 5채널** (Wafer/Bottom/Bin/FrontSide/RearSide)
  - 통합 위치 = **Handler 미수정, Vision 쪽 서버만 개발**
  - 검증 = **기존 SP_RemoteViewer 클라이언트로 송출 확인**
- 기존 프로토타입: `D:\Work\CDT-320\SP_RemoteViewer\` (git 미추적) — 범용 화면캡처 원격뷰어 (서버 `ScreenCaptureServer` + 뷰어 컨트롤 `RemoteViewerControl`).

## Scope summary
QMC.Vision(비전 프로그램) 쪽에만, 모듈(검사 알고리즘 스테이션)별로 그랩 영상을 TCP로 송출하는 "그랩 스트림 서버"를 추가한다. 와이어 프로토콜은 기존 SP_RemoteViewer 클라이언트가 그대로 받을 수 있는 `[4바이트 길이][JPEG]` 형식을 따른다. Handler(QMC.CDT-320)는 일절 수정하지 않으며, 송출 여부는 SP_RemoteViewer 뷰어로 검증한다.

## Requirements
| ID | Category | Requirement | Source | Priority |
|----|----------|-------------|--------|----------|
| R-001 | Equipment Comm | Vision에서 모듈별(5개) 그랩 영상을 TCP로 송출하는 서버를 추가 | 구두+답변 | Must |
| R-002 | Equipment Comm | 채널 = 모듈 단위 5채널: Wafer/Bottom(Inspection)/Bin/FrontSide/RearSide | 답변(채널단위) | Must |
| R-003 | Equipment Comm | 와이어 프로토콜은 `[4바이트 LE 길이][JPEG 바이트]` (SP_RemoteViewer `RemoteViewerControl` 호환) | 답변(검증=SP_RemoteViewer) | Must |
| R-004 | Equipment Comm | 각 채널은 명령포트(5100/5101/5103/5105/5106)와 분리된 별도 뷰어 포트 사용 | 코드분석(포트충돌 회피) | Must |
| R-005 | Business Logic | 이미지 소스 "혼합": 실제 그랩 Bitmap(기본) + 화면영역 캡처(옵션) 둘 다 지원, config로 선택 | 답변(소스=1+2혼합) | Must |
| R-006 | Business Logic | Handler가 연결되지 않은 단독 테스트에서도 영상이 보여야 함(타이머 기반 프레임 확보) | 답변(SP_RemoteViewer로 테스트) | Must |
| R-007 | Equipment Comm | 1:N 다중 클라이언트 지원, 클라이언트 연결/해제 시 안전 | 프로토타입 동등성 | Should |
| R-008 | Data/Settings | 뷰어 포트/소스/FPS/품질을 vision.json(VisionSettings)에 영속 + 마스터 ON/OFF | 코드분석(기존 설정 패턴) | Should |
| R-009 | UI | Handler(QMC.CDT-320) 코드는 절대 수정하지 않음 | 답변(통합위치) | Must |
| R-010 | Equipment Comm | Vision 종료 시 모든 뷰어 서버/스레드/소켓 정리 (Dispose) | 안정성 | Must |
| R-011 | Business Logic | 뷰어 프레임 확보가 카메라 HW를 점유/충돌시키지 않도록 최소 침습 (passive tap 우선, idle 시에만 self-grab) | 코드분석(Camera 비스레드세이프) | Should |

## Categories used
- UI (WinForms) — 본 작업에서는 Handler UI 미수정, Vision UI 변경 없음(서버는 백그라운드).
- Equipment Comm / Embedded — TCP 송출 서버, 포트, 프로토콜.
- Business Logic — 프레임 소스(그랩 tap / 화면캡처), 단독 테스트 가시성.
- Data / Logging / Settings — VisionSettings 영속.

## Open questions
- (해결됨) 채널 단위 → 모듈별 5채널로 확정.
- (해결됨) 이미지 소스 → 그랩+화면캡처 혼합, 기본 그랩.
- 뷰어 포트 기본값: 명령포트 +100 미러링 제안 → Wafer 5200 / Bottom 5201 / Bin 5203 / FrontSide 5205 / RearSide 5206 (설계 단계에서 확정, config로 변경 가능).
- 실 Hik 카메라에서 viewer self-grab과 Handler grab의 동시성: 단독/Sim 테스트에는 무관, 실HW 운용 시 idle(>1.5s)에만 self-grab하도록 제한하여 충돌 최소화. (설계 R-011)
