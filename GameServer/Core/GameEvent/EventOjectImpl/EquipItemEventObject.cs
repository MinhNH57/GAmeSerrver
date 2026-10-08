using GameServer.KiemThe.Core.Item;
using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class EquipItemEventObject : EventObject
    {
        public KPlayer player;
        public KItem item;

        public EquipItemEventObject(KPlayer player,KItem item)
            : base((int)EventTypes.EquipItem)
        {
            this.player = player;
            this.item = item;
        }
    }
}