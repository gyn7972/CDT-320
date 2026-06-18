using System;
using System.IO;

namespace QMC.Common.Data.Store
{
    /// <summary>레시피/설비데이터 저장 루트(가변). 기본은 exe 폴더(AppDomain.BaseDirectory)이며,
    /// 호스트(예: QMC.Vision)가 기동 시 설정값으로 <see cref="Root"/> 를 지정한다.
    /// RecipeDataStore/EquipmentDataStore 는 이 값을 기준으로 경로를 동적 계산한다.</summary>
    public static class DataPaths
    {
        private static string _root = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>데이터 저장 루트 절대경로. 빈 값/공백 지정은 무시(기존 값 유지).</summary>
        public static string Root
        {
            get { return _root; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _root = value;
            }
        }

        /// <summary>루트 폴더 및 표준 하위 폴더(Recipes/Equ^ipmentData)를 생성(없을 때만).</summary>
        public static void EnsureRoot()
        {
            try
            {
                Directory.CreateDirectory(_root);
                Directory.CreateDirectory(Path.Combine(_root, "Recipes"));
                Directory.CreateDirectory(Path.Combine(_root, "EquipmentData"));
            }
            catch
            {
                // 권한/드라이브 부재 등 — 호출측에서 로깅. 여기서는 무시.
            }
        }
    }
}
