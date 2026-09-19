# CDT-320 프로젝트 작업 규칙

이 파일은 이 저장소의 **유일한 상시 작업 규칙**이다. 저장소 전체에 적용하며, 새 규칙은 다른 문서에 복제하지 않고 이 파일만 수정한다. 설계서·체크리스트·인계 문서는 참고 자료이며, 이 파일과 충돌하면 이 파일을 따른다. 시스템·개발자·사용자의 명시적 지시는 이 저장소 규칙보다 우선한다.

## 1. 기준 저장소와 작업 시작

- 현재 기준 저장소는 `D:\Source\CDT-320_New`이다.
- 실제 작업 루트는 경로 문자열을 추측하지 말고 `git rev-parse --show-toplevel`로 확인한다. `.git`과 `QMC.CDT-320.sln`이 함께 있는 루트만 사용한다.
- `D:\00.PROJECT\CDT-320\SourceNew\CDT-320`(이전 기준), `D:\Source\CDT-320`, `D:\Work\CDT-320`, 그 밖의 다른 CDT-320 복사본, 다운로드·문서·Temp 폴더의 코드를 기준으로 사용하거나 현재 저장소에 덮어쓰지 않는다.
- `D:\CDT-320`은 소스 저장소가 아니라 장비 실행 파일과 운영 데이터가 있는 배포 영역이다. 코드 검증 과정에서 이 경로를 소스 루트로 취급하거나 덮어쓰거나 정리하지 않는다.
- 작업을 시작할 때 다음을 확인하고 보고한다.
  1. 확인된 저장소 루트
  2. `git status --short --branch`
  3. 현재 브랜치
  4. 기존 변경 파일과 이번 작업 대상의 겹침 여부
- 이 저장소는 사용자의 지시에 따라 **현재 로컬 `master`에서 직접 작업한다.** 사용자가 해당 작업에서 별도 브랜치나 worktree를 명시적으로 요청하지 않는 한 새 브랜치·worktree를 만들거나 다른 브랜치로 전환하지 않는다.
- 여러 작업이 같은 디렉터리를 공유하므로 작업 시작과 파일 수정 직전에 현재 브랜치를 확인한다. `master`가 아니면 현재 변경과 전환 영향을 먼저 확인하고 사용자에게 알린다. 기존 작업을 덮어쓰거나 임의로 되돌려 `master`에 맞추지 않는다.

## 2. 요청 범위와 작업 트리 보호

- 분석·진단·리뷰 요청은 읽기 전용으로 수행한다. 사용자가 구현이나 수정을 요청한 경우에만 파일을 변경한다.
- 기존 dirty worktree는 다른 작업자의 진행 중인 작업으로 간주한다. 관련 없는 변경을 되돌리거나 덮어쓰거나 재포맷하지 않는다.
- 현재 요청과 직접 관련된 파일만 수정한다. 다른 작업이 이미 변경한 파일과 겹치면 기존 diff와 필요한 추가 변경 범위를 사용자에게 설명하고 승인받은 뒤 수정한다. 같은 범위에 대해 이미 받은 승인은 유지하며, 기존 변경 의도를 보존한다.
- 사용자가 명시적으로 요청하지 않으면 `commit`, `push`, `pull`, `merge`, `rebase`, `reset`, `checkout -- <file>`을 실행하지 않는다.
- 브랜치 전환, 대량 이름 변경, 광범위 자동 포맷, 생성 코드 재생성은 영향 범위를 먼저 확인한다.
- 작업 종료 시 수정·삭제 파일, 검증 결과, 실패 또는 미검증 항목을 보고한다.

## 3. 장비와 운영 데이터 안전

- 실제 장비 구동, 축 이동, IO 출력, 카메라 연결, 자동 사이클 실행은 사용자가 명시적으로 요청하고 장비 상태가 확인된 경우에만 수행한다. 기본 검증 범위는 정적 검사와 별도 출력 폴더 컴파일이다.
- 인터락, 시퀀스, 좌표계, Teaching 값, 모션 순서, 충돌 회피 조건을 변경하기 전에는 현재 동작·위험·수정 방향을 먼저 분석해 보고하고 명시적 승인을 받는다.
- Motion Guard, 인터락, Alarm, 장비 상태 확인을 우회하는 새 이동 경로나 예외 경로를 만들지 않는다.
- 안전 조건을 느슨하게 바꿀 때는 조건 바로 위에 변경 이유와 기계적 전제를 설명하는 한글 주석을 둔다.
- `D:\CDT-320\Config`, `Recipes`, `EquipmentData`, `State`, `Motor`는 운영 상태다. `Log`, `Data`, `Image`, `CrashDumps`도 진단·생산 자료이므로 별도 요청 없이 삭제하지 않는다.
- 저장 상태와 실제 센서·축 상태가 일치하지 않으면 자동 운전을 차단하고 복구가 필요한 상태로 처리한다. 저장값만 믿고 이동을 재개하지 않는다.

## 4. 모션·인터락 안전 불변식

- 인터락은 최후 안전망이다. 시퀀스가 위험 궤적을 먼저 발행한 뒤 인터락 정지에 의존하지 않는다.
- 두 픽커가 설정된 마주보기 안전거리 안에 있으면 최소 한쪽 Y가 Avoid여야 한다. 양쪽 Y가 동시에 전진한 상태를 허용하지 않는다.
- Y 전진은 허용된 공정 phase와 자재 보유 조건에서만 수행한다. 그 외 phase에서는 Y=Avoid를 먼저 보장한다.
- X 이동 전 자기 Y 안전상태와 상대 픽커의 간섭 가능성을 확인한다. 마주보기 구간에서는 상대 Y=Avoid를 확인한다.
- Z 하강은 허용된 Bottom/Side/Place phase에서만 수행한다. Z 하강 중 XYT 보정은 설정된 Correction Window 안으로 제한한다.
- Input/Output 검사 존의 락 또는 리스를 보유한 장치가 있으면 다른 장치의 해당 존 진입을 차단한다.
- phase lease, 존 락, 공유 자원은 자기 픽커가 물리적으로 Safe(Y/Z Avoid)에 도달한 것을 확인한 뒤 해제한다.
- 시작·정지 후 재개·모션 재발행 전에는 실제 좌표와 자재 상태를 다시 읽는다. 안전 배치가 아니면 제한된 안전 후퇴를 먼저 수행하고 완료를 재확인한다.
- Auto와 Manual/Step 동작은 동일한 모션 선행 조건과 인터락을 사용한다. Manual/Step을 우회 경로로 만들지 않는다.

## 5. 시퀀스와 모션 구현

- 장비 시퀀스·모션의 표준 반환형은 `Task<int>`다. 성공은 `0`, 실패는 `-1` 또는 하위 장비가 반환한 오류 코드를 사용한다.
- UI 이벤트 외의 비동기 코드는 `async void`로 작성하지 않는다. 기존 Sequencing/Equipment 코드의 `ConfigureAwait(false)` 정책을 임의로 제거하지 않는다.
- 각 Step 시작 직전에 실제 조건을 다시 확인한다. 이전 Step에서 확인한 상태가 계속 유효하다고 가정하지 않는다.
- 이동은 다음 세 단계를 분리한다.
  1. 이동 명령 발행
  2. 명령 결과 코드 확인
  3. 완료 대기 후 InPosition 또는 Teaching Position 도달 확인
- 명령 결과가 실패이면 즉시 중단하고 Log와 Alarm을 남긴다. 명령 성공만으로 이동 완료로 간주하지 않는다.
- 병렬 이동은 기구적으로 허용된 축에만 사용한다. `Task.WhenAll`로 명령 결과를 모두 확인한 뒤, 별도의 `Task.WhenAll`로 각 축의 완료를 확인한다.
- 병렬이 허용되지 않은 축은 앞 축의 명령·완료·최종 위치 확인이 모두 끝난 뒤 이동한다.
- 무한 폴링, 무기한 `WaitAsync`, UI 스레드의 `Thread.Sleep`, 취소 불가능한 대기를 만들지 않는다. 장비 대기는 `CancellationToken`과 합리적인 timeout을 사용하고 timeout 원인을 Log/Alarm에 남긴다.
- 공유 자원 lease는 `using`/`finally`로 확실히 반환하되, 물리 Safe 확인보다 먼저 반환하지 않는다.
- SignalBus와 핸드셰이크에는 producer, consumer, Set/Reset 소유자, timeout, 재시작 시 재설정 순서를 명확히 둔다. 단순 `IsSet` 확인 후 이동하지 말고 진입 직전 조건을 재확인한다.

## 6. 정지·알람·재개 상태

- 상태 의미를 다음과 같이 통일한다.
  - `Running`: 시퀀스 진행 중
  - `Completed`: 정상 완료
  - `Alarm`: 실패한 Step을 보존한 상태
  - `CycleStopped`: 현재 Step 또는 Cycle 위치를 보존한 협조 정지
  - `Stopped`: 재개 정보를 폐기한 일반 정지
- Step 성공 시 다음 Step을 재개 위치로 저장한다. Step 실패 시 실패한 현재 Step과 실패 원인을 저장한다.
- Alarm 해제 또는 Cycle Stop 후 재개 시 저장된 Step의 조건과 실제 좌표를 다시 확인한 뒤 재개한다.
- 일반 Stop 후 시작은 초기 조건부터 수행한다. 이전 ResumeStep을 사용하지 않는다.
- 양쪽 픽커가 공정 중 정지했다면 동시에 전진 재개하지 않는다. 실제 좌표·자재·완료 근접도를 조정한 뒤 한쪽씩 안전 게이트를 해제한다.
- `OperationCanceledException`, 정상 정지를 나타내는 `SequenceStopException` 등 제어 흐름 예외를 일반 고장으로 오인해 삼키거나 중복 Alarm으로 올리지 않는다. 필요한 정리 후 상위 제어기로 전달한다.

## 7. 예외·로그·알람

- 장비, 모션, 시퀀스, 통신, 파일 저장, 외부 SDK, UI 이벤트 같은 실패 경계에는 `try/catch`를 사용하고 실제 정리 작업이 있을 때 `finally`를 사용한다.
- 단순 계산·값 변환·조건 검사 함수에 의미 없는 `try/catch/finally`를 일괄 추가하지 않는다.
- `catch { }`, 빈 catch, 예외를 성공값으로 바꾸는 처리를 금지한다. 처리할 수 없는 예외는 문맥을 기록한 뒤 재전파한다.
- 실패 로그에는 동작, 대상, 실제값(Actual), 명령값(Command), 목표값(Target), 인터락 사유, Sequence/Step, 예외 내용을 가능한 범위에서 포함한다.
- 신규 운영자용 Log와 Alarm 메시지는 자연스러운 한국어로 작성한다. 식별자·프로토콜 토큰·외부 SDK 원문은 필요한 경우 함께 표시한다.
- 로그는 기존 형식을 따른다.

```csharp
Log.Write("Main", UserSession.Name, "RequestApplicationExit", "사용자가 프로그램 종료를 요청했습니다. - Ok");
```

- UI에서 사용자가 직접 요청한 동작이 실패하면 Common 메시지박스로 실패 원인과 안전 상태를 알린다.
- Sequence 내부 실패는 UI 메시지박스 대신 Log와 Alarm으로 알린다.
- 오류를 표시하는 데서 끝내지 말고 Stop, Alarm, Interlock, 리소스 해제, 장비 Safe 상태를 함께 검토한다.

## 8. WinForms UI

- Form과 UserControl은 `partial`로 작성하고 클래스명과 파일명을 일치시킨다.
- 생성자는 `InitializeComponent()`를 먼저 호출한 뒤 런타임 초기화를 수행한다.
- 컨트롤 선언, 생성, 속성, 배치, 이벤트 연결은 `*.Designer.cs`의 `InitializeComponent()` 안에 인라인으로 둔다.
- Designer 안에 컨트롤 생성·배치용 별도 메서드나 장비 제어, 계산, 데이터 처리, 시퀀스 로직을 넣지 않는다.
- 비즈니스 로직은 일반 `*.cs`, Service, Sequence 클래스로 분리한다.
- `SuspendLayout`/`ResumeLayout`/`Dispose` 구조와 Visual Studio Designer 호환성을 유지한다.
- 컨트롤 이름은 의미 있는 prefix를 사용한다.
  - Button `btn`, Label `lbl`, TextBox `txt`, ComboBox `cmb`
  - CheckBox `chk`, RadioButton `rdo`, DataGridView `grid`
  - Panel `panel`, Timer `timer`
- `button1`, `label1` 같은 자동 이름을 남기지 않는다.
- 이벤트 메서드는 `btnStart_Click`, `timerMain_Tick`, `gridData_CellClick`처럼 `컨트롤명_이벤트명`으로 작성한다.
- 이벤트 안에는 긴 동작을 직접 넣지 않고 입력 검증 후 별도 메서드를 호출한다.

## 9. Material과 영속 데이터

- Material의 단일 기준은 `MaterialStorage.State`와 `MaterialStateService`다. Grid와 UI 컨트롤은 View이며 자체 데이터를 기준 상태로 사용하지 않는다.
- Cassette → Wafer → Die 관계, Input/Good/NG Wafer, 실제 물리 위치를 분리해 추적한다.
- 카세트 스캔, wafer pick/place, barcode, alignment, die pickup/place, vision 결과, cassette unload, lot close 같은 의미 있는 이벤트 후 상태를 저장한다.
- Snapshot은 임시 파일 쓰기 → 이전본 백업 → 원자적 교체 순서로 저장한다.
- 재시작 복구 시 저장 상태와 실제 센서를 비교한다. 불일치하면 Recovery Required로 전환하고 Auto Run을 차단한다.
- JSON은 사람이 읽을 수 있는 pretty format으로 저장한다. 기존 공용 구현이 있으면 `JsonPrettySerializer.WriteObject(...)`를 우선 사용한다.
- 기존 설정·레시피·상태 파일의 직렬화 키를 변경할 때는 명시적 마이그레이션과 하위 호환을 제공한다.

## 10. 외부 Vision 인터페이스와 Sim 호환성

- 이 저장소에는 standalone Vision 실행 프로젝트와 검사 구현을 두지 않는다. Vision 프로그램 소스·카메라 SDK·검사 엔진·전용 테스트 프로젝트를 다시 복사하거나 추가하지 않는다.
- `QMC.CDT-320\Equipment\Vision`과 Handler의 Vision 관련 시퀀스·캘리브레이션·인터락·UI는 외부 Vision PC와 연결하는 Handler 코드이므로 이름만 보고 삭제하지 않는다.
- `QMC.Common\Ui\Vision`의 프레임 코덱과 표시 계약도 Handler가 직접 사용하므로 보존한다.
- 외부 Vision 프로그램의 수정·빌드·배포는 이 저장소의 작업 범위가 아니다.
- TCP 모듈명, 포트, 명령 토큰, AlgorithmKey, CameraId, 직렬화 키, 레시피 Tool ID 같은 와이어·영속 계약 문자열을 단순 코드 정리 목적으로 바꾸지 않는다.
- 계약 변경이 불가피하면 외부 Vision 담당 코드와의 호환 계획 및 기존 데이터 마이그레이션을 먼저 합의한다.
- Sim/DryRun/`UseVision=false`에서도 Handler의 모션 완료 확인과 인터락을 생략하지 않는다.

## 11. C# 구조·명명·프로젝트 등록

- private 필드는 `_camelCase`, 외부 상태는 Property를 사용한다. 외부에서 임의 변경되면 안 되는 상태는 `private set`을 우선한다.
- 메서드는 동사로 시작한다. 상태 확인은 `Is`, 가능 여부는 `Can`, 조건 검사는 `Check`, 값 접근은 `Get`/`Set`, 갱신은 `Update`, 입출력은 `Load`/`Save`를 사용한다.
- 일반 `*.cs` 내부는 다음 순서를 우선한다.
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
- 이 저장소의 구형 프로젝트는 명시적 `<Compile Include>`를 사용한다. 새 `.cs`를 만들거나 이동·이름 변경하면 해당 `.csproj` 등록과 Designer `DependentUpon`을 확인한다.
- 작업과 무관한 using 정리, 파일 전체 재배치, 대량 공백 변경을 만들지 않는다.

## 12. 인코딩과 한글

- `.editorconfig`를 정본으로 따른다. C#, 프로젝트, 설정, JSON, XML, Markdown, 텍스트는 UTF-8 BOM, CRLF, final newline을 사용한다.
- 인코딩 변환은 수정한 파일에만 적용하고 저장소 전체를 일괄 변환하지 않는다.
- 수정 범위에서 깨진 한글 UI 문구·주석·로그를 발견하면 문맥이 명확한 경우 복원한다.
- 의미가 불확실한 깨진 문구는 추측해 저장하지 말고 사용자에게 확인한다.

## 13. 빌드와 검증

- 기본 솔루션은 루트의 `QMC.CDT-320.sln`이며 `QMC.CDT-320`과 `QMC.Common`만 포함한다. Visual Studio 2022와 .NET Framework 4.7.2 Developer Pack이 필요하다.
- 솔루션 빌드의 플랫폼 표기는 `Any CPU`, 개별 csproj는 `AnyCPU`다. x64 네이티브 의존성 검증이 목적이면 솔루션 `x64` 구성을 사용한다.
- 일부 csproj의 `OutputPath`와 기존 `obj/*FileListAbsolute.txt`가 운영 경로 `D:\CDT-320`을 가리킨다. 원본 저장소에서 **Rebuild/Clean은 `OutDir` 지정 여부와 관계없이 실행하지 않는다.** Clean 단계가 과거 파일 목록을 따라 운영 EXE/DLL을 삭제할 수 있다.
- 원본 저장소에서는 별도 `OutDir`를 지정한 `/t:Build`만 허용한다. 완전한 Clean/Rebuild 검증이 필요하면 루트의 `_build_check_handler` 아래에 솔루션과 Handler/Common 소스만 복제하고 `bin`/`obj`를 제외한 복제본에서 수행한 뒤 삭제한다.
- 안전한 기본 컴파일 검증 예시는 다음과 같다.

```powershell
$repo = (git rev-parse --show-toplevel).Trim()
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe'
$verify = Join-Path $repo '_build_check_handler\out'

& $msbuild (Join-Path $repo 'QMC.CDT-320.sln') `
  /t:Build /p:Configuration=Debug '/p:Platform=Any CPU' `
  "/p:OutDir=$verify\" /m /v:minimal
```

- 자동 검증 스크립트는 실행 전 하드코딩된 과거 경로, 요구 런타임(Perl/Python), 실제 장비 연결 여부를 확인한다. 삭제된 로컬 Vision 프로젝트나 존재하지 않는 Simulator를 성공 기준으로 사용하지 않는다.
- 비트리비얼 변경은 `git diff --check`, 관련 정적 검사, 별도 OutDir 컴파일을 수행한다. 중요한 변경은 원본의 증분 Build만으로 완료 판정하지 않고 필요 시 임시 복제본에서 Rebuild한다.
- 실행 파일 잠금으로 복사가 실패하면 코드 오류와 구분한다. 별도 OutDir에서도 실패하는지 확인한다.
- 실제 장비 검증이 불가능하면 수행하지 않은 항목과 필요한 현장 검증 절차를 명확히 남긴다.

## 14. 임시 파일 정리

- 저장소 루트의 상시 문서는 작업 규칙 `AGENTS.md`와 프로젝트 안내 `README.md`만 유지한다.
- 단계별 계획, 체크리스트, 인계서, 작업 보고서, 프롬프트를 루트에 새로 만들지 않는다. 장기 보존이 꼭 필요한 기술 문서는 중복 여부를 확인한 뒤 `docs` 아래의 적절한 위치에 작성하고 `README.md`에서 찾을 수 있게 한다.
- UI 캡처, 테스트 이미지, 매뉴얼 텍스트 추출본, 임시 분석 결과는 루트에 커밋하지 않는다. 검증 중 생성했다면 용도가 끝난 뒤 제거하고, 보존 요청이 있으면 목적에 맞는 하위 폴더에 둔다.
- 안전하게 재생성 가능한 `.vs`, 프로젝트별 `obj`, 명시적으로 만든 `_build_check*`/`_codex_verify*`, `*.tmp`, stale `*.bak`/`*.orig`만 정확한 경로를 확인한 뒤 정리한다.
- `bin`이라는 이름만으로 재귀 삭제하지 않는다. `QMC.CDT-320\Equipment\Bin`은 실제 소스 폴더다.
- Handler/Common의 `bin` 출력은 즉시 실행에 필요할 수 있으므로 목적을 확인한 뒤 정리한다.
- 삭제된 Vision 프로젝트·검사 엔진·카메라/CUDA 테스트 자산을 이 저장소에 다시 생성하지 않는다.
- `QMC.CDT-320`, `QMC.Common`, 장비 Config/Recipe/State/Data는 임시 파일로 취급하지 않는다.
- 광범위한 `git clean -xfd`를 사용하지 않는다. 필요하면 먼저 dry-run 결과를 검토하고 사용자 범위 안의 항목만 표적 삭제한다.

## 15. 완료 체크

1. 현재 요청과 무관한 기존 변경이 보존되었는가?
2. 모션·인터락·통신·영속 계약을 우회하거나 깨뜨리지 않았는가?
3. 새 파일과 이름 변경이 csproj에 반영되었는가?
4. 인코딩, 한글, `git diff --check`가 정상인가?
5. 별도 OutDir 빌드 또는 수행 가능한 대체 검증을 완료했는가?
6. 검증용 임시 산출물을 정리했는가?
7. 수정 파일과 검증 결과, 남은 위험을 사용자에게 보고했는가?
