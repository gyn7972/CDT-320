using System;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        /// <summary>카메라 캘리브레이션 파일 저장과 메모리 반영이 끝날 때까지 운전 진입을 보호한다.</summary>
        public bool TryBeginCameraCalibrationSaveOperation(out IDisposable lease, out string reason)
        {
            lease = null;
            reason = string.Empty;
            if (_status == EquipmentStatus.Alarm || QMC.Common.Alarms.AlarmManager.HasActive)
            {
                reason = "Alarm 원인을 조치하고 RESET을 완료한 뒤 카메라 캘리브레이션 값을 저장하십시오.";
                return false;
            }

            // 좌표값 저장과 START/레시피/수동 동작이 경합하지 않도록 기존 데이터 변경 Gate를 공유한다.
            // 실제 축 정지를 확인하며, 이 작업에서는 모션 명령이나 I/O 출력을 실행하지 않는다.
            if (!TryRegisterRecipeApplyOperation(out reason, requireStoppedAxes: true))
            {
                reason = "카메라 캘리브레이션 저장을 시작할 수 없습니다. " + reason;
                return false;
            }

            bool admitted = false;
            try
            {
                // 보호 등록 직후에도 기존 Alarm/진행 작업/축 정지 검사를 다시 통과해야 저장을 허용한다.
                if (!TryValidateRecipeSwitchStopped(out reason))
                {
                    reason = "카메라 캘리브레이션 저장 준비 중 장비 상태를 확인하지 못했습니다. " + reason;
                    return false;
                }

                lease = new RecipeApplyOperationLease(this);
                admitted = true;
                return true;
            }
            catch (Exception ex)
            {
                reason = "카메라 캘리브레이션 저장의 장비 상태 확인에 실패했습니다. 값을 저장하지 않았습니다. error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "CameraCalibrationSave",
                    reason + ", exception=" + ex + " - Failed");
                return false;
            }
            finally
            {
                if (!admitted)
                    EndRecipeApplyOperation();
            }
        }
    }
}
