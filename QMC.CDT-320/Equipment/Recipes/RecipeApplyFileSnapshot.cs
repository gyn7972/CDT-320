using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Recipes
{
    /// <summary>작업자 확인 전후에 적용할 Project/Unit 파일 내용이 바뀌지 않았는지 확인합니다.</summary>
    internal sealed class RecipeApplyFileSnapshot
    {
        private readonly string _recipeName;
        private readonly Dictionary<string, string> _fileHashes;

        private RecipeApplyFileSnapshot(string recipeName, Dictionary<string, string> fileHashes)
        {
            _recipeName = recipeName;
            _fileHashes = fileHashes;
        }

        internal static RecipeApplyFileSnapshot Capture(string recipeName)
        {
            return new RecipeApplyFileSnapshot(recipeName, ReadHashes(recipeName));
        }

        internal bool IsCurrent(out string reason)
        {
            reason = string.Empty;
            try
            {
                Dictionary<string, string> current = ReadHashes(_recipeName);
                if (current.Count == _fileHashes.Count && _fileHashes.All(pair =>
                    current.ContainsKey(pair.Key) && string.Equals(current[pair.Key], pair.Value, StringComparison.Ordinal)))
                    return true;
                reason = "확인 중 대상 레시피 파일이 변경되었습니다. 최신 파일을 다시 확인한 뒤 적용하십시오. recipe=" + _recipeName;
                return false;
            }
            catch (Exception ex)
            {
                reason = "대상 레시피 파일의 동일성을 확인하지 못했습니다. recipe=" + _recipeName + ", error=" + ex.Message;
                return false;
            }
        }

        internal RecipeApplyFileSnapshot CaptureAfterProjectSave()
        {
            Dictionary<string, string> current = ReadHashes(_recipeName);
            string projectPath = Path.Combine(RecipeStore.Dir, _recipeName + ".Project");
            if (current.Count != _fileHashes.Count || _fileHashes.Any(pair =>
                !current.ContainsKey(pair.Key) ||
                (!string.Equals(pair.Key, projectPath, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(current[pair.Key], pair.Value, StringComparison.Ordinal))))
                throw new InvalidOperationException(
                    "Project 저장 중 Unit 레시피 파일이 변경되었습니다. 최신 파일을 다시 확인한 뒤 적용하십시오. recipe=" + _recipeName);
            return new RecipeApplyFileSnapshot(_recipeName, current);
        }

        private static Dictionary<string, string> ReadHashes(string recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                throw new ArgumentException("레시피 이름이 없습니다.", "recipeName");
            var paths = new List<string> { Path.Combine(RecipeStore.Dir, recipeName + ".Project") };
            string unitDirectory = RecipeDataStore.DirOf(recipeName);
            if (!Directory.Exists(unitDirectory))
                throw new DirectoryNotFoundException("대상 Unit 레시피 폴더가 없습니다: " + unitDirectory);
            paths.AddRange(Directory.GetFiles(unitDirectory, "*.recipe.json", SearchOption.TopDirectoryOnly));
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (SHA256 sha = SHA256.Create())
            {
                foreach (string path in paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        hashes.Add(path, Convert.ToBase64String(sha.ComputeHash(stream)));
                }
            }
            return hashes;
        }
    }
}
