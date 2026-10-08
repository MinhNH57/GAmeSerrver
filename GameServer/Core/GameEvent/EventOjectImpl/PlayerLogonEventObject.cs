using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class PlayerLogonEventObject : EventObject
    {
        public KPlayer player;

        public PlayerLogonEventObject(KPlayer player)
            : base((int)EventTypes.LogOn)
        {
            this.player = player;
        }
    }
}