using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class JoinFishingEventObject : EventObject
    {
        public KPlayer player;
        public int count;

        public JoinFishingEventObject(KPlayer player, int count)
            : base((int)EventTypes.Join_Fishing)
        {
            this.player = player;
            this.count = count;
        }

    }
}