using QMC.Common.Data.Store;

namespace QMC.CDT320.Calibration
{
    public static class CalibrationDataStore
    {
        private const string StorageKey = "CalibrationData";

        public static string FilePath
        {
            get { return EquipmentDataStore.PathOf(StorageKey, "Config"); }
        }

        public static CalibrationData LoadOrCreate()
        {
            CalibrationData data;
            if (TryLoad(out data))
                return data;

            data = new CalibrationData();
            data.EnsureObjects();
            return data;
        }

        public static bool TryLoad(out CalibrationData data)
        {
            data = null;
            try
            {
                DataStoreResult<CalibrationData> result =
                    EquipmentDataStore.Load<CalibrationData>(StorageKey, "Config");
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

        public static bool Save(CalibrationData data)
        {
            string message;
            return Save(data, out message);
        }

        public static bool Save(CalibrationData data, out string message)
        {
            try
            {
                message = string.Empty;
                if (data == null)
                {
                    message = "저장할 CalibrationData가 없습니다.";
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
