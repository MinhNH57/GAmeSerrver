using KF.Contract.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KF.Contract.Interface
{
    public interface IJackpotService
    {
        int InitializeClient(IKuaFuClient callback, KuaFuClientContext clientInfo);
        bool RefreshJarFromDb();

        AsyncDataItem[] GetClientCacheItems(int serverId);

        long GetJarAmount();

        bool SaveJarAmount(long amount);

        long ResetJarAmount(int serverId);

        long AdjustJarAmount(int serverId, long delta);

        JackpotResult TryHitKnbJackpot(int serverId, int playerId, string playerName, long betKnb, string itemName, bool isBigWin, bool isSmallWin, bool notify);

        Dictionary<int, JackpotResult> GetJackpotResults();
        List<JackpotResult> GetJackpotHistory();
        List<TopRoleInfo> GetTopRankings();
        int ClearLuckySpinServerPointData(int serverId);
    }
}
