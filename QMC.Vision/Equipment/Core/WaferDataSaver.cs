using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using QMC.Vision.Config;
using QMC.Vision.DieMaps;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 웨이퍼 1장 단위 실시간 검사 데이터 스냅샷 저장기(Bottom/Side/Bin 공용).
    /// <para>시퀀스 검사 결과(<see cref="VisionCommandCore"/>.InspectOnImageExplicit)가 들어올 때마다
    /// 다이(Index X/Y[,채널]) 단위로 자체 누적(이미지 제외 측정값만 — 뷰어 스토어의 이력 상한과 무관하게 전량 보존)하고,
    /// 활성 레시피 웨이퍼 사양의 다이 수(<see cref="PickupOrderResolver.Count"/> — INPUT DIE 맵/격자+Edge skip 반영)에
    /// 도달하면(=마지막 다이 검사) 날짜 폴더(<see cref="VisionSettings.DataLogPath"/>\yyyy-MM-dd)에
    /// 모드_레시피_HHmmss.csv 로 저장한다.</para>
    /// <para>시간 부하 대책: ① 마지막 다이 도달 후 짧은 대기(디바운스)로 잔여 채널(측면 앞/뒤 4채널)까지 수집,
    /// ② 실제 파일 쓰기는 <see cref="DataSaveQueue"/>(백그라운드 단일 스레드)에서 수행 — 검사/시퀀스 스레드는
    /// 값 누적(사전 갱신)만 하고 즉시 복귀하므로 사이클 타임에 디스크 지연이 더해지지 않는다.</para>
    /// <para>안전망: 새 웨이퍼로 초기화(결과 스토어 세대 변경)될 때 이전 웨이퍼가 미저장이면 그 시점 데이터로 즉시
    /// 저장한다(마지막 개수 미도달 중단 웨이퍼는 파일명에 PARTIAL 표기).</para>
    /// </summary>
    public static class WaferDataSaver
    {
        /// <summary>마지막 다이 도달 후 잔여 채널 수집 대기(ms). 대기 중 추가 결과가 오면 대기를 연장한다.</summary>
        private const int DebounceMs = 1500;

        /// <summary>다이 1행(Index X/Y + 채널) — 측정 지표만 보관(이미지 미보관, 웨이퍼 1장 전량 누적해도 수 MB 미만).</summary>
        private sealed class Row
        {
            public int Picker, Channel, IndexX, IndexY;
            public bool Pass;
            public Dictionary<string, double> Values;
            public DateTime Time;
        }

        private sealed class ModeState
        {
            public long Gen = long.MinValue;                 // InspectionResultStore 세대(Clear 시 +1) 추종
            public readonly Dictionary<string, Row> Rows = new Dictionary<string, Row>(StringComparer.Ordinal);
            public readonly List<Row> Order = new List<Row>();          // 입력 순서 보존(CSV 행 순서)
            public readonly HashSet<string> Dies = new HashSet<string>(StringComparer.Ordinal);   // 고유 다이 수
            public readonly List<string> Keys = new List<string>();     // 지표 칼럼(처음 등장 순서)
            public readonly HashSet<string> KeySeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public bool Saved;                               // 이번 웨이퍼 저장 완료(중복 저장 방지 래치)
            public bool Pending;                             // 마지막 다이 도달 — 디바운스 대기 중
            public Timer Timer;
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, ModeState> _states =
            new Dictionary<string, ModeState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>시퀀스 검사 결과 1건 누적 + 마지막 다이 도달 판정. picker 1~8(전역 픽커, 자동 구동)만 대상(수동 테스트 제외).</summary>
        public static void Accumulate(string mode, int picker, int channel, int indexX, int indexY,
                                      bool pass, IDictionary<string, double> values)
        {
            try
            {
                if (string.IsNullOrEmpty(mode)) return;
                if (picker < 1 || picker > ColletAddress.TotalCollets) return;
                var cfg = VisionConfigStore.Current;
                if (cfg == null || !cfg.DataLogEnable) return;
                var recipe = ActiveRecipeContext.Current;
                if (recipe != null && !recipe.LogEnable) return;   // 레시피별 로그 토글 준수(미등록 시 통과)

                int expected = PickupOrderResolver.Count;   // 레시피 웨이퍼 사양에 적용된 다이 수(0=미확정)
                List<Row> flushRows = null; List<string> flushKeys = null; string flushTag = null;

                lock (_lock)
                {
                    var st = GetState(mode);
                    long gen = InspectionResultStore.GenerationOf(mode);
                    if (gen != st.Gen)
                    {
                        // 새 웨이퍼(결과 스토어 Clear) — 이전 웨이퍼 데이터가 미저장이면 안전망으로 먼저 저장.
                        if (!st.Saved && st.Order.Count > 0)
                        {
                            flushRows = new List<Row>(st.Order);
                            flushKeys = new List<string>(st.Keys);
                            flushTag  = st.Pending ? "" : "PARTIAL";
                        }
                        Reset(st, gen);
                    }

                    // 같은 다이/채널 재검사(멈춤→재개 등)는 제자리 갱신 — 뷰어 스토어의 중복 제거와 동일 규칙.
                    string rowKey = indexX + "_" + indexY + "_" + channel;
                    Row row;
                    if (!st.Rows.TryGetValue(rowKey, out row))
                    {
                        row = new Row();
                        st.Rows[rowKey] = row;
                        st.Order.Add(row);
                    }
                    row.Picker = picker; row.Channel = channel;
                    row.IndexX = indexX; row.IndexY = indexY;
                    row.Pass = pass; row.Time = DateTime.Now;
                    var copy = new Dictionary<string, double>(values != null ? values.Count : 0, StringComparer.OrdinalIgnoreCase);
                    if (values != null)
                        foreach (var kv in values) copy[kv.Key] = kv.Value;
                    row.Values = copy;
                    foreach (var k in copy.Keys)
                        if (st.KeySeen.Add(k)) st.Keys.Add(k);

                    st.Dies.Add(indexX + "_" + indexY);

                    if (!st.Saved && expected > 0 && st.Dies.Count >= expected)
                    {
                        // 마지막 다이 도달 — 잔여 채널 대기(디바운스) 후 저장. 추가 결과가 오면 여기서 대기가 연장된다.
                        st.Pending = true;
                        if (st.Timer == null) st.Timer = new Timer(OnDebounce, mode, Timeout.Infinite, Timeout.Infinite);
                        st.Timer.Change(DebounceMs, Timeout.Infinite);
                    }
                }

                if (flushRows != null)
                    EnqueueWrite(mode, flushRows, flushKeys, flushTag);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WaferDataSaver] Accumulate 실패(" + mode + "): " + ex.Message);
            }
        }

        /// <summary>디바운스 만료 — 마지막 다이 이후 추가 결과가 없으면 이번 웨이퍼 스냅샷을 저장 큐로 넘긴다.</summary>
        private static void OnDebounce(object modeObj)
        {
            string mode = modeObj as string;
            try
            {
                List<Row> rows; List<string> keys;
                lock (_lock)
                {
                    var st = GetState(mode);
                    if (!st.Pending || st.Saved) return;
                    st.Pending = false;
                    st.Saved = true;
                    rows = new List<Row>(st.Order);
                    keys = new List<string>(st.Keys);
                }
                if (rows.Count > 0) EnqueueWrite(mode, rows, keys, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WaferDataSaver] 저장 트리거 실패(" + mode + "): " + ex.Message);
            }
        }

        private static ModeState GetState(string mode)
        {
            ModeState st;
            if (!_states.TryGetValue(mode, out st)) { st = new ModeState(); _states[mode] = st; }
            return st;
        }

        private static void Reset(ModeState st, long gen)
        {
            st.Gen = gen;
            st.Rows.Clear(); st.Order.Clear(); st.Dies.Clear();
            st.Keys.Clear(); st.KeySeen.Clear();
            st.Saved = false; st.Pending = false;
        }

        /// <summary>날짜 폴더\모드_레시피[_태그]_HHmmss.csv 로 백그라운드 저장 등록(경로/파일명은 이 시점에 확정).</summary>
        private static void EnqueueWrite(string mode, List<Row> rows, List<string> keys, string tag)
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                string root = cfg != null ? cfg.EffectiveDataLogPath : VisionSettings.DefaultDataLogPath;
                var recipe = ActiveRecipeContext.Current;
                string recipeName = SanitizeFileName(recipe != null ? recipe.RecipeName : "");
                var now = DateTime.Now;
                string dir = Path.Combine(root, now.ToString("yyyy-MM-dd"));
                var name = new StringBuilder(SanitizeFileName(mode));
                if (recipeName.Length > 0) name.Append('_').Append(recipeName);
                if (!string.IsNullOrEmpty(tag)) name.Append('_').Append(tag);
                name.Append('_').Append(now.ToString("HHmmss")).Append(".csv");
                string path = Path.Combine(dir, name.ToString());

                if (DataSaveQueue.Enqueue("WaferData:" + mode, () => Write(dir, path, rows, keys)))
                    System.Diagnostics.Debug.WriteLine("[WaferDataSaver] " + mode + " 웨이퍼 스냅샷 저장 예약: " + path + " (행 " + rows.Count + ")");
                else
                    System.Diagnostics.Debug.WriteLine("[WaferDataSaver] 저장 큐 등록 실패: " + path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WaferDataSaver] 저장 예약 실패(" + mode + "): " + ex.Message);
            }
        }

        /// <summary>CSV 쓰기(백그라운드 큐 전용). 날짜 폴더가 없으면 생성. 칼럼=고정 6종 + 지표 합집합.</summary>
        private static void Write(string dir, string path, List<Row> rows, List<string> keys)
        {
            Directory.CreateDirectory(dir);
            var ci = CultureInfo.InvariantCulture;
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                var head = new StringBuilder("Time,Index X,Index Y,Picker,Channel,Pass");
                foreach (var k in keys) { head.Append(','); head.Append(Esc(k)); }
                sw.WriteLine(head.ToString());

                foreach (var row in rows)
                {
                    if (row == null) continue;
                    var sb = new StringBuilder();
                    sb.Append(row.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", ci)).Append(',')
                      .Append(row.IndexX).Append(',').Append(row.IndexY).Append(',')
                      .Append(row.Picker).Append(',').Append(row.Channel).Append(',')
                      .Append(row.Pass ? "PASS" : "NG");
                    foreach (var k in keys)
                    {
                        sb.Append(',');
                        double v;
                        if (row.Values != null && row.Values.TryGetValue(k, out v))
                            sb.Append(v.ToString("F4", ci));
                    }
                    sw.WriteLine(sb.ToString());
                }
            }
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
