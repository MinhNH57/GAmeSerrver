using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class TranhDoatLanhThoEventObject : EventObject
    {
        public KPlayer player;

        public TranhDoatLanhThoEventObject(KPlayer player)
            : base((int)EventTypes.Join_TranhDoatLanhTho)
        {
            this.player = player;
        }
    }
}