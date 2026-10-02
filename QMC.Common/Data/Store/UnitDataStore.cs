using System.Reflection;
using System.Runtime.CompilerServices;

namespace QMC.Common.Data.Store
{
    /// <summary>BaseEquipmentNode가 사용하는 Setup / Config / Recipe Store facade입니다.</summary>
    public static class UnitDataStore
    {
        private sealed class TemporaryDryRunValue
        {
            public bool OriginalValue { get; private set; }

            public TemporaryDryRunValue(bool originalValue)
            {
                OriginalValue = originalValue;
            }
        }

        private static readonly ConditionalWeakTable<object, TemporaryDryRunValue> TemporaryDryRunValues =
            new ConditionalWeakTable<object, TemporaryDryRunValue>();
        private static readonly MethodInfo CloneConfig = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        // Keep global runtime overrides out of the persisted Unit configuration.
        public static void RegisterTemporaryDryRunOverride(object config, bool originalValue)
        {
            if (config == null) return;
            TemporaryDryRunValues.GetValue(config, key => new TemporaryDryRunValue(originalValue));
        }

        public static void ClearTemporaryDryRunOverride(object config)
        {
            if (config != null)
                TemporaryDryRunValues.Remove(config);
        }

        public static T GetPersistentConfigSnapshot<T>(T data)
        {
            if ((object)data == null) return data;
            TemporaryDryRunValue temporary;
            if (!TemporaryDryRunValues.TryGetValue(data, out temporary))
                return data;

            // Change only the detached root copy; running sequences retain their mode.
            object snapshot = CloneConfig.Invoke(data, null);
            PropertyInfo dryRun = snapshot.GetType().GetProperty("bDryRun",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (dryRun == null || dryRun.PropertyType != typeof(bool) || !dryRun.CanWrite)
                throw new System.InvalidOperationException("Temporary DryRun configuration has no writable bool bDryRun.");
            dryRun.SetValue(snapshot, temporary.OriginalValue, null);
            return (T)snapshot;
        }

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

        /// <summary>
        /// 지정 Recipe 노드 파일을 기본값 대체 없이 검증하여 로드합니다.
        /// 누락/손상 파일을 기존 객체로 조용히 대체하면 서로 다른 Recipe가 섞일 수 있으므로
        /// 장비 Recipe 적용 경로에서는 이 메서드를 사용합니다.
        /// </summary>
        public static bool TryLoadRecipeRequired<T>(
            string recipeName,
            string storageKey,
            out T data,
            out string reason) where T : new()
        {
            data = default(T);
            reason = string.Empty;

            DataStoreResult<T> result = RecipeDataStore.Load<T>(recipeName, storageKey);
            if (result == null)
            {
                reason =
                    "Recipe 로드 결과가 없습니다. recipe=" + (recipeName ?? string.Empty) +
                    ", storageKey=" + (storageKey ?? string.Empty);
                return false;
            }

            if (!result.Success || result.UsedDefault || result.Data == null)
            {
                reason =
                    "Recipe 노드 로드 실패. recipe=" + (recipeName ?? string.Empty) +
                    ", storageKey=" + (storageKey ?? string.Empty) +
                    ", path=" + (result.Path ?? string.Empty) +
                    ", usedDefault=" + result.UsedDefault +
                    ", message=" + (result.Message ?? string.Empty) +
                    (result.Exception != null
                        ? ", exception=" + result.Exception.Message
                        : string.Empty);
                return false;
            }

            data = result.Data;
            return true;
        }

        public static T LoadRecipeRequired<T>(string recipeName, string storageKey) where T : new()
        {
            T data;
            string reason;
            if (!TryLoadRecipeRequired(recipeName, storageKey, out data, out reason))
                throw new System.IO.InvalidDataException(reason);

            return data;
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
                DataStoreResult result = EquipmentDataStore.Save(GetPersistentConfigSnapshot(data), storageKey, "Config");
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
