using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QMC.CDT320.Materials.Persistence
{
    /// <summary>
    /// PwjStore 격리 자가검증 (문서 §1: 운영 D:\CDT-320 무접촉 — 호출자가 격리 temp root를 준다).
    /// 테스트 프레임워크가 없는 솔루션이므로 정적 메서드로 제공한다.
    /// UI/운영 코드 어디에도 배선되어 있지 않다 — 외부(진단 도구/PowerShell)에서만 호출한다.
    /// </summary>
    public static class PwjSelfTest
    {
        public static string RunAll(string isolatedTempRoot)
        {
            var report = new StringBuilder();
            int passed = 0, failed = 0;
            Action<string, Func<bool>> run = (name, test) =>
            {
                try
                {
                    if (test())
                    {
                        passed++;
                        report.AppendLine("PASS  " + name);
                    }
                    else
                    {
                        failed++;
                        report.AppendLine("FAIL  " + name);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    report.AppendLine("FAIL  " + name + " — " + ex.Message);
                }
            };

            string root = Path.Combine(isolatedTempRoot, "pwj-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);

            run("왕복 등가성 (Save→Load 재조립)", () => TestRoundTrip(root));
            run("문서 재사용 (불변 wafer 미기록)", () => TestDocumentReuse(root));
            run("Manifest ring 회전 (파일 수 상한)", () => TestRingRotation(root));
            run("손상 manifest 거부 (payload hash)", () => TestCorruptManifestRejected(root));
            run("die 전역 순서 체인 보존", () => TestDieOrderPreserved(root));

            report.AppendLine();
            report.AppendLine("결과: PASS=" + passed + ", FAIL=" + failed + ", root=" + root);
            return report.ToString();
        }

        private static MaterialSnapshot BuildSampleSnapshot(int waferCount, int diesPerWafer, int revision)
        {
            var snapshot = new MaterialSnapshot();
            snapshot.SnapshotRevision = revision;
            snapshot.SavedAt = new DateTime(2026, 8, 5, 1, 0, 0, DateTimeKind.Local);
            snapshot.SaveReason = "SelfTest";
            snapshot.RecipeName = "TEST-RECIPE";
            snapshot.LotId = "LOT-TEST";
            snapshot.Cassettes.Add(new CassetteMaterial { CassetteId = "C1", Role = CassetteMaterialRole.Input1 });

            for (int w = 0; w < waferCount; w++)
            {
                var wafer = new WaferMaterial
                {
                    WaferId = "W-" + (w + 1).ToString("00"),
                    WaferInstanceId = "instance-" + (w + 1).ToString("00"),
                    State = WaferMaterialState.Ready
                };
                for (int d = 0; d < diesPerWafer; d++)
                {
                    var die = new DieMaterial
                    {
                        DieId = "D-" + (w + 1).ToString("00") + "-" + d.ToString("000"),
                        WaferID_Input = wafer.WaferId,
                        InputWaferInstanceId = wafer.WaferInstanceId,
                        Wafer_IndexX = d % 10,
                        Wafer_IndexY = d / 10,
                        InputSequenceNo = d + 1
                    };
                    wafer.DieIds.Add(die.DieId);
                    snapshot.Dies.Add(die);
                }
                snapshot.Wafers.Add(wafer);
            }
            return snapshot;
        }

        private static bool TestRoundTrip(string tempRoot)
        {
            string root = Path.Combine(tempRoot, "roundtrip");
            var store = new PwjStore(root, "selftest");
            MaterialSnapshot original = BuildSampleSnapshot(3, 25, 7);
            store.Save(original, "NormalSave", 1);

            MaterialSnapshot loaded = new PwjStore(root, "selftest2").Load();
            if (loaded == null) return false;
            if (loaded.SnapshotRevision != original.SnapshotRevision) return false;
            if (loaded.Wafers.Count != original.Wafers.Count) return false;
            if (loaded.Dies.Count != original.Dies.Count) return false;
            for (int i = 0; i < original.Dies.Count; i++)
                if (!string.Equals(loaded.Dies[i].DieId, original.Dies[i].DieId, StringComparison.Ordinal))
                    return false;
            for (int i = 0; i < original.Wafers.Count; i++)
                if (!string.Equals(loaded.Wafers[i].WaferInstanceId, original.Wafers[i].WaferInstanceId, StringComparison.Ordinal))
                    return false;
            return string.Equals(loaded.LotId, original.LotId, StringComparison.Ordinal);
        }

        private static bool TestDocumentReuse(string tempRoot)
        {
            string root = Path.Combine(tempRoot, "reuse");
            var store = new PwjStore(root, "selftest");
            MaterialSnapshot snapshot = BuildSampleSnapshot(3, 10, 1);
            store.Save(snapshot, "NormalSave", 1);

            var beforeWrites = Directory.GetFiles(root, "state.*.json", SearchOption.AllDirectories)
                .ToDictionary(p => p, File.GetLastWriteTimeUtc);

            // wafer 1장만 변경 (die 하나의 결과 갱신)
            snapshot.Dies[0].Result = DieResult.Good;
            snapshot.SnapshotRevision = 2;
            store.Save(snapshot, "NormalSave", 2);

            var after = Directory.GetFiles(root, "state.*.json", SearchOption.AllDirectories);
            // 변경 wafer는 새 revision 슬롯 파일이 생기고, 불변 wafer 2장의 기존 파일은 재기록되지 않아야 한다.
            int untouched = 0;
            foreach (var pair in beforeWrites)
                if (File.Exists(pair.Key) && File.GetLastWriteTimeUtc(pair.Key) == pair.Value)
                    untouched++;
            return untouched >= 2 && after.Length == beforeWrites.Count + 1;
        }

        private static bool TestRingRotation(string tempRoot)
        {
            string root = Path.Combine(tempRoot, "ring");
            var store = new PwjStore(root, "selftest");
            MaterialSnapshot snapshot = BuildSampleSnapshot(1, 5, 1);
            for (int i = 0; i < 8; i++)
            {
                snapshot.SnapshotRevision = i + 1;
                snapshot.Dies[0].UpdatedAt = snapshot.Dies[0].UpdatedAt.AddSeconds(1);   // 내용 변화 유도
                store.Save(snapshot, "NormalSave", i + 1);
            }

            int manifests = Directory.GetFiles(root, "material-manifest.*.json").Length;
            int docs = Directory.GetFiles(root, "state.*.json", SearchOption.AllDirectories).Length;
            PwjManifest latest = store.TryLoadLatestManifestHeader();
            // 저장 8회에도 manifest ≤3, wafer당 문서 ≤3 (ring 상한), 최신 Generation=8.
            return manifests <= PwjStore.SlotCount && docs <= PwjStore.SlotCount &&
                   latest != null && latest.Generation == 8 && store.Load() != null;
        }

        private static bool TestCorruptManifestRejected(string tempRoot)
        {
            string root = Path.Combine(tempRoot, "corrupt");
            var store = new PwjStore(root, "selftest");
            MaterialSnapshot snapshot = BuildSampleSnapshot(1, 5, 1);
            store.Save(snapshot, "NormalSave", 1);
            snapshot.SnapshotRevision = 2;
            snapshot.Dies[0].Result = DieResult.NG;
            store.Save(snapshot, "NormalSave", 2);

            // 최신 generation(2)의 manifest 파일을 손상시킨다 → 유효 최고본은 generation 1이어야 한다.
            string latestPath = Path.Combine(root, "material-manifest." + (2 % PwjStore.SlotCount) + ".json");
            byte[] corrupted = File.ReadAllBytes(latestPath);
            corrupted[corrupted.Length / 2] ^= 0xFF;
            File.WriteAllBytes(latestPath, corrupted);

            PwjManifest survivor = store.TryLoadLatestManifestHeader();
            if (survivor == null || survivor.Generation != 1)
                return false;
            MaterialSnapshot loaded = store.Load();
            return loaded != null && loaded.SnapshotRevision == 1;
        }

        private static bool TestDieOrderPreserved(string tempRoot)
        {
            string root = Path.Combine(tempRoot, "order");
            var store = new PwjStore(root, "selftest");
            // 두 wafer의 die가 전역 순서에서 교차(interleave)되도록 구성한다.
            MaterialSnapshot snapshot = BuildSampleSnapshot(2, 3, 1);
            var interleaved = new List<DieMaterial>();
            for (int i = 0; i < 3; i++)
            {
                interleaved.Add(snapshot.Dies[i]);        // wafer 1
                interleaved.Add(snapshot.Dies[3 + i]);    // wafer 2
            }
            snapshot.Dies = interleaved;
            store.Save(snapshot, "NormalSave", 1);

            MaterialSnapshot loaded = store.Load();
            if (loaded == null || loaded.Dies.Count != interleaved.Count)
                return false;
            for (int i = 0; i < interleaved.Count; i++)
                if (!string.Equals(loaded.Dies[i].DieId, interleaved[i].DieId, StringComparison.Ordinal))
                    return false;
            return true;
        }
    }
}
