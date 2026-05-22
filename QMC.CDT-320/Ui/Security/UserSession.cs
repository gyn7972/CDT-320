using System;
using System.Collections.Generic;

namespace QMC.CDT_320.Ui.Security
{
    /// <summary>
    /// 현재 로그인 세션 상태. 프로세스 전역 싱글톤.
    /// </summary>
    public static class UserSession
    {
        public static event Action UserChanged;

        public static UserLevel Level    { get; private set; } = UserLevel.None;
        public static string    Name     { get; private set; } = "NONE";

        /// <summary>임시 계정 저장소. 실제로는 파일/DB에서 로드하도록 교체.</summary>
        private static readonly Dictionary<string, (string Password, UserLevel Level)> _accounts
            = new Dictionary<string, (string, UserLevel)>
            {
                { "operator",   ("op",     UserLevel.Operator)    },
                { "engineer",   ("eng",    UserLevel.Engineer)    },
                { "maint",      ("mt",     UserLevel.Maintenance) },
                { "admin",      ("admin",  UserLevel.Admin)       }
            };

        /// <summary>현재 사용자가 <paramref name="min"/> 이상의 레벨을 가지고 있는지.</summary>
        public static bool Has(UserLevel min) => (int)Level >= (int)min;

        public static bool Login(string id, string password)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!_accounts.TryGetValue(id, out var acc)) return false;
            if (acc.Password != password) return false;
            Name  = id;
            Level = acc.Level;
            UserChanged?.Invoke();
            return true;
        }

        public static void Logout()
        {
            Name  = "NONE";
            Level = UserLevel.None;
            UserChanged?.Invoke();
        }

        /// <summary>테스트/데모용 — 인증 없이 레벨 변경.</summary>
        public static void ForceSet(string name, UserLevel level)
        {
            Name  = name ?? "NONE";
            Level = level;
            UserChanged?.Invoke();
        }
    }
}
