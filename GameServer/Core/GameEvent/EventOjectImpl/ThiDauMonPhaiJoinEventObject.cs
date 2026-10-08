using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ThiDauMonPhaiJoinEventObject : EventObject
    {
        public KPlayer player;

        public ThiDauMonPhaiJoinEventObject(KPlayer player)
            : base((int)EventTypes.Join_ThiDauMonPhai)
        {
            this.player = player;
        }
    }
}