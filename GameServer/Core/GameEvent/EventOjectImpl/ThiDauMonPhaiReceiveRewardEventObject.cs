using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ThiDauMonPhaiReceiveRewardEventObject : EventObject
    {
        public KPlayer player;

        public ThiDauMonPhaiReceiveRewardEventObject(KPlayer player)
            : base((int)EventTypes.Receive_ThiDauMonPhai)
        {
            this.player = player;
        }
    }
}