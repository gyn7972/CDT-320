# update_docs_overnight.ps1 — Stage 28~32 종합 문서 3종 갱신
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"

# ════════════════════════════════════════════════════════════════════════════
# DOCUMENT 1 — 설계도 (Stage 28~32 통합)
# ════════════════════════════════════════════════════════════════════════════

$design = @(
    @{ kind="title"; subtitle2="System Architecture & Design (Stage 1~32)" }

    @{ kind="section"; title="§1. 시스템 개요"; subtitle="System Overview" }

    @{ kind="bullets"; title="1.1 시스템 구성"; subtitle="3 프로세스 분산 + 6 Unit 통합"; lines=@(
        "## 메인 핸들러 (QMC.CDT-320)",
        "WinForms .NET 4.7.2 / 5단계 사용자 권한 / 6 메인 탭",
        "1920x1080 CDT-300 스타일 UI, 다국어 (ko/en)",
        "",
        "## Vision PC (QMC.Vision)",
        "별도 프로세스, TCP 5100/5101/5103",
        "Cognex VisionPro 25.2 + OpenCV + Sim 백엔드",
        "",
        "## 3D Simulator (CDT320Simulator)",
        "WPF + HelixToolkit, TCP 7001",
        "",
        "## 6 Unit 통합 완료 (Stage 28~31)",
        "InputLoader / InputStage / TransferPicker / VisionInsp / OutputStage / OutputUnloader"
    ) }

    @{ kind="diagram"; title="1.2 6 Unit 사이클 파이프라인"; subtitle="모든 Unit 협조 동작 통합 (Stage 32)"; shapes=@(
        @{ kind="box"; name="IL"; x=300000; y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="① Input`nLoader"; bold=$true; textSize=900 }
        @{ kind="box"; name="IS"; x=1700000; y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="② Input`nStage"; bold=$true; textSize=900 }
        @{ kind="box"; name="TP"; x=3100000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="③ Transfer`nPicker"; bold=$true; textSize=900 }
        @{ kind="box"; name="VI"; x=4500000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="④ Vision`nInspection"; bold=$true; textSize=900 }
        @{ kind="box"; name="OS"; x=5900000; y=1300000; w=1300000; h=900000; fill="595959"; color="FFFFFF"; text="⑤ Output`nStage"; bold=$true; textSize=900 }
        @{ kind="box"; name="OU"; x=7300000; y=1300000; w=1300000; h=900000; fill="0F2D4F"; color="FFFFFF"; text="⑥ Output`nUnloader"; bold=$true; textSize=900 }
        @{ kind="arrow"; name="a1"; x1=1600000; y1=1750000; x2=1700000; y2=1750000 }
        @{ kind="arrow"; name="a2"; x1=3000000; y1=1750000; x2=3100000; y2=1750000 }
        @{ kind="arrow"; name="a3"; x1=4400000; y1=1750000; x2=4500000; y2=1750000 }
        @{ kind="arrow"; name="a4"; x1=5800000; y1=1750000; x2=5900000; y2=1750000 }
        @{ kind="arrow"; name="a5"; x1=7200000; y1=1750000; x2=7300000; y2=1750000 }
        @{ kind="text"; name="cap"; x=300000; y=2350000; w=8400000; h=300000; text="Stage 28~32 — 모든 Unit 사이클 통합 완료. 24 다이 종단 검증 (Good 22 / NG 2)"; size=1100; bold=$true; align="ctr"; color="ED7D31"; italic=$true }
        @{ kind="box"; name="L1"; x=300000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 26+27`nLoadNextWafer`nFeeder"; textSize=850 }
        @{ kind="box"; name="L2"; x=1700000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 28`nLoadAndPrepare`nVisionAlign`nUnloadWafer"; textSize=850 }
        @{ kind="box"; name="L3"; x=3100000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 29`nPickerComponent`nPickup/Place"; textSize=850 }
        @{ kind="box"; name="L4"; x=4500000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 31`nInspectBottom`nInspectSide`n(sim fallback)"; textSize=850 }
        @{ kind="box"; name="L5"; x=5900000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 30`nReceiveDie`nInspectBin"; textSize=850 }
        @{ kind="box"; name="L6"; x=7300000; y=2900000; w=1300000; h=800000; fill="F2F2F2"; color="333333"; text="Stage 27`nStoreFullWafer`n(매 8 다이)"; textSize=850 }
        @{ kind="text"; name="ev"; x=300000; y=3900000; w=8400000; h=300000; text="Stage 32 — 설비 단위: ShutdownAsync + EmergencyStopAsync 추가"; size=1050; align="ctr"; color="C00000"; bold=$true }
    ) }

    @{ kind="section"; title="§2. Unit 별 통합 결과"; subtitle="Stage 28~31 Per-Unit Detail" }

    @{ kind="bullets"; title="2.1 Stage 28 — InputStageUnit"; subtitle="Wafer Handoff + Vision Alignment"; lines=@(
        "## 단위층 메서드",
        "LoadAndPrepareWaferAsync — ExpanderZ Down + 바코드 + 맵 로드",
        "VisionAlignAndSetupOriginAsync — Theta 보정 + Ref 마크 2점 + Origin 확정",
        "UnloadWaferAsync — StageY 언로드 + ExpanderZ Up + Change 신호",
        "",
        "## 신규 어댑터",
        "WaferLoaderAdapter — InputLoader.FeederY 위치 + Cyl 상태 검사",
        "안전 위치 정의: ≤30mm (홈) OR ≥140mm (인계 위치)",
        "",
        "## SoftLimit 확장",
        "StageY 350mm / CameraX 350mm / StageT ±360°",
        "",
        "## 사이클 통합",
        "LoadNextWafer 끝부분: LoadAndPrepare → Retract → VisionAlign",
        "DoOneDieAsync 시작: MoveInputStageToDieAsync",
        "CycleRunAsync 종료: UnloadInputStageWaferAsync"
    ) }

    @{ kind="bullets"; title="2.2 Stage 29 — TransferPickerUnit"; subtitle="PickerComponent 캡슐화 + 4-Picker"; lines=@(
        "## 단위층 메서드",
        "PickerComponent.PickupAsync — PickerZ 하강 + Vacuum ON + 안정화 대기",
        "PickerComponent.PlaceAsync — PickerZ 하강 + Vacuum OFF + Blow + 상승",
        "TpuArmUnit.InspectBottomVisionAsync — 4 picker bottom 비전 (Trigger + GetResults)",
        "TpuArmUnit.InspectSideVisionAsync — 4면 검사 (90° 회전 포함)",
        "",
        "## SoftLimit 확장",
        "ArmX 1600mm (InputStage~OutputStage 전구간) / ArmY 350mm",
        "PickerZ 100mm / PickerT ±360°",
        "",
        "## 사이클 통합",
        "front.Pickers[0].PickupAsync 호출 (이전엔 VacuumOn 만)",
        "front.Pickers[0].PlaceAsync 호출",
        "front.Pickers[0].PickerZ/T ServoOn 추가"
    ) }

    @{ kind="bullets"; title="2.3 Stage 30 — OutputStageUnit"; subtitle="Good/NG 분류 + Bin 안착 검사"; lines=@(
        "## 단위층 메서드",
        "ReceiveDieAsync — 충돌 회피 인터락 + StageZ Up + StageY 이동 + TPU 신호",
        "InspectBinPositionAsync — TPU Place 완료 대기 + BinCameraX 검사 + 후퇴",
        "RequestWaferChangeAsync — Unloader 위임 (Stage 27 Adapter)",
        "PerformColletCleaningAsync — NgStage 더미 영역",
        "",
        "## SoftLimit 확장",
        "StageY 350mm / StageZ 250mm / BinCameraX 350mm",
        "",
        "## 사이클 통합",
        "DoOneDie Place 후: ReceiveDieAsync(Grade=Good/Ng, Vision Offset 적용)",
        "InspectBinPositionAsync — 모든 다이마다 안착 검사"
    ) }

    @{ kind="bullets"; title="2.4 Stage 31 — VisionInspectionUnit"; subtitle="4-Picker Bottom + 4-Side Inspection"; lines=@(
        "## 단위층 (TpuArmUnit 위임)",
        "InspectBottomVisionAsync 호출 (Stage 29 ArmX 이동 → Bottom 트리거)",
        "InspectSideVisionAsync 호출 (4면 + 90° 회전)",
        "",
        "## Sim 모드 처리",
        "VisionHub.Inspection 미연결 시 sim fallback (skip)",
        "실 운영에서는 VisionHub TCP 호출 활성",
        "",
        "## 결과 활용",
        "Bottom Picker1 OffsetX/Y → OutputStage.ReceiveDieRequest.VisionOffsetX/Y",
        "Side Picker1 IsAllOk → inspPass 반영"
    ) }

    @{ kind="section"; title="§3. Stage 32 — 설비 단위 라이프사이클"; subtitle="Machine-Level Lifecycle" }

    @{ kind="diagram"; title="3.1 설비 라이프사이클"; subtitle="Init → Run → Shutdown / E-Stop"; shapes=@(
        @{ kind="box"; name="init"; x=300000; y=1300000; w=1900000; h=700000; fill="5B9BD5"; color="FFFFFF"; text="① InitAsync`nServoOn + Home`n+ AutoScan I/O 카세트"; bold=$true; textSize=950 }
        @{ kind="box"; name="ready"; x=2400000; y=1300000; w=1500000; h=700000; fill="70AD47"; color="FFFFFF"; text="② Ready`n운전 대기"; bold=$true; textSize=1000 }
        @{ kind="box"; name="run"; x=4100000; y=1300000; w=2000000; h=700000; fill="ED7D31"; color="FFFFFF"; text="③ CycleRunAsync(N)`nLoadNextWafer →`nN 다이 → Store"; bold=$true; textSize=950 }
        @{ kind="box"; name="end"; x=6300000; y=1300000; w=1500000; h=700000; fill="595959"; color="FFFFFF"; text="④ Cycle 종료`nUnloadWafer + CloseLot"; bold=$true; textSize=950 }
        @{ kind="arrow"; name="i2r"; x1=2200000; y1=1650000; x2=2400000; y2=1650000 }
        @{ kind="arrow"; name="r2c"; x1=3900000; y1=1650000; x2=4100000; y2=1650000 }
        @{ kind="arrow"; name="c2e"; x1=6100000; y1=1650000; x2=6300000; y2=1650000 }
        @{ kind="box"; name="sd"; x=300000; y=2400000; w=3500000; h=700000; fill="2D2D30"; color="FFFFFF"; text="ShutdownAsync — 정상 종료"; bold=$true; textSize=1100 }
        @{ kind="box"; name="es"; x=4100000; y=2400000; w=3700000; h=700000; fill="C00000"; color="FFFFFF"; text="EmergencyStopAsync — 비상 정지"; bold=$true; textSize=1100 }
        @{ kind="text"; name="sd-d"; x=300000; y=3200000; w=3500000; h=300000; text="모든 축 Stop + Lot Close + Settings.Save"; size=900; align="ctr"; color="595959" }
        @{ kind="text"; name="es-d"; x=4100000; y=3200000; w=3700000; h=300000; text="모든 축 EStop + Alarm Critical + Servo 유지"; size=900; align="ctr"; color="595959" }
        @{ kind="text"; name="ver"; x=300000; y=3900000; w=8400000; h=400000; text="런타임 검증 — auto-cycle 24 다이: 3 wafer 처리, Good 22 / NG 2, Lot JSON 저장 OK"; size=1100; bold=$true; align="ctr"; color="375623" }
    ) }

    @{ kind="section"; title="§4. 결론"; subtitle="Conclusion (Stage 32 종합)" }

    @{ kind="bullets"; title="4.1 종합 완료"; subtitle="6 Unit + 설비 단위 모두 통합"; lines=@(
        "## 통합 완료",
        "Stage 28: InputStage Vision Align 통합",
        "Stage 29: TPU PickerComponent 캡슐화",
        "Stage 30: OutputStage Receive + Bin",
        "Stage 31: 4-picker Bottom + Side Vision",
        "Stage 32: 설비 라이프사이클 (Shutdown/E-Stop)",
        "",
        "## 검증",
        "verify_all 117/118 PASS — 회귀 무결성",
        "auto-cycle 24 다이 종단 검증 — Good 22 / NG 2",
        "Lot JSON 자동 저장 — BinDistribution 정상",
        "",
        "## 산출물",
        "신규: WaferLoaderAdapter (Equipment/Sim/)",
        "신규: ShutdownAsync / EmergencyStopAsync (MachineController)",
        "수정: InputStage/TPU/OutputStage/OutputUnloader 모두 SoftLimit 확장",
        "문서: 설계도 / 계획서 / 체크리스트 PPT 3종 갱신"
    ) }
)

New-PptxQmc -OutPath "$root\01_CDT320_설계도.pptx" `
    -DocTitle "CDT-320" -DocSubtitle "System Design (Stage 1~32)" `
    -DocSubject "System Architecture" -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" -DocDate "2026. 04. 28 (overnight)" `
    -Slides $design

Write-Output "Document 1 (설계도) — 갱신 완료"
