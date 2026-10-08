using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    public class FamilyFubenJoinEventObject : EventObject
    {
        public KPlayer player;

        public FamilyFubenJoinEventObject(KPlayer player)
            : base((int)EventTypes.FamilyFubenJoint)
        {
            this.player = player;
        }
    }
}
