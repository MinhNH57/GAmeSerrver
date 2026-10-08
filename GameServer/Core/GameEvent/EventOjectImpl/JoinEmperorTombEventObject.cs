using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class JoinEmperorTombEventObject : EventObject
    {
        public KPlayer player;
        public bool IsKuaFuServer;
        public JoinEmperorTombEventObject(KPlayer player, bool IsKuaFuServer)
            : base((int)EventTypes.Join_EmperorTomb)
        {
            this.player = player;
            this.IsKuaFuServer = IsKuaFuServer;
        }
    }
}