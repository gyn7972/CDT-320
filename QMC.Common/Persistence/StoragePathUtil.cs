using System;
using System.IO;

namespace QMC.Common.Persistence
{
    /// <summary>
    /// 영속화 경로에 사용되는 이름(노드 키 / 레시피 이름)을 OS 안전 문자열로 정규화한다.
    /// </summary>
    public static class StoragePathUtil
    {
        /// <summary>
        /// 파일/폴더명으로 사용할 수 없는 문자를 '_' 로 치환한다.
        /// 레시피 이름은 ".Project" 확장자가 붙어 들어올 수 있으므로 제거한다.
        /// </summary>
        public static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_";

            if (name.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".Project".Length);

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return string.IsNullOrEmpty(name) ? "_" : name;
        }
    }
}
