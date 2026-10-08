using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class OpenRandomBoxEventObject : EventObject
    {
        public KPlayer player;
        public int goodsID;

        public OpenRandomBoxEventObject(KPlayer player, int goodsID)
            : base((int)EventTypes.OpenRandomBox)
        {
            this.player = player;
            this.goodsID = goodsID;
        }

    }
}