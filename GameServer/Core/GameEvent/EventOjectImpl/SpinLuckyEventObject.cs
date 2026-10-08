using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// Sự kiện quay Tân Niên Bí Bảo
    /// </summary>
    public class SpinLuckyEventObject : EventObject
    {
        public KPlayer player;
        public int spinAmount;

        public SpinLuckyEventObject(KPlayer player, int spinAmount)
            : base((int)EventTypes.Spin_Lucky)
        {
            this.player = player;
            this.spinAmount = spinAmount;
        }
    }
}
