using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QMC.CDT320.Materials.Persistence
{
    /// <summary>
    /// Per-Wafer JSON + Manifest 코어 store (문서 §8~§10).
    /// - store instance root는 생성자 주입 (production 정적 바인딩과 분리 — 테스트는 격리 temp 사용).
    /// - Manifest 슬롯 3개 / 문서 revision 슬롯 3개 고정 ring — 파일 수 무한 증가 없음.
    /// - 원자 쓰기: 같은 디렉터리 tmp → File.Replace(대상 존재 시) / File.Move.
    /// - 문서 재사용: canonical 바이트 SHA-256이 직전 commit과 같으면 파일을 다시 쓰지 않는다.
    /// - 최신 상태 선택: 파일 mtime/크기를 쓰지 않고 (payload hash 검증 통과 manifest 중
    ///   최고 Generation)만 사용한다. Epoch 불일치는 Recovery Required로 거부한다.
    /// [dormant] 이 클래스는 아직 운영 저장 경로에 배선되지 않았다.
    /// </summary>
    internal sealed class PwjStore
    {
        public const int SlotCount = 3;
        private const string ManifestPrefix = "material-manifest.";
        private const string WafersDirName = "wafers";
        private const string DocFilePrefix = "state.";

        private readonly string _root;
        private readonly string _writerSessionId;

        public PwjStore(string storeInstanceRoot, string writerSessionId)
        {
            if (string.IsNullOrWhiteSpace(storeInstanceRoot))
                throw new ArgumentNullException("storeInstanceRoot");
            _root = Path.GetFullPath(storeInstanceRoot);
            _writerSessionId = string.IsNullOrWhiteSpace(writerSessionId)
                ? Guid.NewGuid().ToString("N")
                : writerSessionId;
        }

        public string Root { get { return _root; } }

        // ------------------------------------------------------------------ 경로/안전

        private string ResolveInside(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) ||
                relativePath.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                relativePath.IndexOf(':') >= 0)
                throw new InvalidOperationException("허용되지 않는 상대 경로입니다. path=" + relativePath);

            string full = Path.GetFullPath(Path.Combine(_root, relativePath));
            if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("경로가 store root를 벗어납니다. path=" + relativePath);
            return full;
        }

        private void EnsureDirectoryNoReparse(string fullDir)
        {
            // root부터 한 component씩 생성/검사 — reparse point(정션/심링크)는 거부한다(§8).
            string current = _root;
            RejectReparse(current);
            string remainder = fullDir.Substring(_root.Length).Trim(Path.DirectorySeparatorChar);
            if (remainder.Length == 0)
                return;
            foreach (string part in remainder.Split(Path.DirectorySeparatorChar))
            {
                current = Path.Combine(current, part);
                if (!Directory.Exists(current))
                    Directory.CreateDirectory(current);
                RejectReparse(current);
            }
        }

        private static void RejectReparse(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            FileAttributes attributes = File.GetAttributes(dir);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Reparse point 경로는 허용하지 않습니다. path=" + dir);
        }

        private static void WriteFileAtomic(string fullPath, byte[] payload)
        {
            string tmp = fullPath + ".tmp";
            // [리뷰 반영 2026-08-05] 전원 차단 시 rename 메타데이터만 살아남고 데이터가 유실되는
            // 창을 줄이기 위해 Flush(true)로 디스크 반영을 강제한다 (§14).
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(payload, 0, payload.Length);
                stream.Flush(true);
            }
            if (File.Exists(fullPath))
            {
                string bak = fullPath + ".rep";
                File.Replace(tmp, fullPath, bak, true);
                try { File.Delete(bak); } catch { }
            }
            else
            {
                File.Move(tmp, fullPath);
            }
        }

        // ------------------------------------------------------------------ Save

        /// <summary>
        /// MaterialSnapshot을 wafer 문서들로 분해해 저장하고 새 Manifest generation을 커밋한다.
        /// 반환: 커밋된 Manifest. 문서 바이트가 직전 커밋과 같으면 해당 문서는 재기록하지 않는다.
        /// </summary>
        public PwjManifest Save(MaterialSnapshot snapshot, string commitKind, long capturedSaveVersion)
        {
            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            // [리뷰 반영 2026-08-05] BLOCKED 종류(RedundancyRepair/RetirementBarrier)와 임의 문자열은
            // 배선 전 오용을 막기 위해 저장 시점에 거부한다.
            if (!string.Equals(commitKind, "NormalSave", StringComparison.Ordinal) &&
                !string.Equals(commitKind, "Migration", StringComparison.Ordinal))
                throw new NotSupportedException("지원하지 않는 CommitKind입니다. kind=" + (commitKind ?? "<null>"));

            EnsureDirectoryNoReparse(_root);
            EnsureDirectoryNoReparse(Path.Combine(_root, WafersDirName));

            PwjManifest previous = TryLoadLatestManifestHeader();
            // [리뷰 반영] stale save 방어(§15): 이전 커밋보다 낮은 SnapshotRevision은 커밋을 거부한다 —
            // 커밋 후 Load가 거부하는 비대칭(스토어 brick)을 저장 시점 차단으로 대체한다.
            if (previous != null && snapshot.SnapshotRevision < previous.SnapshotRevision)
                throw new InvalidOperationException(
                    "SnapshotRevision 역행 저장을 거부합니다. incoming=" + snapshot.SnapshotRevision +
                    ", committed=" + previous.SnapshotRevision);
            string epochId = previous != null && !string.IsNullOrWhiteSpace(previous.EpochId)
                ? previous.EpochId
                : Guid.NewGuid().ToString("N");
            long generation = previous != null ? previous.Generation + 1 : 1;

            var previousEntries = new Dictionary<string, PwjManifestEntry>(StringComparer.OrdinalIgnoreCase);
            if (previous != null && previous.Documents != null)
                foreach (PwjManifestEntry entry in previous.Documents)
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.StableId))
                        previousEntries[entry.StableId] = entry;

            // --- 문서 분해: 전역 Dies 순서 링크를 만들며 입력 instance 기준으로 소유를 나눈다.
            List<PwjWaferDocument> documents = DecomposeSnapshot(snapshot);

            var manifest = new PwjManifest();
            manifest.MaterialSnapshotVersion = snapshot.Version;
            manifest.EpochId = epochId;
            manifest.Generation = generation;
            manifest.SnapshotRevision = snapshot.SnapshotRevision;
            manifest.CommitKind = commitKind ?? "NormalSave";
            manifest.WriterSessionId = _writerSessionId;
            manifest.CapturedSaveVersion = capturedSaveVersion;
            manifest.SnapshotSavedAtUtcTicks = snapshot.SavedAt.ToUniversalTime().Ticks;
            manifest.CommittedAtUtcTicks = DateTime.UtcNow.Ticks;
            manifest.SaveReason = snapshot.SaveReason ?? "";
            manifest.RecipeName = snapshot.RecipeName ?? "";
            manifest.LotId = snapshot.LotId ?? "";
            manifest.PickupBinMode = snapshot.PickupBinMode ?? "All";
            manifest.PickupBinNumbers = snapshot.PickupBinNumbers != null
                ? new List<int>(snapshot.PickupBinNumbers)
                : new List<int>();
            manifest.CassettesPayloadBase64 = Convert.ToBase64String(
                PwjCanonical.Serialize(snapshot.Cassettes ?? new List<CassetteMaterial>()));
            manifest.TotalWaferCount = snapshot.Wafers != null ? snapshot.Wafers.Count(w => w != null) : 0;
            manifest.TotalDieCount = snapshot.Dies != null ? snapshot.Dies.Count(d => d != null) : 0;

            foreach (PwjWaferDocument document in documents)
            {
                byte[] payload = PwjCanonical.Serialize(document);
                string hash = PwjCanonical.Sha256Hex(payload);

                PwjManifestEntry previousEntry;
                PwjManifestEntry entry;
                if (previousEntries.TryGetValue(document.StableId, out previousEntry) &&
                    string.Equals(previousEntry.Sha256, hash, StringComparison.OrdinalIgnoreCase) &&
                    IsReusableDocumentFileIntact(previousEntry))
                {
                    // 내용 불변 + 디스크 실물 검증 통과 → 이전 파일 참조 재사용 (§10).
                    // [리뷰 반영 2026-08-05] 실물 미검증 재사용은 소실/손상 참조를 전 세대로
                    // 전파시키므로, 검증 실패 시 재기록으로 자기치유한다.
                    entry = previousEntry;
                }
                else
                {
                    int nextSlot = previousEntry != null
                        ? (ResolveSlotFromPath(previousEntry.RelativePath) + 1) % SlotCount
                        : 0;
                    string storageKey = document.DocumentKind == PwjWaferDocument.KindOrphanDies
                        ? PwjWaferDocument.OrphanStableId
                        : PwjCanonical.BuildStorageKey(document.StableId);
                    string relative = WafersDirName + Path.DirectorySeparatorChar + storageKey +
                                      Path.DirectorySeparatorChar + DocFilePrefix + nextSlot + ".json";
                    string full = ResolveInside(relative);
                    EnsureDirectoryNoReparse(Path.GetDirectoryName(full));
                    WriteFileAtomic(full, payload);

                    entry = new PwjManifestEntry
                    {
                        DocumentKind = document.DocumentKind,
                        StableId = document.StableId,
                        RelativePath = relative,
                        DocumentSchemaVersion = document.DocumentSchemaVersion,
                        ByteLength = payload.LongLength,
                        Sha256 = hash,
                        WaferCount = document.Wafer != null ? 1 : 0,
                        DieCount = document.Dies != null ? document.Dies.Count : 0
                    };
                }

                manifest.Documents.Add(entry);
                if (document.Wafer != null)
                    manifest.OrderedWaferInstanceIds.Add(document.StableId);
            }

            manifest.NormalizedSnapshotSha256 = ComputeNormalizedSnapshotSha256(manifest);
            manifest.ManifestPayloadSha256 = "";
            byte[] manifestPayload = PwjCanonical.Serialize(manifest);
            manifest.ManifestPayloadSha256 = PwjCanonical.Sha256Hex(manifestPayload);
            byte[] finalPayload = PwjCanonical.Serialize(manifest);

            int manifestSlot = (int)(generation % SlotCount);
            WriteFileAtomic(ResolveInside(ManifestPrefix + manifestSlot + ".json"), finalPayload);
            return manifest;
        }

        private bool IsReusableDocumentFileIntact(PwjManifestEntry entry)
        {
            try
            {
                string full = ResolveInside(entry.RelativePath);
                if (!File.Exists(full))
                    return false;
                byte[] payload = File.ReadAllBytes(full);
                return payload.LongLength == entry.ByteLength &&
                       string.Equals(PwjCanonical.Sha256Hex(payload), entry.Sha256, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static int ResolveSlotFromPath(string relativePath)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(relativePath ?? "");
                int dot = name.LastIndexOf('.');
                int slot;
                if (dot >= 0 && int.TryParse(name.Substring(dot + 1), out slot))
                    return ((slot % SlotCount) + SlotCount) % SlotCount;
            }
            catch { }
            return 0;
        }

        private static List<PwjWaferDocument> DecomposeSnapshot(MaterialSnapshot snapshot)
        {
            var byInstance = new Dictionary<string, PwjWaferDocument>(StringComparer.OrdinalIgnoreCase);
            var documents = new List<PwjWaferDocument>();

            if (snapshot.Wafers != null)
            {
                foreach (WaferMaterial wafer in snapshot.Wafers)
                {
                    if (wafer == null)
                        continue;
                    string instanceId = (wafer.WaferInstanceId ?? "").Trim();
                    if (instanceId.Length == 0)
                        throw new InvalidOperationException(
                            "WaferInstanceId가 비어 있는 wafer는 저장할 수 없습니다. wafer=" + (wafer.WaferId ?? ""));
                    if (byInstance.ContainsKey(instanceId))
                        throw new InvalidOperationException(
                            "중복 WaferInstanceId가 있습니다. instance=" + instanceId);

                    var document = new PwjWaferDocument
                    {
                        DocumentKind = PwjWaferDocument.KindWafer,
                        StableId = instanceId,
                        Wafer = wafer
                    };
                    byInstance[instanceId] = document;
                    documents.Add(document);
                }
            }

            var orphan = new PwjWaferDocument
            {
                DocumentKind = PwjWaferDocument.KindOrphanDies,
                StableId = PwjWaferDocument.OrphanStableId,
                Wafer = null
            };

            string previousDieId = "";   // 전역 head sentinel = 빈 문자열
            // [리뷰 반영 2026-08-05] 공백/중복 DieId는 순서 체인을 오염시켜 Load를 영구 실패시키므로
            // 저장 시점에 거부한다 (§17 6항).
            var seenDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot.Dies != null)
            {
                foreach (DieMaterial die in snapshot.Dies)
                {
                    if (die == null)
                        continue;
                    if (string.IsNullOrWhiteSpace(die.DieId))
                        throw new InvalidOperationException("DieId가 비어 있는 die는 저장할 수 없습니다.");
                    if (!seenDieIds.Add(die.DieId))
                        throw new InvalidOperationException("중복 DieId가 있어 저장할 수 없습니다. dieId=" + die.DieId);
                    PwjWaferDocument owner;
                    string ownerInstance = (die.InputWaferInstanceId ?? "").Trim();
                    if (ownerInstance.Length == 0 || !byInstance.TryGetValue(ownerInstance, out owner))
                        owner = orphan;
                    owner.Dies.Add(die);
                    owner.PreviousGlobalDieIds.Add(previousDieId);
                    previousDieId = die.DieId ?? "";
                }
            }

            if (orphan.Dies.Count > 0)
                documents.Add(orphan);
            return documents;
        }

        /// <summary>
        /// §9 logical digest: 물리 slot 경로/commit 메타데이터에 독립인 논리 상태 descriptor의 SHA-256.
        /// RelativePath/Generation/CommitKind/WriterSessionId/CapturedSaveVersion/CommittedAt은 제외한다.
        /// </summary>
        private static string ComputeNormalizedSnapshotSha256(PwjManifest manifest)
        {
            var builder = new StringBuilder();
            builder.Append(manifest.MaterialSnapshotVersion).Append('\n');
            builder.Append(manifest.SnapshotRevision).Append('\n');
            builder.Append(manifest.SnapshotSavedAtUtcTicks).Append('\n');
            builder.Append(manifest.SaveReason).Append('\n');
            builder.Append(manifest.RecipeName).Append('\n');
            builder.Append(manifest.LotId).Append('\n');
            // [2차 검토수정 2026-08-23] 픽업 BIN 선택도 "무엇을 집을지"를 결정하는 논리 상태다 —
            // digest에서 빠지면 이 필드만 손상돼도 검증을 통과해 잘못된 bin 선택으로 가동된다.
            // (dormant 계층이라 기존 파일과의 digest 호환 부담 없음.)
            builder.Append(manifest.PickupBinMode ?? "All").Append('\n');
            builder.Append(manifest.PickupBinNumbers != null
                ? string.Join(",", manifest.PickupBinNumbers)
                : "").Append('\n');
            builder.Append(manifest.CassettesPayloadBase64).Append('\n');
            foreach (string id in manifest.OrderedWaferInstanceIds)
                builder.Append(id).Append('\n');
            foreach (PwjManifestEntry entry in manifest.Documents)
            {
                builder.Append(entry.DocumentKind).Append('|')
                    .Append((entry.StableId ?? "").Trim().ToLowerInvariant()).Append('|')
                    .Append(entry.DocumentSchemaVersion).Append('|')
                    .Append(entry.ByteLength).Append('|')
                    .Append(entry.Sha256).Append('|')
                    .Append(entry.WaferCount).Append('|')
                    .Append(entry.DieCount).Append('\n');
            }
            builder.Append(manifest.TotalWaferCount).Append('\n');
            builder.Append(manifest.TotalDieCount).Append('\n');
            return PwjCanonical.Sha256Hex(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        // ------------------------------------------------------------------ Load

        /// <summary>유효(payload hash 통과) manifest 중 최고 Generation의 헤더만 반환. 없으면 null.</summary>
        public PwjManifest TryLoadLatestManifestHeader()
        {
            PwjManifest best = null;
            string epoch = null;
            for (int slot = 0; slot < SlotCount; slot++)
            {
                string path = Path.Combine(_root, ManifestPrefix + slot + ".json");
                if (!File.Exists(path))
                    continue;
                PwjManifest candidate = TryParseManifest(File.ReadAllBytes(path));
                if (candidate == null)
                    continue;
                if (epoch == null)
                    epoch = candidate.EpochId;
                else if (!string.Equals(epoch, candidate.EpochId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "서로 다른 EpochId의 Manifest가 공존합니다. Recovery Required. (§9)");
                if (best == null || candidate.Generation > best.Generation)
                {
                    if (best != null && candidate.Generation > best.Generation &&
                        candidate.SnapshotRevision < best.SnapshotRevision)
                        throw new InvalidOperationException(
                            "더 높은 Generation의 SnapshotRevision이 더 낮습니다. 손상으로 거부합니다. (§9)");
                    best = candidate;
                }
            }
            return best;
        }

        private static PwjManifest TryParseManifest(byte[] payload)
        {
            try
            {
                PwjManifest manifest = PwjCanonical.Deserialize<PwjManifest>(payload);
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.ManifestPayloadSha256))
                    return null;
                string claimed = manifest.ManifestPayloadSha256;
                manifest.ManifestPayloadSha256 = "";
                string actual = PwjCanonical.Sha256Hex(PwjCanonical.Serialize(manifest));
                manifest.ManifestPayloadSha256 = claimed;
                return string.Equals(claimed, actual, StringComparison.OrdinalIgnoreCase) ? manifest : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 최신 유효 Manifest에서 MaterialSnapshot을 재조립한다. 문서 hash/logical digest를 전부
        /// 검증하고, 전역 die 체인(PreviousGlobalDieId)으로 Dies 순서를 복원한다. 없으면 null.
        /// </summary>
        public MaterialSnapshot Load()
        {
            PwjManifest manifest = TryLoadLatestManifestHeader();
            if (manifest == null)
                return null;

            var snapshot = new MaterialSnapshot();
            snapshot.Version = manifest.MaterialSnapshotVersion;
            snapshot.SnapshotRevision = manifest.SnapshotRevision;
            snapshot.SavedAt = new DateTime(manifest.SnapshotSavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            snapshot.SaveReason = manifest.SaveReason ?? "";
            snapshot.RecipeName = manifest.RecipeName ?? "";
            snapshot.LotId = manifest.LotId ?? "";
            snapshot.PickupBinMode = string.IsNullOrWhiteSpace(manifest.PickupBinMode) ? "All" : manifest.PickupBinMode;
            snapshot.PickupBinNumbers = manifest.PickupBinNumbers != null
                ? new List<int>(manifest.PickupBinNumbers)
                : new List<int>();
            snapshot.Cassettes = PwjCanonical.Deserialize<List<CassetteMaterial>>(
                Convert.FromBase64String(manifest.CassettesPayloadBase64 ?? "")) ?? new List<CassetteMaterial>();

            var documentsById = new Dictionary<string, PwjWaferDocument>(StringComparer.OrdinalIgnoreCase);
            foreach (PwjManifestEntry entry in manifest.Documents ?? new List<PwjManifestEntry>())
            {
                byte[] payload = File.ReadAllBytes(ResolveInside(entry.RelativePath));
                if (payload.LongLength != entry.ByteLength ||
                    !string.Equals(PwjCanonical.Sha256Hex(payload), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "문서 hash 불일치. stableId=" + entry.StableId + ", path=" + entry.RelativePath);
                PwjWaferDocument document = PwjCanonical.Deserialize<PwjWaferDocument>(payload);
                if (document == null || !string.Equals(document.StableId, entry.StableId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("문서 StableId 불일치. entry=" + entry.StableId);
                documentsById[entry.StableId] = document;
            }

            // Wafers: Manifest 순서(§9 OrderedWaferInstanceIds)로 복원
            foreach (string instanceId in manifest.OrderedWaferInstanceIds ?? new List<string>())
            {
                PwjWaferDocument document;
                if (!documentsById.TryGetValue(instanceId, out document) || document.Wafer == null)
                    throw new InvalidOperationException("Ordered wafer 문서를 찾을 수 없습니다. instance=" + instanceId);
                snapshot.Wafers.Add(document.Wafer);
            }

            // Dies: 전역 체인 복원 — head(선행자 "") 1개, 비순환, 전 die 정확히 1회 방문(§10)
            var dieByPrev = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            int totalPersistedDies = 0;
            foreach (PwjWaferDocument document in documentsById.Values)
            {
                if (document.Dies == null)
                    continue;
                if (document.PreviousGlobalDieIds == null || document.PreviousGlobalDieIds.Count != document.Dies.Count)
                    throw new InvalidOperationException("문서의 die 순서 링크 수가 die 수와 다릅니다. stableId=" + document.StableId);
                for (int i = 0; i < document.Dies.Count; i++)
                {
                    string prev = document.PreviousGlobalDieIds[i] ?? "";
                    if (dieByPrev.ContainsKey(prev))
                        throw new InvalidOperationException("die 순서 체인에 중복 선행자가 있습니다. prev=" + prev);
                    dieByPrev[prev] = document.Dies[i];
                    totalPersistedDies++;
                }
            }

            string cursor = "";
            while (dieByPrev.Count > 0)
            {
                DieMaterial next;
                if (!dieByPrev.TryGetValue(cursor, out next))
                    throw new InvalidOperationException(
                        "die 순서 체인이 끊겼습니다. cursor=" + cursor + ", remaining=" + dieByPrev.Count);
                snapshot.Dies.Add(next);
                dieByPrev.Remove(cursor);
                cursor = next.DieId ?? "";
            }

            if (snapshot.Dies.Count != manifest.TotalDieCount || totalPersistedDies != manifest.TotalDieCount)
                throw new InvalidOperationException(
                    "재조립 die 수가 Manifest와 다릅니다. rebuilt=" + snapshot.Dies.Count +
                    ", manifest=" + manifest.TotalDieCount);
            if (snapshot.Wafers.Count != manifest.TotalWaferCount)
                throw new InvalidOperationException(
                    "재조립 wafer 수가 Manifest와 다릅니다. rebuilt=" + snapshot.Wafers.Count +
                    ", manifest=" + manifest.TotalWaferCount);

            string recomputed = ComputeNormalizedSnapshotSha256(manifest);
            if (!string.Equals(recomputed, manifest.NormalizedSnapshotSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("NormalizedSnapshotSha256 불일치 — logical digest 검증 실패.");

            return snapshot;
        }
    }
}
