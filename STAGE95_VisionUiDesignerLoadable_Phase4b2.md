# Stage 95 Phase 4b-2 — Vision UI 디자이너 로드 가능화 (Finder + Inspector)

> Phase 4b-2 만 수행하고 정지. 4b-3(CameraMapping 단독) 별도 지시 대기. 대상 top-level `D:\Work\source\QMC.Vision\Ui\Pages\`.

## 변환: 2파일 (각 1커밋)
| 파일 | 처리 | 커밋 |
|------|------|------|
| FinderPage | 정적 shell(title/CameraView/JogBox/결과그리드+5컬럼/버튼7/상태)→Designer.cs(그리드 BeginInit/EndInit, InitBtn 헬퍼). 람다 8(7버튼+RoiEdited)→named. | 33a8a7b |
| InspectorPage | 정적 shell(+PASS/FAIL 라벨, 버튼5, 결과그리드 3컬럼)→Designer.cs. 람다 6(5버튼+RoiEdited)→named. | 12167b1 |

## 추출/처리
- 객체초기화자→속성별 / 람다→named EventHandler / 지역컨트롤→필드 / 좌표리터럴.
- **런타임 주입 의존 자식은 Code 유지**: 두 파일 모두 `InspectionLightPanel(_module?.AlgorithmKey, _finder/_inspector?.Id)`(주입 기반 생성자 인자)+`LightLiveTuningPanel`(this 메서드 바인딩) → Designer 직렬화 불가 → `BuildChildPanels()`(Code). JogBox·CameraView(파라미터리스)는 Designer.
- **Stage 87 Dispose(StopLive+_liveTimer)** → Designer.cs 로 이동(components 와 통합).
- **closure 0**: 이벤트 람다 전부 `this`/필드만 캡처. `() => StartLive(0)`/`() => StopLive()` Action 인자는 Code 의 BuildChildPanels 에 잔류(이벤트 아님). LINQ/yield 는 Code 메서드.
- **시그니처 불변**: 양 페이지의 (파라미터리스 + 주입(VisionModule,IPatternFinder/IInspector)) 생성자 + public API(StartLive/StopLive/IsLiveOn 등). _title.Text 는 원본대로 미설정("" 유지 — 동작 무변경).

## 검증 (게이트 = 빌드0 + 정적0 + 스모크)
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0**(선재 System.IO.Ports 1).
- 정적검사 AFTER: 두 Designer.cs **위반 0**.
- 스모크(`cdt-320\designer-phase4b2-test\`): **ALL PASS** — 양 페이지 파라미터리스 인스턴스화+DrawToBitmap 무예외, 양 생성자 보존. FinderPage 렌더에 CameraView/결과그리드/버튼/조명/JogBox/라이브튜닝 전부 정상(Designer shell + Code 자식패널 통합).
- **csproj 4 엔트리(.cs×2 + .Designer×2) 확인**(빌드 grep). linter 재드롭 없음.

## closure 보류: 없음.

## 커밋
2 변환 커밋 → 로컬 master `--no-ff` 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 4b-3 (CameraMapping 단독) 진입 대기 (컨펌 필요).** 이후 4b-4(LightSystemSetupPage 최후).
