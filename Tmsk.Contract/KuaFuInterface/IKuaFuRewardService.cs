using KF.Contract;
using KF.Contract.Data;

namespace KF.Contract.Interface
{
    public interface IKuaFuRewardService
    {
        int InitializeClient(IKuaFuClient callback, KuaFuClientContext clientInfo);

        AsyncDataItem[] GetClientCacheItems(int serverId);

        KuaFuRewardSubmitResult SubmitMailReward(KuaFuRewardMailData reward);

        KuaFuRewardMailData[] PickPendingMailRewards(int sourceServer, int maxCount);

        bool ConfirmMailRewardSent(long rewardId, int sourceServer, int mailId);

        bool ConfirmMailRewardFailed(long rewardId, int sourceServer, string error);
    }
}
