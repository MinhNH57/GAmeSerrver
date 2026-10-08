using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class RemoveItemByLuaEventObject : EventObject
    {
        public KPlayer player;
        public int itemId;
        public int amount;

        public RemoveItemByLuaEventObject(KPlayer player, int itemId, int amount)
            : base((int)EventTypes.RemoveItemByLua)
        {
            this.player = player;
            this.itemId = itemId;
            this.amount = amount;
        }

    }
}