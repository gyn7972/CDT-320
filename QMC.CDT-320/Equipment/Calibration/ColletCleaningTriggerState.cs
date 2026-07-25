using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common;

namespace QMC.CDT320.Calibration
{
    /// <summary>
    /// 콜렛 클리닝 자동 트리거 카운터. 재시작해도 주기가 유지되도록 State 영역에 영속화한다.
    /// (machine_state.json과 같은 D:\CDT-320\State 폴더의 별도 파일)
    /// </summary>
    [DataContract]
    public sealed class ColletCleaningTriggerState
    {
        /// <summary>마지막 클리닝 이후 누적된 웨이퍼 교체 횟수.</summary>
        [DataMember] public int WaferExchangeCount { get; set; }

        /// <summary>마지막 클리닝 이후 누적된 공정 수(die 또는 wafer, 설정 단위에 따름).</summary>
        [DataMember] public int ProcessCount { get; set; }

        /// <summary>현재 Auto 운전 세션에서 Auto 시작 트리거를 이미 처리했는지 여부.</summary>
        [DataMember] public bool AutoStartHandledInCurrentRun { get; set; }

        [DataMember] public DateTime LastTriggeredAt { get; set; }
        [DataMember] public string LastTriggerReason { get; set; } = string.Empty;

        public void EnsureObjects()
        {
            if (WaferExchangeCount < 0)
                WaferExchangeCount = 0;
            if (ProcessCount < 0)
                ProcessCount = 0;
            if (LastTriggerReason == null)
                LastTriggerReason = string.Empty;
            if (LastTriggeredAt <= DateTime.MinValue.AddDays(1) || LastTriggeredAt >= DateTime.MaxValue.AddDays(-1))
                LastTriggeredAt = new DateTime(2000, 1, 1);
        }
    }

    public static class ColletCleaningTriggerStateStore
    {
        private static readonly object Sync = new object();
        private static ColletCleaningTriggerState _cached;

        public static string RootDir { get { return @"D:\CDT-320"; } }
        public static string Dir { get { return Path.Combine(RootDir, "State"); } }
        public static string StatePath { get { return Path.Combine(Dir, "collet_cleaning_trigger.json"); } }

        public static ColletCleaningTriggerState Current
        {
            get
            {
                lock (Sync)
                {
                    if (_cached == null)
                        _cached = Load();
                    return _cached;
                }
            }
        }

        private static ColletCleaningTriggerState Load()
        {
            try
            {
                if (!File.Exists(StatePath))
                {
                    var created = new ColletCleaningTriggerState();
                    created.EnsureObjects();
                    return created;
                }

                using (FileStream fs = File.OpenRead(StatePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ColletCleaningTriggerState));
                    var state = (ColletCleaningTriggerState)serializer.ReadObject(fs);
                    if (state == null)
                        state = new ColletCleaningTriggerState();
                    state.EnsureObjects();
                    return state;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "ColletCleaningTriggerLoad",
                    "콜렛 클리닝 트리거 상태 로드에 실패해 기본값을 사용합니다. path=" + StatePath +
                    ", error=" + ex.Message + " - Failed");
                var fallback = new ColletCleaningTriggerState();
                fallback.EnsureObjects();
                return fallback;
            }
            finally
            {
            }
        }

        public static bool Save()
        {
            lock (Sync)
            {
                ColletCleaningTriggerState state = _cached;
                if (state == null)
                    return false;

                string tmp = StatePath + ".tmp";
                try
                {
                    state.EnsureObjects();
                    if (!Directory.Exists(Dir))
                        Directory.CreateDirectory(Dir);

                    using (FileStream fs = File.Create(tmp))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(ColletCleaningTriggerState));
                        serializer.WriteObject(fs, state);
                    }

                    if (File.Exists(StatePath))
                        File.Delete(StatePath);
                    File.Move(tmp, StatePath);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Write("Main", "SYSTEM", "ColletCleaningTriggerSave",
                        "콜렛 클리닝 트리거 상태 저장에 실패했습니다. path=" + StatePath +
                        ", error=" + ex.Message + " - Failed");
                    return false;
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); }
                    catch { }
                }
            }
        }
    }
}
