using GameServer.Logic;
using Server.Data;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class UseItemEventObject : EventObject
    {
        public KPlayer player;
        public GoodsData itemGD;

        public UseItemEventObject(KPlayer player, GoodsData itemGD)
            : base((int)EventTypes.UseItem)
        {
            this.player = player;
            this.itemGD = itemGD;
        }
    }
}