using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class TongKimKillEventObject : EventObject
    {
        public KPlayer player;
        public GameObject victim;

        public TongKimKillEventObject(KPlayer player, GameObject victim)
            : base((int)EventTypes.Kill_TongKim)
        {
            this.player = player;
            this.victim = victim;
        }
    }
}