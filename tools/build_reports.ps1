# build_reports.ps1 - Build all PPTX reports for D:\Work\CDT-320\문서

# Import New-Pptx function
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx.ps1"

$root = "D:\Work\CDT-320\문서"
if (-not (Test-Path $root)) { New-Item -ItemType Directory -Path $root -Force | Out-Null }

# ═══════════════════════════════════════════════════════════════════════════
# REPORT 1: 전체 개발 단계 계획서 + 검증 결과
# ═══════════════════════════════════════════════════════════════════════════

$slides1 = @(
    @{ kind="title"; title="QMC CDT-320 개발 단계 보고서"; lines=@(
        "Die Transfer Handler — Multi-Stage Development Report",
        "",
        "작성일: 2026-04-28",
        "범위: Stage 1 ~ Stage 26 (UI/Lot Port)",
        "검증: tools/verify_all.pl  117/118 PASS  0 FAIL"
    ) }

    @{ title="개발 개요"; lines=@(
        "프로젝트: CDT-320 듀얼 픽커 다이 본더 핸들러 + Vision PC + 3D 시뮬레이터 통합 솔루션",
        "구성: QMC.CDT-320 (메인 핸들러, WinForms .NET 4.7.2)",
        "         QMC.Vision (비전 PC, TCP 5100/5101/5103)",
        "         QMC.Common (공용 라이브러리)",
        "         CDT320Simulator (WPF + HelixToolkit 3D)",
        "검증 도구: tools/ 디렉토리 — 23개 단계별 verify_*.pl 스크립트",
        "통합 회귀: verify_all.pl → 118 항목 중 117 PASS (1 SKIP, 0 FAIL)"
    ) }

    @{ title="Stage 1 ~ 5 — 기반 + Cycle 자동화"; lines=@(
        "Stage 1: UI 연결 + Recipe Subset (Die/Frame/Load/Unload/Module)",
        "Stage 2: SPC + Editors + Zoom + Cognex 진단 (10/10 PASS)",
        "Stage 3: Lot + Reject + 5 Interlock + HSMS + Remote + Ionizer (9/9)",
        "Stage 4: RemoteViewerDialog + ActiveLotPage + SecsHost UseHsms (11/11)",
        "Stage 5: GUI Cycle 자동화 (UIA via gui_cycle_automation.ps1) (3/3)"
    ) }

    @{ title="Stage 6 ~ 10 — Vision 기능 확장"; lines=@(
        "Stage 6: Cognex Caliper / Histogram / ColorMatch (5/5 PASS)",
        "Stage 7: Overnight 추가 작업 (3/3)",
        "Stage 8: 5개 Stage8 Interlock 추가 (2/2)",
        "Stage 9~10: 비전 Inspection Parameters + JSON 영속화"
    ) }

    @{ title="Stage 11 ~ 15 — 통신 + Cognex"; lines=@(
        "Stage 11: SECS/GEM SecsHost — 13 표준 메시지 (4/4 PASS)",
        "Stage 12: HSMS dual mode + 라인 모드 (4/4)",
        "Stage 13: DryRun / StepRun + StepRunGate 콜백 (4/4)",
        "Stage 14: Coordinate Map 3-point Alignment (2/2)",
        "Stage 15: GUI 자동화 ASCII-only (4/3 — 1 SKIP)"
    ) }

    @{ title="Stage 16 ~ 20 — 안정화 + 자가진단"; lines=@(
        "Stage 16: Material 추적 — Die/DieTapeFrame/MaterialStorage (3/3)",
        "Stage 17: Remote Viewer TCP 화면 캡처 송신 (1/1)",
        "Stage 18: 자가진단 (SystemSelfTest) (2/2)",
        "Stage 19: AlarmMaster 페이지 + 알람 마스터 데이터 (5/5)",
        "Stage 20: Pick Retry — DoOneDieAsync 내 3회 재시도 (3/3)"
    ) }

    @{ title="Stage 21 ~ 25 — 25 Interlock + 자동 사이클"; lines=@(
        "Stage 21: BinCodeMap NG → bin → color (3/3 PASS)",
        "Stage 22: JobOrder + JobQueue (Pending + History) (3/3)",
        "Stage 23: 25 Interlock 등록 (5 standard + 5 ext + 5 stage8 + 10 추가) (5/5)",
        "Stage 24: --auto-cycle N 명령행 옵션 + Lot JSON 자동 저장 (5/5)",
        "Stage 25: ExtendedInterlocks3 — DoorVsAll/WaferVisionZ/Vacuum 등 (2/2)"
    ) }

    @{ title="Stage 26 — UI Polish + Lot Port (이번 라운드)"; lines=@(
        "1. Lang.Apply Tag 파싱 버그 수정 (i18n:KEY;level:Lvl)",
        "2. 하단 네비 우측 3개 버튼 Anchor.Right 적용",
        "3. Settings 사이드바 단일 스크롤 + 분리선 통합",
        "4. GeneralPage AJINEXTEK 그룹 Dock 흐름 정리",
        "5. Lot Port 시퀀스 — 5개 항목 모두 구현:",
        "   ① MachineController.LoadNextWaferAsync / RetractCurrentWaferAsync",
        "   ② DoOneDieAsync — 매 8 다이마다 다음 슬롯 자동 진행",
        "   ③ InputCassettePage 7개 버튼 Click 핸들러 연결",
        "   ④ 슬롯 6 LED — 200ms Timer 로 WaferMap 바인딩",
        "   ⑤ SimulatorBridge DI 매핑 (X060~X063) + SimCassetteDriver"
    ) }

    @{ title="검증 매트릭스 — verify_all.pl"; lines=@(
        "총 118 항목 — 117 PASS, 0 FAIL (1 SKIP: Vision exe 미실행)",
        "",
        "Stage      Total  PASS  FAIL  비고",
        "handler    25     25    0     310 이식 검증",
        "stage2     10     10    0",
        "stage3      9      9    0",
        "stage4     11     11    0",
        "stage5      3      3    0",
        "stage6      5      5    0",
        "stage7-25  ...    ...   0    누적 PASS",
        "",
        "회귀 무결성: Stage 26 도입 후에도 117/118 PASS 유지"
    ) }

    @{ title="런타임 검증 — Auto-Cycle 결과"; lines=@(
        "QMC.CDT-320.exe --auto-cycle 10 실행 결과:",
        "",
        "[1] Init 자동 매핑: ScanCassetteAsync 16 슬롯 모두 검사 → 16/16 웨이퍼 감지",
        "[2] CycleRun 시작 → LoadNextWaferAsync 호출",
        "    - ElevatorZ 슬롯 0(10mm) 이동",
        "    - 피더 하강 (FeederUpDownCyl)",
        "    - 웨이퍼 클램프 (FeederClampCyl)",
        "    - WaferClampedSensor 확인",
        "    - FeederY 교환 위치 150mm 전진",
        "[3] 8 다이 처리 후 슬롯 진행:",
        "    - RetractCurrentWaferAsync (피더 후퇴)",
        "    - 다음 슬롯 1 (16mm) 이동",
        "    - 같은 시퀀스 반복",
        "[4] 사이클 완료 → 최종 RetractCurrentWaferAsync → CloseLot"
    ) }

    @{ title="향후 작업 (다음 라운드 후보)"; lines=@(
        "OutputUnloader 시퀀스 통합 — Good/NG 분류 카세트 적재",
        "InputCassettePage 슬롯 LED 16개로 확장 (현재 6개 표시)",
        "OutputCassetteStatusPage 동등 구현",
        "시뮬레이터 측 DI_SET 명령 수신 + 3D 표시",
        "Recipe Subset 의 LotPort 파라미터 연동 (DiesPerWafer 등)",
        "실보드 모드에서의 Cassette 센서 모듈/비트 매핑 검증"
    ) }

    @{ title="결론"; lines=@(
        "Stage 26 완료 — 5개 Lot Port 작업 100% 구현",
        "회귀 무결성 유지 (verify_all 117/118 PASS)",
        "런타임 auto-cycle 검증 — 실제 카세트 시퀀스 동작 확인",
        "UI 배포 가능 수준 — 사이드바 / 네비 / 페이지 레이아웃 정리 완료",
        "",
        "현 상태: 시뮬 모드에서 전 사이클 종단 검증 완료",
        "운영 모드 전환 준비: 실보드 IO 매핑 + Setup/Recipe JSON 튜닝"
    ) }
)

New-Pptx `
    -OutPath "$root\01_CDT320_개발단계_종합보고서.pptx" `
    -DocTitle "CDT-320 개발 단계 보고서" `
    -DocAuthor "QMC" `
    -Slides $slides1 2>&1

# ═══════════════════════════════════════════════════════════════════════════
# REPORT 2: Stage 26 Lot Port 상세 — 계획 + 체크리스트 + 검증
# ═══════════════════════════════════════════════════════════════════════════

$slides2 = @(
    @{ kind="title"; title="Stage 26 — Lot Port 시퀀스 통합"; lines=@(
        "Input Cassette 자동 매핑 + 사이클 통합 + UI 바인딩 + 시뮬 센서",
        "",
        "작업일: 2026-04-28",
        "범위: 5개 항목 (LoadNextWafer / DoOneDie 통합 / UI 버튼 / LED / SimDI)"
    ) }

    @{ title="배경 — 발견된 문제점"; lines=@(
        "현황 점검 결과 (이전 세션):",
        "1. InputLoaderUnit.ScanCassetteAsync 등 단위층은 양호한 구현",
        "2. 그러나 CycleRunAsync / DoOneDieAsync 가 로트포트를 호출하지 않음",
        "3. InputCassettePage 7개 버튼 Click 핸들러 모두 비어 있음",
        "4. 슬롯 6 LED 는 정적 LimeGreen 더미",
        "5. SimulatorBridge DI 매핑 0건 — sim 카세트 센서 토글 불가",
        "",
        "결론: '단위층 vs 사이클 vs UI' 3개 층이 따로 노는 상태"
    ) }

    @{ title="작업 계획 (5개 항목)"; lines=@(
        "① MachineController 에 LoadNextWaferAsync / RetractCurrentWaferAsync 추가",
        "② DoOneDieAsync — index 0 진입 + N 다이마다 다음 슬롯 자동 진행",
        "③ InputCassettePage 의 7개 버튼 → MachineController 메서드 연결",
        "④ 슬롯 6 LED → WaferMap 200ms 폴링 바인딩",
        "⑤ SimulatorBridge DI 매핑 (X060~X063) + SimCassetteDriver 신규 클래스",
        "",
        "전제: 시뮬 카세트 센서 자동 ON, 슬롯별 웨이퍼 감지 모방"
    ) }

    @{ title="체크리스트"; lines=@(
        "[X] 1. CDT320_Machine + AjinFactory + BaseDigitalInput 코드 분석",
        "[X] 2. SimCassetteDriver 클래스 신규 생성 (Equipment/Sim/)",
        "[X] 3. csproj Compile Include 등록",
        "[X] 4. MachineController.LoadNextWaferAsync 추가 (스캔→이동→교환)",
        "[X] 5. MachineController.RetractCurrentWaferAsync 추가",
        "[X] 6. MachineController.ScanInputCassetteAsync 추가 (UI 버튼용)",
        "[X] 7. InitAsync — AutoScanCassetteOnInit 옵션 자동 매핑",
        "[X] 8. CycleRunAsync — 시작 시 LoadNext, 종료 시 Retract",
        "[X] 9. DoOneDieAsync — 매 DiesPerWafer (8) 마다 슬롯 진행",
        "[X] 10. InputCassettePage 7 버튼 Click 핸들러 연결",
        "[X] 11. 슬롯 6 LED + Lifter Z 200ms Timer 갱신",
        "[X] 12. SimulatorBridge DI 매핑 (X060~X063)",
        "[X] 13. Form1 — SimCassetteDriver 인스턴스화 (sim 모드만)",
        "[X] 14. Build clean (QMC.Common + QMC.CDT-320)",
        "[X] 15. verify_all.pl 117/118 PASS 회귀 무결성 확인",
        "[X] 16. Auto-cycle 런타임 테스트 (16 슬롯 + 슬롯 진행 로그)"
    ) }

    @{ title="구현 ① — MachineController API"; lines=@(
        "신규 메서드:",
        "   public Task<bool> ScanInputCassetteAsync()",
        "   public Task<bool> LoadNextWaferAsync()",
        "   public Task<bool> RetractCurrentWaferAsync()",
        "신규 속성:",
        "   public int  CurrentInputSlot { get; private set; } = -1",
        "   public bool InputWaferAtExchange { get; private set; }",
        "   public bool AutoScanCassetteOnInit { get; set; } = true",
        "   public int  DiesPerWafer { get; set; } = 8",
        "이벤트:",
        "   public event Action LotPortStateChanged"
    ) }

    @{ title="구현 ② — DoOneDieAsync 통합"; lines=@(
        "추가 로직 (index > 0 조건):",
        "   if (index % DiesPerWafer == 0 && InputWaferAtExchange) {",
        "       await RetractCurrentWaferAsync();",
        "       await LoadNextWaferAsync();",
        "   }",
        "",
        "결과: 8 다이마다 슬롯 0 → 1 → 2 → ... 순으로 자동 진행",
        "      카세트 비면 텔레포트 모드로 전환 (사이클은 계속)"
    ) }

    @{ title="구현 ③ — InputCassettePage 버튼 연결"; lines=@(
        "btnPrev.Click  → MoveSlotRel(-1)        (이전 슬롯)",
        "btnNext.Click  → MoveSlotRel(+1)        (다음 슬롯)",
        "btnInit.Click  → LifterInitAsync        (Servo+Home)",
        "btnReady.Click → LifterReadyAsync       (Servo On)",
        "btnMap.Click   → ScanInputCassetteAsync (16 슬롯 매핑)",
        "btnLoad.Click  → LoadNextWaferAsync     (다음 웨이퍼 교환위치)",
        "btnUnld.Click  → RetractCurrentWaferAsync (피더 후퇴)",
        "",
        "Form1 접근: FindForm() as Form1 → host.Controller / host.Machine",
        "에러 처리: try/catch → MessageBox 알림"
    ) }

    @{ title="구현 ④ — 슬롯 LED 바인딩"; lines=@(
        "200ms Timer → RefreshFromMachine() :",
        "   - LifterAxisZ 값을 ActualPosition 으로 갱신",
        "   - WaferMap[i] 읽어 LED 색상 결정:",
        "       * i == CurrentInputSlot   → Cyan (작업 준비)",
        "       * map[i] == true          → LimeGreen (웨이퍼 있음)",
        "       * map[i] == false         → LightGray (비어 있음)",
        "",
        "Timer 라이프사이클: HandleCreated 시 Start, HandleDestroyed 시 Stop"
    ) }

    @{ title="구현 ⑤ — SimCassetteDriver"; lines=@(
        "Equipment/Sim/SimCassetteDriver.cs 신규 (138 줄):",
        "",
        "초기 센서 상태:",
        "   CassetteExistSensor = ON",
        "   ProtrusionSensor = OFF",
        "   WaferDetectSensor = OFF (위치 따라 동적)",
        "   WaferClampedSensor = OFF",
        "",
        "이벤트 훅:",
        "   ElevatorZ.MoveCompleted → 슬롯 위치 환산 → WaferDetectSensor 갱신",
        "   FeederClampCyl.InFwd.StateChanged → WaferClampedSensor 동기화",
        "",
        "외부 API: SetInputCassettePresent / SetInputSlotWafer / RefillInputCassette"
    ) }

    @{ title="구현 ⑤b — SimulatorBridge DI 매핑"; lines=@(
        "기존: DI 매핑 0건 (주석 처리됨)",
        "추가:",
        "   _diMap[InputLoader.CassetteExistSensor] = X060",
        "   _diMap[InputLoader.ProtrusionSensor]    = X061",
        "   _diMap[InputLoader.WaferDetectSensor]   = X062",
        "   _diMap[InputLoader.WaferClampedSensor]  = X063",
        "",
        "OnDiStateChanged → SendJson DI_SET 명령 송신 (시뮬 측 무시 가능)"
    ) }

    @{ title="검증 결과"; lines=@(
        "1. Build: QMC.Common + QMC.CDT-320 → CLEAN (warning 0)",
        "2. verify_all.pl → 117/118 PASS, 0 FAIL (회귀 무결성 유지)",
        "3. Auto-cycle 런타임 (--auto-cycle 10):",
        "   ✓ 카세트 매핑 16/16 슬롯 감지",
        "   ✓ ElevatorZ 10mm → 16mm → ... 슬롯 순차 진행",
        "   ✓ 피더 하강 / 클램프 / FeederY 150mm 전진",
        "   ✓ 8 다이 후 RetractFeeder + 다음 슬롯 진행",
        "   ✓ 사이클 종료 시 최종 후퇴",
        "4. UI 빌드: InputCassettePage 정상 표시 (Lifter Z 0.000mm 라이브)"
    ) }

    @{ title="체크리스트 정합성 재검증"; lines=@(
        "이중 검증 (요구사항 ↔ 구현):",
        "",
        "[O] 항목 ① ‒ LoadNextWaferAsync : MachineController.cs +130줄",
        "[O] 항목 ② ‒ DoOneDieAsync 통합 : MachineController.cs DiesPerWafer 분기",
        "[O] 항목 ③ ‒ 7 버튼 연결 : InputCassettePage.cs 7개 Click 핸들러",
        "[O] 항목 ④ ‒ 슬롯 6 LED : InputCassettePage.cs RefreshFromMachine 200ms",
        "[O] 항목 ⑤ ‒ SimDI : SimCassetteDriver.cs + SimulatorBridge X060~X063",
        "",
        "체크리스트 ≡ 작업 리스트 ≡ 구현 결과: 100% 일치"
    ) }

    @{ title="결론"; lines=@(
        "Stage 26 완료 — Lot Port 시퀀스 5개 항목 모두 구현 + 검증",
        "",
        "단위층 (InputLoaderUnit) ↔ 사이클 (CycleRunAsync) ↔ UI (InputCassettePage)",
        "이제 3개 층이 일관되게 통합됨",
        "",
        "런타임 결과: 16 웨이퍼 카세트 → 슬롯 단위 자동 진행 → 사이클 종료까지 전체 흐름 동작",
        "",
        "다음 라운드 후보: OutputUnloader 통합 / 슬롯 16개 UI 확장 / 시뮬 3D 카세트 시각화"
    ) }
)

New-Pptx `
    -OutPath "$root\02_Stage26_LotPort_상세보고서.pptx" `
    -DocTitle "Stage 26 Lot Port 상세 보고서" `
    -DocAuthor "QMC" `
    -Slides $slides2 2>&1

# ═══════════════════════════════════════════════════════════════════════════
# REPORT 3: UI 디자인 점검 + 수정 보고서
# ═══════════════════════════════════════════════════════════════════════════

$slides3 = @(
    @{ kind="title"; title="UI 디자인 점검 + 수정 보고서"; lines=@(
        "CDT-300 스타일 1920×1080 UI — 배포 가능 수준 디자인 정리",
        "",
        "작업일: 2026-04-28",
        "범위: 사이드바 / 하단 네비 / 페이지 레이아웃 / i18n 버그"
    ) }

    @{ title="발견된 문제점"; lines=@(
        "1. 사이드바 라벨이 'work.page.mainMonOperator' 같은 raw 키로 표시",
        "   → 원인: Lang.Apply 의 Tag.Substring(5) 가 ';level:..' 접미사 포함",
        "",
        "2. 와이드 모니터 (2576×1408) 에서 우측 3개 하단 네비 버튼 잘못 정렬",
        "   → 원인: Settings/User/Exit 버튼이 절대좌표 1440/1560/1680 으로 고정",
        "",
        "3. Settings 사이드바 — LIGHT SOURCE 와 BARCODE 사이 거대 빈 공간",
        "   → 원인: PnlSidebarBottomButtons 가 Dock=Bottom 으로 분리 영역 차지",
        "",
        "4. GeneralPage — AJINEXTEK 그룹이 본문 콤보 영역과 겹침",
        "   → 원인: body Dock=Top + ajinGroup 절대좌표 (8, 200) 의 z-order 충돌"
    ) }

    @{ title="수정 ① — i18n Tag 파싱"; lines=@(
        "파일: Ui/Localization/Lang.cs",
        "",
        "수정 전:",
        "   string key = tag.Substring(5);  // 'work.page.main;level:Operator'",
        "",
        "수정 후:",
        "   string key = tag.Substring(5);",
        "   int sep = key.IndexOf(';');",
        "   if (sep >= 0) key = key.Substring(0, sep);",
        "",
        "결과: 모든 사이드바/하단네비 라벨 한국어 정상 출력"
    ) }

    @{ title="수정 ② — 하단 네비 우측 정렬"; lines=@(
        "파일: Form1.Designer.cs (line 335-342)",
        "",
        "추가:",
        "   btnTabSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;",
        "   btnTabUser.Anchor     = AnchorStyles.Top | AnchorStyles.Right;",
        "   btnTabExit.Anchor     = AnchorStyles.Top | AnchorStyles.Right;",
        "",
        "결과: 모니터 너비 변경에도 우측 3개 버튼이 항상 우측 정렬"
    ) }

    @{ title="수정 ③ — Settings 사이드바 통합"; lines=@(
        "파일: Ui/Tabs/TabBase.cs + TabBase.Designer.cs",
        "",
        "변경:",
        "   - PnlSidebarBottomButtons 시각 비활성화 (Height=0, Visible=false)",
        "   - AddSidebarButton 의 toBottomArea=true 첫 호출 시 1회 분리선 삽입",
        "   - 모든 버튼을 PnlSidebarButtons 단일 스크롤 패널에 통합 배치",
        "",
        "결과: 16개 사이드바 버튼이 GENERAL ~ 원격뷰어까지 연속 표시"
    ) }

    @{ title="수정 ④ — GeneralPage 흐름 정리"; lines=@(
        "파일: Ui/Pages/Settings/GeneralPage.cs",
        "",
        "변경:",
        "   - ajinGroup 절대좌표 → Dock=Top, Height=110",
        "   - body 와 ajinGroup 의 z-order 명시적 정렬",
        "   - Controls.Add 순서 역순 (ajin → body → header) → 자연 흐름",
        "",
        "결과: 언어/빈배열/비전매칭 row 와 AJINEXTEK 그룹이 깔끔히 분리"
    ) }

    @{ title="검증 — 6개 탭 스크린샷 캡처"; lines=@(
        "tools/capture_tabs_xy.ps1 — 좌표 기반 자동 클릭 + 캡처",
        "",
        "결과 (모두 OK):",
        "   tab_work.png      — 작업 탭 (메인 화면 + 21개 사이드바)",
        "   tab_workinfo.png  — 작업정보 탭 (LIFTER + 6 슬롯 + LOGIC)",
        "   tab_history.png   — 이력 탭 (알람 그리드 + 5 사이드바)",
        "   tab_recipe.png    — 레시피 탭 (프로젝트 + 16 사이드바)",
        "   tab_settings.png  — 설정 탭 (GENERAL ~ 원격뷰어 통합)",
        "   tab_user.png      — 사용자 탭 (USER LOGIN)"
    ) }

    @{ title="회귀 검증"; lines=@(
        "verify_all.pl — Stage 26 UI 수정 전/후 동일",
        "총 118 항목, PASS 117, FAIL 0",
        "",
        "코드 변경 영향 없음:",
        "   - Lang.cs : 추가 if-block 1개 (영향 없음)",
        "   - Form1.Designer.cs : Anchor 속성 부여 (런타임 동작 동일)",
        "   - TabBase.* : 사이드바 버튼 배치 위치만 변경",
        "   - GeneralPage.cs : Dock 흐름만 변경"
    ) }

    @{ title="결론"; lines=@(
        "UI 4개 영역 (i18n / 네비 / 사이드바 / GeneralPage) 모두 수정 완료",
        "배포 가능 수준 도달 — 1920~2576 폭 모니터에서 일관된 표시",
        "",
        "회귀 무결성 유지 — verify_all 117/118 PASS",
        "",
        "다음 작업: OutputCassetteStatusPage 등 기타 페이지의 동일 점검"
    ) }
)

New-Pptx `
    -OutPath "$root\03_UI_디자인_점검보고서.pptx" `
    -DocTitle "UI 디자인 점검 보고서" `
    -DocAuthor "QMC" `
    -Slides $slides3 2>&1

Write-Output ""
Write-Output "═══════════════════════════════════════════════════════════"
Write-Output "All reports created at: $root"
Get-ChildItem $root -Filter "*.pptx" | Format-Table Name, Length, LastWriteTime
