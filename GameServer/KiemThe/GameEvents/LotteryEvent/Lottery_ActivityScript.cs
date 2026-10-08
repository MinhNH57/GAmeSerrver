using GameServer.KiemThe.GameEvents.Model;
using GameServer.KiemThe.Logic;

namespace GameServer.KiemThe.GameEvents.LotteryEvent
{
    public class Lottery_ActivityScripts : IActivityScript
    {
        public KTActivity Activity { set; get; }

        public void Begin()
        {
            if (this.Activity == null)
                return;
            
            
        }

        public void Close()
        {
            
        }
    }
    
}