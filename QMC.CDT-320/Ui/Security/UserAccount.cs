using System.Runtime.Serialization;

namespace QMC.CDT_320.Ui.Security
{
    /// <summary>사용자 계정 1건. 비밀번호는 평문이 아닌 해시(salt$hash)로만 저장한다.</summary>
    [DataContract]
    public class UserAccount
    {
        [DataMember] public string Id        { get; set; } = "";
        [DataMember] public int    Level     { get; set; } = (int)UserLevel.Operator;
        [DataMember] public string Hash      { get; set; } = "";   // base64(salt) + "$" + base64(sha256(salt+pw))
        [DataMember] public bool   Enabled   { get; set; } = true;
        [DataMember] public string LastLogin { get; set; } = "";

        /// <summary>JSON 직렬화 제외. Level(int) → UserLevel 편의 변환.</summary>
        public UserLevel LevelEnum
        {
            get { return (UserLevel)Level; }
            set { Level = (int)value; }
        }
    }
}
