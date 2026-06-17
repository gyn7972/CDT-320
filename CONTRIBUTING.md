# Contributing Guidelines

이 문서는 이 저장소(QMC.CDT-320)에 코드를 추가하거나 수정할 때 지켜야 할 기본 규칙을 정리한다. 상세 규칙은 [AGENTS.md](AGENTS.md)를 우선으로 따른다.

## 기본 규칙

### 1. UI 디자인 관련 규칙
- UI 컨트롤(예: WinForms, 사용자 컨트롤)을 생성하거나 수정할 때, 컨트롤의 **선언과 배치(레이아웃) 코드는 디자이너 파일(`*.Designer.cs`)에 인라인으로 작성**한다.
- 디자이너 파일 안에서 컨트롤 생성/배치용 **별도 함수(메소드)를 만들지 않는다.** (`InitializeComponent` 및 기본 속성 설정만 둔다.)

### 2. 비즈니스 로직 관련 규칙
- **모든 비즈니스 로직은 일반 `*.cs` 파일(디자이너 파일이 아닌 코드 비하인드)에 작성**한다.
- 디자이너 파일에는 컨트롤의 **속성 설정 및 이벤트 핸들러 연결만**을 작성한다.

### 3. 예외 처리 규칙
- 함수는 **기본적으로 `try / catch / finally` 구조**로 작성한다.
- `catch` 블록에서는 예외를 무시하지 말고 로그나 알람을 남기고, `finally` 블록에서는 상태 갱신/리소스 정리를 처리한다.

```csharp
public void SomeFunction()
{
    try
    {
        // 주요 로직
    }
    catch (Exception ex)
    {
        Alarms.AlarmManager.Raise(Alarms.AlarmSeverity.Error, "CODE", "Source", ex.Message);
        // 실패 처리
    }
    finally
    {
        // 상태/리소스 정리
    }
}
```

### 4. 모션 함수 작성 규칙
- **모든 모션 함수는 비동기(`async`)로 작성**한다.
- **반환형은 `Task<int>`** 를 사용하고, 결과는 `int` 결과 코드(0 = 성공, 그 외 = 실패 코드)로 반환한다.

```csharp
public async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)
{
    try
    {
        // ...
        return 0;
    }
    catch (Exception ex)
    {
        Alarms.AlarmManager.Raise(Alarms.AlarmSeverity.Error, "AX-MOVE-ABS", Name, ex.Message);
        return -1;
    }
    finally
    {
        UpdateStatus();
    }
}
```

### 5. 알람 및 로그
- 실패 동작은 **자동/수동 구분 없이 로그를 남기고**, 원인을 알 수 있는 메시지를 포함한다.
- 알람은 `Alarms.AlarmManager.Raise(...)`를 사용해서 설비 동작자가 원인을 알 수 있게 한다.

### 6. 파일 인코딩
- 모든 소스 파일은 **한글이 깨지지 않도록 UTF-8 (BOM 없음)** 으로 저장한다.
- 기존 파일의 인코딩이 다르면 수정 시 UTF-8로 변환해서 저장한다.
