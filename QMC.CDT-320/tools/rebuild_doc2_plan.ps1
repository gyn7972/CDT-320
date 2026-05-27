# rebuild_doc2_plan.ps1 — 개발 계획서 완전 재작성 (32 Stage)
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"

$plan = @(
    @{ kind="title"; subtitle2="Development Plan — 32 Stage 종합 (매뉴얼 호환)" }

    @{ kind="section"; title="§1. 프로젝트 목표"; subtitle="Project Goals" }

    @{ kind="bullets"; title="1.1 목표"; subtitle="비전 + 비기능"; lines=@(
        "## 비전",
        "CDT-300 시리즈 다이 트랜스퍼 핸들러를 .NET 4.7.2 / WinForms 로 재구현",
        "Vision PC + 3D 시뮬레이터 통합 — sim 모드 단독 검증 가능",
        "AJINEXTEK AXL 실보드 + Sim 자동 분기 — 1 binary 양쪽 지원",
        "매뉴얼 3종 (CDT-300 + CDT-310 + Vision) 사양 호환",
        "",
        "## 비기능 요구",
        "1920x1080 CDT-300 스타일 UI / 다국어 (ko/en/zh-CN/ja)",
        "5단계 사용자 권한 (None/Operator/Engineer/Maintenance/Admin)",
        "회귀 테스트 자동화 — verify_all 117+ 항목",
        "auto-cycle CLI — 무인 사이클 N 다이 + Lot JSON 저장",
        "SECS/GEM 표준 호스트 통신",
        "",
        "## 산출물",
        "QMC.CDT-320.exe + QMC.Vision.exe + CDT320Simulator.exe",
        "tools/ — Perl + PowerShell 25+ 검증 스크립트",
        "문서/ — 설계도 + 계획서 + 체크리스트 PPT 3종",
        "Markdown — Master + Stage 계획 + Overnight Report"
    ) }

    @{ kind="section"; title="§2. 32 Stage 로드맵"; subtitle="Development Stages 1~32" }

    @{ kind="table"; title="2.1 Stage 1~10";
       cols=@("Stage", "주제", "주요 산출물", "PASS");
       colWidths=@(800000, 2700000, 4400000, 800000);
       rows=@(
         @("1",  "UI + Recipe Subset",       "Form1 + 6 Tab + 4 Subset 페이지 + Self-Test 5종", "25/25")
         @("2",  "SPC + Editors + Cognex",    "SpcChartPage + 5 Editor + ZoomDialog",            "10/10")
         @("3",  "Lot + Reject + Interlock",  "LotStorage + 5 Interlock + HSMS",                 "9/9")
         @("4",  "RemoteViewer + ActiveLot",  "Dialog + Page + S6F11 + Cognex Test",             "11/11")
         @("5",  "GUI Cycle 자동화",          "gui_cycle_automation.ps1 (UIA)",                   "3/3")
         @("6",  "Cognex Caliper/Hist/Color", "4 신규 Vision tool + IEdgeFinder",                 "5/5")
         @("7",  "SECS 13 표준 메시지",        "S1F1/2 ~ S9F1/3/5 + AlarmManager 자동 구독",       "3/3")
         @("8",  "추가 5 Interlock (총 15)",  "ExtendedInterlocks2.cs 5종",                       "2/2")
         @("9~10","문서화 + 정적 Audit",       "README + ARCHITECTURE + USER_GUIDE + audit",      "—")
       )
    }

    @{ kind="table"; title="2.2 Stage 11~20";
       cols=@("Stage", "주제", "주요 산출물", "PASS");
       colWidths=@(800000, 2700000, 4400000, 800000);
       rows=@(
         @("11", "i18n zh-CN",                 "Lang.Zh + RegisterChinese 50 키",                 "4/4")
         @("12", "i18n ja + AlarmMaster",      "Lang.Ja + AlarmCategory 8종 + 18 정의",          "4/4")
         @("13", "DryRun / StepRun",           "ApplyRecipeMode + StepRunGate 콜백",              "4/4")
         @("14", "Coordinate Map",             "AlignmentSolver + ApplyToMotor + Cognex stdev",    "2/2")
         @("15", "GUI ASCII PS1",              "Korean encoding-safe",                             "75%")
         @("16", "Material + SecsItem",        "Die/Frame/Storage + SecsItemFormat 9종",          "3/3")
         @("17", "Remote Viewer TCP",          "remote_viewer_client.ps1",                         "1/1")
         @("18", "자가진단",                    "SystemSelfTest + runtime_cycle_test 증강",       "2/2")
         @("19", "AlarmMaster 페이지",          "DataGridView + Save + 카테고리 필터",            "5/5")
         @("20", "Pick Retry",                 "DoOneDie 3회 재시도 + Backoff",                   "3/3")
       )
    }

    @{ kind="table"; title="2.3 Stage 21~32";
       cols=@("Stage", "주제", "주요 산출물", "PASS");
       colWidths=@(800000, 2700000, 4400000, 800000);
       rows=@(
         @("21", "BinCodeMap",                 "ConvertToBinCode + Color 매핑",                   "3/3")
         @("22", "JobOrder + JobQueue",        "Pending + History + DieMap CSV",                  "3/3")
         @("23", "25 Interlock 등록",           "InterlockRegistry + 다국어 알람",                 "5/5")
         @("24", "auto-cycle CLI",             "--auto-cycle N + Lot JSON 자동",                  "5/5")
         @("25", "ExtendedInterlocks3",        "Door/WaferZ/Vacuum/BinLid/ServoOff",              "2/2")
         @("26", "UI Polish + LotPort",        "i18n fix + 사이드바 + Feeder 5 항목",              "PASS")
         @("27", "Feeder 통합 + 4 GAP fix",     "OutputUnloader + SoftLimit + Init scan",          "PASS")
         @("28", "InputStage 통합",             "WaferLoaderAdapter + VisionAlign",                "PASS")
         @("29", "TPU PickerComponent",         "PickupAsync/PlaceAsync + 4-picker",               "PASS")
         @("30", "OutputStage Receive+Bin",     "ReceiveDieAsync + InspectBinPosition",            "PASS")
         @("31", "Vision Bottom + Side",        "InspectBottom/Side (sim fallback)",               "PASS")
         @("32", "설비 라이프사이클",            "ShutdownAsync + EmergencyStopAsync",              "PASS")
       )
    }

    @{ kind="section"; title="§3. 매뉴얼 사양 보강"; subtitle="Stage 43+ 매뉴얼 호환" }

    @{ kind="bullets"; title="3.1 매뉴얼 추가 사항"; subtitle="CDT-310 매뉴얼 사양 호환"; lines=@(
        "## 통신 채널 6종 (5100-5106)",
        "5100 Wafer Vision (이미 구현)",
        "5101 Inspection (이미 구현)",
        "5103 Bin Vision (이미 구현)",
        "5104 Main Communicator (Stage 43 추가)",
        "5105 TopSide Inspection Vision (Stage 43 추가)",
        "5106 BottomSide Inspection Vision (Stage 43 추가)",
        "",
        "## 시리얼 포트 2종",
        "Port 4 — Wafer Barcode Communicator (Stage 43)",
        "Port 6 — Bin Barcode Communicator (Stage 43)",
        "BarcodeSerialAdapter 신규 — IBarcodeReader 어댑터",
        "",
        "## SimulatorBridge IO 매핑",
        "Simulator IoMap 64+ DI / 80+ DO 호환",
        "MotionMap 37 axes (0~36) 매핑 (Stage 19/20 SideVision Y는 미대응 — 차후 확장)",
        "",
        "## 미반영 항목 (다음 라운드 후보)",
        "TopSide 카메라 모듈 (QMC.Vision)",
        "BottomSide 카메라 모듈",
        "Eject Pin Z 축 (sim axis 8)",
        "Side Vision Y 축 (sim axis 19, 20)"
    ) }

    @{ kind="section"; title="§4. 일정 + 위험"; subtitle="Schedule & Risk" }

    @{ kind="table"; title="4.1 위험 매트릭스";
       cols=@("위험", "영향", "확률", "대응");
       colWidths=@(2700000, 1100000, 1100000, 3600000);
       rows=@(
         @("Soft Limit < 이동 목표",          "Critical", "Med",  "Stage 27 GAP-1 fix — 모든 축 limit 재검토")
         @("NullObject 가 사이클 무력화",     "Major",    "Med",  "Stage 27/28 — Adapter 패턴")
         @("Init 시 Output 미스캔",           "Major",    "Low",  "Stage 27 GAP-2 fix")
         @("InputStage handoff 미연결",       "Major",    "Med",  "Stage 27 GAP-3 fix")
         @("카세트 가득 무한 ALARM",           "Minor",    "Med",  "Stage 27 GAP-4 fix — _cycleCts.Cancel")
         @("Sim 정확도 한계",                  "Low",      "High", "실보드 무관 — 차후 sim 보강")
         @("매뉴얼 사양 불일치",               "Major",    "Med",  "Stage 43 — 6 채널 + 시리얼 호환")
         @("UIA 좌표 매칭 실패",               "Low",      "High", "코드 무결성 무관 — 후속 보정")
       )
    }

    @{ kind="bullets"; title="4.2 검증 게이트"; subtitle="Pass 조건"; lines=@(
        "## 빌드 게이트",
        "G1 — Build clean (warning 0, error 0)",
        "G2 — verify_all 117/118 PASS (회귀 무결성)",
        "",
        "## 런타임 게이트",
        "G3 — auto-cycle 4/16/24/30/50 다이 정상 종료 (ALARM 0)",
        "G4 — Lot JSON 자동 저장 + BinDistribution 정상",
        "G5 — 6 Unit 모두 사이클 호출 경로 활성",
        "",
        "## 정직 audit 게이트",
        "G6 — 'Stage X 완료' 보고 후 사용자 재점검 시 GAP 식별",
        "G7 — 발견된 GAP 모두 fix 적용",
        "G8 — 매뉴얼 사양 vs 구현 대조 — 누락 식별"
    ) }
)

New-PptxQmc -OutPath "$root\02_CDT320_개발계획서.pptx" `
    -DocTitle "CDT-320" -DocSubtitle "Development Plan" `
    -DocSubject "Multi-Stage Development Plan" -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" -DocDate "2026. 04. 29" `
    -Slides $plan

Write-Output "Document 2 (개발계획서) — 완전 재작성 완료"
