using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 玩家退出事件
    /// </summary>
    public class SpinSlotMachineEventObject : EventObject
    {
        public KPlayer player;
        public int spinAmount;

        public SpinSlotMachineEventObject(KPlayer player, int spinAmount)
            : base((int)EventTypes.SpinSlotMachine)
        {
            this.player = player;
            this.spinAmount = spinAmount;
        }
    }
}