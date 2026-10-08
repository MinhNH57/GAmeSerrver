using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class EnhanceWeaponEventObject : EventObject
    {
        public KPlayer player;
        public int enhanceLevel;

        public EnhanceWeaponEventObject(KPlayer player, int enhanceLevel)
            : base((int)EventTypes.Enhance_Weapon)
        {
            this.player = player;
            this.enhanceLevel = enhanceLevel;
        }

    }
}