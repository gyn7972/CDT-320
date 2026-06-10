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
                return EquipmentDataStore.Save(data, storageKey, "Setup").Success;
            }
            catch
            {
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
                return EquipmentDataStore.Save(data, storageKey, "Config").Success;
            }
            catch
            {
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
                return RecipeDataStore.Save(data, recipeName, storageKey).Success;
            }
            catch
            {
                return false;
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
