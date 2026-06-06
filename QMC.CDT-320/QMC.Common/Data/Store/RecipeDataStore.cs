using System;
using System.Collections.Generic;
using System.IO;

namespace QMC.Common.Data.Store
{
    /// <summary>프로젝트/제품별 Recipe 데이터 Store입니다.</summary>
    public static class RecipeDataStore
    {
        public static string Root { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes");

        public static DataStoreResult<T> Load<T>(string recipeName, string storageKey) where T : new()
        {
            try
            {
                if (string.IsNullOrEmpty(recipeName))
                    return DataStoreResult<T>.Ok(new T(), string.Empty, true, "Recipe name is empty.");

                return JsonDataStore.Load<T>(PathOf(recipeName, storageKey));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult Save<T>(T data, string recipeName, string storageKey)
        {
            try
            {
                if (string.IsNullOrEmpty(recipeName))
                    return DataStoreResult.Fail(string.Empty, "Recipe name is empty.");

                return JsonDataStore.Save(data, PathOf(recipeName, storageKey));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult DeleteRecipe(string recipeName)
        {
            try
            {
                return JsonDataStore.DeleteDirectory(DirOf(recipeName));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult CopyRecipe(string sourceRecipeName, string targetRecipeName)
        {
            try
            {
                return JsonDataStore.CopyDirectory(DirOf(sourceRecipeName), DirOf(targetRecipeName));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static DataStoreResult RenameRecipe(string oldRecipeName, string newRecipeName)
        {
            try
            {
                return JsonDataStore.RenameDirectory(DirOf(oldRecipeName), DirOf(newRecipeName));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static List<string> ListUnits(string recipeName)
        {
            var result = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(recipeName))
                    return result;

                string dir = DirOf(recipeName);
                if (!Directory.Exists(dir))
                    return result;

                foreach (string file in Directory.GetFiles(dir, "*.recipe.json"))
                    result.Add(Path.GetFileName(file));

                result.Sort();
                return result;
            }
            catch
            {
                return result;
            }
            finally
            {
            }
        }

        public static string DirOf(string recipeName)
        {
            try
            {
                return Path.Combine(Root, StorageName.Safe(recipeName));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static string PathOf(string recipeName, string storageKey)
        {
            try
            {
                return Path.Combine(DirOf(recipeName), StorageName.Safe(storageKey) + ".recipe.json");
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
