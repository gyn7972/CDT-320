using System;
using System.IO;

namespace QMC.Common.Data.Store
{
    /// <summary>저장 경로에 사용할 이름을 정규화합니다.</summary>
    public static class StorageName
    {
        public static string Safe(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return "_";

                if (name.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - ".Project".Length);

                foreach (char c in Path.GetInvalidFileNameChars())
                    name = name.Replace(c, '_');

                return string.IsNullOrEmpty(name) ? "_" : name;
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
