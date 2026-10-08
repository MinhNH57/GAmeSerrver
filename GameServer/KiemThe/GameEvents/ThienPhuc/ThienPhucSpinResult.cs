using ProtoBuf;

namespace GameServer.KiemThe.GameEvents.ThienPhuc
{
    [ProtoContract]
    public class ThienPhucSpinResult
    {
        [ProtoMember(1)]
        public bool Success { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }

        [ProtoMember(3)]
        public int GuessedNumber { get; set; }

        [ProtoMember(4)]
        public int ResultNumber { get; set; }

        [ProtoMember(5)]
        public bool IsWinner { get; set; }

        [ProtoMember(6)]
        public int RewardItemID { get; set; }

        [ProtoMember(7)]
        public int RewardQuantity { get; set; }

        [ProtoMember(8)]
        public int PlayedToday { get; set; }

        [ProtoMember(9)]
        public int DailyLimit { get; set; }
    }
}
