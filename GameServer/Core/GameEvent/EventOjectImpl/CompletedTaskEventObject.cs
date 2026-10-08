using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class CompletedTaskEventObject : EventObject
    {
        public KPlayer player;
        public TaskClasses taskClass;

        public CompletedTaskEventObject(KPlayer player, TaskClasses taskClass)
            : base((int)EventTypes.CompletedTask)
        {
            this.player = player;
            this.taskClass = taskClass;
        }

    }
}