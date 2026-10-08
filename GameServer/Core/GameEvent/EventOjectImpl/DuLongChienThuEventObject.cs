using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class DuLongChienThuEventObject : EventObject
    {
        public KPlayer player;

        public DuLongChienThuEventObject(KPlayer player)
            : base((int)EventTypes.Join_DuLongChienThu)
        {
            this.player = player;
        }
    }
}