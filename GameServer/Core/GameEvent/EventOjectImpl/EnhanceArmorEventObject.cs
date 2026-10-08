using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class EnhanceArmorEventObject : EventObject
    {
        public KPlayer player;
        public int enhanceLevel;

        public EnhanceArmorEventObject(KPlayer player, int enhanceLevel)
            : base((int)EventTypes.Enhance_Armor)
        {
            this.player = player;
            this.enhanceLevel = enhanceLevel;
        }

    }
}