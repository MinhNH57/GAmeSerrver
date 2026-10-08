using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class CapturePetEventObject : EventObject
    {
        public KPlayer player;
        public int petID;

        public CapturePetEventObject(KPlayer player, int petID)
            : base((int)EventTypes.Capture_Pet)
        {
            this.player = player;
            this.petID = petID;
        }

    }
}