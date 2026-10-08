using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class TieuDaoCocEventObject : EventObject
    {
        public KPlayer player;

        public TieuDaoCocEventObject(KPlayer player)
            : base((int)EventTypes.Join_TieuDaoCoc)
        {
            this.player = player;
        }
    }
}