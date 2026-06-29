using QMC.Common.Data.Store;

namespace QMC.CDT320.Calibration
{
    public static class VisionCameraScaleStore
    {
        private const string StorageKey = "VisionCameraScale";

        public static string FilePath
        {
            get { return EquipmentDataStore.PathOf(StorageKey, "Config"); }
        }

        public static bool TryLoad(out VisionCameraCalibrationData data)
        {
            data = null;
            try
            {
                DataStoreResult<VisionCameraCalibrationData> result =
                    EquipmentDataStore.Load<VisionCameraCalibrationData>(StorageKey, "Config");
                if (!result.Success || result.UsedDefault || result.Data == null)
                    return false;

                data = result.Data;
                data.EnsureObjects();
                return true;
            }
            catch
            {
                data = null;
                return false;
            }
            finally
            {
            }
        }

        public static bool Save(VisionCameraCalibrationData data)
        {
            string message;
            return Save(data, out message);
        }

        public static bool Save(VisionCameraCalibrationData data, out string message)
        {
            try
            {
                message = string.Empty;
                if (data == null)
                {
                    message = "저장할 Camera Scale 데이터가 없습니다.";
                    return false;
                }

                data.EnsureObjects();
                DataStoreResult result = EquipmentDataStore.Save(data, StorageKey, "Config");
                if (result.Success)
                    return true;

                message = result.Message;
                if (result.Exception != null && !string.IsNullOrEmpty(result.Exception.Message) &&
                    !string.Equals(result.Exception.Message, message))
                {
                    message = string.IsNullOrEmpty(message)
                        ? result.Exception.Message
                        : message + " / " + result.Exception.Message;
                }

                if (string.IsNullOrEmpty(message))
                    message = "알 수 없는 저장 실패입니다.";

                return false;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return false;
            }
            finally
            {
            }
        }
    }
}
