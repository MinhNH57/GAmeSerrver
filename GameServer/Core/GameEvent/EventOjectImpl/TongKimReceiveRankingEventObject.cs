using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class TongKimReceiveRankingEventObject : EventObject
    {
        public KPlayer player;
        public int rank;

        public TongKimReceiveRankingEventObject(KPlayer player, int rank)
            : base((int)EventTypes.Receive_TongKimRanking)
        {
            this.player = player;
            this.rank = rank;
        }
    }
}