using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class BachHoDuongKillEventObject : EventObject
    {
        public KPlayer player;
        public GameObject victim;

        public BachHoDuongKillEventObject(KPlayer player, GameObject victim)
            : base((int)EventTypes.Kill_BachHoDuong)
        {
            this.player = player;
            this.victim = victim;
        }
    }
}