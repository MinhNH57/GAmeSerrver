using System;

namespace KF.Contract.Data
{
    public static class KuaFuRewardTemplateIds
    {
        public const string TdltLsvMemberReward = "TDLT_LSV_MEMBER_REWARD";
        public const string LeagueBattleTopReward = "LEAGUE_BATTLE_TOP_REWARD";
        public const string LeagueBattleJoinReward = "LEAGUE_BATTLE_JOIN_REWARD";
    }

    public enum KuaFuRewardMailStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Retry = 3,
        Failed = 4,
    }

    [Serializable]
    public class KuaFuRewardMailData
    {
        public long Id { get; set; }
        public string RewardKey { get; set; }
        public string EventType { get; set; }
        public int SourceServer { get; set; }
        public int RoleId { get; set; }
        public string TemplateId { get; set; }
        public string TemplateArgs { get; set; }
        public int BoundMoney { get; set; }
        public int BoundToken { get; set; }
        public string MailGoods { get; set; }
        public string ItemLog { get; set; }
        public int Status { get; set; }
        public int RetryCount { get; set; }
        public int MailId { get; set; }
        public string LastError { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    [Serializable]
    public class KuaFuRewardSubmitResult
    {
        public bool Success { get; set; }
        public bool IsDuplicate { get; set; }
        public long RewardId { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }
    }
}
