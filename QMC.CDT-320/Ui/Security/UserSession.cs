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

        /// <summary>현재 사용자가 <paramref name="min"/> 이상의 레벨을 가지고 있는지.</summary>
        public static bool Has(UserLevel min) => (int)Level >= (int)min;

        /// <summary>계정/비번 검증은 UserStore(Config\users.json, 해시)로 위임한다.</summary>
        public static bool Login(string id, string password)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var acc = UserStore.Get(id);
            if (acc == null || !acc.Enabled) return false;
            if (!UserStore.Verify(password, acc.Hash)) return false;

            Name  = acc.Id;
            Level = acc.LevelEnum;
            UserStore.UpdateLastLogin(acc.Id);
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
