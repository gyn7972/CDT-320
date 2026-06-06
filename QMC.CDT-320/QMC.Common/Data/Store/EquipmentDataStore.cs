using System;
using System.IO;

namespace QMC.Common.Data.Store
{
    /// <summary>장비 고정 데이터(Setup/Config) Store입니다.</summary>
    public static class EquipmentDataStore
    {
        public static string Root { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData");

        public static DataStoreResult<T> Load<T>(string storageKey, string category) where T : new()
        {
            try
            {
                return JsonDataStore.Load<T>(PathOf(storageKey, category));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult Save<T>(T data, string storageKey, string category)
        {
            try
            {
                return JsonDataStore.Save(data, PathOf(storageKey, category));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static string PathOf(string storageKey, string category)
        {
            try
            {
                return Path.Combine(Root, StorageName.Safe(category), StorageName.Safe(storageKey) + ".json");
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
