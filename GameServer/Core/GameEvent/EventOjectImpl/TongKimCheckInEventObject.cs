using GameServer.Logic;
using Server.Tools;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class TongKimCheckInEventObject : EventObject
    {
        public KPlayer player;
        public bool IsKuaFuServer;

        public TongKimCheckInEventObject(KPlayer player, bool IsKuaFuServer = false)
            : base((int)EventTypes.CheckIn_TongKim)
        {
            this.player = player;
            this.IsKuaFuServer = IsKuaFuServer;
            LogManager.WriteLog(LogTypes.EventTypes, $"Fire TongKimCheckInEventObject: {player.RoleName} CheckIn TongKim Event, IsKuaFuServer={IsKuaFuServer}");
        }
    }
}