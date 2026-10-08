using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ChanNguyenEnhanceEventObject : EventObject
    {
        public KPlayer player;
        public int exp;

        public ChanNguyenEnhanceEventObject(KPlayer player, int exp)
            : base((int)EventTypes.Enhance_ChanNguyen)
        {
            this.player = player;
            this.exp = exp;
        }

    }
}