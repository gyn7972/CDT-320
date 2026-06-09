using System.IO;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// P2 — ParameterSnapshot 파일 입출력 (절대경로 지정). Handler 미러: Read=DataContractJson,
    /// Write=JsonPrettySerializer, 원자적 tmp→Replace. Setup 통합 파일(Config/Setup/vision_setup.json)용.
    /// (Recipe 계층은 QMC.Common.Persistence.UnitRecipeStore 재사용.)
    /// </summary>
    public static class SnapshotFileStore
    {
        public static ParameterSnapshot Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new ParameterSnapshot();
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(ParameterSnapshot));
                    return (ser.ReadObject(fs) as ParameterSnapshot) ?? new ParameterSnapshot();
                }
            }
            catch { return new ParameterSnapshot(); }
        }

        public static bool Save(ParameterSnapshot snap, string path)
        {
            if (snap == null || string.IsNullOrEmpty(path)) return false;
            var tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var fs = File.Create(tmp))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(ParameterSnapshot), snap);
                }
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                return true;
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return false;
            }
        }
    }
}
