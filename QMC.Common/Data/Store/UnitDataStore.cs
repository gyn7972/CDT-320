namespace QMC.Common.Data.Store
{
    /// <summary>BaseEquipmentNode가 사용하는 Setup / Config / Recipe Store facade입니다.</summary>
    public static class UnitDataStore
    {
        public static T LoadSetup<T>(string storageKey) where T : new()
        {
            try
            {
                var result = EquipmentDataStore.Load<T>(storageKey, "Setup");
                return result.Success && result.Data != null ? result.Data : new T();
            }
            catch
            {
                return new T();
            }
            finally
            {
            }
        }

        public static T LoadSetup<T>(string storageKey, T fallback) where T : new()
        {
            try
            {
                var result = EquipmentDataStore.Load<T>(storageKey, "Setup");
                if (!result.Success || result.UsedDefault || result.Data == null)
                    return fallback == null ? new T() : fallback;

                return result.Data;
            }
            catch
            {
                return fallback == null ? new T() : fallback;
            }
            finally
            {
            }
        }

        public static T LoadConfig<T>(string storageKey) where T : new()
        {
            try
            {
                var result = EquipmentDataStore.Load<T>(storageKey, "Config");
                return result.Success && result.Data != null ? result.Data : new T();
            }
            catch
            {
                return new T();
            }
            finally
            {
            }
        }

        public static T LoadConfig<T>(string storageKey, T fallback) where T : new()
        {
            try
            {
                var result = EquipmentDataStore.Load<T>(storageKey, "Config");
                if (!result.Success || result.UsedDefault || result.Data == null)
                    return fallback == null ? new T() : fallback;

                return result.Data;
            }
            catch
            {
                return fallback == null ? new T() : fallback;
            }
            finally
            {
            }
        }

        public static T LoadRecipe<T>(string recipeName, string storageKey) where T : new()
        {
            try
            {
                var result = RecipeDataStore.Load<T>(recipeName, storageKey);
                return result.Success && result.Data != null ? result.Data : new T();
            }
            catch
            {
                return new T();
            }
            finally
            {
            }
        }

        public static T LoadRecipe<T>(string recipeName, string storageKey, T fallback) where T : new()
        {
            try
            {
                var result = RecipeDataStore.Load<T>(recipeName, storageKey);
                if (!result.Success || result.UsedDefault || result.Data == null)
                    return fallback == null ? new T() : fallback;

                return result.Data;
            }
            catch
            {
                return fallback == null ? new T() : fallback;
            }
            finally
            {
            }
        }

        public static bool SaveSetup<T>(T data, string storageKey)
        {
            try
            {
                DataStoreResult result = EquipmentDataStore.Save(data, storageKey, "Setup");
                LogSaveFailureIfNeeded(result, "Setup", storageKey, string.Empty);
                return result.Success;
            }
            catch (System.Exception ex)
            {
                LogStoreException("Setup", storageKey, string.Empty, ex);
                return false;
            }
            finally
            {
            }
        }

        public static bool SaveConfig<T>(T data, string storageKey)
        {
            try
            {
                DataStoreResult result = EquipmentDataStore.Save(data, storageKey, "Config");
                LogSaveFailureIfNeeded(result, "Config", storageKey, string.Empty);
                return result.Success;
            }
            catch (System.Exception ex)
            {
                LogStoreException("Config", storageKey, string.Empty, ex);
                return false;
            }
            finally
            {
            }
        }

        public static bool SaveRecipe<T>(T data, string recipeName, string storageKey)
        {
            try
            {
                DataStoreResult result = RecipeDataStore.Save(data, recipeName, storageKey);
                LogSaveFailureIfNeeded(result, "Recipe", storageKey, recipeName);
                return result.Success;
            }
            catch (System.Exception ex)
            {
                LogStoreException("Recipe", storageKey, recipeName, ex);
                return false;
            }
            finally
            {
            }
        }

        private static void LogSaveFailureIfNeeded(DataStoreResult result, string category, string storageKey, string recipeName)
        {
            try
            {
                if (result != null && result.Success)
                    return;

                string message = result != null ? result.Message : "저장 결과가 없습니다.";
                string path = result != null ? result.Path : string.Empty;
                System.Exception exception = result != null ? result.Exception : null;

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "DATA",
                    "UNIT-DATA-SAVE",
                    "Unit data 저장 실패. category=" + category +
                    ", recipe=" + (recipeName ?? string.Empty) +
                    ", storageKey=" + (storageKey ?? string.Empty) +
                    ", path=" + (path ?? string.Empty) +
                    ", message=" + (message ?? string.Empty) +
                    (exception != null ? ", exception=" + exception.Message : string.Empty));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void LogStoreException(string category, string storageKey, string recipeName, System.Exception ex)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "DATA",
                    "UNIT-DATA-SAVE-EX",
                    "Unit data 저장 예외. category=" + category +
                    ", recipe=" + (recipeName ?? string.Empty) +
                    ", storageKey=" + (storageKey ?? string.Empty) +
                    ", error=" + (ex != null ? ex.Message : string.Empty));
            }
            catch
            {
            }
            finally
            {
            }
        }

        public static bool DeleteSetup(string storageKey)
        {
            try
            {
                return EquipmentDataStore.Delete(storageKey, "Setup").Success;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static bool DeleteConfig(string storageKey)
        {
            try
            {
                return EquipmentDataStore.Delete(storageKey, "Config").Success;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static bool DeleteRecipe(string recipeName, string storageKey)
        {
            try
            {
                return RecipeDataStore.DeleteNode(recipeName, storageKey).Success;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

    }
}
