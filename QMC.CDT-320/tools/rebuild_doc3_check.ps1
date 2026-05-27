# rebuild_doc3_check.ps1 — 체크리스트 완전 재작성 (Stage 1~43)
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"

$check = @(
    @{ kind="title"; subtitle2="Verification Checklist — Stage 1~43 + 매뉴얼 호환" }

    @{ kind="section"; title="§1. 회귀 검증"; subtitle="verify_all.pl Results" }

    @{ kind="table"; title="1.1 Stage 검증 결과 (118 항목)";
       cols=@("Stage 영역", "Total", "PASS", "FAIL", "비고");
       colWidths=@(2200000, 900000, 900000, 900000, 3600000);
       rows=@(
         @("handler_features",     "25",  "25",  "0", "310 이식 (Stage 1)")
         @("stage2 (SPC+Editor)",   "10",  "10",  "0", "5 ParameterEditor + ZoomDialog")
         @("stage3 (Lot+Interlock)", "9",   "9",   "0", "ExtendedInterlocks + HSMS")
         @("stage4 (RemoteViewer)",   "11",  "11",  "0", "Dialog + ActiveLot + UseHsms")
         @("stage5 (GUI Auto)",      "3",   "3",   "0", "UIA powershell")
         @("stage6 (Cognex)",        "5",   "5",   "0", "Caliper/Histogram/ColorMatch")
         @("stage7 (SECS 13)",        "3",   "3",   "0", "S1F1~S9F5")
         @("stage8 (5 Interlock)",    "2",   "2",   "0", "ExtendedInterlocks2")
         @("stage11~12 (i18n)",       "8",   "8",   "0", "zh-CN + ja + AlarmMaster")
         @("stage13 (DryRun)",        "4",   "4",   "0", "ApplyRecipeMode")
         @("stage14 (CoordMap)",      "2",   "2",   "0", "AlignmentSolver")
         @("stage15 (ASCII PS1)",     "4",   "3",   "0", "1 SKIP (Korean encoding)")
         @("stage16 (Material)",      "3",   "3",   "0", "Die/Frame/Storage + SecsItem")
         @("stage17 (RemoteViewer)",  "1",   "1",   "0", "PowerShell client")
         @("stage18 (SelfTest)",      "2",   "2",   "0", "SystemSelfTest")
         @("stage19 (AlarmMaster)",   "5",   "5",   "0", "DataGridView page")
         @("stage20 (Pick Retry)",    "3",   "3",   "0", "DoOneDie 3회 재시도")
         @("stage21~22 (Bin/Job)",    "6",   "6",   "0", "BinCodeMap + JobQueue")
         @("stage23 (25 Interlock)",  "5",   "5",   "0", "InterlockRegistry")
         @("stage24 (auto-cycle)",    "5",   "5",   "0", "CLI + Lot JSON")
         @("stage25 (ExtIL3)",        "2",   "2",   "0", "Door/WaferZ/Vacuum 등")
         @("**TOTAL**",               "118", "117", "0", "회귀 무결성 ✓")
       )
    }

    @{ kind="table"; title="1.2 런타임 종단 검증 (auto-cycle)";
       cols=@("Cycle", "Lot ID", "Good", "NG", "Yield", "ALARM");
       colWidths=@(1100000, 2700000, 800000, 800000, 1000000, 1100000);
       rows=@(
         @("4 다이",   "184407", "3",  "1", "75.0%", "0")
         @("8 다이",   "094707 (29/04)", "7",  "1", "87.5%", "0")
         @("16 다이",  "191101", "14", "2", "87.5%", "0")
         @("24 다이",  "185152", "22", "2", "91.7%", "0")
         @("30 다이",  "190309", "27", "3", "90.0%", "0")
         @("50 다이",  "192102", "46", "4", "92.0%", "0")
       )
    }

    @{ kind="section"; title="§2. Stage 26~43 신규 체크리스트"; subtitle="이번 라운드 작업" }

    @{ kind="table"; title="2.1 Stage 26~32 (6 Unit + 설비)";
       cols=@("Stage", "검증 항목", "PASS", "비고");
       colWidths=@(800000, 4500000, 900000, 2300000);
       rows=@(
         @("26", "Lot Port 5 항목 + Lang.Apply fix + 사이드바 통합",                 "PASS", "InputLoader Feeder 통합")
         @("27", "5 항목 + 4 GAP fix (SoftLimit/InitScan/Handoff/Cassette Full)",   "PASS", "OutputUnloader Adapter")
         @("28", "WaferLoaderAdapter + LoadAndPrepare + VisionAlign + Unload",       "PASS", "InputStage handoff")
         @("29", "PickerComponent.Pickup/Place + 4-picker SoftLimit",                 "PASS", "TPU Pickup capsule")
         @("30", "ReceiveDieAsync + InspectBin + StageY/Z SoftLimit",                 "PASS", "OutputStage 통합")
         @("31", "InspectBottomVisionAsync + InspectSideVisionAsync + sim fallback",  "PASS", "4-picker bottom/side")
         @("32", "ShutdownAsync + EmergencyStopAsync + Lot 정리",                     "PASS", "설비 라이프사이클")
       )
    }

    @{ kind="table"; title="2.2 Stage 33~42 (Polish + Extended)";
       cols=@("Stage", "검증 항목", "PASS", "비고");
       colWidths=@(800000, 4500000, 900000, 2300000);
       rows=@(
         @("33-A", "Console Logger Forward (auto-cycle 가시성)",                      "PASS", "Form1 Controller.LogMessage")
         @("33-B", "Collet Cleaning 매 N 다이",                                       "PASS", "DiesPerColletClean=24")
         @("33-C", "Recipe.Module 파라미터 바인딩",                                    "PASS", "ApplyRecipeMode 확장")
         @("34",   "SimCassetteDriver 슬롯 소비 마킹",                                "PASS", "UI LED 정확도")
         @("35",   "WorkTab Shutdown/EStop 버튼 + i18n",                              "PASS", "AddActionButton 신규")
         @("36",   "InputCassettePage 슬롯 6 → 16",                                  "PASS", "SimDriver 와 동기화")
         @("37",   "OutputCassettePage 라이브 (3카세트 × 25슬롯)",                     "PASS", "StoreFullWafer → SetOutputSlotFilled")
         @("38",   "Recipe ModuleSubsetPage 신규",                                     "PASS", "Pick/Collet/Inspect 옵션")
         @("39",   "Multi-Picker (PickersPerCycle 1~4)",                              "PASS", "Task.WhenAll 동시")
         @("40",   "DualArmMode (Left/Right 교대)",                                   "PASS", "RightArm 활용")
         @("41",   "SECS/HSMS CycleStart/End 이벤트",                                  "PASS", "SecsHost.RaiseEvent")
         @("42",   "100 다이 대규모 검증",                                              "PARTIAL", "52 까지 정상 (sim 속도 한계)")
       )
    }

    @{ kind="table"; title="2.3 Stage 43 매뉴얼 호환";
       cols=@("ID", "검증 항목", "PASS", "비고");
       colWidths=@(800000, 4500000, 900000, 2300000);
       rows=@(
         @("43-A", "VisionMainPort 5104 추가",                                          "PASS", "AppSettings + VisionHub")
         @("43-B", "VisionTopSidePort 5105 추가",                                       "PASS", "AppSettings + VisionHub")
         @("43-C", "VisionBottomSidePort 5106 추가",                                    "PASS", "AppSettings + VisionHub")
         @("43-D", "WaferBarcodeSerialPort 4 추가",                                     "PASS", "AppSettings 정의")
         @("43-E", "BinBarcodeSerialPort 6 추가",                                        "PASS", "AppSettings 정의")
         @("43-F", "BarcodeSerialAdapter (IBarcodeReader 어댑터)",                       "PASS", "신규 클래스")
         @("43-G", "VisionHub.ConnectAllAsync 6 채널 시그니처",                          "PASS", "Wafer/Insp/Bin/Main/TopSide/BottomSide")
         @("43-H", "Form1 자동 연결 6 채널",                                              "PASS", "VisionAutoConnect")
       )
    }

    @{ kind="section"; title="§3. 정합성 + 누락 식별"; subtitle="문서 vs 코드 vs 매뉴얼" }

    @{ kind="diagram"; title="3.1 3중 정합성 매트릭스"; subtitle="매뉴얼 ↔ 문서 ↔ 코드"; shapes=@(
        @{ kind="box"; name="m"; x=300000;  y=1200000; w=2700000; h=900000; fill="C00000"; color="FFFFFF"; text="매뉴얼 (CDT-310/300)`n6 통신 + 시리얼 + 37 axis`n80+ DI/DO"; bold=$true; textSize=950 }
        @{ kind="box"; name="d"; x=3100000; y=1200000; w=2700000; h=900000; fill="ED7D31"; color="FFFFFF"; text="문서 (PPT/MD)`n42 Stage 검증`n매뉴얼 호환 명시"; bold=$true; textSize=950 }
        @{ kind="box"; name="c"; x=5900000; y=1200000; w=2700000; h=900000; fill="70AD47"; color="FFFFFF"; text="코드`n6 채널 + 시리얼 어댑터`n6 Unit 통합"; bold=$true; textSize=950 }
        @{ kind="arrow"; name="md"; x1=3000000; y1=1650000; x2=3100000; y2=1650000 }
        @{ kind="arrow"; name="dc"; x1=5800000; y1=1650000; x2=5900000; y2=1650000 }
        @{ kind="box"; name="ok"; x=1500000; y=2400000; w=6000000; h=500000; fill="E2EFDA"; color="375623"; text="매뉴얼 ≡ 문서 ≡ 코드 (Stage 43 후 일치)"; bold=$true; textSize=1200 }
        @{ kind="box"; name="g1"; x=300000; y=3100000; w=8400000; h=400000; fill="FFF2CC"; color="996600"; text="잔여 GAP: TopSide/BottomSide 카메라 모듈 (QMC.Vision)"; bold=$true; textSize=1000 }
        @{ kind="box"; name="g2"; x=300000; y=3550000; w=8400000; h=400000; fill="FFF2CC"; color="996600"; text="잔여 GAP: Eject Pin Z 축 (Simulator axis 8 미대응)"; bold=$true; textSize=1000 }
        @{ kind="box"; name="g3"; x=300000; y=4000000; w=8400000; h=400000; fill="FFF2CC"; color="996600"; text="잔여 GAP: Side Vision Y 축 (Simulator axis 19/20 미대응)"; bold=$true; textSize=1000 }
        @{ kind="text"; name="conc"; x=300000; y=4600000; w=8400000; h=300000; text="3 잔여 GAP 모두 차후 라운드 (Stage 44+) 후보"; size=1000; align="ctr"; bold=$true; color="595959" }
    ) }

    @{ kind="section"; title="§4. 결론"; subtitle="Final Verdict" }

    @{ kind="bullets"; title="4.1 진정한 완성도"; subtitle="Stage 1~43"; lines=@(
        "## 완료된 작업",
        "Stage 1~25 — 기반 + Vision + 통신 + 안정화 + 25 Interlock",
        "Stage 26~32 — 6 Unit 사이클 통합 + 설비 라이프사이클",
        "Stage 33~42 — Polish + UI + Multi-Picker + DualArm + SECS",
        "Stage 43 — 매뉴얼 6 채널 + Barcode 시리얼 호환",
        "",
        "## 검증",
        "verify_all 117/118 PASS — 회귀 무결성 (43 Stage 도입 후에도)",
        "auto-cycle 4/8/16/24/30/50 다이 모두 정상 종료",
        "100 다이 — 52 까지 정상 (sim 속도 한계)",
        "ALARM 0개 / ERROR 0개",
        "",
        "## 매뉴얼 호환",
        "6 통신 채널 (5100-5106) + 2 시리얼 (Wafer/Bin Barcode)",
        "37 axis Simulator IoMap + 80+ DI/DO",
        "잔여 GAP 3건 (TopSide/BottomSide 카메라, Eject Z, Side Vision Y) — 차후 라운드",
        "",
        "## 다음 라운드 (Stage 44+)",
        "TopSide/BottomSide Vision 카메라 모듈 (QMC.Vision)",
        "Eject Pin Z + Side Vision Y 축 추가",
        "실보드 매핑 + 양산 전환"
    ) }
)

New-PptxQmc -OutPath "$root\03_CDT320_체크리스트.pptx" `
    -DocTitle "CDT-320" -DocSubtitle "Verification Checklist (Final)" `
    -DocSubject "Stage 1~43 Verification" -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" -DocDate "2026. 04. 29" `
    -Slides $check

Write-Output "Document 3 (체크리스트) — 완전 재작성 완료"
Get-ChildItem $root -Filter "*.pptx" | Sort-Object Name | Format-Table Name, Length
