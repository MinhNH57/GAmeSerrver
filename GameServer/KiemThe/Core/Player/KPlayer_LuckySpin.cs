using GameServer.Core.Executor;
using GameServer.KiemThe.GameEvents;
using ProtoBuf;
using Server.Tools;
using System;
using System.Collections.Generic;

namespace GameServer.Logic
{
    public partial class KPlayer
    {

        public LuckySpinData RoleLuckySpinData { get; set; }

        public int NextSpinJackpot { get; set; }

        [ProtoContract]
        public class LuckySpinData
        {
            [ProtoMember(1)]
            public int lastSpinPostion;
            [ProtoMember(2)]
            public Dictionary<int, bool> claimedStatus { get; set; }
            [ProtoMember(3)]
            public DateTime lastUpdateTime;
        }

        public void LoadRoleLuckySpinData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                RoleLuckySpinData = new LuckySpinData();
                RoleLuckySpinData.claimedStatus = new Dictionary<int, bool>();
                return;
            }
            byte[] DataByteArray = Convert.FromBase64String(data);
            RoleLuckySpinData = DataHelper.BytesToObject<LuckySpinData>(DataByteArray, 0, DataByteArray.Length);
            if (RoleLuckySpinData == null)
            {
                RoleLuckySpinData = new LuckySpinData();
            }
            if (RoleLuckySpinData.claimedStatus == null)
            {
                RoleLuckySpinData.claimedStatus = new Dictionary<int, bool>();
            }
        }

        public bool GetMilestoneClaimedStatus_LuckySpin(int milestoneIndex)
        {
            if (RoleLuckySpinData.claimedStatus.ContainsKey(milestoneIndex))
            {
                return RoleLuckySpinData.claimedStatus[milestoneIndex];
            }
            return false;
        }

        public void SetMilestoneClaimedStatus_LuckySpin(int milestoneIndex, bool status)
        {
            if (RoleLuckySpinData.claimedStatus.ContainsKey(milestoneIndex))
            {
                RoleLuckySpinData.claimedStatus[milestoneIndex] = status;
            }
            else
            {
                RoleLuckySpinData.claimedStatus.Add(milestoneIndex, status);
            }
            SaveRoleLuckySpinData();
        }

        public int GetAccumulatedPoints_LuckySpin()
        {
            return Global.GetRoleParamsInt32FromDB(this, RoleParamName.LuckySpinPoint);
        }

        public void SetAccumulatedPoints_LuckySpin(int points)
        {
            Global.SaveRoleParamsInt32ValueToDB(this, RoleParamName.LuckySpinPoint, points, true);
            SaveRoleLuckySpinData();
        }

        public int GetLastSpinPosition_LuckySpin()
        {
            return RoleLuckySpinData.lastSpinPostion;
        }

        public void SetLastSpinPosition_LuckySpin(int position)
        {
            RoleLuckySpinData.lastSpinPostion = position;
            SaveRoleLuckySpinData();
        }

        public void ResetLuckySpinData()
        {
            RoleLuckySpinData = new LuckySpinData();
            RoleLuckySpinData.claimedStatus = new Dictionary<int, bool>();
            Global.SaveRoleParamsInt32ValueToDB(this, RoleParamName.LuckySpinPoint, 0, true);
            SaveRoleLuckySpinData();
        }

        private void SaveRoleLuckySpinData()
        {
            RoleLuckySpinData.lastUpdateTime = TimeUtil.NowDateTime();
            byte[] DataByteArray = DataHelper.ObjectToBytes(RoleLuckySpinData);
            string InfoEncoding = Convert.ToBase64String(DataByteArray);
            Global.SaveRoleParamsStringToDB(this, RoleParamName.LuckySpinRecord, InfoEncoding, false);
        }

    }
}
