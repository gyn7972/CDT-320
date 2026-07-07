using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using QMC.Common.Logging;

namespace QMC.CDT320
{
    /// <summary>
    /// 로그 보존기간 관리(2단계). 앱 시작 30초 후 1회 + 이후 24시간마다 백그라운드로 실행한다.
    /// <list type="number">
    ///   <item><description><b>압축(고정 규칙)</b>: 14일(<see cref="RawKeepDays"/>)이 지난 원본 로그를
    ///   <c>Log\Archive</c> 에 zip 으로 압축 보관하고 원본을 지운다. 최근 14일은 원본을 유지해
    ///   이력 페이지의 과거 날짜 조회(DATE)가 그대로 동작한다.</description></item>
    ///   <item><description><b>압축본 관리(설정)</b>: <see cref="AppSettings.ArchiveKeepDays"/>(OFF/90/180/365)가
    ///   지난 압축본은 최종 삭제한다(복구 불가). 0(OFF) = 무기한 보관.</description></item>
    /// </list>
    /// 대상: Event CSV(분할 조각 포함), 레거시 *.log, 알람 JSON. 제외: Lot 기록·번역 카탈로그(보존 데이터).
    /// 결과는 EventLogger 에 LOG-RETENTION 이벤트로 남긴다(감사 추적).
    /// </summary>
    public static class LogRetentionService
    {
        // --- Const ---

        // 원본 로그 유예기간(고정, 설정 아님). 이 일수 이내의 원본은 압축하지 않아
        // 이력 페이지 DATE 피커로 최근 14일은 원본 그대로 즉시 조회된다.
        private const int RawKeepDays = 14;

        private const int FirstRunDelayMs = 30 * 1000;            // 시작 직후 부하를 피해 30초 뒤 첫 실행
        private const int IntervalMs = 24 * 60 * 60 * 1000;       // 이후 하루 1회

        // --- Fields ---

        private static readonly Regex DateInName = new Regex(@"(\d{4}-\d{2}-\d{2})", RegexOptions.Compiled);
        private static Timer _timer;
        private static int _running;   // 재진입 방지 플래그 (0=유휴, 1=실행 중)

        // --- Properties ---

        public static string ArchiveDir
        {
            get { return Path.Combine(EventLogger.LogRoot, "Archive"); }
        }

        // --- Public Methods ---

        /// <summary>보존기간 관리 타이머를 시작한다(앱 시작 시 1회 호출).</summary>
        public static void Start()
        {
            try
            {
                if (_timer != null)
                    return;

                _timer = new Timer(state => RunOnce(), null, FirstRunDelayMs, IntervalMs);
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// 압축(1단계) + 압축본 정리(2단계)를 1회 수행한다. 개별 파일 실패는 건너뛰고
        /// 다음 실행에서 재시도한다. 백그라운드 스레드에서 호출된다.
        /// </summary>
        public static void RunOnce()
        {
            // 이전 실행이 끝나지 않았으면(대용량 압축 중 등) 겹쳐 돌지 않는다.
            if (Interlocked.Exchange(ref _running, 1) == 1)
                return;

            try
            {
                string summary = "";

                // ── 1단계: 원본 로그 압축 보관 (설정: 사용여부 + 유예일수) ──
                int rawKeepDays = AppSettingsStore.Current.LogCompressDays;
                if (rawKeepDays < 1) rawKeepDays = 1;
                if (rawKeepDays > 365) rawKeepDays = 365;
                // 압축 비활성 시 cutoff 를 최소값으로 두어 어떤 파일도 압축 대상이 되지 않게 한다.
                DateTime rawCutoff = AppSettingsStore.Current.LogCompressEnabled
                    ? DateTime.Today.AddDays(-rawKeepDays)
                    : DateTime.MinValue;
                int zipped = 0, zipFailed = 0;
                long beforeBytes = 0, afterBytes = 0;

                foreach (string path in CollectRawTargets())
                {
                    if (ResolveFileDate(path) >= rawCutoff)
                        continue;   // 유예기간 이내 — 원본 유지 (오늘/기록 중 파일 포함)

                    long size = 0;
                    try { size = new FileInfo(path).Length; } catch { }

                    long zipSize = TryArchive(path);
                    if (zipSize >= 0)
                    {
                        zipped++;
                        beforeBytes += size;
                        afterBytes += zipSize;
                    }
                    else
                    {
                        zipFailed++;
                    }
                }

                if (zipped > 0 || zipFailed > 0)
                {
                    summary += rawKeepDays + "일 경과 원본 압축 " + zipped + "개("
                        + (beforeBytes / 1048576) + "MB → " + (afterBytes / 1048576) + "MB)"
                        + (zipFailed > 0 ? ", 실패 " + zipFailed + "개" : "");
                }

                // ── 2단계: 보존일수(설정)가 지난 압축본 최종 삭제 ──
                // 압축 OFF면 삭제도 하지 않는다(압축 없이 삭제만 쓰는 조합 금지 — 구 설정 방어).
                int archiveKeepDays = AppSettingsStore.Current.LogCompressEnabled
                    ? AppSettingsStore.Current.ArchiveKeepDays
                    : 0;
                if (archiveKeepDays > 0)
                {
                    // 삭제 기간은 압축 유예 기간보다 짧을 수 없다(압축되자마자 삭제되는 것 방지).
                    if (archiveKeepDays < rawKeepDays)
                        archiveKeepDays = rawKeepDays;

                    DateTime zipCutoff = DateTime.Today.AddDays(-archiveKeepDays);
                    int deleted = 0, deleteFailed = 0;
                    long deletedBytes = 0;

                    foreach (string zipPath in CollectArchiveFiles())
                    {
                        if (ResolveFileDate(zipPath) >= zipCutoff)
                            continue;

                        try
                        {
                            long size = new FileInfo(zipPath).Length;
                            File.Delete(zipPath);
                            deleted++;
                            deletedBytes += size;
                        }
                        catch
                        {
                            deleteFailed++;
                        }
                    }

                    if (deleted > 0 || deleteFailed > 0)
                    {
                        summary += (summary.Length > 0 ? " / " : "")
                            + archiveKeepDays + "일 경과 압축본 삭제 " + deleted + "개("
                            + (deletedBytes / 1048576) + "MB)"
                            + (deleteFailed > 0 ? ", 실패 " + deleteFailed + "개" : "");
                    }
                }

                if (summary.Length > 0)
                    EventLogger.Write(EventKind.Event, "SYSTEM", "LOG-RETENTION", summary);
            }
            catch
            {
                // 정리 실패는 치명적이지 않다 — 다음 주기에서 재시도된다.
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        // --- Private Methods ---

        // 압축 대상(원본) 목록. Lot 기록(Log\Lots)·번역 카탈로그(Log\Messages)는 보존 데이터라 제외한다.
        private static IEnumerable<string> CollectRawTargets()
        {
            var list = new List<string>();

            // 이벤트 CSV — 종류별 폴더(설정 오버라이드 포함) 전부. 같은 폴더는 한 번만.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (seen.Add(EventLogger.LogDir))
                AddFiles(list, EventLogger.LogDir, "*.csv");
            foreach (EventKind kind in (EventKind[])Enum.GetValues(typeof(EventKind)))
            {
                string dir = EventLogger.ResolveKindDir(kind);
                if (seen.Add(dir))
                    AddFiles(list, dir, "*.csv");
            }

            AddFiles(list, EventLogger.LogRoot, "*.log");                                 // 레거시 Event_/LCP_280_/Main_ 로그
            AddFiles(list, Path.Combine(EventLogger.LogRoot, "Alarms"), "*.json");        // 알람 이력 JSON
            return list;
        }

        // 2단계 정리 대상 — Archive 폴더의 압축본.
        private static IEnumerable<string> CollectArchiveFiles()
        {
            var list = new List<string>();
            AddFiles(list, ArchiveDir, "*.zip");
            return list;
        }

        private static void AddFiles(List<string> list, string dir, string pattern)
        {
            try
            {
                if (Directory.Exists(dir))
                    list.AddRange(Directory.GetFiles(dir, pattern));
            }
            catch
            {
            }
        }

        // 파일 날짜 판정 — 파일명의 yyyy-MM-dd 우선, 없으면 마지막 수정 날짜.
        // 판정에 실패하면 오늘로 취급해 절대 정리하지 않는다(안전 우선).
        private static DateTime ResolveFileDate(string path)
        {
            try
            {
                Match m = DateInName.Match(Path.GetFileName(path));
                DateTime parsed;
                if (m.Success && DateTime.TryParse(m.Value, out parsed))
                    return parsed.Date;

                return File.GetLastWriteTime(path).Date;
            }
            catch
            {
                return DateTime.Today;
            }
        }

        // 파일 1개를 Log\Archive\<파일명>.zip 으로 압축하고, 성공 확인 후에만 원본을 지운다.
        // 반환: 압축본 크기(바이트), 실패 시 -1(원본 유지 → 다음 실행에서 재시도).
        private static long TryArchive(string path)
        {
            try
            {
                Directory.CreateDirectory(ArchiveDir);
                // 종류별 폴더의 CSV 는 파일명(날짜)이 같아 충돌하므로 상위 폴더명을 접두로 붙여 유일하게 만든다.
                string parentDir = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
                string zipBase = string.IsNullOrEmpty(parentDir)
                    ? Path.GetFileName(path)
                    : parentDir + "_" + Path.GetFileName(path);
                string zipPath = Path.Combine(ArchiveDir, zipBase + ".zip");

                // 같은 이름의 압축본이 이미 있으면(직전 실행이 원본 삭제 직전에 중단된 경우)
                // 다시 압축하지 않고 원본 삭제만 이어서 한다.
                if (!File.Exists(zipPath))
                {
                    string tempPath = zipPath + ".tmp";
                    using (var zipStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    using (var entry = zip.CreateEntry(Path.GetFileName(path), CompressionLevel.Optimal).Open())
                    using (var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        source.CopyTo(entry);   // 스트리밍 복사 — 대용량 파일도 메모리 상한 고정
                    }

                    File.Move(tempPath, zipPath);   // 완성된 뒤에만 정식 이름으로 — 중단돼도 깨진 zip 이 남지 않음
                }

                if (new FileInfo(zipPath).Length <= 0)
                    return -1;

                File.Delete(path);
                return new FileInfo(zipPath).Length;
            }
            catch
            {
                return -1;
            }
        }
    }
}
