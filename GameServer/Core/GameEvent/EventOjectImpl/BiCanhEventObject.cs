using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class BiCanhEventObject : EventObject
    {
        public KPlayer player;

        public BiCanhEventObject(KPlayer player)
            : base((int)EventTypes.Join_TuLuyenBiCanh)
        {
            this.player = player;
        }
    }
}