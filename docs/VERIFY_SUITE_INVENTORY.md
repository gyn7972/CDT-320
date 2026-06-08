# verify 스위트 재정립 — 정찰 인벤토리 (읽기 전용)

> 생성: 2026-06-08. **읽기 전용 정찰** — 원본 스크립트·코드 미수정. 비침습 repoint 사본(`tmp_verify_recon`, 측정 후 삭제)으로 1회 측정.
> 후속: `PROMPT_VerifyAll_Repoint_Rebaseline.md` 가 본 결과로 repoint·트리아지·베이스라인 확정.

## 0. 요약 (TL;DR)
- 스크립트 **48개** = verify_all(오케스트레이터) 1 + 테마별 4(comm/cognex_runtime/handler_features/vision_features) + verify_stageN 43개(stage 2–32, 43–54; **stage1·33–42 없음**). 위치 `D:/Work/source/tools/`. **중첩 사본 없음**(`QMC.CDT-320/tools/` 부재).
- 경로 참조: **전 스크립트가 `$ROOT` 한 줄 하드코딩** = `D:/Work/CDT-320/QMC.CDT-320`(은퇴 백업 repo). 예외: `verify_comm.pl` 만 `$ROOT="D:/Work/CDT-320"`(상위). `verify_all.pl` 은 서브스크립트 경로를 `D:/Work/CDT-320/QMC.CDT-320/tools/...` 로 직접 하드코딩 → **현행 tools/ 사본이 아닌 백업본을 실행**(메모리의 "false signal" 근원). 그 외 추가 하드코딩 절대경로 없음(C:/ 등 0).
- **빌드 호출 없음**: 스크립트는 컴파일하지 않고 `bin/Debug/*.exe` **존재 여부**(=빌드 성공 proxy) + 소스 `grep` 패턴 + `count`/디렉토리 체크만 함. 런타임 실행은 `verify_comm`(QMC.Vision.exe spawn + TCP), `verify_cognex_runtime`(프로세스 체크) 둘 뿐 — verify_all 기본 세트에서 제외됨.
- **현 flat 트리에선 단일 repoint `$ROOT→D:/Work/source` 로 충분**: 구 레이아웃은 Vision이 Handler proj 아래 nested 였고 스크립트가 `$ROOT/QMC.Vision`(Vision계열)·`$ROOT/QMC.CDT-320`(Handler계열) 두 관례를 혼용했으나, flat 트리에선 `QMC.Vision`·`QMC.CDT-320`·`tools` 가 모두 `D:/Work/source` 직하 형제라 단일 치환이 세 관례를 동시 충족. (상대경로화도 가능 — tools/ 가 repo 루트 직하이므로 `FindBin` 기반 `$ROOT=$Bin/..`.)

## 1. repoint 시뮬 결과 (비침습 1회)
사본을 `$ROOT→D:/Work/source` 치환 후 verify_all 세트(handler_features + stage2–32 + stage43–54 + vision_features) 실행:

```
TOTAL 198 / PASS 117 / FAIL 80
```
(백업 기준 199/184/14 대비 FAIL 급증 — 주원인은 ① Handler exe 미빌드 ② origin 머지로 Handler 아키텍처 교체)

### FAIL 80건 분류
| 범주 | 건수 | 성격 | 분류 |
|---|---|---|---|
| **A** BUILD `QMC.CDT-320.exe` 미존재 | **38** | 본 세션서 Handler 미빌드(스크립트는 exe 존재=빌드성공 proxy). Handler 빌드 시 통과 | 환경(회귀 아님) |
| **B** 대상 파일 **부재** | **28** | 구 310-포팅 Handler 스캐폴딩(Interlocks 1–3·Alarm·per-Unit Equipment·SubsetPageBase 등) 이 origin 머지로 교체·삭제 | **(ii) 코드진화** |
| **C** 파일 존재 + **패턴** FAIL | **13** | 파일은 있으나 grep 심볼/문구 불일치 (stale vs 회귀) | (ii) stale / (iii) 후보 |
| **D** `count=0`/디렉토리 | **2** | 런타임 산출물(Lot JSON 등) — 사이클 미실행 | 런타임 데이터(회귀 아님) |

### ★ (iii) 실제 회귀 의심: **0건** (C 13건 전수 (ii) 확정)
- **Vision (내 디자이너 스윕 영역) 4건 — 전부 기능 정상, 패턴만 stale:**
  - `stage2 ParameterEditorBase+Host`: `abstract class` 패턴 0 — 실제는 `public abstract **partial** class ParameterEditorBase`(Phase2 partial 삽입). abstract 메서드 BuildEditor/Load/Save 전부 존재.
  - `stage2 ZoomDialog`: `MouseWheel` 0 — 이벤트 배선이 **ZoomDialog.Designer.cs 로 이동**(`_canvas.MouseWheel += OnWheel`), 줌로직(`_zoom*delta`)·DoubleClick .cs 잔존.
  - `stage2 ConfigurationPage`: 라벨 문구 `Cognex VisionPro diagnostics` 만 0 — ProbeCognex·CognexBackend·CognexLoaded 전부 존재(기능 무손상).
  - `stage52 Form1`: 모듈 **명칭 변경** — `TopSide/BottomSideInspectionModule` → 실제 `Bottom/Front/RearSideInspectionModule`(현 Modules 디렉토리 확인).
- **Handler 9건 — 내가 미수정, 팀 권위 코드와 구 AI 심볼명 불일치:**
  - `MachineController.cs`(팀 코드 **4761줄**)에 구 패턴 `MaxRetries`/`while(attempt<MaxRetries)`(PickRetry)·`JobQueue.Enqueue`·`BinCodeMap.ConvertToBinCode` 부재(반면 `DoOneDieAsync`·`MoveAxisAsync` 는 존재). handler_features/stage29/30/31.
  - `CDT320Machine.cs`(stage48 PostPnp), `DieMapPage.cs`(handler_features), `RemoteViewerDialog.cs`(stage4) — 동일하게 구 스캐폴딩 심볼 기준.
- 결론: verify 스위트는 **백업 repo의 구 AI Handler 스캐폴딩** 기준으로 작성 → 현 flat 트리의 **팀 권위 Handler(origin 머지본)** 와 파일/심볼 체계가 달라 대량 (ii). 내 최근 작업(Vision 디자이너 스윕 + resx)이 깬 것은 **없음**.

## 2. 스크립트 인벤토리 (48)
대상: **V**=Vision(현 트리 정상, 패턴 stale → 수정), **H**=Handler(구 스캐폴딩 기준 → 팀 코드와 불일치, 폐기/재작성), **RT**=런타임/자동화 존재체크, **RUN**=exe spawn(정적 세트 제외), **ORCH**=오케스트레이터.

| 스크립트 | 줄 | 대상 | 검사 요약 | 적용성 판정 |
|---|---|---|---|---|
| verify_all.pl | 58 | ORCH | 서브스크립트 순회·TOTAL/PASS/FAIL 합산 | 서브경로 하드코딩(백업) → **repoint 필수** |
| verify_handler_features.pl | 260 | H | Handler 빌드+Interlock/MachineController/DieMap/Subset 통합 grep | (ii) 대량 부재/불일치 → 재작성/폐기 |
| verify_vision_features.pl | 181 | V | Vision 빌드+CameraVector/VisionScale 등 310이식 grep | repoint 후 검토(요약형식 상이로 0/0/0 파싱) → 수정 |
| verify_comm.pl | 256 | RUN | Vision.exe spawn + TCP 5100/5101/5103 라운드트립 | 런타임 — 정적 세트 제외, 별도 |
| verify_cognex_runtime.pl | 99 | RUN | QMC.Vision 프로세스 가동 체크 | 런타임 — 별도 |
| verify_stage2.pl | 111 | V | SpcChart/ParameterEditor/ZoomDialog/Cognex진단/csproj | (ii) stale 패턴 → **수정**(partial·Designer배선·문구) |
| verify_stage3.pl | 113 | H | Extended5 Interlock(Eject/Loader/…) | (ii) ExtendedInterlocks.cs 부재 → 폐기 |
| verify_stage4.pl | 107 | V+H | Vision 빌드/ConfigurationPage + RemoteViewerDialog(H) | 혼합 → Vision부 수정, Handler부 분리/폐기 |
| verify_stage5.pl | 56 | H | Lot 로그 디렉토리(런타임) | (D) 런타임 — 사이클 후만 유효 |
| verify_stage6.pl | 44 | V | IEdgeFinder/CognexCaliper/Histogram/ColorMatch | **(i) repoint-ok**(5/5 PASS) |
| verify_stage7.pl | 39 | H | Handler 빌드 + 기능 grep | (ii) 폐기/재작성 |
| verify_stage8.pl | 25 | H | Extended2 Interlock 5종 | (ii) 파일 부재 → 폐기 |
| verify_stage9.pl | 29 | H | Handler 빌드 + grep | (ii) 폐기/재작성 |
| verify_stage10.pl | 33 | H | Handler 빌드 + audit_memory/threading.pl 존재 | (ii)/(env) |
| verify_stage11.pl | 32 | H | Lang.cs(로컬라이제이션) | (ii) 경로/심볼 검토 |
| verify_stage12.pl | 39 | H | AlarmMaster Category/Definition/16종 | (ii) AlarmMaster.cs 부재 → 폐기 |
| verify_stage13.pl | 32 | H | MachineController grep | (ii) 불일치 → 재작성 |
| verify_stage14.pl | 27 | RT | verify_cognex_runtime.pl 존재 | (i) repoint-ok(2/2) |
| verify_stage15.pl | 51 | RT | gui_cycle_automation.ps1 존재 | (i) repoint-ok |
| verify_stage16.pl | 34 | H | SecsItem/SecsMessage | (ii) 경로 검토 |
| verify_stage17.pl | 23 | RT | remote_viewer_client.ps1 존재 | (i) repoint-ok(1/1) |
| verify_stage18.pl | 27 | RT | runtime_cycle_test.pl/gui automation 존재 | (i) repoint-ok(2/2) |
| verify_stage19.pl | 37 | H | AlarmMasterPage/SettingsTab/AlarmManager/Lang | (ii) 부재·불일치 → 폐기 |
| verify_stage20.pl | 29 | H | AlarmHistoryPage/HistoryTab | (ii) 검토 |
| verify_stage21.pl | 31 | H | RecipeStore | (ii) 검토 |
| verify_stage22.pl | 27 | H | DieMapGenerator/DieMapPage | (ii) 일부 존재·일부 부재 |
| verify_stage23.pl | 37 | H | AlarmDefinition 다국어 | (ii) AlarmMaster 부재 → 폐기 |
| verify_stage24.pl | 57 | H | Program/Form1/Lot JSON(런타임) | (ii)+(D) |
| verify_stage25.pl | 26 | H | Extended3 Interlock | (ii) 부재 → 폐기 |
| verify_stage26.pl | 34 | H | MachineController/InputCassettePage/SimCassette | (ii) 불일치 |
| verify_stage27.pl | 39 | H | OutputPages/InputFeeder | (ii) 부재·불일치 |
| verify_stage28.pl | 43 | H | WaferLoaderAdapter/CDT320Machine/InputStageUnit | (ii) 부재 |
| verify_stage29.pl | 34 | H | MachineController Pick/Place·TransferPickerUnit | (ii) 불일치/부재 |
| verify_stage30.pl | 34 | H | MachineController ReceiveDie·OutputStageUnit | (ii) 불일치/부재 |
| verify_stage31.pl | 35 | H | MachineController InspectVision·TpuArm | (ii) 불일치/부재 |
| verify_stage32.pl | 27 | H | Handler grep | (ii) 검토 |
| verify_stage43.pl | 36 | H | Handler 축/유닛 grep | (ii) 폐기/재작성 |
| verify_stage44.pl | 34 | H | InputStageUnit EjectPinZ·TpuArm SideVisionY 축 | (ii) 부재 |
| verify_stage45.pl | 35 | H | OperationPanelUnit(Tower/Buzzer) | (ii) 파일 부재 → 폐기 |
| verify_stage46.pl | 33 | H | ResourceSensorsUnit(CDA/Vacuum) | (ii) 파일 부재 → 폐기 |
| verify_stage47.pl | 28 | H | IonizerUnit | (ii) 파일 부재 → 폐기 |
| verify_stage48.pl | 28 | H | PostPnpTransferUnit/CDT320Machine 통합 | (ii) 부재/불일치 |
| verify_stage49.pl | 29 | H | Handler grep | (ii) 검토 |
| verify_stage50.pl | 32 | H | Handler grep | (ii) 검토 |
| verify_stage51.pl | 29 | H | Handler grep | (ii) 검토 |
| verify_stage52.pl | 33 | V | TopSide/BottomSideInspectionModule + Form1 배선 | (ii) **모듈 명칭변경** → 수정(Bottom/Front/Rear) |
| verify_stage53.pl | 27 | H | Handler grep | (ii) 검토 |
| verify_stage54.pl | 34 | H | Handler grep | (ii) 검토 |

## 3. 단계 분할 권고
1. **repoint(기계적·전수)**: 48개 전부 `$ROOT` 한 줄 치환 — `D:/Work/CDT-320/QMC.CDT-320`→`D:/Work/source`(verify_comm 은 `D:/Work/CDT-320`→`D:/Work/source`), verify_all.pl 서브스크립트/vision 경로 2줄을 `D:/Work/source/tools/` 로. **상대경로화 권장**(`use FindBin; my $ROOT="$FindBin::Bin/.."`) → 트리 이동에 불변. 위험 낮음.
2. **트리아지**:
   - **수정(패턴 갱신) ~5**: stage2, stage4(Vision부), stage6(이미 통과), stage52, vision_features — 현 트리 대상 정상, 디자이너 스윕/명칭변경 반영해 grep 패턴만 갱신.
   - **폐기 또는 전면 재작성 ~36**: handler_features + Handler stage(3,5,7,8,9,10,11,12,13,16,19–32,43–51,53,54). 구 AI 310-포팅 스캐폴딩 기준이라 팀 권위 Handler(origin 머지본)와 파일/심볼 불일치. → **폐기**(권장; 팀 Handler는 범위 밖) 또는 팀 아키텍처 기준 재작성(대공수).
   - **별도(런타임)**: verify_comm, verify_cognex_runtime — exe spawn, 정적 베이스라인서 분리.
   - **유지(자동화 존재체크) ~4**: stage14,15,17,18 — repoint 후 통과.
3. **신규 베이스라인 예상**: 정적 세트를 "Vision + 자동화 존재체크"로 축소 시 (수정 후) 대략 **PASS ~30 / FAIL 0**(소규모·전녹색). Handler 빌드까지 포함하고 Handler 스크립트를 재작성하지 않는 한 구 Handler 항목은 영구 (ii) → **폐기가 정합**.
4. **(iii) 실제 회귀: 없음** — 정상화(repoint/재작성)보다 우선 보고할 회귀 신호 없음. Vision(내 작업 영역) 무손상 확인됨.
