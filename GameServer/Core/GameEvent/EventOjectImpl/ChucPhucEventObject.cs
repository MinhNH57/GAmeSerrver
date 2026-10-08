using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ChucPhucEventObject : EventObject
    {
        public KPlayer player;

        public ChucPhucEventObject(KPlayer player)
            : base((int)EventTypes.ChucPhuc)
        {
            this.player = player;
        }
    }
}