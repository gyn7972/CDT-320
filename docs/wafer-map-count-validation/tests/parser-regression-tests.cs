using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QMC.CDT320.DieMaps;
using QMC.Common.Logging;

namespace WaferMapCountValidation.Tests
{
    internal static class ParserRegressionTests
    {
        private const string Records = "X= 185 Y= 200 B= 1\nX= 186 Y= 200 B= 18\nX= 185 Y= 199 B= 1\n";
        private static int _passed;
        private static int _failed;
        private static string _scratch;

        private sealed class SourceRecord
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Bin { get; set; }
        }

        private static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Expected: scratch-directory rke-map-directory legacy-map-directory skip-real-files");
                return 2;
            }

            _scratch = Path.GetFullPath(args[0]);
            if (!Directory.Exists(_scratch))
                throw new DirectoryNotFoundException("The test runner must create the isolated scratch directory.");

            RunSyntheticTests();
            if (!bool.Parse(args[3]))
                RunRealFileTests(args[1], args[2]);
            Console.WriteLine("SUMMARY passed=" + _passed + ", failed=" + _failed +
                              ", realFiles=" + (bool.Parse(args[3]) ? "SKIPPED" : "23"));
            return _failed == 0 ? 0 : 1;
        }

        private static void RunSyntheticTests()
        {
            PassRad("equal-counts", "3", "3", Records, 0);
            PassRad("auxiliary-count-greater", "3", "5", Records, 1);
            PassRad("auxiliary-count-smaller", "3", "2", Records, 1);
            PassRad("leading-zero-counts", "0003", "0005", Records, 1);
            PassRad("measurement-lines-ignored", "3", "3", Records + "1234 0.1 0.2 0.3\nE= EOW\n", 0);
            PassRad("inspection-items-are-not-count-headers", "3", "3",
                "ITEMS /%bad/ /&bad/\nSPEC 0.1 0.2 /%bad/ /&bad/\n" + Records, 0);
            PassRad("post-record-specification-is-not-a-header", "3", "3",
                Records + "[SPEC /%bad/ /&bad/]\n", 0);
            PassRad("legacy-record-suffixes", "3", "3",
                "X= 185 Y= 200 B= 1 D= 12 S= 3 Z= 0 C= 1 RD= 4\n" +
                "X= 186 Y= 200 B= 18 RD= 3\nX= 185 Y= 199 B= 1\n", 0);

            FailRad("declared-count-too-large", Header("4", "4") + Records);
            FailRad("declared-count-too-small", Header("2", "2") + Records);
            FailRad("matching-auxiliary-does-not-replace-percent", Header("4", "3") + Records);
            FailRad("duplicate-coordinate", Header("3", "3") +
                "X= 185 Y= 200 B= 1\nX= 185 Y= 200 B= 18\nX= 185 Y= 199 B= 1\n");
            FailRad("same-bin-duplicate-coordinate", Header("3", "3") +
                "X= 185 Y= 200 B= 1\nX= 185 Y= 200 B= 1\nX= 185 Y= 199 B= 1\n");
            FailRad("different-counts-with-duplicate-coordinate-no-success-warning", Header("3", "5") +
                "X= 185 Y= 200 B= 1\nX= 185 Y= 200 B= 18\nX= 185 Y= 199 B= 1\n");
            FailRad("different-counts-with-malformed-record-no-success-warning", Header("3", "5") +
                Records + "X= abc Y= 199 B= 1\n");
            FailRad("no-records", Header("3", "3") + "E= EOW\n");
            FailRad("both-counts-missing", "[TEST]\n" + Records);
            FailRad("percent-missing", "[TEST/10470/08013/&3/]\n" + Records);
            FailRad("auxiliary-missing", "[TEST/10470/08013/%3/]\n" + Records);

            foreach (string token in new[] { "0", "-3", "abc", "3abc", "3.0", "2147483648" })
            {
                string name = token.Replace('-', 'm').Replace('.', '_');
                FailRad("invalid-percent-" + name, Header(token, "3") + Records);
                FailRad("invalid-auxiliary-" + name, Header("3", token) + Records);
            }

            FailRad("duplicate-header-line", Header("3", "3") + Header("3", "3") + Records);
            FailRad("duplicate-percent-same-line", "[TEST/10470/08013/%3/%3/&3/]\n" + Records);
            FailRad("duplicate-auxiliary-same-line", "[TEST/10470/08013/%3/&3/&3/]\n" + Records);
            FailRad("conflicting-percent-counts", "[TEST/10470/08013/%3/%4/&3/]\n" + Records);
            FailRad("conflicting-auxiliary-counts", "[TEST/10470/08013/%3/&3/&4/]\n" + Records);
            FailRad("duplicate-percent-later-line", Header("3", "3") + "[/%3/]\n" + Records);
            FailRad("duplicate-auxiliary-later-line", Header("3", "3") + "[/&3/]\n" + Records);
            FailRad("unterminated-duplicate-percent", Header("3", "3") + "[/%BAD]\n" + Records);
            FailRad("unterminated-duplicate-auxiliary", Header("3", "3") + "[/&BAD]\n" + Records);
            FailRad("unterminated-auxiliary", "[TEST/10470/08013/%3/&3]\n" + Records);
            string headerPadding = string.Concat(Enumerable.Repeat("[COMMENT]\n", 35));
            FailRad("duplicate-percent-after-line-30", Header("3", "3") + headerPadding + "[/%3/]\n" + Records);
            FailRad("duplicate-auxiliary-after-line-30", Header("3", "3") + headerPadding + "[/&3/]\n" + Records);
            Test("count-header-after-line-30", delegate
            {
                string path = Fixture("count-header-after-line-30", headerPadding + Header("3", "3") + Records);
                EventLogger.Warnings.Clear();
                DieMap map = DieMapGenerator.LoadWaferMapTextOrThrow(path);
                Check(map.Entries.Count == 3 && map.SourceDeclaredCount == 3, "Late header was not validated.");
                Check(EventLogger.Warnings.Count == 0, "Equal late counts emitted a warning.");
            });

            foreach (string malformed in new[]
            {
                "X= abc Y= 200 B= 1", "X= 185 Y= abc B= 1", "X= 185 Y= 200 B= abc",
                "X= 185 Y= 200 B= 1abc", "X= 185 Y= 200", "X= 185.0 Y= 200 B= 1",
                "X= 2147483648 Y= 200 B= 1", "X= 185 Y= 2147483648 B= 1",
                "X= 185 Y= 200 B= 2147483648"
            })
            {
                // Valid records still match the header: silently ignoring this line must fail too.
                FailRad("malformed-record-" + (_passed + _failed), Header("3", "3") + Records + malformed + "\n");
            }

            Test("place-grid-control-without-rad-header", delegate
            {
                string path = Fixture("place-grid-control", "PLACE_WAFER_ROW\tPLACE_WAFER_COL\n2\t1\n2\t2\n1\t1\n");
                EventLogger.Warnings.Clear();
                DieMap map = DieMapGenerator.LoadWaferMapTextOrThrow(path);
                Check(map.SourceFormat == "PLACE GRID TXT" && map.Entries.Count == 3, "PLACE grid parsing changed.");
                Check(map.DieMapX == 2 && map.DieMapY == 2, "PLACE grid dimensions changed.");
                Check(EventLogger.Warnings.Count == 0, "PLACE grid emitted a RAD warning.");
            });
            Test("camtek-control-without-rad-header", delegate
            {
                string path = Fixture("camtek-control", "ROWCT:2\nCOLCT:2\nXDIES:1\nYDIES:1\nRowData:001 ___\nRowData:018 @@@\n");
                EventLogger.Warnings.Clear();
                DieMap map = DieMapGenerator.LoadCamtekWaferMapTextOrThrow(path);
                Check(map.Entries.Count == 3 && map.DieMapX == 2 && map.DieMapY == 2, "CAMTEK geometry changed.");
                Check(map.Entries.Count(entry => entry.IsTarget) == 2, "CAMTEK mark exclusion changed.");
                Check(EventLogger.Warnings.Count == 0, "CAMTEK emitted a RAD warning.");
            });
        }

        private static void RunRealFileTests(string rkeDirectory, string legacyDirectory)
        {
            int[] rkeBin1 = { 512, 531, 495, 481, 523, 479, 443, 499, 498, 536, 526, 530, 501, 478, 508, 481 };
            for (int index = 0; index < rkeBin1.Length; index++)
            {
                string name = "YZAMH." + (index + 2).ToString("00", CultureInfo.InvariantCulture);
                RealFile(Path.Combine(rkeDirectory, name), 730, 732, 27, 36, 10.470, 8.013, rkeBin1[index]);
            }

            string[] legacyNames = { "YZ9XFA3.14", "YZ9XH1.07", "YZ9XH1.08", "YZ9XF22.10", "YZ9XF22.11", "YZ9XFA3.13-R1", "YZ8XRD.07" };
            int[] legacyBin1 = { 465, 699, 584, 576, 568, 276, 1135 };
            for (int index = 0; index < legacyNames.Length; index++)
                RealFile(Path.Combine(legacyDirectory, legacyNames[index]), 1257, 1257, 35, 47, 8.12, 6.12, legacyBin1[index]);
        }

        private static void RealFile(string path, int count, int auxiliary, int gridX, int gridY, double pitchX, double pitchY, int bin1)
        {
            Test("real-" + Path.GetFileName(path), delegate
            {
                byte[] before = FileHash(path);
                try
                {
                    EventLogger.Warnings.Clear();
                    DieMap map = DieMapGenerator.LoadWaferMapTextOrThrow(path);
                    Check(map.Entries.Count == count && map.SourceDeclaredCount == count, "Record or declared count changed.");
                    Check(map.DieMapX == gridX && map.DieMapY == gridY, "Grid dimensions changed.");
                    Near(map.PitchX, pitchX, "PitchX");
                    Near(map.PitchY, pitchY, "PitchY");
                    Near(map.OriginX, -(gridX - 1) * pitchX / 2.0, "OriginX");
                    Near(map.OriginY, -(gridY - 1) * pitchY / 2.0, "OriginY");
                    Check(map.Entries.Count(entry => entry.BinCode == 1 && entry.IsTarget) == bin1, "BIN1 count changed.");
                    List<SourceRecord> originalRecords = ReadOriginalRecords(path);
                    var originalTriples = new HashSet<string>(originalRecords.Select(record =>
                        RecordKey(record.X, record.Y, record.Bin)), StringComparer.Ordinal);
                    // Existing normalization keeps positive BIN values and marks every nonpositive BIN as non-target 255.
                    // YZ9XFA3.13-R1 contains 687 raw BIN=0 records; preserving that established rule is intentional.
                    var expectedTriples = new HashSet<string>(originalRecords.Select(record =>
                        RecordKey(record.X, record.Y, record.Bin > 0 ? record.Bin : 255)), StringComparer.Ordinal);
                    var mappedTriples = new HashSet<string>(map.Entries.Select(entry =>
                        RecordKey(entry.OriginalMapX, entry.OriginalMapY, entry.BinCode)), StringComparer.Ordinal);
                    Check(originalRecords.Count == count && originalTriples.Count == count && expectedTriples.SetEquals(mappedTriples),
                        "Original X/Y/positive-BIN triples or established nonpositive-BIN normalization changed.");
                    var expectedTargets = new HashSet<string>(originalRecords.Where(record => record.Bin > 0).Select(record =>
                        RecordKey(record.X, record.Y, record.Bin)), StringComparer.Ordinal);
                    Check(map.Entries.All(entry => entry.IsTarget == expectedTargets.Contains(
                        RecordKey(entry.OriginalMapX, entry.OriginalMapY, entry.BinCode))),
                        "Original BIN target eligibility changed.");
                    Check(map.Entries.Select(entry => entry.OriginalMapX + "," + entry.OriginalMapY).Distinct().Count() == count,
                        "Original addresses are duplicated.");
                    Check(map.Entries.Select(entry => entry.DieUid).Distinct().Count() == count, "Die identities are duplicated.");
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        Check(entry.DieMapX >= 0 && entry.DieMapX < gridX && entry.DieMapY >= 0 && entry.DieMapY < gridY,
                            "Local address is outside the grid.");
                        Check(Finite(entry.PosX) && Finite(entry.PosY) && Finite(entry.EquipmentGridX) && Finite(entry.EquipmentGridY),
                            "Non-finite entry geometry.");
                        Near(entry.PosX, (entry.DieMapX - (gridX - 1) / 2.0) * pitchX, "PosX");
                        Near(entry.PosY, (entry.DieMapY - (gridY - 1) / 2.0) * pitchY, "PosY");
                    }
                    CheckWarnings(path, count, auxiliary, count, count == auxiliary ? 0 : 1);
                }
                finally
                {
                    Check(before.SequenceEqual(FileHash(path)), "Read-only test changed an original map.");
                }
            });
        }

        private static void PassRad(string name, string percent, string auxiliary, string records, int warningCount)
        {
            Test(name, delegate
            {
                string path = Fixture(name, Header(percent, auxiliary) + records);
                EventLogger.Warnings.Clear();
                DieMap map = DieMapGenerator.LoadWaferMapTextOrThrow(path);
                Check(map.Entries.Count == 3 && map.SourceDeclaredCount == 3, "Header count replaced actual records.");
                Check(map.DieMapX == 2 && map.DieMapY == 2, "Synthetic grid dimensions changed.");
                Check(map.Entries.Count(entry => entry.BinCode == 1 && entry.IsTarget) == 2, "BIN labels changed.");
                CheckWarnings(path, int.Parse(percent, CultureInfo.InvariantCulture), int.Parse(auxiliary, CultureInfo.InvariantCulture), 3, warningCount);
            });
        }

        private static void FailRad(string name, string content)
        {
            Test(name, delegate
            {
                string path = Fixture(name, content);
                EventLogger.Warnings.Clear();
                try
                {
                    DieMapGenerator.LoadWaferMapTextOrThrow(path);
                }
                catch (InvalidDataException error)
                {
                    Check(!string.IsNullOrWhiteSpace(error.Message), "Failure omitted its cause.");
                    Check(EventLogger.Warnings.Count == 0, "Invalid data emitted an accepted-map count warning.");
                    return;
                }
                throw new InvalidOperationException("Corrupt RAD map was accepted.");
            });
        }

        private static void CheckWarnings(string path, int percent, int auxiliary, int actual, int expected)
        {
            Check(EventLogger.Warnings.Count == expected, "Unexpected warning count: " + EventLogger.Warnings.Count);
            if (expected == 0)
                return;
            string warning = EventLogger.Warnings[0];
            Check(warning.Contains(Path.GetFileName(path)), "Warning omitted the file identity.");
            Check(warning.Contains("%=" + percent), "Warning omitted the percent count.");
            Check(warning.Contains("&=" + auxiliary), "Warning omitted the auxiliary count.");
            Check(warning.Contains("actual=" + actual), "Warning omitted the actual record count.");
        }

        private static string Header(string percent, string auxiliary)
        {
            return "[TEST/00/TEST/TEST/10470/08013/%" + percent + "/&" + auxiliary + "/]\n";
        }

        private static List<SourceRecord> ReadOriginalRecords(string path)
        {
            // Independent oracle: tokenize source records instead of reusing the parser's regex or map values.
            var records = new List<SourceRecord>();
            foreach (string line in File.ReadLines(path))
            {
                string text = line.TrimStart();
                if (!text.StartsWith("X=", StringComparison.Ordinal))
                    continue;
                string[] tokens = text.Split(new[] { ' ', '\t', '=' }, StringSplitOptions.RemoveEmptyEntries);
                Check(tokens.Length >= 6 && tokens[0] == "X" && tokens[2] == "Y" && tokens[4] == "B",
                    "Independent source reader found an unexpected X/Y/BIN layout.");
                int x = int.Parse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int y = int.Parse(tokens[3], NumberStyles.Integer, CultureInfo.InvariantCulture);
                int bin = int.Parse(tokens[5], NumberStyles.Integer, CultureInfo.InvariantCulture);
                records.Add(new SourceRecord { X = x, Y = y, Bin = bin });
            }
            return records;
        }

        private static string RecordKey(int x, int y, int bin)
        {
            return x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture) + "," +
                   bin.ToString(CultureInfo.InvariantCulture);
        }

        private static string Fixture(string name, string content)
        {
            string path = Path.Combine(_scratch, name + ".txt");
            File.WriteAllText(path, content.Replace("\n", "\r\n"), new UTF8Encoding(true));
            return path;
        }

        private static byte[] FileHash(string path)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return hash.ComputeHash(stream);
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void Near(double actual, double expected, string label)
        {
            Check(Finite(actual) && Math.Abs(actual - expected) < 0.000001, label + " differs: " + actual + " / " + expected);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Test(string name, Action action)
        {
            try
            {
                action();
                _passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception error)
            {
                _failed++;
                Console.Error.WriteLine("FAIL " + name + ": " + error.GetType().Name + ": " + error.Message);
            }
        }
    }
}
