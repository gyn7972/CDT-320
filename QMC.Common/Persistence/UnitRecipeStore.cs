using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.Common.Persistence
{
    /// <summary>
    /// Unit 레시피 영속화 ? 레시피 이름(번호)별로 N개 관리.
    /// RecipeStore 와 동일한 <c>./Recipes</c> 루트를 공유하며,
    /// 레시피 이름 폴더 안에 Unit 별 파일을 둔다.
    /// 파일 경로: <c>./Recipes/{recipeName}/{storageKey}.recipe.json</c>
    /// </summary>
    public static class UnitRecipeStore
    {
        /// <summary>레시피 루트 디렉토리 (RecipeStore 와 공유).</summary>
        public static string Root { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes");

        /// <summary>
        /// 지정한 레시피 이름의 노드 레시피를 로드한다.
        /// 파일이 없거나 실패하면 기본값으로 초기화된 새 인스턴스를 반환한다.
        /// </summary>
        public static T Load<T>(string recipeName, string storageKey) where T : new()
        {
            if (string.IsNullOrEmpty(recipeName))
                return new T();

            var path = PathOf(recipeName, storageKey);
            if (!File.Exists(path))
                return new T();

            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    var obj = ser.ReadObject(fs);
                    return obj == null ? new T() : (T)obj;
                }
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// 지정한 레시피 이름으로 노드 레시피를 저장한다. 임시 파일 교체로 손상을 방지한다.
        /// 성공하면 true, 실패하면 false 를 반환한다.
        /// </summary>
        public static bool Save<T>(T data, string recipeName, string storageKey)
        {
            if ((object)data == null || string.IsNullOrEmpty(recipeName) || string.IsNullOrEmpty(storageKey))
                return false;

            var path = PathOf(recipeName, storageKey);
            var tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var fs = File.Create(tmp))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(T), data);
                }

                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);

                return true;
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); }
                catch { }
                return false;
            }
        }

        /// <summary>레시피 폴더 전체를 삭제한다 (해당 레시피의 모든 Unit 레시피 제거).</summary>
        public static bool DeleteRecipe(string recipeName)
        {
            if (string.IsNullOrEmpty(recipeName))
                return false;

            var dir = DirOf(recipeName);
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>레시피 폴더를 복사한다 (새 이름으로 모든 Unit 레시피 복제).</summary>
        public static bool CopyRecipe(string sourceRecipeName, string targetRecipeName)
        {
            if (string.IsNullOrEmpty(sourceRecipeName) || string.IsNullOrEmpty(targetRecipeName))
                return false;

            var src = DirOf(sourceRecipeName);
            var dst = DirOf(targetRecipeName);
            try
            {
                if (!Directory.Exists(src))
                    return false;

                Directory.CreateDirectory(dst);
                foreach (var file in Directory.GetFiles(src))
                {
                    var name = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(dst, name), true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>레시피 폴더의 이름을 변경한다.</summary>
        public static bool RenameRecipe(string oldRecipeName, string newRecipeName)
        {
            if (string.IsNullOrEmpty(oldRecipeName) || string.IsNullOrEmpty(newRecipeName))
                return false;

            var src = DirOf(oldRecipeName);
            var dst = DirOf(newRecipeName);
            try
            {
                if (!Directory.Exists(src) || Directory.Exists(dst))
                    return false;

                Directory.Move(src, dst);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>지정한 레시피 폴더에 저장된 Unit 레시피 파일 이름 목록.</summary>
        public static List<string> ListUnits(string recipeName)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(recipeName))
                return result;

            var dir = DirOf(recipeName);
            try
            {
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.recipe.json"))
                        result.Add(Path.GetFileName(f));
                    result.Sort();
                }
            }
            catch { }
            return result;
        }

        private static string DirOf(string recipeName)
        {
            return Path.Combine(Root, StoragePathUtil.SafeName(recipeName));
        }

        private static string PathOf(string recipeName, string storageKey)
        {
            return Path.Combine(DirOf(recipeName), StoragePathUtil.SafeName(storageKey) + ".recipe.json");
        }
    }
}
