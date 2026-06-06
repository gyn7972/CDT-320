# CDT-320 Architecture

## 1. 시스템 토폴로지

```
                           ┌──────────────────────────────────┐
                           │          QMC.Vision PC           │
                           │  ┌─────────┐ ┌─────────────────┐ │
                           │  │ Cognex  │ │ Hikvision MVS   │ │
                           │  │ VisPro  │ │ GigE Camera SDK │ │
                           │  │  25.2   │ │                 │ │
                           │  └────┬────┘ └────┬────────────┘ │
                           │       │           │              │
                           │  ┌────▼───────────▼────────────┐ │
                           │  │ IVisionBackend / ICamera    │ │
                           │  └────┬─────────────────────────┘ │
                           │       │                           │
                           │  ┌────▼────────────────────────┐  │
                           │  │  3 VisionModules            │  │
                           │  │  - Wafer       (port 5100)  │  │
                           │  │  - BottomInsp  (port 5101)  │  │
                           │  │  - Bin         (port 5103)  │  │
                           │  └────┬────────────────────────┘  │
                           │       │ TCP line protocol         │
                           └───────┼───────────────────────────┘
                                   │
                           ┌───────▼───────────────────────────┐
                           │      QMC.CDT-320 (Handler)        │
                           │                                   │
                           │  ┌─────────────────────────────┐  │
                           │  │ MachineController            │  │
                           │  │  - InitAsync                 │  │
                           │  │  - StartAsync / StopAsync    │  │
                           │  │  - CycleRunAsync             │  │
                           │  │  - DoOneDieAsync (Pick→...   │  │
                           │  │      Match → Inspect → Place)│  │
                           │  │  - AlignWaferAsync           │  │
                           │  │  - MoveAxisAsync (Interlock) │  │
                           │  └────┬────────────────────────┬─┘  │
                           │       │                        │   │
                           │  ┌────▼─────┐  ┌──────────────▼─┐ │
                           │  │ Job/Lot  │  │ Materials       │ │
                           │  │ Queue    │  │  - Die          │ │
                           │  └──────────┘  │  - DieTapeFrame │ │
                           │                │  - Storage      │ │
                           │  ┌──────────┐  └─────────────────┘ │
                           │  │ Vision   │                      │
                           │  │ TcpClient│  ┌─────────────────┐ │
                           │  └────┬─────┘  │ Interlocks (15) │ │
                           │       │        └─────────────────┘ │
                           │  ┌────▼─────┐  ┌─────────────────┐ │
                           │  │ Bridge   │  │ DieMap+BinCode  │ │
                           │  │ (Sim TCP)│  └─────────────────┘ │
                           │  └────┬─────┘                      │
                           │       │      ┌──────────────┐      │
                           │       │      │ SECS Host    │      │
                           │       │      │  (line+HSMS) │      │
                           │       │      └──────┬───────┘      │
                           └───────┼─────────────┼──────────────┘
                                   │             │ TCP 5000
                          ┌────────▼─────┐    ┌──▼──────────────┐
                          │ CDT320Simul. │    │ Host (SECS/GEM) │
                          │  port 7001   │    │  (외부 시스템)  │
                          │  WPF 3D      │    └─────────────────┘
                          └──────────────┘
```

## 2. 통신 레이어

### 2.1 Vision ↔ Handler (Line Protocol)
```
TX:  MODULE|CMD|arg1|arg2|...\n
RX:  ACK|MODULE|CMD|result\n   또는  ERR|MODULE|CMD|msg\n

비동기 푸시 (Vision → Handler):
     EPD|MODULE\n         (Exposure Done)
     ARM|MODULE|reason\n  (Alarm)
```

명령:
- PING / EXPOSE / GRAB / TRAIN / MATCH / INSPECT
- SCALE (캘리브레이션) / ROT_CENTER / DISTORT / CAM_SWITCH / FOCUS_VAL

### 2.2 Handler ↔ Simulator (JSON)
```json
{"cmd":"AXIS_MOVE","axis":9,"pos":300,"vel":500}
{"cmd":"DO_SET","port":"Y003","val":1}
{"evt":"AXIS_DONE","axis":9,"pos":300}
{"cmd":"HELLO","role":"master","from":"QMC.CDT-320"}
```

### 2.3 Handler ↔ Host (SECS-II)
- **Line mode** (기본): `RC|<command>|<args>\n`
- **HSMS mode** (E37 simplified): 4-byte length prefix + SecsMessage bytes
- 표준 메시지: S1F1, S1F13, S2F41, S5F1, S5F3, S6F11, S7F3, S9F*

## 3. 데이터 흐름 (사이클 1회)

```
1. CycleRunAsync(N)
2. LotStorage.OpenLot(LOT-id, recipe, N)
3. for each die in 0..N:
   4. JobQueue.Enqueue(JobOrder{Type=Pick, DieUid})
   5. JobQueue.MarkRunning
   6. MoveAxisAsync(picker.X, pickPos)  // Interlock 검증
   7. VisionHub.Wafer.MatchAsync("DieFinder")
      ↓ (실패 시 3회 재시도)
   8. die.X/Y/R 갱신 + Picker.VacuumOn
   9. VisionHub.Inspection.InspectAsync("SurfaceInspector")
   10. MoveAxisAsync(picker.X, placePos)
   11. VisionHub.Bin.InspectAsync("PlacementInspector")
   12. Picker.VacuumOff
   13. die.BinCode = BinCodeMap.ConvertToBinCode(die)
   14. ActiveLot.RecordDie(binCode, isGood)
   15. SubPortMaterialRejector.ShouldReject? → 별도 슬롯 좌표
   16. JobQueue.MarkDone or MarkFailed
   17. EventReport → SECS Host (S6F11)
14. LotStorage.CloseLot
15. DieMapSaver.SaveCycleResult / AppendLotSummary
```

## 4. 설정 파일 (`bin/Debug/Config/`)

| 파일 | 용도 |
|---|---|
| `settings.json` | AppSettings (Language, Sim/Vision Host/Port, AjinIRQ, Vision auto-connect) |
| `bin_codes.json` | BinCodeMap (NG → bin, bin → color) |
| `material_specs.json` | DieSpec / TapeFrameSpec |
| `coord_map.json` | 어파인 픽셀↔모터 매핑 매트릭스 |
| `reject_config.json` | SubPortMaterialRejector (threshold + 좌표) |
| `vision.json` (Vision exe 측) | VisionSettings (port, scale, vector, log path) |

## 5. 출력 디렉토리 (`bin/Debug/Log/`)

| 디렉토리 | 내용 |
|---|---|
| `Log/Lots/yyyyMMdd_LOT-*.json` | Lot 통계 (per-cycle close 시) |
| `Log/DieMap/yyyy-MM-dd/*.csv` | Cycle 별 다이맵 결과 |
| `Log/Image/yyyy-MM-dd/<chipUid>/*.png` | 다이별 그랩 이미지 |
| `Log/Data/vision_yyyyMMdd.csv` | DataLogSaver — 30 칼럼 다이별 검사 결과 |
| `Log/Event/...csv` | EventLogger 일자별 로그 |

## 6. 패키지 의존성

### QMC.CDT-320 (Handler)
- System.Drawing, System.Windows.Forms, System.Runtime.Serialization
- QMC.Common (자체)
- (선택) AJINEXTEK AXL.dll (P/Invoke 동적 로드)

### QMC.Vision
- System.Drawing, System.Windows.Forms, System.Windows.Forms.DataVisualization
- (선택) Cognex.VisionPro.dll, PMAlign.dll, Blob.dll, Caliper.dll
- (선택) MvCameraControl.Net.dll (Hikvision MVS)

## 7. 확장 지점

- **새 비전 백엔드** → `IVisionBackend` 구현 + `VisionFactory.Switch`
- **새 카메라** → `ICamera` 구현 + `CameraFactory.CreateById`
- **새 Inspection 알고리즘** → `IInspector` 구현 + 백엔드별 `CreateInspector`
- **새 Interlock** → `MotionInterlock` 상속 + `InterlockRegistry.Register`
- **새 SECS 메시지** → `SecsMessage.S?F?` 헬퍼 추가 + `HandleHsmsMessage` switch
- **새 비전 명령** → `VisionTcpServer.ProcessLine` switch + `VisionModule` 메서드
