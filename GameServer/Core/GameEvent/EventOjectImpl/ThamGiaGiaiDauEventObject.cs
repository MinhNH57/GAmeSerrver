using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ThamGiaGiaiDauEventObject : EventObject
    {
        public KPlayer player;

        public ThamGiaGiaiDauEventObject(KPlayer player)
            : base((int)EventTypes.Join_LeagueBattle)
        {
            this.player = player;
        }

    }
}