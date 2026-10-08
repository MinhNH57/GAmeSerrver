using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class ChangeGatherPointEventObject : EventObject
    {
        public KPlayer player;
        public int amount;
        public int repeat;

        public ChangeGatherPointEventObject(KPlayer player, int amount, int repeat)
            : base((int)EventTypes.Change_CurGatherPoint)
        {
            this.player = player;
            this.amount = amount;
            this.repeat = repeat;
        }

    }
}