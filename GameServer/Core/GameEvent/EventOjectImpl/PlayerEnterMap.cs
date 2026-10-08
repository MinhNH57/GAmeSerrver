using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    
    public class PlayerEnterMap : EventObject
    {
        public KPlayer player { get; private set; }
        public int mapId { get; private set; }
      

        public PlayerEnterMap(KPlayer player, int mapId)
            : base((int)EventTypes.PlayEnterMap)
        {
            this.player = player;
            this.mapId = mapId;
       
        }
    }
}
