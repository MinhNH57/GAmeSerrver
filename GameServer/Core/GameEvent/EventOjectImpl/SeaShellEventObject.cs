using GameServer.Logic;
using Server.Data;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class SeaShellEventObject : EventObject
    {
        public KPlayer player;
        public int count = 1;

        public SeaShellEventObject(KPlayer player, int count)
            : base((int)EventTypes.Turn_SeashellCircle)
        {
            this.player = player;
            this.count = count;
        }
    }
}