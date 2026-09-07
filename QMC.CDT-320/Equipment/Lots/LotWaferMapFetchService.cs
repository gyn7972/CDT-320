using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QMC.CDT320.DieMaps;
using QMC.Common.Logging;

namespace QMC.CDT320.Lots
{
    /// <summary>
    /// [P5 2026-08-24] 웨이퍼맵 캐시 항목. 파일명 = 웨이퍼 바코드 문자열 그대로(팀장님 확정 —
    /// 네트워크 폴더의 맵은 바코드와 1:1이며 LOT/슬롯 번호로는 특정 불가).
    /// </summary>
    internal sealed class LotWaferMapSlotInfo
    {
        /// <summary>이 맵의 키 = 웨이퍼 바코드(= 원격/캐시 파일명).</summary>
        public string Barcode = "";
        public string LocalPath = "";
        public int DieCount;
        public Dictionary<int, int> BinDieCounts = new Dictionary<int, int>();
        /// <summary>[P4 2026-08-22] 파일 헤더 BO=(bin 번호 순서)/BN=(이름) 조합으로 만든 bin 번호→이름 사전(없으면 빈 사전).</summary>
        public Dictionary<int, string> BinNames = new Dictionary<int, string>();
        /// <summary>
        /// 파일 헤더 1행의 팹 내부 웨이퍼 ID. 파일명(=바코드)과 다른 사례가 실측 확인돼
        /// (구 테스트 파일 YZ8XRD.07 내부 ID=YZ8RW_..W15) 기록/로그용으로만 쓴다 — 알람 판정 금지.
        /// </summary>
        public string InternalMapId = "";
        /// <summary>파싱 당시 로컬 캐시 파일의 수정시각/크기 — 캐시 단락(재파싱 생략) 판정용.</summary>
        public DateTime FileWriteUtc;
        public long FileLength;
        public DateTime ParsedAtUtc;
    }

    /// <summary>
    /// [P3 2026-08-22, P5 2026-08-24 바코드 키 전환] 웨이퍼맵 네트워크 수신/캐시 서비스.
    /// - 파일 식별: 바코드 = 파일명(1:1). 바코드를 읽은 뒤에만 맵을 찾을 수 있으므로
    ///   LOT 단위 프리페치/전수 확인은 구조적으로 불가 — 관련 API는 P5에서 제거했다.
    /// - 설정(AppSettings.NetworkWaferMapFolder)이 비어 있으면 전 기능 무동작.
    /// - 원격 접근은 전부 타임아웃 래핑(UNC 단절 시 SMB 대기로 스레드가 수십 초 잠기는 것 방어).
    /// - 파일은 Config\WaferMap 로컬 캐시로 복사 후 설정 포맷의 파서로 읽는다
    ///   ([캠택맵 2026-08-27] NetworkWaferMapFormat: Rad=LoadWaferMapTextOrThrow / Camtek=LoadCamtekWaferMapTextOrThrow).
    ///   원격 접근 불가 시 로컬 캐시본 폴백(재기동/단절 중 같은 웨이퍼 재처리 대응).
    /// </summary>
    internal static class LotWaferMapFetchService
    {
        private const int RemoteAccessTimeoutMs = 5000;
        private const int RemoteCopyTimeoutMs = 30000;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, LotWaferMapSlotInfo> Cache =
            new Dictionary<string, LotWaferMapSlotInfo>(StringComparer.OrdinalIgnoreCase);
        // [P5] BIN 선택 다이얼로그의 "기준 맵" — 가장 최근에 수신/파싱된 맵.
        private static LotWaferMapSlotInfo _latestFetched;
        private static readonly string LocalCacheDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "WaferMap");

        public static bool IsConfigured
        {
            get { return !string.IsNullOrWhiteSpace(ResolveNetworkFolder()); }
        }

        public static string ResolveNetworkFolder()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.NetworkWaferMapFolder != null
                ? settings.NetworkWaferMapFolder.Trim()
                : "";
        }

        public static string ResolveLocalCacheDirectory()
        {
            return LocalCacheDirectory;
        }

        /// <summary>[캠택맵 2026-08-27] 설정(NetworkWaferMapFormat)이 Camtek인지. 기본/그 외 값은 Rad.</summary>
        public static bool IsCamtekFormatConfigured
        {
            get
            {
                AppSettings settings = AppSettingsStore.Current;
                return settings != null &&
                       string.Equals((settings.NetworkWaferMapFormat ?? "").Trim(), "Camtek",
                           StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// [캠택맵 2026-08-27] LOT 네트워크 맵 공통 파싱 헬퍼 — 설정 포맷에 따라 파서를 고른다
        /// (자동 판별 없음 = 팀장님 지시). 파싱 실패는 선택 포맷을 명시해 다시 던진다:
        /// 설정과 실제 파일 포맷이 다른 오설정을 예외 메시지만으로 판별할 수 있게 한다.
        /// 확장자 기반 판단 금지 — 파일명(=바코드)은 무가공 유지.
        /// </summary>
        internal static DieMap LoadConfiguredFormatOrThrow(string path)
        {
            bool camtek = IsCamtekFormatConfigured;
            try
            {
                return camtek
                    ? DieMapGenerator.LoadCamtekWaferMapTextOrThrow(path)
                    : DieMapGenerator.LoadWaferMapTextOrThrow(path);
            }
            catch (ArgumentException) { throw; }
            catch (FileNotFoundException) { throw; }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "format=" + (camtek ? "Camtek" : "Rad") +
                    " — 설정과 실제 파일 포맷이 다른지 확인하세요. " + ex.Message, ex);
            }
        }

        /// <summary>설정 화면 [연결 확인]용: 폴더 존재 + 목록 조회를 타임아웃 안에 시도한다.</summary>
        public static bool TryCheckFolderAccessible(out string detail)
        {
            return TryCheckFolderAccessible(ResolveNetworkFolder(), out detail);
        }

        private static bool TryCheckFolderAccessible(string folder, out string detail)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                detail = "네트워크 웨이퍼맵 폴더가 설정되지 않았습니다(기능 꺼짐).";
                return false;
            }

            string capturedDetail = "";
            bool ok = RunWithTimeout(() =>
            {
                if (!Directory.Exists(folder))
                {
                    capturedDetail = "폴더를 찾을 수 없습니다: " + folder;
                    return false;
                }

                int fileCount = Directory.GetFiles(folder).Length;
                capturedDetail = "접근 가능. 폴더=" + folder + ", 파일 " + fileCount + "개";
                return true;
            }, RemoteAccessTimeoutMs, out string timeoutDetail);

            // [검토수정 2026-08-22] 타임아웃/예외 사유가 있으면 그것을 우선한다 — 늦게 도착한 워커의
            // capturedDetail("접근 가능...")이 실패 응답에 섞여 표시되는 레이스 방지.
            detail = !string.IsNullOrWhiteSpace(timeoutDetail) ? timeoutDetail : capturedDetail;
            return ok;
        }

        /// <summary>
        /// LOT 생성 전 폴더 접근을 확인한다. USE가 꺼져 있으면 네트워크에 접근하지 않는다.
        /// 빈 문자열은 시작 가능, 그 외 문자열은 시작을 차단할 오류 사유다.
        /// 개별 맵 파일은 기존대로 웨이퍼 바코드 판독 후 확인한다.
        /// </summary>
        public static async Task<string> CheckLotStartFolderAsync(string lotId)
        {
            string normalizedLotId = (lotId ?? "").Trim();
            string folder = "";
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return BuildLotStartFolderError(normalizedLotId, folder, "네트워크 웨이퍼맵 설정을 읽을 수 없습니다.");
                if (!settings.UseLotNetworkWaferMap)
                    return "";

                folder = ResolveNetworkFolder();
                if (string.IsNullOrWhiteSpace(folder))
                    return BuildLotStartFolderError(normalizedLotId, folder,
                        "USE가 켜져 있지만 NETWORK WAFER MAP FOLDER 경로가 비어 있습니다.");

                // UI를 멈추지 않고 기존 5초 제한 안에서 확인한다. LOT/Recipe 기록은 이 검사 후 수행한다.
                string accessDetail = "";
                bool accessible = await Task.Run(() => TryCheckFolderAccessible(folder, out accessDetail)).ConfigureAwait(false);
                AppSettings currentSettings = AppSettingsStore.Current;
                if (currentSettings == null || !currentSettings.UseLotNetworkWaferMap ||
                    !string.Equals(folder, ResolveNetworkFolder(), StringComparison.Ordinal))
                {
                    return BuildLotStartFolderError(normalizedLotId, folder,
                        "접근 확인 중 USE 또는 폴더 설정이 변경되었습니다. 현재 설정을 확인한 후 다시 시작하세요.");
                }
                if (!accessible)
                    return BuildLotStartFolderError(normalizedLotId, folder, accessDetail);

                EventLogger.Write(EventKind.Event, "SYSTEM", "LOT-MAP-FETCH",
                    "LOT 시작 전 웨이퍼맵 폴더 접근 확인 완료. lot=" + normalizedLotId +
                    ", detail=" + accessDetail + " (맵 파일은 웨이퍼 바코드 판독 후 확인)");
                return "";
            }
            catch (Exception ex)
            {
                return BuildLotStartFolderError(normalizedLotId, folder, "폴더 접근 확인 중 오류: " + ex.Message);
            }
        }

        private static string BuildLotStartFolderError(string lotId, string folder, string detail)
        {
            string message = "네트워크 웨이퍼맵 폴더 접근 확인에 실패했습니다.\r\n" + detail +
                "\r\n경로: " + (string.IsNullOrWhiteSpace(folder) ? "(미설정)" : folder) +
                "\r\n\r\nLOT을 시작하지 않았습니다. 설정/연결을 확인한 후 다시 시작하세요.";
            EventLogger.Write(EventKind.Alarm, "SYSTEM", "LOT-MAP-START-BLOCKED",
                "LOT 시작 차단. lot=" + lotId + ", folder=" + folder + ", detail=" + detail);
            return message;
        }

        /// <summary>[P5] BIN 선택 다이얼로그용: 가장 최근에 수신/파싱된 맵(없으면 false).</summary>
        public static bool TryGetLatestFetchedInfo(out LotWaferMapSlotInfo info)
        {
            lock (Sync)
                info = _latestFetched;
            return info != null;
        }

        /// <summary>
        /// [P5 2026-08-24] 바코드로 웨이퍼맵 1개 수신: 원격 &lt;폴더&gt;\&lt;바코드&gt; 확인 → 로컬 캐시 복사
        /// (원격이 더 새것일 때만, 임시명 복사 후 교체) → 파싱 → 캐시 등록.
        /// 원격 접근 불가(타임아웃/단절)여도 로컬 캐시본이 있으면 Warning 후 그것으로 진행한다.
        /// 동기 호출이지만 내부 원격 접근은 전부 타임아웃 래핑이라 상한이 있다(최대 30초).
        /// </summary>
        public static bool TryFetchWaferMapByBarcode(string barcode, out LotWaferMapSlotInfo info, out string reason)
        {
            info = null;
            reason = "";

            string fileName = (barcode ?? "").Trim();
            string nameProblem;
            if (!IsValidBarcodeFileName(fileName, out nameProblem))
            {
                reason = "바코드가 파일명으로 유효하지 않습니다: " + nameProblem;
                return false;
            }

            string folder = ResolveNetworkFolder();
            if (string.IsNullOrWhiteSpace(folder))
            {
                reason = "네트워크 웨이퍼맵 폴더가 설정되지 않았습니다.";
                return false;
            }

            string remotePath = Path.Combine(folder, fileName);
            string localDir = ResolveLocalCacheDirectory();
            string localPath = Path.Combine(localDir, fileName);

            // 원격 최신 여부를 매번 확인한다(웨이퍼당 1회 호출이라 부담 없음). 파일이 같으면
            // 복사/재파싱은 아래 신선도 단락이 생략한다.
            string copyFailure = "";
            bool copied = RunWithTimeout(() =>
            {
                if (!File.Exists(remotePath))
                {
                    // 원격에 없어도 로컬 캐시가 있으면 그것으로 진행한다(네트워크 단절/재기동 대비).
                    if (File.Exists(localPath))
                        return true;
                    copyFailure = "원격 파일이 없습니다: " + remotePath;
                    return false;
                }

                Directory.CreateDirectory(localDir);
                DateTime remoteWriteUtc = File.GetLastWriteTimeUtc(remotePath);
                if (File.Exists(localPath) &&
                    File.GetLastWriteTimeUtc(localPath) >= remoteWriteUtc &&
                    new FileInfo(localPath).Length == new FileInfo(remotePath).Length)
                {
                    return true; // 로컬 캐시가 최신 — 복사 생략.
                }

                string tempPath = localPath + ".fetch.tmp";
                File.Copy(remotePath, tempPath, true);
                try
                {
                    if (File.Exists(localPath))
                        File.Delete(localPath);
                    File.Move(tempPath, localPath);
                    File.SetLastWriteTimeUtc(localPath, remoteWriteUtc);
                }
                catch (IOException replaceEx)
                {
                    // [2차 검토수정 2026-08-23] 교체 경합: 기존 로컬 본이 살아 있으면 그것으로 진행하고
                    // 다음 조회에서 교체를 재시도한다. 기존 본마저 없으면 실패로 전파한다.
                    if (!File.Exists(localPath))
                        throw;
                    EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                        "로컬 캐시 교체 실패 — 기존 캐시본으로 진행(다음 조회에서 재시도). file=" +
                        fileName + ", error=" + replaceEx.Message);
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                            "임시 수신 파일 정리 실패(다음 수신 시 덮어씀). file=" + tempPath +
                            ", error=" + cleanupEx.Message);
                    }
                }
                return true;
            }, RemoteCopyTimeoutMs, out string timeoutDetail);

            if (!copied)
            {
                string remoteFailure = string.IsNullOrWhiteSpace(copyFailure) ? timeoutDetail : copyFailure;

                // [2차 검토수정 2026-08-23] 로컬 캐시 폴백은 타임아웃 래퍼 밖에서 — 원격이 행이면
                // 래퍼 안 워커는 File.Exists(remotePath)부터 묶여 로컬 확인조차 못 한다.
                if (File.Exists(localPath))
                {
                    EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                        "원격 웨이퍼맵 접근 실패 — 로컬 캐시본으로 진행합니다. file=" + fileName +
                        ", 원격실패=" + remoteFailure);
                }
                else
                {
                    reason = remoteFailure;
                    return false;
                }
            }

            // 신선도 단락: 이미 파싱된 항목이 있고 로컬 파일이 그때와 동일하면 재파싱 생략.
            if (TryGetFreshCachedInfo(fileName, localPath, out info))
            {
                lock (Sync)
                    _latestFetched = info;
                return true;
            }

            try
            {
                LotWaferMapSlotInfo parsed = ParseLocalFile(fileName, localPath);
                lock (Sync)
                {
                    Cache[fileName] = parsed;
                    _latestFetched = parsed;
                }
                info = parsed;
                return true;
            }
            catch (Exception ex)
            {
                reason = "웨이퍼맵 파싱 실패: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// [P5] 바코드 → 파일명 유효성: 경로 탈출/예약 문자 차단(fail-closed).
        /// 바코드가 곧 파일명이므로 판독 오염 문자열이 폴더 밖 경로를 만들지 못하게 한다.
        /// </summary>
        private static bool IsValidBarcodeFileName(string fileName, out string problem)
        {
            problem = "";
            if (string.IsNullOrWhiteSpace(fileName))
            {
                problem = "빈 문자열";
                return false;
            }

            if (fileName == "." || fileName == "..")
            {
                problem = "예약 경로명";
                return false;
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                fileName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                problem = "파일명에 쓸 수 없는 문자 포함: " + fileName;
                return false;
            }

            return true;
        }

        // [검토수정 2026-08-22] 캐시 항목이 존재하고 로컬 파일의 수정시각/크기가 파싱 당시와 같으면 반환.
        private static bool TryGetFreshCachedInfo(string fileName, string localPath, out LotWaferMapSlotInfo info)
        {
            lock (Sync)
            {
                if (!Cache.TryGetValue(fileName, out info) || info == null)
                {
                    info = null;
                    return false;
                }
            }

            try
            {
                var fi = new FileInfo(localPath);
                if (fi.Exists &&
                    fi.LastWriteTimeUtc == info.FileWriteUtc &&
                    fi.Length == info.FileLength)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                    "캐시 신선도 확인 실패(재파싱으로 진행). file=" + fileName + ", error=" + ex.Message);
            }

            info = null;
            return false;
        }

        private static LotWaferMapSlotInfo ParseLocalFile(string barcode, string localPath)
        {
            // [2차 검토수정 2026-08-23] 파일 스탯을 파싱 "전"에 로컬 변수로 확정한다 — FileInfo 속성은
            // 첫 접근 시점에 stat하므로 파싱 중 파일이 교체되면 "새 파일의 스탯 + 옛 파일의 내용"이
            // 캐시에 남아 신선도 검사가 영구히 통과했다.
            var fileStat = new FileInfo(localPath);
            DateTime fileWriteUtc = fileStat.LastWriteTimeUtc;
            long fileLength = fileStat.Length;

            // [캠택맵 2026-08-27] 설정 포맷(Rad/Camtek)에 따라 파서 선택.
            DieMap map = LoadConfiguredFormatOrThrow(localPath);
            var info = new LotWaferMapSlotInfo
            {
                Barcode = barcode,
                LocalPath = localPath,
                DieCount = map != null && map.Entries != null ? map.Entries.Count : 0,
                FileWriteUtc = fileWriteUtc,
                FileLength = fileLength,
                ParsedAtUtc = DateTime.UtcNow
            };

            if (map != null && map.Entries != null)
            {
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;
                    int bin = entry.BinCode;
                    int count;
                    info.BinDieCounts.TryGetValue(bin, out count);
                    info.BinDieCounts[bin] = count + 1;
                }
            }

            ReadHeaderExtras(localPath, info);
            return info;
        }

        // [P4 2026-08-22, 검토수정 2026-08-22] 헤더 부가정보를 한 번의 리더로 읽는다:
        //  - 1행 [..] 안 첫 토큰 = 팹 내부 웨이퍼 ID(기록용 — 알람 판정 금지)
        //  - BO=(bin 번호 목록) / BN=(같은 순서의 이름 목록) → 번호→이름 사전
        // 실패는 Warning 1줄 남기고 부가정보 없이 진행한다(표시 보조 정보이므로 파싱 실패로 승격하지 않음).
        // [캠택맵 2026-08-27] Camtek 설정이면 LOT:/WAFER: 헤더를 대신 읽는다(아래 전용 리더).
        private static void ReadHeaderExtras(string path, LotWaferMapSlotInfo info)
        {
            if (IsCamtekFormatConfigured)
            {
                ReadCamtekHeaderExtras(path, info);
                return;
            }

            try
            {
                string binOrderLine = null;
                string binNameLine = null;
                using (var reader = new StreamReader(path))
                {
                    for (int i = 0; i < 80; i++)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;

                        if (i == 0)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("[", StringComparison.Ordinal))
                            {
                                // [2차 검토수정 2026-08-23] '[' 뒤 공백을 먼저 걷어내고 cut>=0으로 판정.
                                string inner = trimmed.TrimStart('[').TrimStart();
                                int cut = inner.IndexOfAny(new[] { ' ', '\t', ']' });
                                info.InternalMapId = (cut >= 0 ? inner.Substring(0, cut) : inner).Trim();
                            }
                        }

                        if (binOrderLine == null && line.StartsWith("BO=", StringComparison.OrdinalIgnoreCase))
                            binOrderLine = line.Substring(3);
                        else if (binNameLine == null && line.StartsWith("BN=", StringComparison.OrdinalIgnoreCase))
                            binNameLine = line.Substring(3);

                        if (binOrderLine != null && binNameLine != null && i > 0)
                            break;
                    }
                }

                if (binOrderLine == null || binNameLine == null)
                    return;

                string[] orders = binOrderLine.Split(',');
                string[] names = binNameLine.Split(',');
                int count = Math.Min(orders.Length, names.Length);
                for (int i = 0; i < count; i++)
                {
                    int bin;
                    if (!int.TryParse(orders[i].Trim(), out bin) || bin <= 0)
                        continue;
                    string name = (names[i] ?? "").Trim();
                    if (!string.IsNullOrEmpty(name) && !info.BinNames.ContainsKey(bin))
                        info.BinNames[bin] = name;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                    "웨이퍼맵 헤더 부가정보(BIN 이름/내부 ID) 읽기 실패 — 표시 정보 없이 진행. file=" +
                    Path.GetFileName(path) + ", error=" + ex.Message);
            }
        }

        // [캠택맵 2026-08-27] Camtek 헤더 부가정보: LOT:+WAFER: → InternalMapId = "LOT.WAFER"
        // (파일명=바코드와의 대조 "기록용" — 기존 알람 판정 금지 정책 그대로).
        // BinNames는 빈 사전 유지 — 캠택엔 빈 이름 정보가 없어 BIN 선택 다이얼로그는 번호-만 폴백으로 동작.
        // 실패는 Warning 1줄 남기고 부가정보 없이 진행한다(RAD 리더와 동일 정책).
        private static void ReadCamtekHeaderExtras(string path, LotWaferMapSlotInfo info)
        {
            try
            {
                string lot = null;
                string wafer = null;
                using (var reader = new StreamReader(path))
                {
                    for (int i = 0; i < 80; i++)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;

                        string text = line.Trim();
                        if (lot == null && text.StartsWith("LOT:", StringComparison.OrdinalIgnoreCase))
                            lot = text.Substring(4).Trim();
                        else if (wafer == null && text.StartsWith("WAFER:", StringComparison.OrdinalIgnoreCase))
                            wafer = text.Substring(6).Trim();

                        if (lot != null && wafer != null)
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(lot) && !string.IsNullOrWhiteSpace(wafer))
                    info.InternalMapId = lot + "." + wafer;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "SYSTEM", "LOT-MAP-FETCH",
                    "캠택 웨이퍼맵 헤더 부가정보(LOT/WAFER) 읽기 실패 — 표시 정보 없이 진행. file=" +
                    Path.GetFileName(path) + ", error=" + ex.Message);
            }
        }

        /// <summary>
        /// UNC 파일 작업을 백그라운드 스레드에서 실행하고 제한 시간만 기다린다.
        /// 시간 초과 시 false (작업 스레드는 자연 종료되게 둔다 — SMB 블로킹은 취소가 불가능하다).
        /// </summary>
        private static bool RunWithTimeout(Func<bool> action, int timeoutMs, out string timeoutDetail)
        {
            timeoutDetail = "";
            try
            {
                Task<bool> task = Task.Run(action);
                if (!task.Wait(timeoutMs))
                {
                    timeoutDetail = "네트워크 응답 제한 시간(" + timeoutMs + "ms)을 초과했습니다.";
                    return false;
                }
                return task.Result;
            }
            catch (AggregateException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                timeoutDetail = "네트워크 접근 오류: " + inner.Message;
                return false;
            }
            catch (Exception ex)
            {
                timeoutDetail = "네트워크 접근 오류: " + ex.Message;
                return false;
            }
        }
    }
}
