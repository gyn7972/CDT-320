using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using QMC.Common.Data.Store;

namespace QMC.CDT_320.Ui.Security
{
    /// <summary>
    /// 사용자 계정 저장소. Config\users.json 에 영속화하고 비밀번호는 salt+SHA-256 해시로만 보관한다.
    /// 파일이 없으면 기본 계정(operator/engineer/maint/admin)을 1회 생성한다(로그인 잠김 방지).
    /// </summary>
    public static class UserStore
    {
        private static List<UserAccount> _accounts = new List<UserAccount>();

        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => Path.Combine(Dir, "users.json");

        static UserStore()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static IReadOnlyList<UserAccount> All => _accounts;

        public static UserAccount Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _accounts.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path_))
                {
                    _accounts = CreateDefaults();
                    Save();
                    return;
                }
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<UserAccount>));
                    var list = ser.ReadObject(fs) as List<UserAccount>;
                    _accounts = (list != null && list.Count > 0) ? list : CreateDefaults();
                }
            }
            catch
            {
                _accounts = CreateDefaults();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                using (var fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(List<UserAccount>), _accounts);
                }
            }
            catch
            {
            }
        }

        /// <summary>계정 추가/갱신(ID 기준). 저장까지 수행.</summary>
        public static void Upsert(UserAccount account)
        {
            if (account == null || string.IsNullOrWhiteSpace(account.Id)) return;
            var existing = Get(account.Id);
            if (existing != null) _accounts.Remove(existing);
            _accounts.Add(account);
            Save();
        }

        public static void Remove(string id)
        {
            var a = Get(id);
            if (a == null) return;
            _accounts.Remove(a);
            Save();
        }

        public static void UpdateLastLogin(string id)
        {
            var a = Get(id);
            if (a == null) return;
            a.LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Save();
        }

        // --- 비밀번호 해시 ---

        /// <summary>평문 비밀번호 → "base64(salt)$base64(sha256(salt+pw))".</summary>
        public static string Hash(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);
            byte[] h = Compute(password, salt);
            return Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(h);
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                if (string.IsNullOrEmpty(stored) || stored.IndexOf('$') < 0) return false;
                var parts = stored.Split('$');
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                byte[] actual = Compute(password, salt);
                if (expected.Length != actual.Length) return false;
                int diff = 0;
                for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
                return diff == 0; // 상수시간 비교
            }
            catch
            {
                return false;
            }
        }

        private static byte[] Compute(string password, byte[] salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] pw = Encoding.UTF8.GetBytes(password ?? string.Empty);
                byte[] buf = new byte[salt.Length + pw.Length];
                Buffer.BlockCopy(salt, 0, buf, 0, salt.Length);
                Buffer.BlockCopy(pw, 0, buf, salt.Length, pw.Length);
                return sha.ComputeHash(buf);
            }
        }

        /// <summary>최초 1회 생성되는 기본 계정(기존 데모 자격증명과 동일, 해시 저장).</summary>
        public static List<UserAccount> CreateDefaults()
        {
            return new List<UserAccount>
            {
                new UserAccount { Id = "operator", Level = (int)UserLevel.Operator,    Hash = Hash("op"),    Enabled = true },
                new UserAccount { Id = "engineer", Level = (int)UserLevel.Engineer,    Hash = Hash("eng"),   Enabled = true },
                new UserAccount { Id = "maint",    Level = (int)UserLevel.Maintenance, Hash = Hash("mt"),    Enabled = true },
                new UserAccount { Id = "admin",    Level = (int)UserLevel.Admin,       Hash = Hash("admin"), Enabled = true },
            };
        }
    }
}
