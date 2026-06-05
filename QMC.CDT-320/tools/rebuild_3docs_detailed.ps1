# rebuild_3docs_detailed.ps1 — Stage 1~32 master 문서 기반 PPT 3종 완전 재작성
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"

# ════════════════════════════════════════════════════════════════════════════
# DOCUMENT 1 — 설계도 (System Design) — 완전 재작성, 매뉴얼 호환 반영
# ════════════════════════════════════════════════════════════════════════════

$design = @(
    @{ kind="title"; subtitle2="System Architecture & Design — 매뉴얼 3종 호환 (CDT-310 + CDT-300)" }

    @{ kind="section"; title="§1. 프로젝트 개요"; subtitle="Project Overview" }

    @{ kind="bullets"; title="1.1 시스템 개요"; subtitle="CDT-310 매뉴얼 사양 + CDT-320 확장"; lines=@(
        "## 프로젝트 정의",
        "CDT-300 시리즈 듀얼 픽커 다이 본더 핸들러 — .NET 4.7.2 / WinForms 재구현",
        "Vision PC + 3D 시뮬레이터 통합 (sim 모드 단독 검증 가능)",
        "AJINEXTEK AXL 실보드 + Sim 자동 분기 (1 binary 양쪽 지원)",
        "",
        "## 매뉴얼 3종 출처",
        "ASEKorea_CDT-300_User_Manual_20181012.doc — CDT-300 운영 매뉴얼",
        "2026.01.23_CDT310.docx — CDT-310 하드웨어/통신/IO 사양 (162KB)",
        "2025.08.19_CDT310 Vision.docx — Vision 통신 사양 (40KB)",
        "",
        "## 매뉴얼 호환 통신 채널 6종 (Stage 43 추가)",
        "5100 — Wafer Vision Communicator",
        "5101 — Inspection (Bottom) Vision Communicator",
        "5103 — Bin Vision Communicator",
        "5104 — Main Communicator (NEW)",
        "5105 — TopSide Inspection Vision (NEW)",
        "5106 — BottomSide Inspection Vision (NEW)",
        "",
        "## 시리얼 포트 2종 (Stage 43 추가)",
        "Port 4 — Wafer Barcode Communicator",
        "Port 6 — Bin Barcode Communicator"
    ) }

    @{ kind="diagram"; title="1.2 시스템 블록 다이어그램 (3 프로세스)"; subtitle="매뉴얼 사양 + Sim 통합"; shapes=@(
        @{ kind="box"; name="vision";    x=300000;  y=1300000; w=2000000; h=1300000; fill="5B9BD5"; color="FFFFFF"; text="QMC.Vision`n(Cognex/OpenCV/Sim)`n────────`n5100/5101/5103`n+5104/5105/5106"; bold=$true; textSize=850 }
        @{ kind="box"; name="handler";   x=2500000; y=1300000; w=2700000; h=1300000; fill="ED7D31"; color="FFFFFF"; text="QMC.CDT-320`n(Handler)`n────────`n6 Tabs / 5 권한`n6 Unit + 25 Interlock`nLot/Material/Job"; bold=$true; textSize=950 }
        @{ kind="box"; name="sim";       x=5400000; y=1300000; w=2000000; h=1300000; fill="70AD47"; color="FFFFFF"; text="CDT320Simulator`n(WPF 3D)`n────────`nTCP 7001`n37 axes / IO Map"; bold=$true; textSize=900 }
        @{ kind="box"; name="real";      x=2500000; y=2900000; w=2700000; h=700000;  fill="0F2D4F"; color="FFFFFF"; text="AJINEXTEK AXL Board`n37 axes + DI/DO + Cylinders"; bold=$true; textSize=950 }
        @{ kind="box"; name="bc";        x=5400000; y=2900000; w=2000000; h=700000;  fill="C00000"; color="FFFFFF"; text="Barcode Scanners`nSerial Port 4 / 6"; bold=$true; textSize=900 }
        @{ kind="arrow"; name="vh"; x1=2300000; y1=1850000; x2=2500000; y2=1850000 }
        @{ kind="arrow"; name="hs"; x1=5200000; y1=1850000; x2=5400000; y2=1850000 }
        @{ kind="arrow"; name="hr"; x1=3850000; y1=2600000; x2=3850000; y2=2900000 }
        @{ kind="arrow"; name="bcr"; x1=5800000; y1=2600000; x2=5800000; y2=2900000 }
        @{ kind="text"; name="cap"; x=300000; y=3900000; w=7400000; h=400000; text="라인 프로토콜 (Vision) + JSON HELLO (Sim) + P/Invoke AXL.dll (실보드)"; size=950; align="ctr"; color="595959"; italic=$true }
    ) }

    @{ kind="section"; title="§2. 4-Layer 아키텍처"; subtitle="Layered Architecture" }

    @{ kind="diagram"; title="2.1 Handler 4-Layer 구조"; subtitle="명확한 책임 분리"; shapes=@(
        @{ kind="box"; name="L1"; x=400000; y=1300000; w=8200000; h=600000; fill="ED7D31"; color="FFFFFF"; text="UI Layer — Form1 + 6 Tabs (Work/WorkInfo/History/Recipe/Settings/User) + 50+ Pages + 17 Dialogs"; bold=$true; textSize=950 }
        @{ kind="box"; name="L2"; x=400000; y=2050000; w=8200000; h=600000; fill="1F4E79"; color="FFFFFF"; text="Controller — MachineController (Init/Start/Stop/CycleRun/Shutdown/EStop)"; bold=$true; textSize=950 }
        @{ kind="box"; name="L3"; x=400000; y=2800000; w=8200000; h=600000; fill="595959"; color="FFFFFF"; text="Equipment — CDT320_Machine + 6 Units + 25 Interlocks + Lot/Material/Job/SECS/Vision"; bold=$true; textSize=900 }
        @{ kind="box"; name="L4"; x=400000; y=3550000; w=8200000; h=600000; fill="0F2D4F"; color="FFFFFF"; text="HAL — BaseAxis / BaseDigitalIO / BaseCylinder + AjinFactory (Sim ↔ Ajin 자동 분기)"; bold=$true; textSize=950 }
        @{ kind="text"; name="d1"; x=400000; y=1900000; w=8200000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d2"; x=400000; y=2650000; w=8200000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d3"; x=400000; y=3400000; w=8200000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d4"; x=400000; y=4300000; w=8200000; h=300000; text="UseAjin=true → AjinSystem.Open(IRQ) → P/Invoke AXL.dll  /  false → Sim* fallback"; size=1000; bold=$true; align="ctr"; color="333333" }
    ) }

    @{ kind="section"; title="§3. 6 Unit 통합 (Stage 28~32)"; subtitle="Equipment Layer Detail" }

    @{ kind="diagram"; title="3.1 사이클 파이프라인 (좌→우 단방향)"; subtitle="모든 Unit + 사이클 통합 (Stage 28~32)"; shapes=@(
        @{ kind="box"; name="U1"; x=300000;  y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="① Input`nLoader`n(Stage 26)"; bold=$true; textSize=900 }
        @{ kind="box"; name="U2"; x=1700000; y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="② Input`nStage`n(Stage 28)"; bold=$true; textSize=900 }
        @{ kind="box"; name="U3"; x=3100000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="③ Transfer`nPicker`n(Stage 29)"; bold=$true; textSize=900 }
        @{ kind="box"; name="U4"; x=4500000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="④ Vision`nInspection`n(Stage 31)"; bold=$true; textSize=900 }
        @{ kind="box"; name="U5"; x=5900000; y=1300000; w=1300000; h=900000; fill="595959"; color="FFFFFF"; text="⑤ Output`nStage`n(Stage 30)"; bold=$true; textSize=900 }
        @{ kind="box"; name="U6"; x=7300000; y=1300000; w=1300000; h=900000; fill="0F2D4F"; color="FFFFFF"; text="⑥ Output`nUnloader`n(Stage 27)"; bold=$true; textSize=900 }
        @{ kind="arrow"; name="a1"; x1=1600000; y1=1750000; x2=1700000; y2=1750000 }
        @{ kind="arrow"; name="a2"; x1=3000000; y1=1750000; x2=3100000; y2=1750000 }
        @{ kind="arrow"; name="a3"; x1=4400000; y1=1750000; x2=4500000; y2=1750000 }
        @{ kind="arrow"; name="a4"; x1=5800000; y1=1750000; x2=5900000; y2=1750000 }
        @{ kind="arrow"; name="a5"; x1=7200000; y1=1750000; x2=7300000; y2=1750000 }
        @{ kind="text"; name="t1"; x=300000; y=2400000; w=8400000; h=300000; text="단위층 — 각 Unit 의 메서드는 사이클 통합 전부터 정의됨 (LoadAndPrepare/Pickup/Receive 등)"; size=950; bold=$true; align="ctr"; color="333333" }
        @{ kind="box"; name="L1"; x=300000;  y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="ScanCassette`nMoveToTargetSlot`nMoveToExchange`nRetractFeeder"; textSize=800 }
        @{ kind="box"; name="L2"; x=1700000; y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="LoadAndPrepare`nVisionAlignAndSetupOrigin`nMultiScanAndPickup`nUnloadWafer"; textSize=750 }
        @{ kind="box"; name="L3"; x=3100000; y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="ArmX/ArmY 이동`nPickerComponent`nPickup/Place`n4-Picker 동시"; textSize=800 }
        @{ kind="box"; name="L4"; x=4500000; y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="InspectBottom`n(4 picker)`nInspectSide`n(4면)"; textSize=800 }
        @{ kind="box"; name="L5"; x=5900000; y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="ReceiveDie`n(Good/Ng 분류)`nInspectBin`n(안착 검사)"; textSize=800 }
        @{ kind="box"; name="L6"; x=7300000; y=2900000; w=1300000; h=1100000; fill="F2F2F2"; color="333333"; text="ScanAllCassettes`nStoreFullWafer`nSupplyEmptyWafer`nExchangeWafer"; textSize=750 }
        @{ kind="text"; name="t2"; x=300000; y=4200000; w=8400000; h=300000; text="이벤트 — 매 8 다이마다 Output 적재, 매 24 다이마다 Collet Cleaning"; size=1000; align="ctr"; color="ED7D31"; bold=$true }
    ) }

    @{ kind="bullets"; title="3.2 Unit 별 상세 (1)"; subtitle="InputLoader / InputStage / TransferPicker"; lines=@(
        "## InputLoaderUnit (Stage 26~28)",
        "축: ElevatorZ (axis 0) / FeederY (axis 1)",
        "실린더: FeederUpDownCyl / FeederClampCyl",
        "센서: CassetteExist / Protrusion / WaferDetect / WaferClamped",
        "메서드: ScanCassetteAsync / MoveToTargetSlotAsync / MoveToExchangePositionAsync / RetractFeederAsync",
        "",
        "## InputStageUnit (Stage 28)",
        "축: StageY(2) / StageT(3) / ExpanderZ(4) / CameraX(5) / NeedleBlockX(6) / NeedleZ(7)",
        "DO: NeedleVacuum",
        "메서드: LoadAndPrepareWaferAsync (ExpanderZ Down + 바코드 + 맵 로드)",
        "         VisionAlignAndSetupOriginAsync (Theta 보정 + Origin 확정)",
        "         MultiScanAndPickupAsync / UnloadWaferAsync",
        "",
        "## TransferPickerUnit (Stage 29)",
        "LeftArm (front): ArmX(9)/ArmY(10) + 4 Pickers (T11/Z12, T13/Z14, T15/Z16, T17/Z18)",
        "RightArm (rear): ArmX(21)/ArmY(22) + 4 Pickers (T23/Z24 ~ T29/Z30)",
        "각 Picker: PickupAsync (PickerZ Down + Vacuum) / PlaceAsync / MoveToFocusAsync",
        "TpuArmUnit: InspectBottomVisionAsync / InspectSideVisionAsync / PlaceDiesAsync"
    ) }

    @{ kind="bullets"; title="3.3 Unit 별 상세 (2)"; subtitle="Vision / OutputStage / OutputUnloader"; lines=@(
        "## VisionInspectionUnit (Stage 31)",
        "TpuArmUnit 위임 (4 picker bottom 비전 + 4면 side 비전)",
        "VisionHub.Inspection 미연결 시 sim fallback (skip)",
        "Bottom Picker1 OffsetX/Y → ReceiveDieRequest.VisionOffsetX/Y",
        "Side Picker1 IsAllOk → inspPass 반영",
        "",
        "## OutputStageUnit (Stage 30)",
        "GoodStage (StageY/StageZ) + NgStage (StageY/StageZ)",
        "BinCameraX (axis 34) — 안착 검사",
        "메서드: ReceiveDieAsync (충돌 회피 + Z up + Y 이동)",
        "         InspectBinPositionAsync (TPU 완료 대기 + 비전 검사)",
        "         RequestWaferChangeAsync (Unloader 위임)",
        "         PerformColletCleaningAsync (NgStage 더미 영역)",
        "",
        "## OutputUnloaderUnit (Stage 27)",
        "축: ElevatorZ (axis 36) / FeederY (axis 35)",
        "3 카세트: NG / Good1 / Good2 (각 25 슬롯)",
        "메서드: ScanAllCassettesAsync (75 슬롯 스캔)",
        "         StoreFullWaferAsync (target, slotIndex)",
        "         SupplyEmptyWaferAsync / ExchangeWaferSequenceAsync"
    ) }

    @{ kind="section"; title="§4. 통신 + 인터락"; subtitle="Communications & Safety" }

    @{ kind="diagram"; title="4.1 6 Vision 채널 + 시리얼 (매뉴얼 호환)"; subtitle="Stage 43 추가 채널"; shapes=@(
        @{ kind="box"; name="c1"; x=300000;  y=1300000; w=2700000; h=600000; fill="5B9BD5"; color="FFFFFF"; text="5100 Wafer Vision (matching/align)"; bold=$true; textSize=950 }
        @{ kind="box"; name="c2"; x=300000;  y=1950000; w=2700000; h=600000; fill="5B9BD5"; color="FFFFFF"; text="5101 Inspection (Bottom)"; bold=$true; textSize=950 }
        @{ kind="box"; name="c3"; x=300000;  y=2600000; w=2700000; h=600000; fill="5B9BD5"; color="FFFFFF"; text="5103 Bin Vision (안착)"; bold=$true; textSize=950 }
        @{ kind="box"; name="c4"; x=3100000; y=1300000; w=2700000; h=600000; fill="ED7D31"; color="FFFFFF"; text="5104 Main Communicator (NEW)"; bold=$true; textSize=950 }
        @{ kind="box"; name="c5"; x=3100000; y=1950000; w=2700000; h=600000; fill="ED7D31"; color="FFFFFF"; text="5105 Top Side Inspection (NEW)"; bold=$true; textSize=950 }
        @{ kind="box"; name="c6"; x=3100000; y=2600000; w=2700000; h=600000; fill="ED7D31"; color="FFFFFF"; text="5106 Bottom Side Inspection (NEW)"; bold=$true; textSize=950 }
        @{ kind="box"; name="s1"; x=5900000; y=1300000; w=2700000; h=600000; fill="C00000"; color="FFFFFF"; text="Serial Port 4 — Wafer Barcode"; bold=$true; textSize=950 }
        @{ kind="box"; name="s2"; x=5900000; y=1950000; w=2700000; h=600000; fill="C00000"; color="FFFFFF"; text="Serial Port 6 — Bin Barcode"; bold=$true; textSize=950 }
        @{ kind="box"; name="se"; x=5900000; y=2600000; w=2700000; h=600000; fill="595959"; color="FFFFFF"; text="SECS/HSMS Port 5000 (S6F11/S5F1)"; bold=$true; textSize=950 }
        @{ kind="text"; name="lab"; x=300000; y=3450000; w=8400000; h=400000; text="모든 채널은 미연결 시 sim fallback — 단독 sim 검증 가능"; size=1000; align="ctr"; bold=$true; color="375623" }
    ) }

    @{ kind="bullets"; title="4.2 25 Interlock 등록"; subtitle="Stage 3 + 8 + 23 + 25"; lines=@(
        "## 5 Standard (Stage 3 — ExtendedInterlocks.cs)",
        "EjectVsLoaderInterlock / LoaderVsStageInterlock / UnloaderVsStageInterlock",
        "EjectPickerVsBinInterlock / BinGuideVsBinFeederInterlock",
        "",
        "## 5 Extended (Stage 8 — ExtendedInterlocks2.cs)",
        "LifterVsExpanderInterlock / BarcodeVsLoaderInterlock",
        "SubPortVsPickerInterlock / ColletCleanerVsPickerInterlock / EmgStopVsAllInterlock",
        "",
        "## 5 ExtendedInterlocks3 (Stage 25)",
        "DoorVsAllInterlock / WaferVisionZVsStageLifterInterlock",
        "VacuumVsPickerInterlock / BinLidVsBinVisionInterlock / ServoOffInterlock",
        "",
        "## 10 추가 (Stage 23 — 누적 25)",
        "InterlockRegistry.VerifyMove(axisName, position, out reason)",
        "차단 시 AlarmManager.Raise + false 반환 + 로그",
        "",
        "## Feeder 측 인터락",
        "MoveElevatorWithProtrusionGuardAsync — 10ms ProtrusionSensor 폴링 + 즉시 EStop"
    ) }

    @{ kind="section"; title="§5. 결론"; subtitle="Conclusion" }

    @{ kind="bullets"; title="5.1 설계 완성도"; subtitle="매뉴얼 사양 + 32 Stage 확장"; lines=@(
        "## 매뉴얼 호환",
        "CDT-310 통신 6채널 (5100-5106) — Stage 43 추가",
        "Wafer/Bin Barcode 시리얼 — Stage 43 추가",
        "37 axis + 80+ DI/DO — Simulator IoMap 호환",
        "",
        "## 4-Layer 아키텍처",
        "UI / Controller / Equipment / HAL — 각 layer 독립",
        "AjinFactory.Ready 검사 — 안전 fallback",
        "",
        "## 6 Unit 사이클 통합 (Stage 27~32)",
        "InputLoader → InputStage → TPU → Vision → OutputStage → OutputUnloader",
        "모든 Unit 의 사이클 호출 경로 활성 + 종단 검증",
        "",
        "## 검증",
        "verify_all 117/118 PASS",
        "auto-cycle 50 다이 — Yield 92% / ALARM 0",
        "Lot JSON 자동 저장 16+ 개"
    ) }
)

New-PptxQmc -OutPath "$root\01_CDT320_설계도.pptx" `
    -DocTitle "CDT-320" -DocSubtitle "System Design (Manual-Compatible)" `
    -DocSubject "System Architecture & Design Document" -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" -DocDate "2026. 04. 29" `
    -Slides $design

Write-Output "Document 1 (설계도) — 완전 재작성 완료"
