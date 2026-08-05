using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace QMC.CDT320.Materials.Persistence
{
    /// <summary>
    /// Per-Wafer JSON + Manifest 영속 형식의 모델/직렬화/해시 계약.
    /// 기준: docs/material-state/material-per-wafer-json-manifest-implementation-prompt.txt §8~§10.
    /// [야간 1차 구현 범위] NormalSave/Migration CommitKind만 완전 지원.
    /// RedundancyRepair/RetirementBarrier는 필드만 존재(BLOCKED — stage2_report.md §6-2).
    /// 이 계층은 아직 어떤 운영 경로에도 배선되지 않았다(dormant).
    /// </summary>
    internal static class PwjCanonical
    {
        /// <summary>
        /// Canonical 직렬화: DataContractJsonSerializer 출력은 (동일 데이터 → 동일 바이트)가
        /// 보장된다 — 멤버 순서는 DataMember 선언 순서로 고정, 공백/들여쓰기 없음, BOM 없음,
        /// DateTime은 틱 기반(\/Date()\/) 표현이라 문화권/시각대 독립이다.
        /// 문서 §10 canonical 계약(BOM 없음/공백 없음/문화권 독립)을 이 성질로 충족한다.
        /// </summary>
        // DateTime은 기본 DCJS 동작(UTC 변환 틱 표현)이 DateTime.MinValue(KST 등 +offset 지역)에서
        // "UTC 변환 시 범위 초과" 예외를 던진다 — 레거시 스토어가 NormalizeSnapshotDateTimes로
        // 우회하는 실제 결함. 여기서는 라운드트립("o") 포맷으로 변환 없이 직렬화한다
        // (Kind 보존, invariant culture, MinValue 안전).
        private static DataContractJsonSerializerSettings CreateSettings()
        {
            return new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("o", System.Globalization.CultureInfo.InvariantCulture)
                {
                    DateTimeStyles = System.Globalization.DateTimeStyles.RoundtripKind
                }
            };
        }

        public static byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T), CreateSettings());
                serializer.WriteObject(stream, value);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] payload)
        {
            using (var stream = new MemoryStream(payload))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), CreateSettings());
                return (T)serializer.ReadObject(stream);
            }
        }

        public static string Sha256Hex(byte[] payload)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(payload ?? new byte[0]);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }

        /// <summary>
        /// canonical storage key (§8): WaferInstanceId → Trim → invariant 소문자 정규화 →
        /// UTF-8 SHA-256 full hex. 표시 문자/경로 위험 문자와 무관한 안전 키.
        /// </summary>
        public static string BuildStorageKey(string waferInstanceId)
        {
            string normalized = (waferInstanceId ?? "").Trim().ToLowerInvariant();
            if (normalized.Length == 0)
                throw new InvalidOperationException("WaferInstanceId가 비어 있어 storage key를 만들 수 없습니다.");
            return Sha256Hex(Encoding.UTF8.GetBytes(normalized));
        }
    }

    [DataContract]
    internal sealed class PwjManifest
    {
        public const int CurrentStoreFormatVersion = 1;

        [DataMember(Order = 1)] public int StoreFormatVersion { get; set; } = CurrentStoreFormatVersion;
        [DataMember(Order = 2)] public int MaterialSnapshotVersion { get; set; }
        [DataMember(Order = 3)] public string EpochId { get; set; } = "";
        [DataMember(Order = 4)] public long Generation { get; set; }
        [DataMember(Order = 5)] public long SnapshotRevision { get; set; }
        /// <summary>NormalSave | Migration | RedundancyRepair | RetirementBarrier (후자 2종 미구현)</summary>
        [DataMember(Order = 6)] public string CommitKind { get; set; } = "";
        [DataMember(Order = 7)] public string WriterSessionId { get; set; } = "";
        [DataMember(Order = 8)] public long CapturedSaveVersion { get; set; }
        [DataMember(Order = 9)] public long SourceGeneration { get; set; } = -1;
        [DataMember(Order = 10)] public long SnapshotSavedAtUtcTicks { get; set; }
        [DataMember(Order = 11)] public long CommittedAtUtcTicks { get; set; }
        [DataMember(Order = 12)] public string SaveReason { get; set; } = "";
        [DataMember(Order = 13)] public string RecipeName { get; set; } = "";
        [DataMember(Order = 14)] public string LotId { get; set; } = "";
        /// <summary>Cassettes 전체(슬롯 참조 포함)의 canonical 바이트 — 작은 전역 checkpoint(§9).</summary>
        [DataMember(Order = 15)] public string CassettesPayloadBase64 { get; set; } = "";
        [DataMember(Order = 16)] public List<string> OrderedWaferInstanceIds { get; set; } = new List<string>();
        [DataMember(Order = 17)] public List<PwjManifestEntry> Documents { get; set; } = new List<PwjManifestEntry>();
        [DataMember(Order = 18)] public int TotalWaferCount { get; set; }
        [DataMember(Order = 19)] public int TotalDieCount { get; set; }
        [DataMember(Order = 20)] public string NormalizedSnapshotSha256 { get; set; } = "";
        /// <summary>이 필드만 빈 값으로 둔 canonical payload 전체의 SHA-256 (§9).</summary>
        [DataMember(Order = 21)] public string ManifestPayloadSha256 { get; set; } = "";
    }

    [DataContract]
    internal sealed class PwjManifestEntry
    {
        [DataMember(Order = 1)] public string DocumentKind { get; set; } = "";
        [DataMember(Order = 2)] public string StableId { get; set; } = "";
        [DataMember(Order = 3)] public string RelativePath { get; set; } = "";
        [DataMember(Order = 4)] public int DocumentSchemaVersion { get; set; }
        [DataMember(Order = 5)] public long ByteLength { get; set; }
        [DataMember(Order = 6)] public string Sha256 { get; set; } = "";
        [DataMember(Order = 7)] public int WaferCount { get; set; }
        [DataMember(Order = 8)] public int DieCount { get; set; }
    }

    /// <summary>
    /// Wafer 문서(§10): WaferMaterial 1개 + canonical owner Die 목록 + 전역 순서 링크.
    /// 저장마다 바뀌는 전역 값(SnapshotRevision/SavedAt/SaveReason 등)은 넣지 않는다 —
    /// 내용이 같으면 바이트가 같아야 재사용(미기록)이 성립한다.
    /// </summary>
    [DataContract]
    internal sealed class PwjWaferDocument
    {
        public const int CurrentSchemaVersion = 1;
        public const string KindWafer = "WaferDocument";
        public const string KindOrphanDies = "OrphanDieDocument";
        /// <summary>소유 wafer가 없는 die(InputWaferInstanceId 공백)를 담는 특수 문서의 StableId.</summary>
        public const string OrphanStableId = "__orphan-dies__";

        [DataMember(Order = 1)] public int DocumentSchemaVersion { get; set; } = CurrentSchemaVersion;
        [DataMember(Order = 2)] public string DocumentKind { get; set; } = KindWafer;
        [DataMember(Order = 3)] public string StableId { get; set; } = "";
        [DataMember(Order = 4)] public WaferMaterial Wafer { get; set; }
        [DataMember(Order = 5)] public List<DieMaterial> Dies { get; set; } = new List<DieMaterial>();
        /// <summary>
        /// Dies[i]의 전역 순서 선행자 DieId (persistence 전용 링크, §10).
        /// 첫 전역 die는 null sentinel(빈 문자열). 전체 문서 집합에서 정확히 하나의 head를 갖는
        /// 비순환 체인을 이루어야 하며, 이 체인으로 MaterialSnapshot.Dies 순서를 복원한다.
        /// </summary>
        [DataMember(Order = 6)] public List<string> PreviousGlobalDieIds { get; set; } = new List<string>();
    }
}
