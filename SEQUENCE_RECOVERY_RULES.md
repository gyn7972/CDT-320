# Sequence Recovery Rules

## 기본 규칙

- 모든 시퀀스 모션/실행 함수는 `Task<int>`를 반환한다.
- 성공은 `0`, 실패는 `-1` 또는 장비/모션에서 반환한 실패 코드를 사용한다.
- 각 Step은 시작 전에 조건을 다시 확인한다.
- Step이 성공하면 다음 Step을 `ResumeStep`으로 저장한다.
- Step이 실패해서 알람이 발생하면 실패한 Step을 `ResumeStep`으로 저장한다.
- 알람 해제 후 다시 시작하면 `ResumeStep`의 조건을 다시 확인하고, 문제가 없을 때 그 Step부터 다시 실행한다.
- Cycle Stop은 현재 진행 위치를 보존한다. 다시 Start/Cycle Run 하면 저장된 Step 또는 Cycle index부터 재개한다.
- Stop은 작업을 폐기하는 정지로 본다. 다시 시작하면 초기 조건부터 시작한다.

## 상태 의미

- `Running`: 시퀀스 진행 중.
- `Completed`: 정상 완료.
- `Alarm`: 실패 Step을 저장한 상태. 알람 해제 후 같은 Step부터 재개 가능.
- `CycleStopped`: 사용자가 Cycle Stop을 요청한 상태. 다음 시작에서 저장 위치부터 재개 가능.
- `Stopped`: 일반 Stop. 재개 위치를 지우고 처음부터 시작한다.

## 작성 패턴

1. Step enum을 만든다.
2. Step 시작 전 조건 확인 함수를 만든다.
3. 조건이 맞으면 모션/동작을 실행한다.
4. 실패하면 로그, 알람, 실패 사유를 남기고 현재 Step을 저장한다.
5. 성공하면 다음 Step을 저장한다.

## 로그/알람 규칙

- 실패는 Auto/Manual 구분 없이 반드시 로그를 남긴다.
- 로그 형식은 `Log.Write("Main", UserSession.Name 또는 "SYSTEM", source, message)` 기준을 따른다.
- UI 조작 실패는 Common 메시지박스로 사용자에게 알린다.
- 시퀀스 내부 실패는 알람으로 알린다.
