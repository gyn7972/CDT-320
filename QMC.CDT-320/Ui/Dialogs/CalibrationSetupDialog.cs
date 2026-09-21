using System;
using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class CalibrationSetupDialog : Form, ILocalizedView
    {
        private readonly string _title;
        private readonly string _purpose;
        private readonly string _storageGuide;
        private bool _busy;

        private void InitializeLocalization()
        {
            Lang.BindKey(lblTitle, "calibration.calibrationsetup.lblTitle");
            Lang.BindKey(lblPurposeHeader, "calibration.calibrationsetup.lblPurposeHeader");
            Lang.BindKey(lblStorageHeader, "calibration.calibrationsetup.lblStorageHeader");
            Lang.BindKey(lblStatusHeader, "calibration.calibrationsetup.lblStatusHeader");
            Lang.BindKey(btnCheck, "calibration.calibrationsetup.btnCheck");
            Lang.BindKey(btnClose, "calibration.calibrationsetup.btnClose");
            Lang.BindKey(this, "calibration.calibrationsetup.this");
        }

        public void ApplyLanguage()
        {
            // 언어 변경은 표시만 무효화하며 선택/입력/설정값을 다시 불러오지 않습니다.
            Invalidate(true);
        }

        public CalibrationSetupDialog(string title, string purpose, string storageGuide)
        {
            try
            {
                _title = string.IsNullOrWhiteSpace(title) ? "CALIBRATION" : title;
                _purpose = purpose ?? string.Empty;
                _storageGuide = storageGuide ?? string.Empty;

                InitializeComponent();
                InitializeLocalization();
                ApplyText();
                WireEvents();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-INIT", "Calibration 설정창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void ApplyText()
        {
            CalibrationDialogText.BindStatus(this, _title);
            CalibrationDialogText.BindStatus(lblTitle, _title);
            CalibrationDialogText.BindStatus(lblPurpose, _purpose);
            CalibrationDialogText.BindStatus(lblStorageGuide, _storageGuide);
            Lang.BindFormat(lblStatus, "calibration.status.s218");
        }

        private void WireEvents()
        {
            try
            {
                btnCheck.Click += btnCheck_Click;
                btnClose.Click += delegate { Close(); };
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-EVENT", _title + " 이벤트 연결 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    CalibrationDialogText.BindStatus(lblStatus, reason);
                    QMC.Common.MessageDialog.Show(this, reason, _title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Lang.BindFormat(lblStatus, "calibration.status.s219");
            }
            catch (Exception ex)
            {
                string message = _title + " 상태 확인 중 예외가 발생했습니다: " + ex.Message;
                CalibrationDialogText.BindStatus(lblStatus, message);
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-CHECK", message);
                QMC.Common.MessageDialog.Show(this, message, _title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private bool CanRunManualCalibration(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = Owner as Form1;
                if (host == null)
                    host = FindHostForm();

                if (host == null || host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않아 캘리브레이션을 실행할 수 없습니다.";
                    return false;
                }

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 캘리브레이션을 실행하세요.";
                    return false;
                }

                EquipmentStatus status = host.Controller.Status;
                if (status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 캘리브레이션을 실행할 수 없습니다.";
                    return false;
                }

                if (status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 캘리브레이션을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "캘리브레이션 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    Form1 host = form as Form1;
                    if (host != null)
                        return host;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            try
            {
                btnCheck.Enabled = enabled;
                btnClose.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}

namespace QMC.CDT_320.Ui.Dialogs
{
    internal static class CalibrationDialogText
    {
        private static readonly System.Collections.Generic.Dictionary<string, string> Keys =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "  Load Interval", "calibration.item.load_interval" },
                { "  Process Interval", "calibration.item.process_interval" },
                { "  Process Unit", "calibration.item.process_unit" },
                { "1) MOVE READY  2) USE CURRENT  3) MOVE TEACH  4) START CAL", "calibration.needlepin.status" },
                { "ACTUAL", "calibration.item.actual.upper" },
                { "ALL", "calibration.item.all.upper" },
                { "APPLY T\r\nHOME", "calibration.item.apply_t_home.upper" },
                { "AUTO CALIBRATION", "calibration.item.auto_calibration.upper" },
                { "AVOID 복귀", "calibration.colletchange.btnAvoid" },
                { "Admin 권한에서만 Needle Pin Calibration 결과값을 수동 수정할 수 있습니다.", "calibration.status.s171" },
                { "Arrive Dwell", "calibration.item.arrive_dwell" },
                { "Auto Calibration 사용 설정을 저장했습니다.", "calibration.status.s140" },
                { "Auto Calibration 설정 저장 대상이 없습니다.", "calibration.status.s137" },
                { "Auto Calibration 설정을 불러올 수 없습니다.", "calibration.status.s134" },
                { "Auto Calibration 설정을 장비 Config에 저장하지 못했습니다.", "calibration.status.s139" },
                { "Auto Calibration 전체 작업을 완료했습니다.", "calibration.status.s122" },
                { "Auto Calibration 정지 요청을 보냈습니다.", "calibration.status.s142" },
                { "Auto Calibration을 실행 중입니다.", "calibration.status.s120" },
                { "Auto Calibration이 실패했습니다. Alarm/Event Log와 진행 이력을 확인하세요.", "calibration.status.s121" },
                { "Auto Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m010" },
                { "Auto Calibration이 정지 요청으로 중단되었습니다.", "calibration.status.s123" },
                { "Auto calibration is running. Request a stop before closing the window?", "calibration.message.m010" },
                { "AutoFocus", "calibration.item.autofocus" },
                { "Axis", "calibration.item.axis" },
                { "BATCH (선택 Output 고정 · 직렬 측정: Front P4→P1, Rear P4→P1)", "calibration.placez.batchGroup" },
                { "BATCH (일괄 콜렛 캘리브레이션 · 각 side C4 → C3 → C2 → C1 순, 측정 후 COC 연속)", "calibration.collet.batchGroup" },
                { "BATCH (직렬 측정: Front P4→P1, Rear P4→P1)", "calibration.pickupz.batchGroup" },
                { "BATCH START", "calibration.item.batch_start.upper" },
                { "BATCH 실행할 콜렛이 선택되지 않았습니다. F/R C1~C4 체크박스를 선택하세요.", "calibration.status.s023" },
                { "BATCH가 정지 요청으로 중단되었습니다.", "calibration.status.s027" },
                { "BackOff Distance", "calibration.item.backoff_distance" },
                { "Batch로 측정할 Picker를 하나 이상 선택하세요.", "calibration.status.s066" },
                { "Blow Pulse Time", "calibration.item.blow_pulse_time" },
                { "Blow Settle Time", "calibration.item.blow_settle_time" },
                { "CAL SETTING", "calibration.item.cal_setting.upper" },
                { "CALIBRATION", "calibration.item.calibration.upper" },
                { "CHECK", "calibration.item.check.upper" },
                { "CHECK READY", "calibration.item.check_ready.upper" },
                { "CLEAR ALL", "calibration.item.clear_all.upper" },
                { "CLOSE", "calibration.item.close.upper" },
                { "COC", "calibration.item.coc.upper" },
                { "COC \r\nRE-CAL", "calibration.item.coc_re_cal.upper" },
                { "COC START", "calibration.item.coc_start.upper" },
                { "COC START로 선택 Collet의 1차 회전 중심을 먼저 검출하세요.", "calibration.status.s015" },
                { "COC T Speed", "calibration.item.coc_t_speed" },
                { "COC Xmm", "calibration.item.coc_xmm" },
                { "COC Xpx", "calibration.item.coc_xpx" },
                { "COC Ymm", "calibration.item.coc_ymm" },
                { "COC Ypx", "calibration.item.coc_ypx" },
                { "COC 실패. Alarm/Event Log를 확인하세요.", "calibration.status.s019" },
                { "COC가 정지 요청으로 중단되었습니다.", "calibration.status.s020" },
                { "COLLET", "calibration.item.collet.upper" },
                { "COLLET BATCH", "calibration.message.m003" },
                { "COLLET CAL", "calibration.message.m000" },
                { "COLLET CAL 사용", "calibration.auto.chkColletCal" },
                { "COLLET CALIBRATION", "calibration.item.collet_calibration.upper" },
                { "COLLET CHANGE MODE", "calibration.item.collet_change_mode.upper" },
                { "COLLET CLEANING", "calibration.item.collet_cleaning.upper" },
                { "COLLET CLEANING HISTORY", "calibration.item.collet_cleaning_history.upper" },
                { "COLLET COC", "calibration.message.m002" },
                { "CURRENT POSITION", "calibration.item.current_position.upper" },
                { "Calibration", "calibration.item.calibration" },
                { "Calibration Valid", "calibration.item.calibration_valid" },
                { "Cannot move to the collet replacement position during automatic operation.\r\nStop operation and try again.", "calibration.message.m024" },
                { "Cap Near Touch Offset", "calibration.item.cap_near_touch_offset" },
                { "Cap Search 100um Max", "calibration.item.cap_search_100um_max" },
                { "Cap Search 10um Max", "calibration.item.cap_search_10um_max" },
                { "Cap Search 1um Max", "calibration.item.cap_search_1um_max" },
                { "Clean Z Acc", "calibration.item.clean_z_acc" },
                { "Clean Z Dec", "calibration.item.clean_z_dec" },
                { "Clean Z Speed", "calibration.item.clean_z_speed" },
                { "Coarse Search Acc", "calibration.item.coarse_search_acc" },
                { "Coarse Search Dec", "calibration.item.coarse_search_dec" },
                { "Coarse Search Speed", "calibration.item.coarse_search_speed" },
                { "Collet Calibration 설정과 저장값을 다시 불러왔습니다.", "calibration.status.s007" },
                { "Collet Calibration 실패. Alarm/Event Log를 확인하세요.", "calibration.status.s010" },
                { "Collet Calibration 정지 요청으로 중단되었습니다.", "calibration.status.s012" },
                { "Collet Calibration 파라미터를 저장했습니다. 측정 결과와 Picker 티칭값은 변경하지 않았습니다.", "calibration.status.s041" },
                { "Collet Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m001" },
                { "Collet No", "calibration.item.collet_no" },
                { "Collet calibration is running. Request a stop before closing the window?", "calibration.message.m001" },
                { "Collet replacement", "calibration.message.m022" },
                { "Contact Offset", "calibration.item.contact_offset" },
                { "Contact Z Offset", "calibration.item.contact_z_offset" },
                { "DIE", "calibration.item.die.upper" },
                { "Diagonal", "calibration.item.diagonal" },
                { "Die", "calibration.item.die" },
                { "Die Height", "calibration.item.die_height" },
                { "Die Thickness", "calibration.item.die_thickness" },
                { "Disable On Replace", "calibration.item.disable_on_replace" },
                { "EjectPinZ / NeedleCap", "calibration.item.ejectpinz_needlecap" },
                { "EjectPinZ Cal", "calibration.item.ejectpinz_cal" },
                { "EjectPinZ Cal Position", "calibration.item.ejectpinz_cal_position" },
                { "ExpanderZ Process", "calibration.item.expanderz_process" },
                { "FILM", "calibration.item.film.upper" },
                { "FINAL X", "calibration.item.final_x.upper" },
                { "FINAL Y", "calibration.item.final_y.upper" },
                { "FINAL Z", "calibration.item.final_z.upper" },
                { "FLOW Z", "calibration.item.flow_z.upper" },
                { "FRONT PICKER", "calibration.item.front_picker.upper" },
                { "Fail If Flow Already On", "calibration.item.fail_if_flow_already_on" },
                { "False", "calibration.item.false" },
                { "Film Height", "calibration.item.film_height" },
                { "Film Thickness", "calibration.item.film_thickness" },
                { "Finder", "calibration.item.finder" },
                { "Fine Search Acc", "calibration.item.fine_search_acc" },
                { "Fine Search Dec", "calibration.item.fine_search_dec" },
                { "Fine Search Speed", "calibration.item.fine_search_speed" },
                { "Flat Collet Offset", "calibration.item.flat_collet_offset" },
                { "Flow Off Confirm Timeout", "calibration.item.flow_off_confirm_timeout" },
                { "Flow Poll Interval", "calibration.item.flow_poll_interval" },
                { "Flow Stable", "calibration.item.flow_stable" },
                { "Form1/Controller를 찾을 수 없습니다.", "calibration.status.s176" },
                { "Front", "calibration.item.front" },
                { "Front/Rear Picker Y Avoid 이동 완료.", "calibration.status.s035" },
                { "Front/Rear 픽커 Z 전체 Avoid 이동 완료.", "calibration.status.s030" },
                { "FrontPickerX Avoid", "calibration.item.frontpickerx_avoid" },
                { "Good", "calibration.item.good" },
                { "Good/NG Output Stage 제품 확인 센서를 찾을 수 없어 PlaceZ Calibration을 시작하지 않습니다.", "calibration.status.s131" },
                { "INPUT 쪽", "calibration.colletchange.rdoInputSide" },
                { "ITEM", "calibration.item.item.upper" },
                { "InputStage Recipe/Setup이 없습니다.", "calibration.status.s175" },
                { "InputStageUnit을 찾을 수 없습니다.", "calibration.status.s158" },
                { "InputStageUnit을 찾을 수 없습니다. CalibrationPage에서 다시 열어 주세요.", "calibration.status.s168" },
                { "LAST CLEANED", "calibration.item.last_cleaned.upper" },
                { "MOVE READY", "calibration.item.move_ready.upper" },
                { "MOVE START", "calibration.item.move_start.upper" },
                { "MOVE TEACH", "calibration.item.move_teach.upper" },
                { "MOVE TOUCH", "calibration.item.move_touch.upper" },
                { "Max Extra Press", "calibration.item.max_extra_press" },
                { "Move Acc", "calibration.item.move_acc" },
                { "Move Avoid After Cal", "calibration.item.move_avoid_after_cal" },
                { "Move Avoid After Scan", "calibration.item.move_avoid_after_scan" },
                { "Move Dec", "calibration.item.move_dec" },
                { "Move Speed", "calibration.item.move_speed" },
                { "Move Timeout", "calibration.item.move_timeout" },
                { "NEEDLE CAL", "calibration.message.m014" },
                { "NEEDLE PIN CAL", "calibration.item.needle_pin_cal.upper" },
                { "NEEDLE Z CAL", "calibration.item.needle_z_cal.upper" },
                { "NG", "calibration.item.ng.upper" },
                { "Needle Calibration 설정을 불러올 수 없습니다.", "calibration.status.s148" },
                { "Needle Calibration 정지 요청을 보냈습니다. 축 정지 로그를 확인하세요.", "calibration.status.s146" },
                { "Needle CalibrationData를 찾을 수 없습니다.", "calibration.status.s152" },
                { "Needle Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m013" },
                { "Needle Pin Calibration", "calibration.item.needle_pin_calibration" },
                { "Needle Pin Calibration 실행 중입니다.", "calibration.status.s194" },
                { "Needle Pin Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m016" },
                { "Needle Pin Calibration이 정지 요청으로 중단되었습니다.", "calibration.status.s197" },
                { "Needle Z Calibration", "calibration.item.needle_z_calibration" },
                { "Needle Z Calibration 설정값을 저장했습니다.", "calibration.status.s153" },
                { "Needle Z Calibration이 정지 요청으로 중단되었습니다.", "calibration.status.s165" },
                { "Needle calibration is running. Request a stop before closing the window?", "calibration.message.m013" },
                { "Needle pin calibration is running. Request a stop before closing the window?", "calibration.message.m016" },
                { "NeedleCap Teaching Position", "calibration.item.needlecap_teaching_position" },
                { "NeedleCapTouchPosition", "calibration.item.needlecaptouchposition" },
                { "NeedlePin Teaching Position", "calibration.item.needlepin_teaching_position" },
                { "NeedlePinFlushPosition", "calibration.item.needlepinflushposition" },
                { "NeedlePinReadyPosition", "calibration.item.needlepinreadyposition" },
                { "NeedleX", "calibration.item.needlex" },
                { "NeedleX Cal", "calibration.item.needlex_cal" },
                { "NeedleX Cal Position", "calibration.item.needlex_cal_position" },
                { "NeedleX To VisionX", "calibration.item.needlex_to_visionx" },
                { "NeedleY To VisionY", "calibration.item.needley_to_visiony" },
                { "NeedleZ", "calibration.item.needlez" },
                { "NeedleZ Cal", "calibration.item.needlez_cal" },
                { "NeedleZ Cal Position", "calibration.item.needlez_cal_position" },
                { "OFF", "calibration.item.off.upper" },
                { "OFFSET X", "calibration.item.offset_x.upper" },
                { "OFFSET Y", "calibration.item.offset_y.upper" },
                { "OK", "calibration.item.ok.upper" },
                { "OLD PICK", "calibration.item.old_pick.upper" },
                { "OLD PLACE", "calibration.item.old_place.upper" },
                { "ON", "calibration.item.on.upper" },
                { "OUTPUT", "calibration.item.output.upper" },
                { "OUTPUT 쪽", "calibration.colletchange.rdoOutputSide" },
                { "Output", "calibration.item.output" },
                { "Output Place 위치 위에 Picker를 위치시킨 후 START SCAN을 실행하세요.", "calibration.placez.status" },
                { "Output Stage Material Data 확인에서 사용자가 진행을 취소했습니다.", "calibration.status.s133" },
                { "Output Stage 제품 센서 확인에서 사용자가 진행을 취소했습니다.", "calibration.status.s132" },
                { "OutputCameraX Avoid", "calibration.item.outputcamerax_avoid" },
                { "OutputStageUnit이 없어 PlaceZ Calibration 준비 상태를 확인할 수 없습니다.", "calibration.status.s130" },
                { "P-Y AVOID", "calibration.item.p_y_avoid.upper" },
                { "PARAMETER", "calibration.item.parameter.upper" },
                { "PARAMETER SAVE", "calibration.item.parameter_save.upper" },
                { "PICK Z", "calibration.item.pick_z.upper" },
                { "PICK Z CAL readiness check", "calibration.message.m011" },
                { "PICK Z CAL 사용", "calibration.auto.chkPickZCal" },
                { "PICK Z CAL 준비 확인", "calibration.message.m011" },
                { "PICKER", "calibration.item.picker.upper" },
                { "PICKUP Z CAL", "calibration.item.pickup_z_cal.upper" },
                { "PLACE Z", "calibration.item.place_z.upper" },
                { "PLACE Z CAL", "calibration.item.place_z_cal.upper" },
                { "PLACE Z CAL readiness check", "calibration.message.m012" },
                { "PLACE Z CAL 사용", "calibration.auto.chkPlaceZCal" },
                { "PLACE Z CAL 준비 확인", "calibration.message.m012" },
                { "PickUp Z Calibration", "calibration.item.pickup_z_calibration" },
                { "PickUp Z Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m006" },
                { "PickUp Z calibration is running. Request a stop before closing the window?", "calibration.message.m006" },
                { "PickUpZ Batch가 정지 요청으로 중단되었습니다.", "calibration.status.s108" },
                { "PickUpZ Calibration 설정값을 저장했습니다.", "calibration.status.s092" },
                { "PickUpZ Calibration 정지 요청을 보냈습니다. Z축 정지 로그를 확인하세요.", "calibration.status.s117" },
                { "PickUpZ CalibrationData를 찾을 수 없습니다.", "calibration.status.s090" },
                { "PickUpZ Calibration이 정지 요청으로 중단되었습니다.", "calibration.status.s113" },
                { "Picker Material Data 확인에서 사용자가 진행을 취소했습니다.", "calibration.status.s128" },
                { "Picker No", "calibration.item.picker_no" },
                { "Picker Y Avoid 이동 실패. Alarm/Event Log를 확인하세요.", "calibration.status.s036" },
                { "Picker Y Avoid 이동 전 Front/Rear 픽커 Z 전체를 먼저 Avoid 위치로 이동하세요. (Z-AVOID 버튼)", "calibration.status.s034" },
                { "Picker Y Avoid 이동이 정지 요청으로 중단되었습니다.", "calibration.status.s037" },
                { "Picker 제품 유무와 시작 조건을 확인하고 있습니다.", "calibration.status.s119" },
                { "Picker에 제품이 감지되어 시작하지 않습니다.", "calibration.status.s127" },
                { "Pin Ready Below Flush", "calibration.item.pin_ready_below_flush" },
                { "Pin Search 10um Max", "calibration.item.pin_search_10um_max" },
                { "Pin Search 1um Max", "calibration.item.pin_search_1um_max" },
                { "Place On Cleaned Cell", "calibration.item.place_on_cleaned_cell" },
                { "Place Z Calibration", "calibration.item.place_z_calibration" },
                { "Place Z Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?", "calibration.message.m004" },
                { "Place Z calibration is running. Request a stop before closing the window?", "calibration.message.m004" },
                { "PlaceZ Batch가 정지 요청으로 중단되었습니다.", "calibration.status.s075" },
                { "PlaceZ Calibration 설정값을 저장했습니다.", "calibration.status.s051" },
                { "PlaceZ Calibration 정지 요청을 보냈습니다. Z축 정지 로그를 확인하세요.", "calibration.status.s086" },
                { "PlaceZ CalibrationData를 찾을 수 없습니다.", "calibration.status.s047" },
                { "PlaceZ Calibration이 정지 요청으로 중단되었습니다.", "calibration.status.s080" },
                { "Position Offset X", "calibration.item.position_offset_x" },
                { "Position Offset Y", "calibration.item.position_offset_y" },
                { "Press Count", "calibration.item.press_count" },
                { "REAR PICKER", "calibration.item.rear_picker.upper" },
                { "RELOAD", "calibration.item.reload.upper" },
                { "RESULT", "calibration.item.result.upper" },
                { "RETRY", "calibration.item.retry.upper" },
                { "RUN LOG", "calibration.item.run_log.upper" },
                { "Ready 위치 이동 완료. Needle/Camera를 조그로 맞춘 뒤 USE CURRENT로 티칭하세요.", "calibration.status.s185" },
                { "Ready 위치 이동이 정지 요청으로 중단되었습니다.", "calibration.status.s187" },
                { "Ready 위치로 이동 중입니다. OutputCamera/Picker는 Avoid, InputStage는 Process로 이동합니다.", "calibration.status.s184" },
                { "Rear", "calibration.item.rear" },
                { "RearPickerX Avoid", "calibration.item.rearpickerx_avoid" },
                { "Repeat Count", "calibration.item.repeat_count" },
                { "Repeat Lift Height", "calibration.item.repeat_lift_height" },
                { "Repeat Tolerance", "calibration.item.repeat_tolerance" },
                { "Retry On NG", "calibration.item.retry_on_ng" },
                { "Rim Collet Offset", "calibration.item.rim_collet_offset" },
                { "Rim Height", "calibration.item.rim_height" },
                { "SAVE", "calibration.item.save.upper" },
                { "SAVE BOT.\r\nPOS", "calibration.item.save_bot_pos.upper" },
                { "SAVE HISTORY", "calibration.item.save_history.upper" },
                { "SAVE RESULT", "calibration.item.save_result.upper" },
                { "SAVED COLLET OFFSET", "calibration.item.saved_collet_offset.upper" },
                { "SAVED PICK", "calibration.item.saved_pick.upper" },
                { "SAVED PICKUP Z", "calibration.item.saved_pickup_z.upper" },
                { "SAVED PLACE", "calibration.item.saved_place.upper" },
                { "SAVED PLACE Z", "calibration.item.saved_place_z.upper" },
                { "SAVED RESULT", "calibration.item.saved_result.upper" },
                { "SELECT ALL", "calibration.item.select_all.upper" },
                { "SEQ STOP", "calibration.item.seq_stop.upper" },
                { "SETTING", "calibration.item.setting.upper" },
                { "SIDE", "calibration.item.side.upper" },
                { "START", "calibration.item.start.upper" },
                { "START CAL", "calibration.item.start_cal.upper" },
                { "START SCAN", "calibration.item.start_scan.upper" },
                { "START Z", "calibration.item.start_z.upper" },
                { "STOP", "calibration.item.stop.upper" },
                { "Score Min", "calibration.item.score_min" },
                { "Search Max Distance", "calibration.item.search_max_distance" },
                { "Search Start Offset", "calibration.item.search_start_offset" },
                { "Side", "calibration.item.side" },
                { "Side AutoFocus", "calibration.item.side_autofocus" },
                { "StageT Process", "calibration.item.staget_process" },
                { "StageY", "calibration.item.stagey" },
                { "StageY Process", "calibration.item.stagey_process" },
                { "StageY Process Position", "calibration.item.stagey_process_position" },
                { "Start Z", "calibration.item.start_z" },
                { "T Teaching", "calibration.item.t_teaching" },
                { "T Teaching을 포함한 Collet Calibration 파라미터를 저장하지 못했습니다. 저장 상태를 확인한 후 다시 시도하세요.", "calibration.status.s040" },
                { "T ZERO", "calibration.item.t_zero.upper" },
                { "TARGET", "calibration.item.target.upper" },
                { "TARGET (선택 콜렛 일괄 클리닝 · 각 side C4 → C3~1 순, 전부 클린 후 전부 검사)", "calibration.colletcleaning.targetGroup" },
                { "TEACHING DATA", "calibration.item.teaching_data.upper" },
                { "THETA", "calibration.item.theta.upper" },
                { "TOTAL", "calibration.item.total.upper" },
                { "The equipment is not ready.", "calibration.message.m023" },
                { "The selected replacement X position is 0.000 mm (it may not have been taught).\r\nMove anyway?", "calibration.message.m021" },
                { "Theta Gain", "calibration.item.theta_gain" },
                { "Theta Retry", "calibration.item.theta_retry" },
                { "Theta Tol", "calibration.item.theta_tol" },
                { "Touch NeedleX Position", "calibration.item.touch_needlex_position" },
                { "Touch Poll Interval", "calibration.item.touch_poll_interval" },
                { "Touch Sensor", "calibration.item.touch_sensor" },
                { "Touch Stable", "calibration.item.touch_stable" },
                { "Touch StageY Position", "calibration.item.touch_stagey_position" },
                { "Trig Auto Start", "calibration.item.trig_auto_start" },
                { "Trig Good Bin Load", "calibration.item.trig_good_bin_load" },
                { "Trig Process Count", "calibration.item.trig_process_count" },
                { "True", "calibration.item.true" },
                { "UNIT", "calibration.item.unit.upper" },
                { "USE CURRENT", "calibration.item.use_current.upper" },
                { "USE CURRENT로 티칭 위치를 확인하고 START CAL을 실행하세요.", "calibration.needle.status" },
                { "Updated", "calibration.item.updated" },
                { "VAC OFF", "calibration.item.vac_off.upper" },
                { "VAC OFF\r\nFLOW ?", "calibration.status.s089" },
                { "VAC OFF\r\nFLOW OFF", "calibration.status.s088" },
                { "VAC OFF\r\nFLOW ON", "calibration.status.s087" },
                { "VALID", "calibration.item.valid.upper" },
                { "VALUE", "calibration.item.value.upper" },
                { "Vacuum On Delay", "calibration.item.vacuum_on_delay" },
                { "Vacuum Re-On Delay", "calibration.item.vacuum_re_on_delay" },
                { "Valid", "calibration.item.valid" },
                { "Vision Pixel Offset X", "calibration.item.vision_pixel_offset_x" },
                { "Vision Pixel Offset Y", "calibration.item.vision_pixel_offset_y" },
                { "Vision Target", "calibration.item.vision_target" },
                { "Vision Target은 문자열 항목이라 키패드 수정 대상이 아닙니다.", "calibration.status.s170" },
                { "Vision Timeout", "calibration.item.vision_timeout" },
                { "VisionUnit CalibrationData를 찾을 수 없습니다.", "calibration.status.s173" },
                { "VisionX Cal", "calibration.item.visionx_cal" },
                { "VisionX Cal Position", "calibration.item.visionx_cal_position" },
                { "Wafer", "calibration.item.wafer" },
                { "WaferStageTouchSensor를 찾을 수 없습니다.", "calibration.status.s159" },
                { "XY Fine Max", "calibration.item.xy_fine_max" },
                { "XY Gain X", "calibration.item.xy_gain_x" },
                { "XY Gain Y", "calibration.item.xy_gain_y" },
                { "XY Retry", "calibration.item.xy_retry" },
                { "XY Tol", "calibration.item.xy_tol" },
                { "XY Tol Mode", "calibration.item.xy_tol_mode" },
                { "Y/T/Z를 Avoid로 후퇴한 뒤 X를 교체 위치로 이동합니다.", "calibration.colletchange.lblStatus" },
                { "Z AVOID", "calibration.item.z_avoid.upper_alt" },
                { "Z-AVOID", "calibration.item.z_avoid.upper" },
                { "교체 대상 선택", "calibration.colletchange.groupSelect" },
                { "교체 방향", "calibration.colletchange.lblSideCaption" },
                { "교체 위치 X", "calibration.colletchange.lblTargetCaption" },
                { "교체 위치 이동", "calibration.colletchange.btnMove" },
                { "니들 Z 보정", "calibration.message.m015" },
                { "니들 보정", "calibration.message.m014" },
                { "니들 핀 보정", "calibration.message.m017" },
                { "닫기", "calibration.colletchange.btnClose" },
                { "대기", "calibration.auto.lblCurrentTarget" },
                { "대기 중입니다.", "calibration.collet.lblStatus" },
                { "대기 중입니다. Collet과 보정 조건을 확인한 뒤 START를 실행하세요.", "calibration.status.s000" },
                { "대기 중입니다. 대상 콜렛과 클리닝 조건을 확인한 뒤 START를 실행하세요.", "calibration.status.s202" },
                { "대기 중입니다. 실제 캘리브레이션 시퀀스는 다음 단계에서 연결합니다.", "calibration.status.s218" },
                { "데이터 저장 위치 제안", "calibration.calibrationsetup.lblStorageHeader" },
                { "마지막 정상 측정 이후 Needle Z 결과가 변경되어 SAVE RESULT를 차단했습니다. 다시 측정하세요.", "calibration.status.s155" },
                { "목적", "calibration.calibrationsetup.lblPurposeHeader" },
                { "배출 Z 보정", "calibration.message.m005" },
                { "배출 Z 보정 준비 확인", "calibration.message.m012" },
                { "빈 웨이퍼 준비가 확인되지 않아 PickZ Calibration을 시작하지 않습니다.", "calibration.status.s129" },
                { "사용 항목 선택 및 저장", "calibration.auto.grpSelection" },
                { "사용 항목이 변경되었습니다. SAVE를 눌러 저장하세요.", "calibration.status.s118" },
                { "상태", "calibration.calibrationsetup.lblStatusHeader" },
                { "선택 Picker Z축을 찾을 수 없습니다.", "calibration.status.s062" },
                { "선택된 콜렛이 없습니다.", "calibration.status.s207" },
                { "선택한 교체 위치 X가 0.000 mm입니다(티칭 전일 수 있음).\r\n그대로 이동할까요?", "calibration.message.m021" },
                { "설정을 불러왔습니다. 1) MOVE READY  2) USE CURRENT  3) MOVE TEACH  4) START CAL 순서로 진행하세요.", "calibration.status.s169" },
                { "설정을 불러왔습니다. Output Good/NG와 Picker를 선택한 후 START SCAN을 실행하세요.", "calibration.status.s048" },
                { "설정을 불러왔습니다. Picker를 필름 위에 위치시킨 후 START SCAN을 실행하세요.", "calibration.status.s091" },
                { "설정을 불러왔습니다. 모든 숫자는 더블클릭 키패드로 입력하세요.", "calibration.status.s149" },
                { "설정을 저장했습니다.", "calibration.status.s205" },
                { "설정을 저장했습니다. 티칭 위치(Cal Position)와 측정 결과는 변경하지 않았습니다.", "calibration.status.s181" },
                { "설정을 확인한 후 START를 누르세요.", "calibration.auto.lblStatus" },
                { "수동 캘리브레이션 실행 가능 상태입니다. 실제 시퀀스 연결 후 이 게이트를 공통으로 사용합니다.", "calibration.status.s219" },
                { "실행 가능한 상태입니다.", "calibration.status.s006" },
                { "실행 가능한 상태입니다. 선택 Output Stage와 Picker 기준으로 PlaceZ Calibration을 실행할 수 있습니다.", "calibration.status.s064" },
                { "실행 가능한 상태입니다. 터치 센서 위치와 Z 시작 위치를 확인하세요.", "calibration.status.s160" },
                { "실행 가능한 상태입니다. 현재 XY 위치가 웨이퍼 필름 위인지 확인하세요.", "calibration.status.s100" },
                { "실행 중인 Needle Calibration 시퀀스가 없습니다.", "calibration.status.s145" },
                { "실행 중인 PickUpZ Calibration 시퀀스가 없습니다.", "calibration.status.s116" },
                { "실행 중인 PlaceZ Calibration 시퀀스가 없습니다.", "calibration.status.s085" },
                { "웨이퍼 필름 위에 Picker를 위치시킨 후 START SCAN을 실행하세요.", "calibration.pickupz.status" },
                { "자동 보정", "calibration.message.m009" },
                { "자동 운전 중에는 콜렛 교체 위치로 이동할 수 없습니다.\r\n정지 후 다시 시도하세요.", "calibration.message.m024" },
                { "장비가 준비되지 않았습니다.", "calibration.status.s060" },
                { "저장된 Auto Calibration 사용 설정을 불러왔습니다.", "calibration.status.s135" },
                { "저장된 회전 중심으로 X/Y 이동 후 COC를 다시 실행하고 있습니다.", "calibration.status.s017" },
                { "저장할 Needle Z 측정 결과가 없습니다. START CAL을 정상 완료한 뒤 SAVE RESULT를 누르세요.", "calibration.status.s154" },
                { "저장할 PickUpZ 측정 결과가 없습니다. START SCAN 또는 BATCH를 먼저 완료하세요.", "calibration.status.s093" },
                { "저장할 PlaceZ 측정 결과가 없습니다. START SCAN 또는 BATCH를 먼저 완료하세요.", "calibration.status.s053" },
                { "정지 처리 중입니다. 완료 후 창을 닫으세요.", "calibration.status.s008" },
                { "진행 상태", "calibration.auto.grpProgress" },
                { "진행 순서: Front 4→3→2→1, Rear 4→3→2→1 / PlaceZ는 Good Stage 기준 8회 측정하며 Good·NG Stage 모두 Empty 확인", "calibration.auto.lblPlaceZGuide" },
                { "콜렛 교체", "calibration.message.m022" },
                { "콜렛 보정", "calibration.message.m000" },
                { "콜렛 세척", "calibration.message.m020" },
                { "콜렛 일괄 보정", "calibration.message.m003" },
                { "콜렛 클리닝 실패. Alarm/Event Log를 확인하세요.", "calibration.status.s209" },
                { "콜렛 클리닝 실행 중입니다.", "calibration.status.s208" },
                { "콜렛 클리닝을 완료했습니다.", "calibration.status.s211" },
                { "콜렛 클리닝이 정지되었습니다.", "calibration.status.s212" },
                { "콜렛 회전 중심 측정", "calibration.message.m002" },
                { "티칭 값을 화면에 반영했으나 저장하지 못했습니다: Form1 호스트를 찾을 수 없습니다.", "calibration.status.s177" },
                { "티칭 위치 이동 완료. 위치가 맞으면 START CAL을 실행하세요.", "calibration.status.s190" },
                { "티칭 위치 이동이 정지 요청으로 중단되었습니다.", "calibration.status.s192" },
                { "티칭 위치로 이동 중입니다.", "calibration.status.s189" },
                { "픽업 Z 보정", "calibration.message.m007" },
                { "픽업 Z 보정 준비 확인", "calibration.message.m011" },
                { "픽커", "calibration.colletchange.lblPickerCaption" },
                { "픽커 Z Avoid 이동 실패. Alarm/Event Log를 확인하세요.", "calibration.status.s031" },
                { "픽커 Z Avoid 이동이 정지 요청으로 중단되었습니다.", "calibration.status.s032" },
                { "현재 StageY/NeedleX/EjectPinZ/NeedleZ 위치를 캘리브레이션 티칭값으로 넣었습니다. PARAMETER SAVE로 저장하세요.", "calibration.status.s150" },
                { "현재 정지 요청할 Collet Calibration 동작이 없습니다.", "calibration.status.s044" },
                { "현재 활성 Recipe가 없어 회전 중심 기계 좌표를 저장할 수 없습니다.", "calibration.status.s016" },
                { "활성 Recipe가 없습니다. PickPosition 저장을 위해 Recipe를 먼저 로드하세요.", "calibration.status.s099" },
                { "활성 Recipe가 없습니다. PlacePosition 저장을 위해 Recipe를 먼저 로드하세요.", "calibration.status.s061" },
                { "활성 Recipe가 없어 PickUpZ 측정 결과를 저장할 수 없습니다.", "calibration.status.s094" },
                { "활성 Recipe가 없어 PlaceZ 측정 결과를 저장할 수 없습니다.", "calibration.status.s054" },
            };

        public static string Display(string raw)
        {
            return raw != null && Keys.TryGetValue(raw, out var key) ? Lang.T(key) : raw;
        }

        public static void BindStatus(Control control, string raw)
        {
            if (raw != null && Keys.TryGetValue(raw, out var key)) Lang.BindKey(control, key);
            else Lang.BindFormat(control, "calibration.raw", raw);
        }

        public static void BindGrid(DataGridView grid)
        {
            Lang.BindReadOnlyCells(grid, Display, cell => cell.ColumnIndex == 0 || cell.ReadOnly);
        }

        public static void BindCombo(ComboBox combo)
        {
            Lang.BindChoices(combo, Display);
        }
    }
}
