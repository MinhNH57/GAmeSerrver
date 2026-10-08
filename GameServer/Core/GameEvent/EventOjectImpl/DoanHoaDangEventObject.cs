using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class DoanHoaDangEventObject : EventObject
    {
        public KPlayer player;

        public DoanHoaDangEventObject(KPlayer player)
            : base((int)EventTypes.Join_DoanHoaDang)
        {
            this.player = player;
        }
    }
}