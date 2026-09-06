using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Tabs;
using QMC.CDT_320.Ui.Util;

namespace QMC.CDT_320
{
    public partial class Form1
    {
        private sealed class InputStageRunReviewOneShotState
        {
            public readonly InputStageRunReviewDialog Dialog;
            public readonly int SessionGeneration;
            public readonly System.Threading.Tasks.TaskCompletionSource<bool> Completion =
                new System.Threading.Tasks.TaskCompletionSource<bool>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);

            public InputStageRunReviewOneShotState(InputStageRunReviewDialog dialog, int sessionGeneration)
            {
                Dialog = dialog;
                SessionGeneration = sessionGeneration;
            }
        }

        private InputStageRunReviewOneShotState _inputStageRunReviewOneShot;
        private readonly HashSet<InputStageRunReviewDialog> _inputStageRunReviewDrainingDialogs =
            new HashSet<InputStageRunReviewDialog>();

        private void OnInputStageUserConfirmRequested()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnInputStageUserConfirmRequested));
                return;
            }

            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            if (stage == null)
                return;

            if (_inputStageRunReviewDialog != null && !_inputStageRunReviewDialog.IsDisposed)
            {
                _inputStageRunReviewDialog.Activate();
                _inputStageRunReviewDialog.BringToFront();
                return;
            }

            InputStageRunReviewDialog dialog = null;
            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                QMC.CDT320.DieMaps.DieMap stageMap = MaterialStateService.BuildInputDieMapFromStageWafer();
                if (wafer == null || stageMap == null || stageMap.Entries == null || stageMap.Entries.Count == 0)
                    throw new InvalidOperationException("InputStage 사용자 확인 화면에 표시할 Wafer/Die Map 데이터가 없습니다.");

                bool alignComplete = wafer.HasInputStageAlignResult && wafer.HasInputStageThetaAlignResult;
                bool mappingComplete = wafer.HasInputStageDieMappingResult &&
                                       !wafer.InputStageDieMappingInvalidatedByAlignChange;
                QMC.CDT320.Recipes.RecipeProject project = _currentRecipe ??
                    QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                QMC.CDT320.Recipes.PickupSubset pickup = project != null
                    ? (project.InputPickup ?? project.Pickup ?? new QMC.CDT320.Recipes.PickupSubset())
                    : new QMC.CDT320.Recipes.PickupSubset();
                string recipeName = project != null && !string.IsNullOrWhiteSpace(project.FileName)
                    ? project.FileName
                    : ActiveRecipeName;
                string mappingReference = !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId)
                    ? wafer.DieMapFrameObjId
                    : stageMap.FrameObjId;

                dialog = new InputStageRunReviewDialog();
                _inputStageRunReviewDialog = dialog;
                int sessionGeneration = ++_inputStageRunReviewSessionGeneration;
                ClearInputStageRunReviewPendingOffset();
                dialog.SetMode(InputStageRunReviewMode.MappingReview);
                dialog.SetPickupOptions(pickup);
                dialog.SetDieMap(stageMap);
                dialog.SetWorkflowState(
                    wafer.WaferId,
                    recipeName,
                    QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                    QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected,
                    alignComplete,
                    mappingReference,
                    mappingComplete,
                    "WAITING USER CONFIRM");
                if (wafer.InputStageAlignManualFallback && !wafer.InputStageAlignManualFallbackThetaDone)
                    dialog.SetManualAlignFallbackThetaRequired(true);
                dialog.SetAxisPositions(
                    stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0,
                    stage.StageY != null ? stage.StageY.ActualPosition : 0.0,
                    stage.StageT != null ? stage.StageT.ActualPosition : 0.0);
                dialog.SetAxisPositionProvider(() => new double[]
                {
                    ReadCachedAxisPosition(stage.CameraX),
                    ReadCachedAxisPosition(stage.StageY),
                    ReadCachedAxisPosition(stage.StageT)
                });
                dialog.SetFailureDetail(
                    string.Empty,
                    "확인: 현재 Align/Die Mapping 결과로 Auto PickUp 공정을 계속합니다." + Environment.NewLine +
                    "취소: Picker Ready를 발행하지 않고 센터 검출/T Align부터 다시 수행한 뒤 Die Mapping과 확인을 반복합니다.");
                dialog.SetReviewValid(alignComplete && mappingComplete, "USER CONFIRM REQUIRED");
                dialog.SetNonproductionReviewMode(MaterialStateService.IsInputStageReviewNonProductionMode(
                    out string nonProductionReviewReason));
                dialog.SetAutoReviewMode(true);

                // [사용자 확정 2026-08-17] 죽은 세션 컨펌 차단 — Auto가 CycleStop 등으로 이미
                // 종료된 뒤 눌린 결정은 받아줄 시퀀스가 없어 무음으로 사라졌다(15:4x 실사례:
                // 다이얼로그만 닫히고 다이 촬영이 시작되지 않음). 모든 결정 경로에서 세션을
                // 확인하고, 죽었으면 이유를 화면과 디스크 로그에 남긴다.
                Func<string, bool> ensureReviewSessionAlive = delegate(string decisionName)
                {
                    if (Controller != null && Controller.IsInputStageRunReviewManualActive)
                        return true;

                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        "UI",
                        "IN-REVIEW-DECISION-DEAD-SESSION",
                        "InputStageRunReview",
                        "Auto 시퀀스가 정지된 상태에서 Review 결정이 눌려 처리하지 않았습니다. decision=" + decisionName);
                    dialog.RestoreAfterDecisionFailure(
                        "Auto 시퀀스가 정지되어 이 확인을 처리할 수 없습니다. 이 창을 닫고 START로 다시 시작하세요.");
                    return false;
                };

                dialog.StartRunRequested += delegate
                {
                    if (!ensureReviewSessionAlive("ConfirmAndContinue"))
                        return;
                    if (Controller.IsInputStageRunReviewActionBusy)
                    {
                        dialog.RestoreAfterDecisionFailure("수동 동작 또는 Jog가 진행 중입니다. STOP 후 다시 확인하세요.");
                        return;
                    }
                    if (!CanSubmitInputStageReviewGeometry(dialog, out string geometryReason))
                    {
                        dialog.RestoreAfterDecisionFailure(geometryReason);
                        return;
                    }
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.ConfirmAndContinue));
                };
                dialog.AbortAutoRequested += delegate
                {
                    if (!ensureReviewSessionAlive("AbortAuto(RetryAlign)"))
                        return;
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryAlign));
                };
                dialog.AlignRetryRequested += delegate
                {
                    if (!ensureReviewSessionAlive("RetryAlign"))
                        return;
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryAlign));
                };
                dialog.MappingRetryRequested += delegate
                {
                    if (!ensureReviewSessionAlive("RetryMapping"))
                        return;
                    stage.ConfirmFromUi(BuildInputStageRunReviewResult(
                        dialog,
                        InputStageRunReviewDecision.RetryMapping));
                };
                dialog.SelectedDieMoveRequested += delegate
                {
                    RunInputStageReviewMoveSelectedDieAsync(dialog);
                };
                dialog.JogRequested += delegate(object sender, InputStageReviewJogEventArgs args)
                {
                    StartInputStageRunReviewJogAsync(dialog, args);
                };
                dialog.JogStopRequested += delegate
                {
                    StopInputStageRunReviewJogAsync(dialog, "Review Jog STOP");
                };
                dialog.ReviewActionStopRequested += delegate
                {
                    StopInputStageRunReviewActionAsync(dialog, "Review 수동 동작 STOP");
                };
                dialog.ThetaCorrectionRequested += delegate
                {
                    RunInputStageReviewThetaCorrectionAsync(dialog);
                };
                dialog.DieDetectionRequested += delegate
                {
                    RunInputStageReviewDieDetectionAsync(dialog);
                };
                dialog.OffsetApplyRequested += delegate
                {
                    ApplyInputStageRunReviewPendingOffset(dialog);
                };
                dialog.GeometryVerificationRequested += async delegate
                {
                    await VerifyInputStageRunReviewGeometryAsync(dialog).ConfigureAwait(true);
                };
                dialog.SelectedDieChanged += delegate
                {
                    ClearInputStageRunReviewPendingOffset();
                };
                dialog.VisionTestRequested += delegate
                {
                    OpenInputStageRunReviewVisionTest(dialog);
                };
                dialog.WaferVisionControlStartRequested += delegate
                {
                    StartInputStageRunReviewEmbeddedVisionAsync(dialog);
                };
                dialog.WaferVisionControlStopRequested += delegate
                {
                    StopInputStageRunReviewEmbeddedVision(
                        dialog,
                        true,
                        "Wafer Vision Live/Grab 사용을 종료했습니다.");
                };
                dialog.BuzzerStopRequested += delegate
                {
                    StopRunReviewBuzzer();
                };

                InputStageRunReviewDialog sessionDialog = dialog;
                dialog.FormClosed += delegate
                {
                    CleanupInputStageRunReviewSessionAsync(sessionDialog, sessionGeneration);
                };

                StartRunReviewBuzzer();
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "Align/Die Mapping 사용자 확인 화면을 표시했습니다. wafer=" + (wafer.WaferId ?? "") + " - Wait");
                // Main UI 접근을 허용하기 위해 unowned modeless top-level 창으로 연다.
                // 정리는 FormClosed 기반 CleanupInputStageRunReviewSessionAsync 단일 경로에서 수행한다.
                dialog.Show();

                // [오픈 시 자동 Live 2026-08-17, 팀장님 지시] 화면이 열리면 Wafer Vision을 Live 상태로 켠다.
                // 기존 "비전 사용 시작" 경로를 그대로 1회 호출하므로 안전 Scope 확보/중복 방지 판정은 동일하다.
                // 실패(비전 미연결·다른 화면 Live 점유 등)해도 알람 없이 상태 문구만 남고 화면은 정상 사용한다.
                StartInputStageRunReviewEmbeddedVisionAsync(dialog);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 화면 표시 실패: " + ex.Message + " - Failed");
                stage.FailUserConfirmFromUi("InputStage 사용자 확인 화면을 표시하지 못했습니다. " + ex.Message);
                if (dialog != null)
                    CleanupInputStageRunReviewSessionAsync(dialog, _inputStageRunReviewSessionGeneration);
            }
        }

        /// <summary>
        /// Review Modeless 세션의 단일 정리 경로입니다.
        /// FormClosed, Sequence 종료, 전역 STOP, Main Form 종료가 중복 호출해도 안전한 idempotent 구조입니다.
        /// </summary>
        private async void CleanupInputStageRunReviewSessionAsync(
            InputStageRunReviewDialog dialog,
            int sessionGeneration)
        {
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<InputStageRunReviewDialog, int>(
                        CleanupInputStageRunReviewSessionAsync), dialog, sessionGeneration);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "Review Cleanup UI 전환 실패: " + ex.Message + " - Failed");
                }
                return;
            }

            if (_inputStageRunReviewCleanedGeneration >= sessionGeneration)
                return;
            _inputStageRunReviewCleanedGeneration = sessionGeneration;

            // 이미 새 Review 세션이 시작된 뒤 늦게 도착한 이전 세션 정리라면
            // 모션/TCS 관련 정리는 건너뛰고 이전 dialog 자원만 해제한다.
            bool isCurrentSession = _inputStageRunReviewSessionGeneration == sessionGeneration;

            try { EndRunReviewBuzzer(); } catch { }

            if (isCurrentSession)
            {
                ClearInputStageRunReviewPendingOffset();
                try
                {
                    if (Controller != null)
                        Controller.CancelInputStageRunReviewAction();
                }
                catch { }
                if (!await WaitForInputStageRunReviewOneShotCompletionAsync(dialog, sessionGeneration).ConfigureAwait(true))
                {
                    if (_inputStageRunReviewSessionGeneration == sessionGeneration)
                        _inputStageRunReviewCleanedGeneration = sessionGeneration - 1;
                    return;
                }
                if (_inputStageRunReviewSessionGeneration != sessionGeneration ||
                    !ReferenceEquals(dialog, _inputStageRunReviewDialog))
                {
                    if (dialog != null && !dialog.IsDisposed)
                        dialog.Dispose();
                    return;
                }
                StopInputStageRunReviewEmbeddedVision(
                    dialog,
                    false,
                    "Review 종료로 내장 Wafer Vision을 정지했습니다.", true);
                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                {
                    try
                    {
                        await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                            "Review Cleanup Vision Test 종료 대기 실패: " + ex.Message + " - Failed");
                    }
                }
                ClearInputStageRunReviewPendingOffset();
            }

            if (dialog != null)
            {
                try
                {
                    if (!dialog.IsDisposed)
                        dialog.Dispose();
                }
                catch { }
            }

            if (ReferenceEquals(_inputStageRunReviewDialog, dialog) &&
                _inputStageRunReviewSessionGeneration == sessionGeneration)
                _inputStageRunReviewDialog = null;
        }

        private void OnInputStageUserConfirmWaitEnded()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnInputStageUserConfirmWaitEnded));
                return;
            }

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            if (dialog != null && !dialog.IsDisposed &&
                (Controller == null || !Controller.IsInputStageRunReviewManualActive))
            {
                // Vision Test owned form이 종료를 취소하는 동안 부모를 먼저 닫지 않는다.
                // ManualStateChanged 경로가 Vision 요청 종료를 await한 뒤 Review 창을 닫는다.
                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                    return;
                dialog.CloseFromSequence();
            }
        }

        private UserConfirmResult BuildInputStageRunReviewResult(
            InputStageRunReviewDialog dialog,
            InputStageRunReviewDecision decision)
        {
            var result = new UserConfirmResult
            {
                IsConfirmed = decision == InputStageRunReviewDecision.ConfirmAndContinue,
                Decision = decision,
                WaferId = dialog != null ? dialog.WaferId : string.Empty,
                MappingRevision = dialog != null ? dialog.MappingRevision : string.Empty,
                StartDieUid = dialog != null ? dialog.StartDieUid : string.Empty,
                StartDieIndex = dialog != null ? dialog.StartDieIndex : 0,
                ReviewSessionGeneration = _inputStageRunReviewSessionGeneration,
                ReviewRequestGeneration = _inputStageRunReviewRequestGeneration,
                GeometryVerificationToken = _inputStageRunReviewGeometryToken ?? string.Empty
            };

            if (dialog != null)
            {
                result.OrderedDieIds = dialog.OrderedDieIds.ToList();
                QMC.CDT320.DieMaps.DieMap draft = dialog.DraftDieMap;
                if (draft != null && draft.Entries != null)
                {
                    result.DieStates = draft.Entries
                        .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.DieUid))
                        .Select(entry => new InputStageRunReviewDieState
                        {
                            DieId = entry.DieUid,
                            IsTarget = entry.IsTarget,
                            Result = entry.Result,
                            BinCode = entry.BinCode,
                            HasPosition = !double.IsNaN(entry.PosX) && !double.IsInfinity(entry.PosX) &&
                                          !double.IsNaN(entry.PosY) && !double.IsInfinity(entry.PosY),
                            PositionX = entry.PosX,
                            PositionY = entry.PosY
                        })
                        .ToList();
                    result.HasMapOrigin = !double.IsNaN(draft.OriginX) && !double.IsInfinity(draft.OriginX) &&
                                          !double.IsNaN(draft.OriginY) && !double.IsInfinity(draft.OriginY);
                    result.MapOriginX = draft.OriginX;
                    result.MapOriginY = draft.OriginY;
                }
            }

            return result;
        }

        private void OnInputStageUserConfirmProcessingFailed(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnInputStageUserConfirmProcessingFailed), message);
                return;
            }

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            if (dialog != null && !dialog.IsDisposed)
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.RestoreAfterDecisionFailure(message + " 현재 조건으로 VERIFY MAP 후 다시 확인하세요.");
            }
        }

        private async void OnInputStageRunReviewManualStateChanged(bool active, string waferId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string>(OnInputStageRunReviewManualStateChanged), active, waferId);
                return;
            }

            if (active)
                return;

            // 이전 세션의 늦은 비활성 알림이 새 Review 세션 창을 닫지 않도록 방어한다.
            if (Controller != null && Controller.IsInputStageRunReviewManualActive)
                return;

            InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
            int sessionGeneration = _inputStageRunReviewSessionGeneration;
            try
            {
                if (Controller != null)
                    Controller.CancelInputStageRunReviewAction();
                if (!await WaitForInputStageRunReviewOneShotCompletionAsync(dialog, sessionGeneration).ConfigureAwait(true))
                    return;
                if (_inputStageRunReviewSessionGeneration != sessionGeneration ||
                    !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                    (Controller != null && Controller.IsInputStageRunReviewManualActive))
                    return;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "Review Manual 종료 확인에 실패하여 안전 Scope를 유지합니다. " + ex.Message + " - Failed");
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetStatusMessage("Review 동작 정지 확인에 실패했습니다. STOP 후 장비 상태를 확인하세요.");
                return;
            }
            StopInputStageRunReviewEmbeddedVision(
                dialog,
                false,
                "Review Manual 종료로 내장 Wafer Vision을 정지했습니다.", true);
            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                try
                {
                    await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                        "Review Manual 종료 시 Vision Test 화면 종료 대기 실패: " + ex.Message + " - Failed");
                }
            }
            ClearInputStageRunReviewPendingOffset();
            if (dialog != null && !dialog.IsDisposed)
                dialog.CloseFromSequence();
        }

        private async void RunInputStageReviewMoveSelectedDieAsync(InputStageRunReviewDialog dialog)
        {
            DieMapEntry entry = dialog != null ? dialog.SelectedDie : null;
            if (entry == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "이동할 Die를 먼저 선택하세요.");
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "선택 Die의 Mapping 절대좌표로 이동하시겠습니까?\r\n" +
                "UID=" + (entry.DieUid ?? "") + "\r\n" +
                "X=" + entry.PosX.ToString("F6") + " mm, Y=" + entry.PosY.ToString("F6") + " mm",
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();

            await RunInputStageReviewOneShotAsync(
                dialog,
                "Move Selected Die",
                async (stage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    // Map 절대좌표 이동은 Auto 속도가 아니라 Manual Sequence 화면에서 설정한
                    // Ready 속도(%)를 사용합니다. Review는 Auto와 병행될 수 있으므로
                    // 전역 READY Scope 없이 이 이동 명령에만 퍼센트를 적용합니다.
                    int result = await stage.MoveVisionPointSafelyAtReadySequenceSpeedAsync(
                        entry.PosX,
                        entry.PosY,
                        "InputStageRunReview.MoveSelectedDie").ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException("선택 Die 좌표 이동 실패. result=" + result);
                    return "선택 Die 좌표 이동을 완료했습니다. UID=" + (entry.DieUid ?? "");
                },
                true).ConfigureAwait(true);
        }

        /// <summary>
        /// Review 화면 Encoder 표시용으로 MotionMonitor 캐시 스냅샷을 우선 사용하고,
        /// 캐시가 없으면 축의 마지막 ActualPosition 값을 반환한다. 보드 I/O를 호출하지 않는다.
        /// </summary>
        private double ReadCachedAxisPosition(QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return 0.0;
            try
            {
                var snapshot = MotionMonitor != null ? MotionMonitor.GetLatest(axis) : null;
                return snapshot != null ? snapshot.ActualPosition : axis.ActualPosition;
            }
            catch
            {
                return axis.ActualPosition;
            }
        }

        private async System.Threading.Tasks.Task RunInputStageReviewOneShotAsync(
            InputStageRunReviewDialog dialog,
            string actionName,
            Func<InputStageUnit, System.Threading.CancellationToken, System.Threading.Tasks.Task<string>> action,
            bool allowEmbeddedVisionScopeReuse = false)
        {
            if (dialog == null || dialog.IsDisposed || !ReferenceEquals(dialog, _inputStageRunReviewDialog))
                return;
            if (_inputStageRunReviewDrainingDialogs.Contains(dialog) ||
                (_inputStageRunReviewOneShot != null && !_inputStageRunReviewOneShot.Completion.Task.IsCompleted))
            {
                dialog.SetStatusMessage("이전 Review 동작이 종료 중입니다. 완료 후 다시 실행하세요.");
                return;
            }
            var oneShot = new InputStageRunReviewOneShotState(dialog, _inputStageRunReviewSessionGeneration);
            _inputStageRunReviewOneShot = oneShot;
            IDisposable workScope = null;
            string finalStatus = string.Empty;
            bool reusedEmbeddedVisionScope = false;
            try
            {
                if (dialog == null || dialog.IsDisposed || Controller == null ||
                    !Controller.IsInputStageRunReviewManualActive)
                    throw new InvalidOperationException("활성 InputStage Review Manual 세션이 없습니다.");
                if (Machine == null || Machine.InputStageUnit == null)
                    throw new InvalidOperationException("InputStage Unit이 없습니다.");

                reusedEmbeddedVisionScope =
                    allowEmbeddedVisionScopeReuse &&
                    !_inputStageRunReviewEmbeddedVisionTransition &&
                    _inputStageRunReviewEmbeddedVisionScope != null &&
                    dialog.IsWaferVisionControlActive;

                if (reusedEmbeddedVisionScope)
                {
                    string safetyReason;
                    if (!Controller.AreInputStageRunReviewPickersSafe(out safetyReason))
                    {
                        throw new InvalidOperationException(
                            "Review " + actionName + " 직전 Picker 안전 재확인에 실패했습니다. " + safetyReason);
                    }
                    dialog.SetWaferVisionMoveBusy(
                        true,
                        actionName + " 동작 중입니다. Live 영상은 유지되며 STOP으로 취소할 수 있습니다.");
                }
                else
                {
                    dialog.SetBusy(true, actionName + " 동작 중입니다. STOP으로 취소할 수 있습니다.");
                    workScope = await Controller.BeginInputStageRunReviewWorkAsync(
                        ManualMotionScopeKind.ProcessSequence,
                        actionName,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);
                }

                System.Threading.CancellationToken actionToken =
                    Controller.InputStageRunReviewActionToken;
                actionToken.ThrowIfCancellationRequested();
                using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginManualSequenceProcessMove(
                    "InputStageRunReview." + actionName))
                {
                    finalStatus = await action(
                        Machine.InputStageUnit,
                        actionToken).ConfigureAwait(true);
                }
                dialog.SetAxisPositions(
                    Machine.InputStageUnit.CameraX != null ? Machine.InputStageUnit.CameraX.ActualPosition : 0.0,
                    Machine.InputStageUnit.StageY != null ? Machine.InputStageUnit.StageY.ActualPosition : 0.0,
                    Machine.InputStageUnit.StageT != null ? Machine.InputStageUnit.StageT.ActualPosition : 0.0);
            }
            catch (OperationCanceledException)
            {
                finalStatus = actionName + " 동작이 STOP/취소되었습니다.";
            }
            catch (Exception ex)
            {
                finalStatus = actionName + " 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewAction",
                    finalStatus + " - Failed");
            }
            finally
            {
                try
                {
                    if (workScope != null)
                        workScope.Dispose();
                    if (dialog != null && !dialog.IsDisposed)
                    {
                        if (_inputStageRunReviewDrainingDialogs.Contains(dialog))
                        {
                            dialog.SetWaferVisionMoveBusy(true, "Review 동작 종료를 확인하고 있습니다.");
                            dialog.SetBusy(true, "Review 동작 종료를 확인하고 있습니다.");
                        }
                        else if (reusedEmbeddedVisionScope && dialog.IsWaferVisionControlActive)
                            dialog.SetWaferVisionMoveBusy(false, finalStatus);
                        else
                            dialog.SetBusy(false, finalStatus);
                    }
                }
                finally
                {
                    if (ReferenceEquals(_inputStageRunReviewOneShot, oneShot))
                        _inputStageRunReviewOneShot = null;
                    oneShot.Completion.TrySetResult(true);
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> WaitForInputStageRunReviewOneShotCompletionAsync(
            InputStageRunReviewDialog dialog, int sessionGeneration)
        {
            if (dialog == null || !ReferenceEquals(dialog, _inputStageRunReviewDialog) ||
                sessionGeneration != _inputStageRunReviewSessionGeneration)
                return false;
            InputStageRunReviewOneShotState oneShot = _inputStageRunReviewOneShot;
            if (oneShot != null && (!ReferenceEquals(oneShot.Dialog, dialog) ||
                oneShot.SessionGeneration != sessionGeneration))
                return false;

            _inputStageRunReviewDrainingDialogs.Add(dialog);
            if (dialog != null && !dialog.IsDisposed)
            {
                dialog.SetReviewStopPending(true);
                dialog.SetWaferVisionMoveBusy(true, "STOP: 진행 중인 이동/검출 종료를 기다립니다.");
                dialog.SetBusy(true, "STOP: 진행 중인 이동/검출 종료를 기다립니다.");
            }
            try
            {
                // 기존 이동/촬상 callback이 끝날 때까지 현재 Review lease를 보유한다.
                InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
                int configuredTimeout = stage != null && stage.Config != null ? stage.Config.SequenceMoveTimeoutMs : 10000;
                int timeout = Math.Min(30000, configuredTimeout > 0 ? configuredTimeout : 10000);
                if (oneShot != null && await System.Threading.Tasks.Task.WhenAny(
                    oneShot.Completion.Task, System.Threading.Tasks.Task.Delay(timeout)).ConfigureAwait(true) != oneShot.Completion.Task)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "Review STOP 후 동작 완료 대기 시간이 초과되어 안전 Scope를 유지합니다. session=" + sessionGeneration + ", timeoutMs=" + timeout + " - Failed");
                    if (!dialog.IsDisposed)
                        dialog.SetStatusMessage("동작 종료가 확인되지 않아 Review 안전 영역을 유지합니다. STOP 및 장비 상태를 확인하세요.");
                    return false;
                }
                if (sessionGeneration != _inputStageRunReviewSessionGeneration ||
                    !ReferenceEquals(dialog, _inputStageRunReviewDialog))
                    return false;

                // 기존 Jog 정지 처리를 먼저 수행한다. 새로운 이동 명령은 발행하지 않는다.
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (stage != null)
                {
                    BaseAxis[] axes = { stage.CameraX, stage.StageY, stage.StageT, stage.EjectPinZ };
                    var elapsed = System.Diagnostics.Stopwatch.StartNew();
                    while (axes.Any(axis => axis != null && axis.IsMoving) && elapsed.ElapsedMilliseconds < timeout)
                        await System.Threading.Tasks.Task.Delay(20).ConfigureAwait(true);
                    if (axes.Any(axis => axis != null && axis.IsMoving))
                    {
                        QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                            "Review STOP 후 축 정지를 확인하지 못해 안전 Scope를 유지합니다. session=" + sessionGeneration + " - Failed");
                        if (!dialog.IsDisposed)
                            dialog.SetStatusMessage("축 정지가 확인되지 않아 Review 안전 영역을 유지합니다. STOP 및 축 상태를 확인하세요.");
                        return false;
                    }
                }
                return sessionGeneration == _inputStageRunReviewSessionGeneration &&
                    ReferenceEquals(dialog, _inputStageRunReviewDialog);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "Review STOP 완료 확인에 실패하여 안전 Scope를 유지합니다. session=" + sessionGeneration + ", " + ex.Message + " - Failed");
                if (!dialog.IsDisposed)
                    dialog.SetStatusMessage("Review 정지 확인에 실패했습니다. 안전 영역을 유지하며 STOP 및 장비 상태 확인이 필요합니다.");
                return false;
            }
        }

        private async void StartInputStageRunReviewJogAsync(
            InputStageRunReviewDialog dialog,
            InputStageReviewJogEventArgs args)
        {
            IDisposable scope = null;
            try
            {
                if (dialog == null || args == null || Controller == null ||
                    !Controller.IsInputStageRunReviewManualActive || Machine == null || Machine.InputStageUnit == null)
                    return;
                if (_inputStageRunReviewDrainingDialogs.Contains(dialog))
                    return;

                // X/Y 이동은 검출 기준을 바꾸고 T 이동은 좌표 검증 조건도 바꾼다.
                ClearInputStageRunReviewPendingOffset();

                // [Live 중 Jog 허용 2026-08-17, 팀장님 지시] Wafer Vision Live는 Review work scope를
                //   계속 보유하므로 IsInputStageRunReviewActionBusy가 true다. 기존에는 여기서
                //   무조건 거절해 Live 상태에서는 Jog가 전혀 동작하지 않았다.
                //   현재 기준: MOVE SELECTED DIE와 동일하게 그 Live scope를 재사용한다
                //   (RunInputStageReviewOneShotAsync의 allowEmbeddedVisionScopeReuse와 같은 판정·안전 재확인).
                bool reuseVisionScope =
                    !_inputStageRunReviewEmbeddedVisionTransition &&
                    _inputStageRunReviewEmbeddedVisionScope != null &&
                    dialog.IsWaferVisionControlActive;

                if (_inputStageRunReviewJogScope != null ||
                    (!reuseVisionScope && Controller.IsInputStageRunReviewActionBusy))
                {
                    dialog.SetBusy(false, "다른 Review 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                    return;
                }

                if (reuseVisionScope)
                {
                    // Scope를 새로 잡지 않으므로 Picker 안전 조건을 이 시점에 다시 확인한다.
                    string jogSafetyReason;
                    if (!Controller.AreInputStageRunReviewPickersSafe(out jogSafetyReason))
                    {
                        dialog.SetBusy(true,
                            "Jog 직전 Picker 안전 재확인에 실패했습니다. " + jogSafetyReason);
                        return;
                    }
                }

                InputStageUnit stage = Machine.InputStageUnit;
                BaseAxis axis = args.Axis == InputStageReviewJogAxis.VisionX
                    ? stage.CameraX
                    : args.Axis == InputStageReviewJogAxis.WaferY
                        ? stage.StageY
                        : stage.StageT;
                if (axis == null)
                    throw new InvalidOperationException("Jog 대상 축이 없습니다. axis=" + args.Axis);

                JogSpeedType speedType;
                double customSpeed = 0.0;
                if (string.Equals(args.Speed, "Coarse", StringComparison.OrdinalIgnoreCase))
                {
                    speedType = JogSpeedType.Coarse;
                }
                else if (string.Equals(args.Speed, "Medium", StringComparison.OrdinalIgnoreCase))
                {
                    speedType = JogSpeedType.Custom;
                    customSpeed = axis.Config != null
                        ? Math.Max(0.000001, axis.Config.JogCoarseVelocity * 0.5)
                        : 0.5;
                }
                else
                {
                    speedType = JogSpeedType.Fine;
                }

                if (args.IsStepMode)
                {
                    // Step 모드: one-shot 이동 후 즉시 안전영역 scope를 반환한다.
                    await RunInputStageReviewOneShotAsync(
                        dialog,
                        "Jog Step:" + args.Axis,
                        async (stageUnit, token) =>
                        {
                            token.ThrowIfCancellationRequested();
                            int stepResult = await stageUnit.JogStepAsync(
                                axis,
                                args.Direction,
                                speedType,
                                customSpeed,
                                args.StepDistance).ConfigureAwait(false);
                            if (stepResult != 0)
                                throw new InvalidOperationException(
                                    "Step Jog 실패. axis=" + args.Axis + ", result=" + stepResult);
                            return args.Axis + " Step Jog(" +
                                   args.StepDistance.ToString("0.###") + ") 완료.";
                        },
                        // Live 중이면 기존 Vision scope를 재사용한다(새 scope 획득 시 Busy로 거절됨).
                        true).ConfigureAwait(true);
                    return;
                }

                _inputStageRunReviewJogStartPending = true;
                if (reuseVisionScope)
                {
                    // Live scope 재사용: 새 scope를 잡지 않으므로 _inputStageRunReviewJogScope는 null로 둔다.
                    //   (StopInputStageRunReviewJogCoreAsync가 null이면 Dispose하지 않으므로 Live가 유지된다.)
                    dialog.SetWaferVisionMoveBusy(true,
                        args.Axis + " Jog 중입니다. Live 영상은 유지되며 버튼을 놓거나 STOP으로 정지합니다.");
                }
                else
                {
                    dialog.SetBusy(true, args.Axis + " Jog 시작 중입니다. 버튼을 놓거나 STOP을 누르세요.");
                    scope = await Controller.BeginInputStageRunReviewWorkAsync(
                        ManualMotionScopeKind.ProcessSequence,
                        "Jog:" + args.Axis,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);
                }

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (!_inputStageRunReviewJogStartPending ||
                    actionToken.IsCancellationRequested ||
                    !Controller.IsInputStageRunReviewManualActive)
                {
                    throw new OperationCanceledException(
                        "Jog 안전영역 대기 중 STOP/MouseUp 또는 Review 종료가 요청되었습니다.",
                        actionToken);
                }

                _inputStageRunReviewJogScope = scope;
                _inputStageRunReviewJogAxis = axis;
                _inputStageRunReviewJogReusedVisionScope = reuseVisionScope;
                _inputStageRunReviewJogStartPending = false;
                scope = null;

                int result;
                using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginManualSequenceProcessMove(
                    "InputStageRunReview.Jog:" + args.Axis))
                {
                    result = await stage.JogContinuousAsync(
                        axis,
                        args.Direction,
                        speedType,
                        customSpeed).ConfigureAwait(true);
                }
                if (result != 0)
                    throw new InvalidOperationException("Jog 명령 실패. axis=" + args.Axis + ", result=" + result);

                if (reuseVisionScope)
                {
                    dialog.SetWaferVisionMoveBusy(true,
                        args.Axis + " Jog 중입니다. Live 영상은 유지되며 버튼을 놓거나 STOP으로 정지합니다.");
                }
                else
                {
                    dialog.SetBusy(true, args.Axis + " Jog 중입니다. 버튼을 놓거나 STOP을 누르세요.");
                }
            }
            catch (OperationCanceledException)
            {
                _inputStageRunReviewJogStartPending = false;
                if (scope != null)
                {
                    scope.Dispose();
                    scope = null;
                }
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 시작이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                _inputStageRunReviewJogStartPending = false;
                if (scope != null)
                    scope.Dispose();
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 실패: " + ex.Message);
            }
        }

        private async void StopInputStageRunReviewJogAsync(
            InputStageRunReviewDialog dialog,
            string reason)
        {
            try
            {
                bool hasJog = _inputStageRunReviewJogStartPending ||
                              _inputStageRunReviewJogScope != null ||
                              _inputStageRunReviewJogAxis != null;
                if (!hasJog)
                    return;

                // Live scope 재사용 Jog는 그 scope의 Action Token을 취소하면 Live까지 끊긴다.
                // 이 경우 축 정지만 수행하고 Review Action은 취소하지 않는다(2026-08-17 팀장님 지시).
                bool reusedVisionScope = _inputStageRunReviewJogReusedVisionScope;
                if (Controller != null && !reusedVisionScope)
                    Controller.CancelInputStageRunReviewAction();
                await StopInputStageRunReviewJogCoreAsync().ConfigureAwait(true);
                if (dialog != null && !dialog.IsDisposed)
                {
                    InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
                    if (stage != null)
                    {
                        dialog.SetAxisPositions(
                            stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0,
                            stage.StageY != null ? stage.StageY.ActualPosition : 0.0,
                            stage.StageT != null ? stage.StageT.ActualPosition : 0.0);
                    }
                    string stopStatus = string.IsNullOrWhiteSpace(reason) ? "Jog를 정지했습니다." : reason;
                    if (reusedVisionScope && dialog.IsWaferVisionControlActive)
                        dialog.SetWaferVisionMoveBusy(false, stopStatus);
                    else
                        dialog.SetBusy(false, stopStatus);
                }
            }
            catch (Exception ex)
            {
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Jog 정지 실패: " + ex.Message);
            }
        }

        private async System.Threading.Tasks.Task StopInputStageRunReviewJogCoreAsync()
        {
            BaseAxis axis = _inputStageRunReviewJogAxis;
            IDisposable scope = _inputStageRunReviewJogScope;
            bool startPending = _inputStageRunReviewJogStartPending;
            _inputStageRunReviewJogAxis = null;
            _inputStageRunReviewJogScope = null;
            // 재사용 Jog는 scope가 애초에 null이라 Dispose 대상이 없다(Live 유지). 플래그만 정리한다.
            _inputStageRunReviewJogReusedVisionScope = false;
            _inputStageRunReviewJogStartPending = false;

            try
            {
                InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
                if (stage != null)
                {
                    if (axis != null)
                        await stage.StopJogAsync(axis).ConfigureAwait(true);
                    else if (startPending)
                    {
                        if (stage.CameraX != null) await stage.StopJogAsync(stage.CameraX).ConfigureAwait(true);
                        if (stage.StageY != null) await stage.StopJogAsync(stage.StageY).ConfigureAwait(true);
                        if (stage.StageT != null) await stage.StopJogAsync(stage.StageT).ConfigureAwait(true);
                    }
                }
            }
            finally
            {
                if (scope != null)
                    scope.Dispose();
            }
        }

        private async void StopInputStageRunReviewActionAsync(
            InputStageRunReviewDialog dialog,
            string reason)
        {
            int sessionGeneration = _inputStageRunReviewSessionGeneration;
            ClearInputStageRunReviewPendingOffset();
            try
            {
                if (Controller != null)
                    Controller.CancelInputStageRunReviewAction();

                if (_inputStageRunReviewVisionTestDialog != null &&
                    !_inputStageRunReviewVisionTestDialog.IsDisposed)
                {
                    await _inputStageRunReviewVisionTestDialog.RequestClose().ConfigureAwait(true);
                }

                if (!await WaitForInputStageRunReviewOneShotCompletionAsync(dialog, sessionGeneration).ConfigureAwait(true))
                    return;
                if (_inputStageRunReviewSessionGeneration != sessionGeneration ||
                    !ReferenceEquals(dialog, _inputStageRunReviewDialog))
                    return;
                StopInputStageRunReviewEmbeddedVision(
                    dialog,
                    true,
                    "Review STOP으로 내장 Wafer Vision을 정지했습니다.", true);
                if (dialog != null && !dialog.IsDisposed)
                {
                    bool busy = Controller != null && Controller.IsInputStageRunReviewActionBusy;
                    dialog.SetBusy(busy, string.IsNullOrWhiteSpace(reason)
                        ? "Review 수동 동작 정지를 요청했습니다."
                        : reason);
                }
            }
            catch (Exception ex)
            {
                _inputStageRunReviewDrainingDialogs.Add(dialog);
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "Review 수동 동작 정지 실패로 안전 Scope를 유지합니다. " + ex.Message + " - Failed");
                if (dialog != null && !dialog.IsDisposed)
                {
                    dialog.SetReviewStopPending(true);
                    dialog.SetWaferVisionMoveBusy(true, "Review 수동 동작 정지 실패: " + ex.Message);
                    dialog.SetBusy(true, "Review 수동 동작 정지 실패: " + ex.Message);
                }
            }
        }

        private async void RunInputStageReviewThetaCorrectionAsync(InputStageRunReviewDialog dialog)
        {
            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stage == null || stage.StageT == null || wafer == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "StageT 또는 InputStage Wafer 정보가 없습니다.");
                return;
            }

            double referenceT = stage.ResolveWaferAlignReferenceT();
            double correctedT = stage.StageT.ActualPosition;
            double offsetT = correctedT - referenceT;
            string limitReason = string.Empty;
            if (Math.Abs(offsetT) <= 0.000001 ||
                !stage.IsWaferAlignThetaOffsetWithinLimit(offsetT, out limitReason))
            {
                dialog.SetBusy(false,
                    Math.Abs(offsetT) <= 0.000001
                        ? "T 보정 Offset이 0이라 저장할 수 없습니다."
                        : limitReason);
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "현재 StageT 위치를 T 보정값으로 저장하시겠습니까?\r\n" +
                "Reference=" + referenceT.ToString("F6") + "\r\n" +
                "Current=" + correctedT.ToString("F6") + "\r\n" +
                "Offset=" + offsetT.ToString("F6") +
                "\r\n저장 후 Die Mapping 재실행이 필요합니다.",
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();

            await RunInputStageReviewOneShotAsync(
                dialog,
                "T Correction",
                (inputStage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    wafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    inputStage.SetCurrentWaferMaterial(wafer);
                    inputStage.ApplyWaferAlignThetaResult(referenceT, correctedT, offsetT);
                    MaterialStateService.SaveInputStageThetaAlignResult(
                        wafer,
                        referenceT,
                        correctedT,
                        offsetT);
                    return System.Threading.Tasks.Task.FromResult(
                        "T 보정값을 저장했습니다. Die Mapping을 다시 실행하세요. Offset=" + offsetT.ToString("F6"));
                },
                true).ConfigureAwait(true);

            // 수동 폴백 얼라인 웨이퍼: T 보정 저장이 실제 반영됐을 때만 확정 잠금을 해제한다.
            if (dialog != null && !dialog.IsDisposed && wafer.InputStageAlignManualFallbackThetaDone)
                dialog.NotifyThetaCorrectionCompleted();

            if (dialog != null && !dialog.IsDisposed && !wafer.HasInputStageDieMappingResult)
            {
                dialog.SetWorkflowState(
                    wafer.WaferId,
                    ActiveRecipeName,
                    QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                    QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected,
                    wafer.HasInputStageAlignResult && wafer.HasInputStageThetaAlignResult,
                    wafer.DieMapFrameObjId,
                    false,
                    "MAPPING REQUIRED");
                dialog.SetReviewValid(false, "MAPPING REQUIRED");

                // T 보정 후 재매핑 수동 클릭 대기에서 흐름이 끊기지 않도록 재매핑을 자동 제출한다.
                if (dialog.TrySubmitAutoMappingRetryAfterThetaCorrection())
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "T 보정 저장 후 Die Mapping 재실행을 자동 제출했습니다. wafer=" + (wafer.WaferId ?? "") + " - Ok");
                }
                else
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                        "T 보정 저장 후 Die Mapping 자동 재실행 제출이 거부되어 수동 안내로 폴백합니다. wafer=" + (wafer.WaferId ?? "") + " - Check");
                    dialog.SetStatusMessage("T 보정값을 저장했습니다. [RUN DIE MAPPING]을 눌러 Die Mapping을 다시 실행하세요.");
                }
            }
        }

        private async void RunInputStageReviewDieDetectionAsync(InputStageRunReviewDialog dialog)
        {
            DieMapEntry entry = dialog != null ? dialog.SelectedDie : null;
            InputStageUnit stage = Machine != null ? Machine.InputStageUnit : null;
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (entry == null || stage == null || wafer == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "다이 검출 기준 Die/Stage/Wafer 정보가 없습니다.");
                return;
            }

            string thetaReason;
            if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out thetaReason))
            {
                dialog.SetBusy(false, thetaReason);
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "현재 Vision 화면에서 선택 Die 중심을 검출하시겠습니까?\r\n" +
                "UID=" + (entry.DieUid ?? "") + "\r\n" +
                "기준 X=" + entry.PosX.ToString("F6") + ", Y=" + entry.PosY.ToString("F6"),
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            ClearInputStageRunReviewPendingOffset();

            // [DIE DETECTION Live 정지/복귀 2026-08-17, 팀장님 지시] 검출은 Grab을 직접 사용하므로
            // Live 수신과 카메라를 다툰다. 검출 직전 Live만 멈추고(안전 Scope는 유지) 검출 종료 후
            // finally에서 반드시 복귀시킨다. 복귀 실패는 알람 없이 상태 문구로만 안내한다(팀장님 확정).
            int detectionSessionGeneration = _inputStageRunReviewSessionGeneration;
            long detectionRequestGeneration = _inputStageRunReviewRequestGeneration;
            bool waferLivePaused = dialog.PauseWaferVisionLiveForAction();
            if (waferLivePaused)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVision",
                    "DIE DETECTION 실행을 위해 Wafer Vision Live를 일시 정지했습니다. - Check");
            }

            try
            {
            string detectedWaferId = dialog.WaferId;
            string detectedMappingRevision = dialog.MappingRevision;
            string detectedDieUid = entry.DieUid ?? string.Empty;
            int detectedDieMapX = entry.DieMapX;
            int detectedDieMapY = entry.DieMapY;
            double detectedReferenceX = entry.PosX;
            double detectedReferenceY = entry.PosY;
            string detectedDraftSignature = BuildInputStageRunReviewDraftSignature(dialog);
            InputStageReviewGeometryContext detectedContext;
            string geometryReason;
            if (!TryCaptureInputStageReviewContext(dialog, false, out detectedContext, out geometryReason))
            {
                dialog.SetBusy(false, geometryReason);
                return;
            }
            DieMap detectedDraft = dialog.DraftDieMap;
            double detectedFinalX = 0.0, detectedFinalY = 0.0, detectedFinalT = 0.0;
            double detectedOffsetX = 0.0;
            double detectedOffsetY = 0.0;
            bool detectionSucceeded = false;
            _inputStageRunReviewDieDetectionSimulated = false;

            await RunInputStageReviewOneShotAsync(
                dialog,
                "Die Detection",
                async (inputStage, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    EnsureInputStageReviewRequestCurrent(dialog, detectedContext, detectedDraft);
                    inputStage.ApplyWaferAlignThetaResult(
                        wafer.InputStageAlignReferenceT,
                        wafer.InputStageAlignCorrectedT,
                        wafer.InputStageAlignOffsetT);

                    if (inputStage.EjectPinZ != null && inputStage.Recipe != null && inputStage.Recipe.EjectPinZ != null)
                    {
                        int ejectResult = await inputStage.MoveInputStageAxis(
                            WaferStageAxis.EjectPinZ,
                            inputStage.Recipe.EjectPinZ.AvoidPosition,
                            JogSpeedType.Fine,
                            0.0).ConfigureAwait(false);
                        if (ejectResult != 0)
                            throw new InvalidOperationException("Die 검출 전 EjectPinZ Avoid 이동 실패. result=" + ejectResult);
                    }

                    token.ThrowIfCancellationRequested();
                    EnsureInputStageReviewRequestCurrent(dialog, detectedContext, detectedDraft);
                    double targetT;
                    if (inputStage.TryResolveWaferAlignThetaTarget(out targetT))
                    {
                        int thetaResult = await inputStage.MoveInputStageAxis(
                            WaferStageAxis.WaferT,
                            targetT,
                            JogSpeedType.Fine,
                            0.0).ConfigureAwait(false);
                        if (thetaResult != 0)
                            throw new InvalidOperationException("Die 검출 전 StageT 보정 위치 이동 실패. result=" + thetaResult);
                    }

                    double currentX = inputStage.CameraX.ActualPosition;
                    double currentY = inputStage.StageY.ActualPosition;
                    double currentT = inputStage.StageT.ActualPosition;
                    EnsureInputStageReviewAxesStationary(inputStage);
                    VisionAlignResult vision = await RequestInputStageRunReviewDieVisionAsync(
                        inputStage,
                        entry,
                        currentX,
                        currentY,
                        token).ConfigureAwait(false);
                    if (vision == null ||
                        double.IsNaN(vision.DeltaX) || double.IsInfinity(vision.DeltaX) ||
                        double.IsNaN(vision.DeltaY) || double.IsInfinity(vision.DeltaY))
                    {
                        throw new InvalidOperationException("InputPickDie Vision 검출 결과가 유효하지 않습니다.");
                    }

                    token.ThrowIfCancellationRequested();
                    EnsureInputStageReviewRequestCurrent(dialog, detectedContext, detectedDraft);
                    EnsureInputStageReviewCaptureUnchanged(inputStage, currentX, currentY, currentT);

                    // Wafer 채널 라이브 Delta는 카메라 순수 오프셋(raw)이므로 InputToBottomOffset 감산 없이 그대로 사용한다.
                    double centerDeltaX = vision.DeltaX;
                    double centerDeltaY = -vision.DeltaY;
                    double detectedCenterX = currentX + centerDeltaX;
                    double detectedCenterY = currentY + centerDeltaY;
                    double offsetX = detectedCenterX - detectedReferenceX;
                    double offsetY = detectedCenterY - detectedReferenceY;
                    string offsetReason;
                    InputStageReviewOffsetCandidate candidate;
                    if (!InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(
                        detectedContext, offsetX, offsetY, GetInputStageReviewOffsetLimits(inputStage),
                        out candidate, out offsetReason))
                        throw new InvalidOperationException(offsetReason);

                    int moveResult = await inputStage.MoveVisionPointSafelyAsync(
                        detectedCenterX,
                        detectedCenterY,
                        JogSpeedType.Fine,
                        0.0,
                        "InputStageRunReview.DieDetectionCenterMove").ConfigureAwait(false);
                    if (moveResult != 0)
                        throw new InvalidOperationException("검출 Die 중심 좌표 이동 실패. result=" + moveResult);

                    token.ThrowIfCancellationRequested();
                    EnsureInputStageReviewRequestCurrent(dialog, detectedContext, detectedDraft);
                    EnsureInputStageReviewAxesStationary(inputStage);
                    detectedFinalX = inputStage.CameraX.ActualPosition;
                    detectedFinalY = inputStage.StageY.ActualPosition;
                    detectedFinalT = inputStage.StageT.ActualPosition;
                    detectedOffsetX = offsetX;
                    detectedOffsetY = offsetY;
                    detectionSucceeded = true;
                    // 시뮬레이션 경로(UseVision=false)는 offset이 항상 0이므로 실제 검출과 구분해 표시한다.
                    if (_inputStageRunReviewDieDetectionSimulated)
                        return "[비전 미사용 - 시뮬레이션] 실제 Die 검출을 수행하지 않았습니다. " +
                               "공칭 좌표로 이동만 했고 Offset은 0입니다. 설정에서 비전 사용을 켠 뒤 다시 실행하세요. X=" +
                               offsetX.ToString("F6") + ", Y=" + offsetY.ToString("F6");

                    return "Die 검출 완료. Offset 적용 버튼으로 Draft Map에 반영하세요. X=" +
                           offsetX.ToString("F6") + ", Y=" + offsetY.ToString("F6");
                },
                true).ConfigureAwait(true);

            // 시뮬레이션 경로의 offset은 항상 0이며 실측이 아니므로 pending으로 등록하지 않는다.
            // (등록하면 APPLY OFFSET이 "적용 성공"으로 보고되어 실제 보정을 한 것처럼 오인된다.)
            if (detectionSucceeded && _inputStageRunReviewDieDetectionSimulated)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "UI", "IN-REVIEW-DIE-DETECT-SIM",
                    "InputStageRunReview",
                    "비전 미사용(시뮬레이션) Die 검출이므로 Offset을 적용 대상으로 등록하지 않았습니다. " +
                    "die=" + (detectedDieUid ?? "-"));
            }
            else if (detectionSucceeded && IsInputStageReviewRequestCurrent(dialog, detectedContext, detectedDraft) &&
                     string.Equals(detectedDraftSignature, BuildInputStageRunReviewDraftSignature(dialog), StringComparison.Ordinal))
            {
                _inputStageRunReviewOffsetPending = true;
                _inputStageRunReviewPendingOffsetX = detectedOffsetX;
                _inputStageRunReviewPendingOffsetY = detectedOffsetY;
                _inputStageRunReviewPendingOffsetWaferId = detectedWaferId;
                _inputStageRunReviewPendingOffsetMappingRevision = detectedMappingRevision;
                _inputStageRunReviewPendingOffsetDieUid = detectedDieUid;
                _inputStageRunReviewPendingOffsetDieMapX = detectedDieMapX;
                _inputStageRunReviewPendingOffsetDieMapY = detectedDieMapY;
                _inputStageRunReviewPendingOffsetReferenceX = detectedReferenceX;
                _inputStageRunReviewPendingOffsetReferenceY = detectedReferenceY;
                _inputStageRunReviewPendingOffsetDraftSignature = detectedDraftSignature;
                _inputStageRunReviewPendingGeometryContext = detectedContext;
                _inputStageRunReviewPendingCaptureX = detectedFinalX;
                _inputStageRunReviewPendingCaptureY = detectedFinalY;
                _inputStageRunReviewPendingCaptureT = detectedFinalT;
                LogInputStageReviewGeometry("DETECTION", detectedContext,
                    "offset=" + detectedOffsetX.ToString("F6") + "/" + detectedOffsetY.ToString("F6"));
            }
            }
            finally
            {
                // 성공/실패/취소 어느 경로로 끝나도 Live를 복귀시킨다(정지한 경우에만).
                if (waferLivePaused && dialog != null && !dialog.IsDisposed &&
                    ReferenceEquals(dialog, _inputStageRunReviewDialog) && Controller != null &&
                    detectionSessionGeneration == _inputStageRunReviewSessionGeneration &&
                    detectionRequestGeneration == _inputStageRunReviewRequestGeneration &&
                    !_inputStageRunReviewDrainingDialogs.Contains(dialog) &&
                    Controller.IsInputStageRunReviewManualActive &&
                    !Controller.InputStageRunReviewActionToken.IsCancellationRequested)
                {
                    bool resumed = dialog.StartWaferVisionLive();
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVision",
                        "DIE DETECTION 종료 후 Wafer Vision Live 복귀 시도. resumed=" + resumed +
                        (resumed ? " - Ok" : " - Check"));
                    if (!resumed)
                    {
                        // 복귀 실패는 알람 없이 상태 문구만 남긴다(팀장님 확정 2026-08-17).
                        dialog.SetStatusMessage(
                            "DIE DETECTION은 종료했으나 Live 영상 복귀에 실패했습니다. " +
                            "[비전 사용 종료] 후 [비전 사용 시작]으로 다시 켜주세요.");
                    }
                }
            }
        }

        /// <summary>
        /// Review 화면의 Wafer 영상/명령 기능(Live·Grab·측정)이 실제로 동작 가능한 상태인지 판정한다.
        /// 명령 채널(VisionHub.Wafer)이 없으면 CAM_SWITCH/EXPOSE가 no-op이 되어 화면만 "사용 중"으로 보인다.
        /// </summary>
        private static bool IsWaferVisionLinkReady()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && !settings.UseVision)
                    return false;

                QMC.CDT320.VisionComm.VisionTcpClient client = QMC.CDT320.VisionComm.VisionHub.Wafer;
                return client != null && client.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        private async System.Threading.Tasks.Task<VisionAlignResult> RequestInputStageRunReviewDieVisionAsync(
            InputStageUnit stage,
            DieMapEntry entry,
            double currentX,
            double currentY,
            System.Threading.CancellationToken token)
        {
            bool connected = QMC.CDT320.VisionComm.VisionHub.Wafer != null &&
                             QMC.CDT320.VisionComm.VisionHub.Wafer.IsConnected;
            AppSettings settings = AppSettingsStore.Current;
            bool visionDisabled = settings != null && !settings.UseVision;

            // 비전을 쓰지 않도록 설정된 경우(의도된 시뮬레이션)에만 공칭 좌표 fallback을 허용한다.
            // 이때 offset은 항상 0이 되므로 "실제 검출"이 아님을 호출부가 반드시 표시해야 한다.
            if (visionDisabled)
            {
                _inputStageRunReviewDieDetectionSimulated = true;
                return new VisionAlignResult
                {
                    DeltaX = entry.PosX - currentX,
                    DeltaY = currentY - entry.PosY,
                    DeltaTheta = 0.0
                };
            }

            // 비전을 쓰는 설정인데 연결이 없으면 가짜 성공을 만들지 않고 실패로 처리한다.
            // (예전에는 여기서도 공칭 좌표 fallback을 반환해 "검출 완료 X=0 Y=0"으로 보고했다.)
            if (!connected)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "UI", "IN-REVIEW-DIE-DETECT-NO-VISION",
                    "InputStageRunReview",
                    "Die 검출 불가: Wafer Vision이 연결되지 않았습니다. UseVision=true인데 연결이 없어 검출을 실패로 처리합니다.");
                return null;
            }

            QMC.CDT320.VisionComm.MatchResultDto match = await QMC.CDT320.VisionComm.AutoVisionRequestService.MatchAsync(
                QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                QMC.CDT320.VisionComm.VisionToolIds.Wafer.DieFinder,
                0,
                5000,
                token).ConfigureAwait(false);
            if (match == null || !match.Success)
                return null;

            VisionAlignResult bottom = QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ToAlignResult(
                QMC.CDT320.VisionComm.AutoVisionChannel.Wafer,
                match,
                0.15);
            if (bottom == null)
                return null;

            QMC.CDT320.Calibration.VisionCameraPixelCalibration camera =
                QMC.CDT320.Calibration.VisionCameraCalibrationTransform.ResolveCamera(
                    null,
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer) ??
                new QMC.CDT320.Calibration.VisionCameraPixelCalibration();
            camera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);
            if (match.HasImageSize)
                camera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);

            return new VisionAlignResult
            {
                DeltaX = bottom.DeltaX,
                DeltaY = camera.PixelToMmOffsetY(match.Y),
                DeltaTheta = bottom.DeltaTheta,
                PitchX = bottom.PitchX,
                PitchY = bottom.PitchY
            };
        }

        private void ApplyInputStageRunReviewPendingOffset(InputStageRunReviewDialog dialog)
        {
            if (!_inputStageRunReviewOffsetPending || dialog == null)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "적용할 Die Detection Offset이 없습니다.");
                return;
            }

            if (!string.Equals(dialog.WaferId, _inputStageRunReviewPendingOffsetWaferId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(dialog.MappingRevision, _inputStageRunReviewPendingOffsetMappingRevision, StringComparison.OrdinalIgnoreCase))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false, "Die Detection 이후 Wafer/Mapping이 변경되어 Offset을 적용할 수 없습니다.");
                return;
            }

            string pendingReason;
            if (!CanApplyInputStageReviewPendingOffset(dialog, out pendingReason))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false, pendingReason);
                return;
            }

            string currentSignature = BuildInputStageRunReviewDraftSignature(dialog);
            if (!string.Equals(
                currentSignature,
                _inputStageRunReviewPendingOffsetDraftSignature,
                StringComparison.Ordinal))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false,
                    "Die Detection 이후 Review Draft 상태/좌표/순서가 변경되어 Offset을 적용할 수 없습니다. 다시 검출하세요.");
                return;
            }

            DieMap draft = dialog.DraftDieMap;
            DieMapEntry referenceEntry = draft != null && draft.Entries != null
                ? draft.Entries.FirstOrDefault(candidate => candidate != null &&
                    string.Equals(
                        candidate.DieUid ?? string.Empty,
                        _inputStageRunReviewPendingOffsetDieUid,
                        StringComparison.OrdinalIgnoreCase))
                : null;
            if (referenceEntry == null ||
                referenceEntry.DieMapX != _inputStageRunReviewPendingOffsetDieMapX ||
                referenceEntry.DieMapY != _inputStageRunReviewPendingOffsetDieMapY ||
                Math.Abs(referenceEntry.PosX - _inputStageRunReviewPendingOffsetReferenceX) > 0.000000001 ||
                Math.Abs(referenceEntry.PosY - _inputStageRunReviewPendingOffsetReferenceY) > 0.000000001)
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false,
                    "Die Detection 기준 Die UID/Grid/좌표가 현재 Draft와 달라 Offset을 적용할 수 없습니다. 다시 검출하세요.");
                return;
            }

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                dialog,
                "검출 Offset을 Review Draft 전체 Die 좌표에 적용하시겠습니까?\r\n" +
                "기준 UID=" + _inputStageRunReviewPendingOffsetDieUid +
                ", Grid=(" + _inputStageRunReviewPendingOffsetDieMapX +
                "," + _inputStageRunReviewPendingOffsetDieMapY + ")\r\n" +
                "기준 X=" + _inputStageRunReviewPendingOffsetReferenceX.ToString("F6") +
                ", Y=" + _inputStageRunReviewPendingOffsetReferenceY.ToString("F6") + "\r\n" +
                "X=" + _inputStageRunReviewPendingOffsetX.ToString("F6") +
                ", Y=" + _inputStageRunReviewPendingOffsetY.ToString("F6"),
                "InputStage Review",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            // 확인 대화상자 중 정지/세션 변경/설정 변경도 다시 확인한다.
            string reason;
            if (!CanApplyInputStageReviewPendingOffset(dialog, out reason))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false, reason);
                return;
            }
            if (!MaterialStateService.TryRecordInputStageReviewOffset(
                _inputStageRunReviewPendingGeometryContext, dialog.DraftDieMap,
                _inputStageRunReviewPendingOffsetX, _inputStageRunReviewPendingOffsetY, out reason))
            {
                ClearInputStageRunReviewPendingOffset();
                dialog.SetBusy(false, reason);
                return;
            }
            LogInputStageReviewGeometry("APPLY", _inputStageRunReviewPendingGeometryContext,
                "offset=" + _inputStageRunReviewPendingOffsetX.ToString("F6") + "/" +
                _inputStageRunReviewPendingOffsetY.ToString("F6"));
            if (dialog.ApplyDraftCoordinateOffset(
                _inputStageRunReviewPendingOffsetX,
                _inputStageRunReviewPendingOffsetY,
                out reason))
            {
                ClearInputStageRunReviewPendingOffset();
            }
            else
            {
                dialog.SetBusy(false, reason);
            }
        }

        private static string BuildInputStageRunReviewDraftSignature(InputStageRunReviewDialog dialog)
        {
            if (dialog == null || dialog.DraftDieMap == null || dialog.DraftDieMap.Entries == null)
                return string.Empty;

            DieMap map = dialog.DraftDieMap;
            var text = new System.Text.StringBuilder();
            text.Append(map.FrameObjId ?? string.Empty).Append('|')
                .Append(map.DieMapX).Append('|').Append(map.DieMapY).Append('|')
                .Append(map.OriginX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(map.OriginY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(dialog.StartDieUid ?? string.Empty).Append('|')
                .Append(string.Join(",", dialog.OrderedDieIds ?? new List<string>())).Append('|');

            foreach (DieMapEntry entry in map.Entries
                .Where(candidate => candidate != null)
                .OrderBy(candidate => candidate.DieUid ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.DieMapY)
                .ThenBy(candidate => candidate.DieMapX))
            {
                text.Append(entry.DieUid ?? string.Empty).Append(':')
                    .Append(entry.Index).Append(':')
                    .Append(entry.DieMapX).Append(':').Append(entry.DieMapY).Append(':')
                    .Append(entry.PosX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(entry.PosY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(entry.IsTarget ? '1' : '0').Append(':')
                    .Append((int)entry.Result).Append(':').Append(entry.BinCode).Append(':')
                    .Append(entry.SequenceNo).Append(';');
            }

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text.ToString());
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        private void ClearInputStageRunReviewPendingOffset()
        {
            ++_inputStageRunReviewRequestGeneration;
            MaterialStateService.InvalidateInputStageReviewVerification(
                _inputStageRunReviewSessionGeneration, _inputStageRunReviewRequestGeneration);
            _inputStageRunReviewGeometryToken = string.Empty;
            _inputStageRunReviewVerifiedContext = null;
            _inputStageRunReviewPendingGeometryContext = null;
            if (_inputStageRunReviewDialog != null && !_inputStageRunReviewDialog.IsDisposed)
                _inputStageRunReviewDialog.SetGeometryVerified(false, string.Empty);
            _inputStageRunReviewOffsetPending = false;
            _inputStageRunReviewPendingOffsetX = 0.0;
            _inputStageRunReviewPendingOffsetY = 0.0;
            _inputStageRunReviewPendingOffsetWaferId = string.Empty;
            _inputStageRunReviewPendingOffsetMappingRevision = string.Empty;
            _inputStageRunReviewPendingOffsetDieUid = string.Empty;
            _inputStageRunReviewPendingOffsetDieMapX = 0;
            _inputStageRunReviewPendingOffsetDieMapY = 0;
            _inputStageRunReviewPendingOffsetReferenceX = 0.0;
            _inputStageRunReviewPendingOffsetReferenceY = 0.0;
            _inputStageRunReviewPendingOffsetDraftSignature = string.Empty;
        }

        private async void StartInputStageRunReviewEmbeddedVisionAsync(
            InputStageRunReviewDialog dialog)
        {
            if (_inputStageRunReviewDrainingDialogs.Contains(dialog))
                return;
            if (_inputStageRunReviewEmbeddedVisionTransition)
                return;

            if (Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "활성 Review Manual 세션이 없어 Wafer Vision을 시작할 수 없습니다.");
                return;
            }

            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                dialog.SetBusy(false, "기존 Vision Test 화면을 먼저 종료하세요.");
                return;
            }

            if (_inputStageRunReviewEmbeddedVisionScope != null ||
                (dialog != null && dialog.IsWaferVisionControlActive))
            {
                dialog.SetBusy(true, "내장 Wafer Vision이 이미 사용 중입니다.");
                return;
            }

            if (Controller.IsInputStageRunReviewActionBusy ||
                _inputStageRunReviewVisionTestScope != null)
            {
                dialog.SetBusy(false, "다른 Review 수동 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                return;
            }

            if (QMC.CDT320.VisionComm.VisionViewerRegistry.IsStreaming(
                QMC.CDT_320.Equipment.Vision.VisionViewerPorts.Wafer))
            {
                dialog.SetBusy(false,
                    "다른 화면에서 Wafer Vision Live를 사용 중입니다. 해당 Live를 먼저 종료하세요.");
                return;
            }

            IDisposable scope = null;
            _inputStageRunReviewEmbeddedVisionTransition = true;
            try
            {
                dialog.SetBusy(true, "Wafer Vision 안전 영역을 확보하고 있습니다. STOP으로 취소할 수 있습니다.");
                scope = await Controller.BeginInputStageRunReviewWorkAsync(
                    ManualMotionScopeKind.ProcessSequence,
                    "Embedded Wafer Vision",
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (dialog == null ||
                    dialog.IsDisposed ||
                    !Controller.IsInputStageRunReviewManualActive ||
                    actionToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        "Wafer Vision 시작 전에 Review Manual 세션이 종료되었습니다.");
                }

                // Scope 대기 중 다른 화면이 Live를 시작했을 수 있으므로 명령 허용 직전에 다시 확인합니다.
                if (QMC.CDT320.VisionComm.VisionViewerRegistry.IsStreaming(
                    QMC.CDT_320.Equipment.Vision.VisionViewerPorts.Wafer))
                {
                    throw new InvalidOperationException(
                        "다른 화면에서 Wafer Vision Live를 사용 중입니다. 해당 Live를 먼저 종료하세요.");
                }

                _inputStageRunReviewEmbeddedVisionScope = scope;
                scope = null;

                // 이 버튼은 영상뿐 아니라 "맵 더블클릭 Die 이동"을 위한 모션 안전 Scope 확보 용도로도 쓰인다.
                // 따라서 비전 미연결이어도 차단하지 않되, Live/Grab/측정이 불가하다는 사실을 문구로 분명히 알린다.
                // (예전에는 미연결에도 "Live/Grab/측정 기능을 사용할 수 있습니다"로 표시해 연결된 것으로 오인됐다.)
                bool visionLinkReady = IsWaferVisionLinkReady();
                string scopeStatus = visionLinkReady
                    ? "Wafer Vision 안전 영역을 확보했습니다. 상단 Live/Grab/측정 기능을 사용할 수 있습니다."
                    : "안전 영역만 확보했습니다. Wafer Vision이 연결되지 않아 Live/Grab/측정은 동작하지 않습니다(좌표 이동만 가능).";
                if (!dialog.SetWaferVisionControlActive(true, scopeStatus))
                {
                    throw new InvalidOperationException("내장 Wafer Vision Viewer 구성에 실패했습니다.");
                }
                // [자동 Live 2026-08-17, 팀장님 지시] Scope 확보 직후 Live 영상을 켠다.
                // Live 중에도 액션 버튼은 사용 가능하며(UpdateActionAvailability), DIE DETECTION만
                // 자체적으로 Live를 잠시 멈췄다가 복귀시킨다.
                bool liveStarted = visionLinkReady && dialog.StartWaferVisionLive();
                dialog.SetBusy(true, !visionLinkReady
                    ? "안전 영역 사용 중입니다(영상 불가). 종료 또는 STOP 후 다른 Review 동작을 실행하세요."
                    : liveStarted
                        ? "Wafer Vision Live 사용 중입니다. 아래 버튼들을 그대로 사용할 수 있습니다."
                        : "Wafer Vision 사용 중입니다(Live 자동 시작 실패 — 상단 Live 버튼으로 켜세요).");
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope를 시작했습니다. visionLink=" +
                    (visionLinkReady ? "READY" : "NOT-READY") + " - Start");
                if (!visionLinkReady)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning, "UI", "IN-REVIEW-VISION-SCOPE-NO-LINK",
                        "InputStageRunReview",
                        "Wafer Vision 미연결 상태로 안전 Scope만 확보했습니다. Live/Grab/측정은 동작하지 않습니다.");
                }
            }
            catch (OperationCanceledException)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewEmbeddedVisionScope();
                if (dialog != null && !dialog.IsDisposed)
                {
                    dialog.SetWaferVisionControlActive(false, string.Empty);
                    dialog.SetBusy(false, "Wafer Vision 시작이 STOP/취소되었습니다.");
                }
            }
            catch (Exception ex)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewEmbeddedVisionScope();
                if (dialog != null && !dialog.IsDisposed)
                {
                    dialog.SetWaferVisionControlActive(false, string.Empty);
                    dialog.SetBusy(false, "Wafer Vision 시작 실패: " + ex.Message);
                }
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 시작 실패: " + ex.Message + " - Failed");
            }
            finally
            {
                _inputStageRunReviewEmbeddedVisionTransition = false;
            }
        }

        private void StopInputStageRunReviewEmbeddedVision(
            InputStageRunReviewDialog dialog,
            bool restoreViewer,
            string status,
            bool stopConfirmed = false)
        {
            if (!ReferenceEquals(dialog, _inputStageRunReviewDialog))
                return;
            if (_inputStageRunReviewDrainingDialogs.Contains(dialog) && !stopConfirmed)
            {
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetStatusMessage("Review 동작 종료가 아직 확인되지 않았습니다. STOP 및 장비 상태를 확인하세요.");
                return;
            }
            InputStageRunReviewOneShotState oneShot = _inputStageRunReviewOneShot;
            if (oneShot != null && ReferenceEquals(oneShot.Dialog, dialog) && !oneShot.Completion.Task.IsCompleted)
            {
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetStatusMessage("진행 중인 Review 동작을 STOP으로 종료한 뒤 비전 사용을 종료하세요.");
                return;
            }
            try
            {
                // CAM_SWITCH OFF와 Viewer 수신 Thread 정지를 먼저 완료한 뒤 Resource Lease를 반환합니다.
                if (dialog != null && !dialog.IsDisposed)
                    dialog.StopWaferVision();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 정지 실패: " + ex.Message + " - Failed");
            }
            finally
            {
                DisposeInputStageRunReviewEmbeddedVisionScope();
                _inputStageRunReviewEmbeddedVisionTransition = false;
                _inputStageRunReviewDrainingDialogs.Remove(dialog);
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetReviewStopPending(false);
            }

            if (restoreViewer && dialog != null && !dialog.IsDisposed)
            {
                dialog.SetWaferVisionControlActive(false, status);
                bool busy = Controller != null && Controller.IsInputStageRunReviewActionBusy;
                dialog.SetBusy(busy, status);
            }
        }

        private void DisposeInputStageRunReviewEmbeddedVisionScope()
        {
            IDisposable scope = _inputStageRunReviewEmbeddedVisionScope;
            _inputStageRunReviewEmbeddedVisionScope = null;
            if (scope == null)
                return;

            try
            {
                scope.Dispose();
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope를 종료했습니다. - End");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    UserSession.Name,
                    "InputStageRunReviewVision",
                    "내장 Wafer Vision 안전 Scope 종료 실패: " + ex.Message + " - Failed");
            }
        }

        private async void OpenInputStageRunReviewVisionTest(InputStageRunReviewDialog dialog)
        {
            if (_inputStageRunReviewDrainingDialogs.Contains(dialog))
                return;
            if (Controller == null || !Controller.IsInputStageRunReviewManualActive)
            {
                if (dialog != null)
                    dialog.SetBusy(false, "활성 Review Manual 세션이 없습니다.");
                return;
            }

            if (_inputStageRunReviewEmbeddedVisionScope != null ||
                (dialog != null && dialog.IsWaferVisionControlActive))
            {
                dialog.SetBusy(true, "내장 Wafer Vision을 먼저 종료한 뒤 Vision Test를 실행하세요.");
                return;
            }

            // 미연결 상태로 Vision Test 창을 열면 안전 Scope와 lease만 점유한 채
            // Review의 JOG/ACTION 전체가 창을 닫을 때까지 봉쇄된다. 진입 전에 막는다.
            if (!IsWaferVisionLinkReady())
            {
                if (dialog != null)
                    dialog.SetBusy(false,
                        "Wafer Vision이 연결되지 않아 Vision Test를 실행할 수 없습니다. 설정에서 비전 연결/사용을 확인하세요.");
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "UI", "IN-REVIEW-VISION-TEST-NO-LINK",
                    "InputStageRunReview",
                    "Vision Test 진입 거부: Wafer Vision 미연결(불필요한 Scope/lease 점유 방지).");
                return;
            }

            if (_inputStageRunReviewVisionTestDialog != null &&
                !_inputStageRunReviewVisionTestDialog.IsDisposed)
            {
                _inputStageRunReviewVisionTestDialog.BringToFront();
                _inputStageRunReviewVisionTestDialog.Activate();
                dialog.SetBusy(true, "Vision Test 화면이 열려 있습니다. 종료하거나 STOP을 누르세요.");
                return;
            }

            if (Controller.IsInputStageRunReviewActionBusy || _inputStageRunReviewVisionTestScope != null)
            {
                dialog.SetBusy(false, "다른 Review 수동 동작이 진행 중입니다. STOP 후 다시 시도하세요.");
                return;
            }

            IDisposable scope = null;
            try
            {
                dialog.SetBusy(true, "Vision Test 안전 영역을 확보하고 있습니다. STOP으로 취소할 수 있습니다.");
                scope = await Controller.BeginInputStageRunReviewWorkAsync(
                    ManualMotionScopeKind.ProcessSequence,
                    "Vision Test",
                    System.Threading.CancellationToken.None).ConfigureAwait(true);

                System.Threading.CancellationToken actionToken = Controller.InputStageRunReviewActionToken;
                if (dialog.IsDisposed ||
                    !Controller.IsInputStageRunReviewManualActive ||
                    actionToken.IsCancellationRequested)
                    throw new OperationCanceledException("Vision Test 시작 전에 Review Manual 세션이 종료되었습니다.");

                _inputStageRunReviewVisionTestScope = scope;
                scope = null;
                WaferVisionTestDialog visionDialog = WaferVisionTestDialog.OpenReview(
                    dialog,
                    Controller.InputStageRunReviewActionToken);
                _inputStageRunReviewVisionTestDialog = visionDialog;
                visionDialog.FormClosed += InputStageRunReviewVisionTestDialog_FormClosed;
                dialog.SetBusy(true, "Vision Test 화면이 열려 있습니다. 종료하거나 STOP을 누르세요.");
            }
            catch (OperationCanceledException)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewVisionTestScope();
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Vision Test 시작이 STOP/취소되었습니다.");
            }
            catch (Exception ex)
            {
                if (scope != null)
                    scope.Dispose();
                DisposeInputStageRunReviewVisionTestScope();
                if (dialog != null && !dialog.IsDisposed)
                    dialog.SetBusy(false, "Vision Test 시작 실패: " + ex.Message);
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                    "Vision Test 시작 실패: " + ex.Message + " - Failed");
            }
        }

        private void InputStageRunReviewVisionTestDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            WaferVisionTestDialog visionDialog = sender as WaferVisionTestDialog;
            if (visionDialog != null)
                visionDialog.FormClosed -= InputStageRunReviewVisionTestDialog_FormClosed;

            if (ReferenceEquals(_inputStageRunReviewVisionTestDialog, visionDialog))
                _inputStageRunReviewVisionTestDialog = null;
            DisposeInputStageRunReviewVisionTestScope();

            InputStageRunReviewDialog reviewDialog = _inputStageRunReviewDialog;
            if (reviewDialog != null && !reviewDialog.IsDisposed &&
                Controller != null && Controller.IsInputStageRunReviewManualActive)
            {
                reviewDialog.SetBusy(false, "Vision Test 화면을 종료했습니다.");
            }
            else if (reviewDialog != null && !reviewDialog.IsDisposed)
            {
                // Global STOP/Coordinator 종료 중에는 자식 창이 완전히 닫힌 뒤 부모 Review를 닫는다.
                reviewDialog.CloseFromSequence();
            }
        }

        private void DisposeInputStageRunReviewVisionTestScope()
        {
            IDisposable scope = _inputStageRunReviewVisionTestScope;
            _inputStageRunReviewVisionTestScope = null;
            if (scope != null)
            {
                try { scope.Dispose(); }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReviewVisionTest",
                        "Vision Test Review 작업 스코프 종료 실패: " + ex.Message + " - Failed");
                }
            }
        }

        private void StartRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.StartRunReviewBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.On();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 시작 실패: " + ex.Message + " - Failed");
            }
        }

        private void StopRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.StopBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.Off();

                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자가 리뷰 화면에서 부저 정지를 요청했습니다. - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 정지 실패: " + ex.Message + " - Failed");
            }
        }

        private void EndRunReviewBuzzer()
        {
            try
            {
                if (OpPanelMonitor != null)
                    OpPanelMonitor.EndRunReviewBuzzer();
                else
                    Machine?.OpPanelUnit?.Buzzer?.Off();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", UserSession.Name, "InputStageRunReview",
                    "사용자 확인 부저 종료 처리 실패: " + ex.Message + " - Failed");
            }
        }

        private void OnVisionHubChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnVisionHubChanged)); return; }
            // VisionHub 연결 상태를 상단 VIS 표시로 반영합니다.
            bool connected = QMC.CDT320.VisionComm.VisionHub.AllConnected;
            // Vision 연결 상태는 상단 VISION 점등으로만 표시한다.
            // (이전에는 Barcode Name 라벨을 "VIS O/X"로 덮어써서 진행 웨이퍼 바코드명을 볼 수 없었다.)
            // 상단 VISION 점등을 실제 연결 상태에 동기화(끊기면 소등).
            if (dotVision != null) dotVision.IsOn = connected;

            // Review 창이 열려 있으면 Wafer 채널 연결 상태를 전달한다.
            // (창은 개창 시 스냅샷만 갖고 있어서, 이후 끊기거나 재연결되어도 반영되지 않았다.)
            NotifyInputStageRunReviewVisionLink();
        }

        /// <summary>열려 있는 Review 창에 Wafer Vision 연결 상태를 전달한다.</summary>
        private void NotifyInputStageRunReviewVisionLink()
        {
            try
            {
                InputStageRunReviewDialog dialog = _inputStageRunReviewDialog;
                if (dialog == null || dialog.IsDisposed)
                    return;

                dialog.SetWaferVisionConnectionState(IsWaferVisionLinkReady());
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "UI", "IN-REVIEW-VISION-LINK",
                    "InputStageRunReview",
                    "Review 창에 Wafer Vision 연결 상태 전달 실패: " + ex.Message);
            }
        }

    }
}
