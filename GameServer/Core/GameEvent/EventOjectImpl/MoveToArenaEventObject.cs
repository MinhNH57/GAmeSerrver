using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class MoveToArenaEventObject : EventObject
    {
        public KPlayer player;

        public MoveToArenaEventObject(KPlayer player)
            : base((int)EventTypes.Move_ToArena)
        {
            this.player = player;
        }
    }
}