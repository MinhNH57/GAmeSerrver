using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class RemoveItemEventObject : EventObject
    {
        public KPlayer player;
        public int itemId;
        public int amount;

        public RemoveItemEventObject(KPlayer player, int itemId, int amount)
            : base((int)EventTypes.RemoveItem)
        {
            this.player = player;
            this.itemId = itemId;
            this.amount = amount;
        }

    }
}