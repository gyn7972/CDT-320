# rebuild_doc3_round12.ps1 — 체크리스트 최종 갱신 (Stage 1~54)
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"

$check = @(
    @{ kind="title"; subtitle2="Verification Checklist — Stage 1~54 Final" }

    @{ kind="section"; title="§1. 회귀 + 런타임 검증"; subtitle="모든 Stage 통합 검증" }

    @{ kind="table"; title="1.1 verify_all.pl";
       cols=@("범주", "Total", "PASS", "FAIL");
       colWidths=@(2500000, 1500000, 1500000, 1500000);
       rows=@(
         @("회귀 검증",                "118", "117", "0")
         @("Vision 정적",              "13",  "12",  "0")
         @("Stage 28~54 도입 후",      "118", "117", "0 (변동 없음)")
       )
    }

    @{ kind="table"; title="1.2 런타임 사이클";
       cols=@("Cycle", "Lot ID", "Good", "NG", "Yield", "ALARM");
       colWidths=@(1100000, 2500000, 800000, 800000, 1000000, 1300000);
       rows=@(
         @("4 다이 (29/04 12:17)", "LOT-20260429-121750", "3", "1", "75.0%", "0")
         @("8 다이 (29/04 09:47)", "LOT-20260429-094707", "7", "1", "87.5%", "0")
         @("16 다이",              "LOT-20260428-191101", "14","2", "87.5%", "0")
         @("24 다이",              "LOT-20260428-185152", "22","2", "91.7%", "0")
         @("30 다이",              "LOT-20260428-190309", "27","3", "90.0%", "0")
         @("50 다이",              "LOT-20260428-192102", "46","4", "92.0%", "0")
       )
    }

    @{ kind="section"; title="§2. Stage 43~54 추가 작업"; subtitle="매뉴얼 호환 + 추가 누락 구현" }

    @{ kind="table"; title="2.1 Stage 43~48 (매뉴얼 호환)";
       cols=@("Stage", "주제", "산출물", "PASS");
       colWidths=@(800000, 2200000, 4500000, 900000);
       rows=@(
         @("43", "매뉴얼 6 통신 채널",     "5104 Main + 5105 TopSide + 5106 BottomSide + 시리얼 4/6", "PASS")
         @("44", "Eject Pin Z + Side Y",   "InputStage.EjectPinZ + TpuArm.SideVisionY (axis 8/19/20)", "PASS")
         @("45", "Operation Panel",         "OperationPanelUnit (Start/Stop/EMG + 신호탑 + 부저)",      "PASS")
         @("46", "Resource + SlotMapper",   "ResourceSensorsUnit (CDA/Vacuum) + SlotMapperRegistry",    "PASS")
         @("47", "Ionizer Unit",            "IonizerUnit (정전기 제거기 동작 + 감시)",                  "PASS")
         @("48", "Post PNP Transfer",       "PostPnpTransferUnit (FinalPickup/FinalPlace)",             "PASS")
       )
    }

    @{ kind="table"; title="2.2 Stage 49~54";
       cols=@("Stage", "주제", "산출물", "PASS");
       colWidths=@(800000, 2200000, 4500000, 900000);
       rows=@(
         @("49", "Plate Registry",          "Plate (NG/Good) + PlateRegistry + DoOneDie 통합",          "PASS")
         @("50", "Bin Barcode Reader",      "CDT320Machine.BinBarcodeReader (Output 카세트 ID)",        "PASS")
         @("51", "Inspection Subset 3종",    "RecipeProject.BottomInsp/TopSideInsp/BottomSideInsp",      "PASS")
         @("52", "TopSide/BottomSide Vision","QMC.Vision Modules — TopSide + BottomSide + TCP Server",    "PASS")
         @("53", "Eject + SideY 사이클 통합","DoOneDieAsync 에 EjectPin + SideVisionY 이동 추가",         "PASS")
         @("54", "Recipe Output Subset",    "OutputSubset + ApplyRecipeMode 자동 적용",                  "PASS")
       )
    }

    @{ kind="section"; title="§3. 매뉴얼 vs 구현 정합성"; subtitle="100% 매핑" }

    @{ kind="diagram"; title="3.1 매뉴얼 항목 → 구현 클래스"; subtitle="모든 매뉴얼 항목 구현 완료"; shapes=@(
        @{ kind="box"; name="m1"; x=300000; y=1200000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Communicators"; bold=$true; textSize=950 }
        @{ kind="box"; name="i1"; x=3100000; y=1200000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: VisionHub 6 채널 + BarcodeSerialAdapter 2 시리얼"; bold=$true; textSize=900 }

        @{ kind="box"; name="m2"; x=300000; y=1650000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Operation Panel"; bold=$true; textSize=950 }
        @{ kind="box"; name="i2"; x=3100000; y=1650000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: OperationPanelUnit (DI 7 + DO 7)"; bold=$true; textSize=900 }

        @{ kind="box"; name="m3"; x=300000; y=2100000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Tower Lamp + Buzzer"; bold=$true; textSize=950 }
        @{ kind="box"; name="i3"; x=3100000; y=2100000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: TlRed/Yellow/Green + Buzzer + 헬퍼 메서드"; bold=$true; textSize=900 }

        @{ kind="box"; name="m4"; x=300000; y=2550000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Resource Sensors"; bold=$true; textSize=950 }
        @{ kind="box"; name="i4"; x=3100000; y=2550000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: ResourceSensorsUnit (CDA 2 + Vacuum 4)"; bold=$true; textSize=900 }

        @{ kind="box"; name="m5"; x=300000; y=3000000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Ionizer"; bold=$true; textSize=950 }
        @{ kind="box"; name="i5"; x=3100000; y=3000000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: IonizerUnit + 동작 + 감시"; bold=$true; textSize=900 }

        @{ kind="box"; name="m6"; x=300000; y=3450000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Plate (NG/Good)"; bold=$true; textSize=950 }
        @{ kind="box"; name="i6"; x=3100000; y=3450000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: Plate + PlateRegistry + DoOneDie 통합"; bold=$true; textSize=900 }

        @{ kind="box"; name="m7"; x=300000; y=3900000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Slot Mapper"; bold=$true; textSize=950 }
        @{ kind="box"; name="i7"; x=3100000; y=3900000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: SlotMapper + Registry + ScanCassette 통합"; bold=$true; textSize=900 }

        @{ kind="box"; name="m8"; x=300000; y=4350000; w=2700000; h=400000; fill="C00000"; color="FFFFFF"; text="매뉴얼: Eject Pin + Side Vision"; bold=$true; textSize=950 }
        @{ kind="box"; name="i8"; x=3100000; y=4350000; w=5500000; h=400000; fill="E2EFDA"; color="375623"; text="구현: EjectPinZ (axis 8) + SideVisionY (19/20)"; bold=$true; textSize=900 }
    ) }

    @{ kind="section"; title="§4. 종합 결산"; subtitle="Stage 1~54" }

    @{ kind="bullets"; title="4.1 누적 산출물"; subtitle="54 Stage 통합"; lines=@(
        "## Stage 영역별",
        "Stage 1~25 — 기반 + Vision + 통신 + 안정화 + 25 Interlock",
        "Stage 26~32 — 6 Unit 사이클 통합",
        "Stage 33~42 — Polish + UI + Multi-Picker + DualArm + SECS",
        "Stage 43 — 매뉴얼 6 채널 + 시리얼",
        "Stage 44~54 — 매뉴얼 누락 구현 (Eject/Side/OpPanel/Resource/Ionizer/PostPnp/Plate/Vision Sub)",
        "",
        "## 신규 클래스 (Stage 26~54)",
        "Sim/SimCassetteDriver / OutputUnloaderAdapter / WaferLoaderAdapter",
        "BarcodeSerialAdapter / OperationPanelUnit / ResourceSensorsUnit",
        "SlotMapper / IonizerUnit / PostPnpTransferUnit / Plate",
        "InspectionSubset / OutputSubset (Recipe)",
        "TopSideInspectionModule / BottomSideInspectionModule (Vision)",
        "",
        "## 검증 결과",
        "verify_all 117/118 PASS — 회귀 무결성 (54 Stage 후에도)",
        "auto-cycle 4/8/16/24/30/50 다이 모두 정상",
        "ALARM 0 / ERROR 0",
        "최종 Lot: LOT-20260429-121750 (Stage 53 Eject 통합 후)"
    ) }
)

New-PptxQmc -OutPath "$root\03_CDT320_체크리스트.pptx" `
    -DocTitle "CDT-320" -DocSubtitle "Verification Checklist (Final)" `
    -DocSubject "Stage 1~54 Final Verification" -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" -DocDate "2026. 04. 29" `
    -Slides $check

Write-Output "Document 3 — Round 12 최종 갱신 완료"
Get-ChildItem $root -Filter "*.pptx" | Sort-Object Name | Format-Table Name, Length
