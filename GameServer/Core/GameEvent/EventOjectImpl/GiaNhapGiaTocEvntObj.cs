using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class GiaNhapGiaTocEvntObj : EventObject
    {
        public KPlayer player;

        public GiaNhapGiaTocEvntObj(KPlayer player)
            : base((int)EventTypes.Join_GiaToc)
        {
            this.player = player;
        }

    }
}