using System.Collections.Generic;
using GameServer.Logic;
using Server.Data;

namespace GameServer.Core.GameEvent.EventOjectImpl
{

    public class CheckInLienDau : EventObject
    {
        public KPlayer player;
        public List<KPlayer> teammates;

        public CheckInLienDau(KPlayer player, List<KPlayer> teammates)
            : base((int)EventTypes.CheckIn_TeamBattle)
        {
            this.player = player;
            this.teammates = teammates;
        }
    }
}