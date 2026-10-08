using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class CraftingEventObject : EventObject
    {
        public KPlayer player;
        public int itemID;

        public CraftingEventObject(KPlayer player, int itemID)
            : base((int)EventTypes.Crafting)
        {
            this.player = player;
            this.itemID = itemID;
        }

    }
}