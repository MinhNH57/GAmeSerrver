using GameServer.Core.Executor;
using GameServer.KiemThe.GameEvents;
using ProtoBuf;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameServer.Logic.KPlayer;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        public RefundEventData RoleRefundEventData { get; set; }


        [ProtoContract]
        public class RefundEventData
        {
            [ProtoMember(1)]
            public Dictionary<int, RoleRefundPack> RefundPack { get; set; }
            [ProtoMember(2)]
            public DateTime lastUpdateTime;
        }

        [ProtoContract]
        public class RoleRefundPack
        {
            [ProtoMember(1)]
            public int claimedMilestone;
            [ProtoMember(2)]
            public string purchaseTime;
        }

        public void LoadRoleRefundEventData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                RoleRefundEventData = new RefundEventData();
                RoleRefundEventData.RefundPack = new Dictionary<int, RoleRefundPack>();
                return;
            }
            byte[] DataByteArray = Convert.FromBase64String(data);
            RoleRefundEventData = DataHelper.BytesToObject<RefundEventData> (DataByteArray, 0, DataByteArray.Length);
            if (RoleRefundEventData == null || RoleRefundEventData.RefundPack == null)
                ResetRefundData();
            RefundEventManager.InitClientData(this);
        }

        public int GetClaimedMilestone(int packId)
        {
            if(RoleRefundEventData.RefundPack.ContainsKey(packId))
            {
                return RoleRefundEventData.RefundPack[packId].claimedMilestone;
            }
            return 0;
        }

        public int GetDaysElapedSincePackagePurchase(int packId)
        {
            if (RoleRefundEventData.RefundPack.ContainsKey(packId))
            {
                int nDate = TimeUtil.NowDateTime().DayOfYear;
                int daysElaped = nDate - DateTime.Parse(RoleRefundEventData.RefundPack[packId].purchaseTime).DayOfYear + 1;
                return daysElaped;
            }
            return 0;
        }

        public void SetPurchaseRefundPackData(int packId)
        {
            RoleRefundPack packData = new RoleRefundPack();
            packData.claimedMilestone = 0;
            packData.purchaseTime = TimeUtil.NowDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            RoleRefundEventData.RefundPack[packId] = packData;
            SaveRoleRefundEventData();
        }

        public void ResetRefundData()
        {
            RoleRefundEventData = new RefundEventData();
            RoleRefundEventData.RefundPack = new Dictionary<int, RoleRefundPack>();
            SaveRoleRefundEventData();
        }

        public void SetClaimRefundMilestoneData(int packId,int milestone)
        {
            RoleRefundEventData.RefundPack[packId].claimedMilestone = milestone;
            SaveRoleRefundEventData();
        }

        private void SaveRoleRefundEventData()
        {
            this.RoleRefundEventData.lastUpdateTime = TimeUtil.NowDateTime();
            byte[] DataByteArray = DataHelper.ObjectToBytes(this.RoleRefundEventData);
            string InfoEncoding = Convert.ToBase64String(DataByteArray);
            Global.SaveRoleParamsStringToDB(this, RoleParamName.RefundEventRecord, InfoEncoding,false);
        }
    }
}
