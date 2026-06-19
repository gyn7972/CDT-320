# CDT-320 Coding Rules

이 문서는 `QMC.CDT-320` 코드베이스에서 Codex가 작업할 때 따라야 하는 고정 규칙이다.
새 채팅이나 새 작업을 시작할 때 먼저 이 파일을 확인하고, 아래 기준을 적용한다.

## UI 작성 규칙

- UI class를 생성하거나 수정할 때 컨트롤 선언과 배치는 Designer 파일에 인라인으로 작성한다.
- Designer 파일 안에서 컨트롤 생성/배치용 별도 함수를 만들지 않는다.
- Designer 파일에는 필요한 값 설정과 이벤트 연결만 둔다.
- 비즈니스 로직은 모두 일반 `*.cs` 파일에 작성한다.
- Form, UserControl은 `partial` class로 작성하고, 클래스명은 파일명과 맞춘다.
- 생성자에서는 `InitializeComponent();`를 우선 호출하고, 이후 런타임 초기화 메소드를 호출한다.
- `.Designer.cs`에는 UI 생성, 컨트롤 속성 설정, 컨트롤 배치, 이벤트 연결만 둔다.
- `.Designer.cs`에는 장비 제어, 시퀀스, 계산, 데이터 처리 로직을 넣지 않는다.
- `.Designer.cs`의 `SuspendLayout()` / `ResumeLayout()` / `Dispose()` 구조를 임의로 깨지 않는다.
- Designer 파일은 Visual Studio Designer와 충돌할 수 있으므로 필요한 경우에만 조심해서 수정한다.
- 컨트롤 이름은 의미 있는 prefix를 사용한다.
  - Button: `btn`
  - Label: `lbl`
  - TextBox: `txt`
  - ComboBox: `cmb`
  - CheckBox: `chk`
  - RadioButton: `rdo`
  - DataGridView: `grid`
  - Panel: `panel`
  - Timer: `timer`
- `button1`, `label1`, `textBox1` 같은 자동 이름을 그대로 사용하지 않는다.
- 이벤트 함수명은 컨트롤명 + 이벤트명 형태로 작성한다. 예: `btnStart_Click`, `timerMain_Tick`, `gridData_CellClick`.
- 이벤트 함수 안에는 긴 로직을 직접 넣지 말고 조건 확인 후 실제 처리 메소드를 호출하는 구조로 작성한다.

## 코드 작성 규칙

- 함수는 기본적으로 `try / catch / finally` 구조로 작성한다.
- 예외 발생 시 사용자가 동작 멈춤 원인을 알 수 있도록 로그 또는 알람을 남긴다.
- 모션 함수는 비동기로 작성하고 반환형은 `Task<int>`를 사용한다.
- 장비 동작, Material 변경, Motion 이동, Manual Action에는 필요한 로그와 알람을 작성한다.
- 동작이 실패했을 때는 자동/수동 동작 여부와 관계없이 반드시 로그를 남긴다.
- 실패 로그에는 어떤 동작이 실패했는지와 왜 실패했는지를 포함한다.
- 로그는 아래 형태를 기준으로 작성한다.

```csharp
Log.Write("Main", UserSession.Name, "RequestApplicationExit", "Application exit requested by user. - Ok");
```

- UI에서 사용자가 직접 제어한 동작이 실패하면 `Common`의 메시지박스를 사용해서 실패 사실과 원인을 알려준다.
- Sequence 내부에서 발생한 실패는 UI 메시지박스가 아니라 Alarm으로 알려준다.
- Sequence 실패도 반드시 로그를 남기고, Alarm에는 설비 동작자가 원인을 알 수 있는 메시지를 넣는다.
- `catch { }`처럼 예외를 무시하지 않는다.
- UI 메시지만 띄우고 끝내지 말고, 장비 상태가 안전하게 정리되도록 Stop, Alarm, Interlock 처리를 고려한다.
- UI Thread에서 `while` 무한 대기나 `Thread.Sleep()`으로 장비 시퀀스를 제어하지 않는다.
- UI 이벤트에서 직접 모션을 길게 실행하지 말고 별도 메소드, Service, Sequence Class로 분리한다.
- 클래스 내부에서만 사용하는 필드는 `private`로 두고 `_camelCase`를 사용한다.
- 외부 접근이 필요한 상태값은 필드를 직접 공개하지 말고 Property를 사용한다.
- 장비 상태 Property는 외부에서 임의 변경되지 않도록 `private set`을 우선 고려한다.
- 메소드명은 동사로 시작한다. 예: `StartAuto`, `StopAuto`, `MoveReadyPosition`, `UpdateStatus`, `LoadRecipe`, `SaveRecipe`.
- bool 반환 메소드는 의미에 맞게 `Is`, `Can`, `Check` prefix를 사용한다.
  - `Is`: 현재 상태 확인
  - `Can`: 동작 가능 여부 확인
  - `Check`: 조건 검사, 실패 시 알람 가능
  - `Get`: 값 가져오기
  - `Set`: 값 설정
  - `Update`: 화면이나 상태 갱신
  - `Load`: 파일/데이터 읽기
  - `Save`: 파일/데이터 저장

## 작업 트리 보호 규칙

- 이 프로젝트는 작업 트리가 dirty 상태인 경우가 많으므로, 작업 시작 시 `git status --short`로 변경 상태를 확인한다.
- 사용자가 명시적으로 요청하지 않는 한 기존 변경을 되돌리지 않는다.
- 현재 작업과 직접 관련된 파일만 수정한다.
- 관련 파일에 이미 변경이 있으면 먼저 내용을 읽고 기존 변경과 충돌하지 않게 이어서 수정한다.
- 빌드 산출물, 임시 검증 폴더, 실행 중 생성 파일은 사용자가 요청하지 않는 한 정리하거나 삭제하지 않는다.

## 빌드 검증 규칙

- 기본 출력 경로 빌드는 실행 중인 `QMC.CDT-320.exe` 또는 Visual Studio가 DLL을 잠근 경우 복사 단계에서 실패할 수 있다.
- 컴파일 검증은 가능하면 별도 `OutDir`를 지정해서 수행한다.
- 권장 빌드 명령:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe' `
  'D:\00.PROJECT\CDT-320\Source\CDT320Simulator\QMC.CDT-320\QMC.CDT-320.sln' `
  /t:Build /p:Configuration=Debug /p:Platform="Any CPU" `
  /p:OutDir="D:\00.PROJECT\CDT-320\Source\CDT320Simulator\QMC.CDT-320\_codex_verify\build\" `
  /v:minimal
```

- 일반 빌드가 `MSB3021` 또는 `MSB3027`로 실패하고 메시지에 DLL 잠금이 표시되면, 코드 컴파일 실패가 아니라 실행 중인 프로세스의 파일 잠금일 수 있다.
- 별도 `OutDir` 빌드가 성공하면 그 사실을 최종 보고에 명확히 적는다.

## `.cs` 파일 배치 순서

일반 `*.cs` 파일은 아래 순서를 우선으로 정리한다.

1. Const / Readonly
2. Fields
3. Properties
4. Constructor
5. Initialize Methods
6. Event Methods
7. Public Methods
8. Sequence / Motion Methods
9. Private Methods
10. Check Methods
11. UI Update Methods
12. Utility Methods

핵심 기준은 위쪽에는 상태값, 중간에는 생성자/초기화/이벤트/외부 호출 함수, 아래쪽에는 내부 처리/검사/UI 갱신/공통 유틸 함수를 배치하는 것이다.

## 파일/인코딩 규칙

- 한글이 깨지지 않도록 파일은 UTF-8로 저장한다.
- 기존 파일 인코딩이 UTF-8이 아니면 수정 시 UTF-8로 변환해서 저장한다.

## Material 작업 기준

- Material 구조와 동작 기준은 repo 루트의 `MATERIAL_ARCHITECTURE_PLAN.md`를 우선으로 따른다.
- Material UI는 실제 `MaterialStorage.State`와 `MaterialStateService`를 통해 읽고 쓴다.
- Grid나 화면 컨트롤은 Material Data의 View일 뿐이며, 임시 UI 데이터가 기준이 되면 안 된다.

## 다른 프로젝트 적용 기준

- 이 규칙을 다른 프로젝트에서도 사용하려면 해당 프로젝트 루트에 이 `AGENTS.md` 파일을 복사한다.
- Material 구조 기획까지 함께 적용해야 하는 프로젝트라면 `MATERIAL_ARCHITECTURE_PLAN.md`도 함께 복사한다.
- 프로젝트명이 다르면 문서 첫 문장의 코드베이스 이름만 새 프로젝트명에 맞게 수정한다.

## JSON Save Rule

- JSON 파일 저장은 한 줄 압축 저장이 아니라 사람이 읽기 쉬운 UTF-8 pretty format으로 저장한다.
- DataContractJsonSerializer를 사용할 때는 가능하면 `JsonPrettySerializer.WriteObject(...)`를 사용한다.

## Sequence Recovery Rule

- 모든 시퀀스 실행/모션 함수는 `Task<int>`를 기준으로 작성한다.
- 성공은 `0`, 실패는 `-1` 또는 장비/모션 실패 코드를 반환한다.
- 시퀀스는 Step 단위로 작성하고, 각 Step 시작 전에 조건을 다시 확인한다.
- Step 성공 시 다음 Step을 재개 위치로 저장한다.
- 알람 발생 시 실패한 Step을 재개 위치로 저장한다.
- 알람 해제 후 다시 시작하면 저장된 Step의 조건을 다시 확인하고, 문제가 없으면 그 Step부터 재시작한다.
- Cycle Stop은 현재 위치/Step을 보존한다. 다시 시작하면 저장된 위치부터 재개한다.
- Stop은 현재 시퀀스 재개 정보를 폐기한다. 다시 시작하면 초기 조건부터 시작한다.
- 시퀀스 실패는 반드시 로그와 알람을 남긴다.
- 공통 규칙은 repo 루트의 `SEQUENCE_RECOVERY_RULES.md`를 따른다.

## Sequence Motion Safety Rule

- 시퀀스 클래스에서 모터/축 이동 명령을 내린 뒤에는 절대 바로 다음 Step이나 다음 간섭 가능 축으로 넘어가지 않는다.
- 이동 명령과 이동 완료 확인은 코드에서 분리해서 작성한다.
  - 예: `Move...CommandAsync(...)`로 이동 명령 결과 확인
  - 예: `Wait...InPosition...Async(...)`로 이동 완료, InPosition, Teaching Position 확인
- 이동 명령 후에는 반드시 명령 결과를 확인한다. `result != 0`이면 즉시 실패 처리하고 Alarm/Log를 남긴다.
- 이동 명령 결과가 성공이어도 반드시 `Wait`로 이동 완료를 확인하고, 최종적으로 InPosition 또는 Teaching Position 도착 여부를 확인한다.
- 병렬 이동이 허용된 축은 `Task.WhenAll(...)`로 이동 명령을 동시에 내리고, 각 결과를 모두 확인한다. 이후 완료 대기도 별도 `Task.WhenAll(...)`로 수행하고 각 결과를 모두 확인한다.
- 병렬 이동이 허용되지 않은 축은 이전 축의 명령 결과, Wait, InPosition/Teaching 확인이 모두 끝난 뒤 다음 축을 이동한다.
- 이 규칙은 시뮬레이션, DryRun, 실장비 모두 동일하게 적용한다. 시뮬레이션에서도 이동 완료 확인을 생략하지 않는다.
- 이 규칙을 어기면 장비 충돌/사고 위험이 있으므로 새 시퀀스 작성과 기존 시퀀스 수정 시 최우선으로 검토한다.

# Korean Text Recovery Rule

- 코드 수정 중 한글이 깨진 UI 문구, 주석, 로그 문구를 발견하면 가능한 범위에서 함께 복원한다.
- 복원 대상은 내가 직접 수정하는 파일과 그 작업에 필요한 주변 코드로 제한한다.
- 문맥상 의미가 명확한 깨진 문구는 자연스러운 한국어로 복원한다.
- 의미가 불확실한 문구는 임의로 추측해서 저장하지 말고 사용자에게 확인한다.
- 새로 저장하는 파일은 한글이 깨지지 않도록 UTF-8로 저장한다.
