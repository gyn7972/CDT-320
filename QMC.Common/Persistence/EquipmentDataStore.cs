using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Persistence
{
    /// <summary>
    /// Setup / Config 영속화 ? 노드(Unit/Component)당 1개.
    /// 전원 OFF 후에도 유지되는 기구 설정값/사양 파라미터를 저장한다.
    /// 파일 경로: <c>./EquipmentData/{category}/{storageKey}.json</c>
    /// </summary>
    public static class EquipmentDataStore
    {
        /// <summary>Setup / Config 루트 디렉토리.</summary>
        public static string Root { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData");

        /// <summary>
        /// 노드 데이터를 로드한다. 파일이 없거나 실패하면 기본값으로 초기화된 새 인스턴스를 반환한다.
        /// </summary>
        public static T Load<T>(string storageKey, string category) where T : new()
        {
            var path = PathOf(storageKey, category);
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
        /// 노드 데이터를 저장한다. 임시 파일에 쓴 뒤 교체하여 부분 쓰기로 인한 손상을 방지한다.
        /// 성공하면 true, 실패하면 false 를 반환한다.
        /// </summary>
        public static bool Save<T>(T data, string storageKey, string category)
        {
            if ((object)data == null || string.IsNullOrEmpty(storageKey))
                return false;

            var path = PathOf(storageKey, category);
            var tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var fs = File.Create(tmp))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    ser.WriteObject(fs, data);
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

        private static string PathOf(string storageKey, string category)
        {
            return Path.Combine(Root, category, StoragePathUtil.SafeName(storageKey) + ".json");
        }
    }
}
