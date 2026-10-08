using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.KiemThe.Entities
{
    /// <summary>
    /// Thứ tự biến toàn cục hệ thống được lưu trong DB
    /// </summary>
    public enum SystemGlobalParameterIndexes
    {
        LARK_TOKEN = -1,
        /// <summary>
        /// Tổng số vỏ sò được hệ thống tích lũy của người chơi
        /// </summary>
        STORAGE_SEASHELL = 1,
        FLOWER_BET = 2,
        TOP_RANKING_GROUP_0 = 3,
        TOP_RANKING_GROUP_1 = 4,
        TOP_RANKING_GROUP_2 = 5,
        TOP_RANKING_GROUP_X = 6,
        TOP_RANKING_SEASHELL = 7,
        TOP_RANKING_SLOT_MAX = 8,
        TOP_RANKING_SLOT_POINT = 9,
        TOP_RANKING_TEAMMATE_GIFT_SENT = 10,
        TOP_RANKING_TEAMMATE_GIFT_RECEIVE = 11,
        TOP_RANKING_SONGLONG_VSL = 12,
        TOP_RANKING_OFFERING = 13,
        TOP_RANKING_LUCKYSPIN = 14,
    }
}
