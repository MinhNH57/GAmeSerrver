using System;

namespace KF.Contract.Data
{
    [Serializable]
    public class JackpotResult
    {
        public int ID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public bool IsBigWin { get; set; }
        public bool IsSmallWin { get; set; }
        public long AwardKnb { get; set; }
        public long JarBefore { get; set; }
        public long JarAfter { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }

        public bool notify { get; set; }
    }

    [Serializable]
    public class TopRoleInfo
    {
        public int Rid { get; set; }
        public string Rname { get; set; }
        public int ZoneId { get; set; }
        public long Pvalue { get; set; }
        public int Rank { get; set; }
    }
}
