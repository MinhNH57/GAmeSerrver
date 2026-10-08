using GameServer.Logic;
using Server.Tools;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class BachHoDuongJoinEventObject : EventObject
    {
        public KPlayer player;
        public bool IsKuaFuServer;
        public BachHoDuongJoinEventObject(KPlayer player,bool IsKuaFuServer)
            : base((int)EventTypes.Join_BachHoDuong)
        {
            this.player = player;
            this.IsKuaFuServer = IsKuaFuServer;
            LogManager.WriteLog(LogTypes.EventTypes, $"Fire BachHoDuongJoinEventObject: {player.RoleName} Join BachHoDuong Event, IsKuaFuServer={IsKuaFuServer}");
        }
    }
}