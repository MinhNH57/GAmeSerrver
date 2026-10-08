using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ThamGiaDuaThuEventObject : EventObject
    {
        public KPlayer player;
        public int betPack;

        public ThamGiaDuaThuEventObject(KPlayer player, int betPack)
            : base((int)EventTypes.Join_PetRace)
        {
            this.player = player;
            this.betPack = betPack;
        }

    }
}