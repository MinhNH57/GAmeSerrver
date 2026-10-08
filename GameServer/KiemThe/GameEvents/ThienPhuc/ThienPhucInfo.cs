using ProtoBuf;

namespace GameServer.KiemThe.GameEvents.ThienPhuc
{
    [ProtoContract]
    public class ThienPhucInfo
    {
        [ProtoMember(1)]
        public bool IsActive { get; set; }

        [ProtoMember(2)]
        public string EventName { get; set; }

        [ProtoMember(3)]
        public int MinNumber { get; set; }

        [ProtoMember(4)]
        public int MaxNumber { get; set; }

        [ProtoMember(5)]
        public int PlayedToday { get; set; }

        [ProtoMember(6)]
        public int DailyLimit { get; set; }

        [ProtoMember(7)]
        public int RequiredItemID { get; set; }

        [ProtoMember(8)]
        public int RequiredItemCount { get; set; }

        [ProtoMember(9)]
        public long RequestToken { get; set; }
    }
}
