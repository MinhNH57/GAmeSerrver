using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.ParticleSystem;

namespace GameServer.KiemThe.Core.Bot
{
    class BotChallengeSession
    {
        public KPlayer Player;
        public KTBot Bot;
        public long StartedAt;
        public long ExpiresAt;
        public bool Finished;
    }
    public class BotChallengeManager
    {
        //TryBegin(player, bot);       // Kiểm tra và bắt đầu
        //IsOpponents(first, second);  // Hai đối tượng có đang tỷ thí không
        //Tick(bot);                  // Kiểm tra trận và điều khiển BOT
        //Finish(session, winner);    // Kết thúc và dọn trạng thái
        public void TryBegin(KPlayer player, KTBot bot)
        {

        }

        public bool IsOpponents(KPlayer first, KTBot second)
        {
            return false;
        }

    }
}
