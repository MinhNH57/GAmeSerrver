using GameServer.Logic;
using Server.Tools;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class EventComposeEventObject : EventObject
    {
        public KPlayer player;
        public int itemID;
        public int multiple = 1;

        public EventComposeEventObject(KPlayer player, int itemID, int multiple)
            : base((int)EventTypes.Compose_Event_Item)
        {
            this.player = player;
            this.itemID = itemID;
            this.multiple = multiple;
        }

    }
}