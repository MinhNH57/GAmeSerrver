using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Core.Task;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.KiemThe.LuaSystem.Logic;
using GameServer.Logic;
using GameServer.Server;
using ProtoBuf;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace GameServer.KiemThe.GameEvents.QuestEvent
{

    public class QuestEventManager : IEventListener
    {

        [ProtoContract]
        [XmlRoot(ElementName = "QuestEvent")]
        public class QuestEventConfig
        {
            [XmlElement(ElementName = "TimeKey")]
            public string timeKey = "QuestEvent";
            
            [XmlElement(ElementName = "Quest")] public List<QuestEvent> quests;

            [ProtoMember(4)]
            [XmlIgnore]
            public Dictionary<string, int> status;
        }

        [ProtoContract]
        [XmlRoot(ElementName = "Quest")]
        public class QuestEvent
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "ID")] public int ID;
            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "Title")]
            public string title;
            [ProtoMember(3)]
            [XmlAttribute(AttributeName = "Description")]
            public string description;
            [ProtoMember(4)]
            [XmlAttribute(AttributeName = "Goal")] public int goal;
            [ProtoMember(5)]
            [XmlAttribute(AttributeName = "EventType")] public int eventType;
            [ProtoMember(6)]
            [XmlAttribute(AttributeName = "ExtraParams")] public string extraParams;

            [ProtoMember(7)]
            [XmlElement(ElementName = "Item")] public List<RewardItem> rewards;

        }

        [ProtoContract]
        [XmlRoot(ElementName = "Item")]
        public class RewardItem
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "ID")] public int ID;

            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "Count")]
            public int Count;
        }

        private const string CONFIG_PATH = "Config/KT_Interactive/QuestEvent.xml";
        private static QuestEventConfig Config;


        private static QuestEventManager instance = new QuestEventManager();

        public static QuestEventManager getInstance()
        {
            return instance;
        }

        public static void Init()
        {
            SetupEvent();


            var path = Global.GameResPath(CONFIG_PATH);
            if (!File.Exists(path))
            {
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(QuestEventConfig));
                Config = xmlSerializer.Deserialize(fs) as QuestEventConfig;
            }
        }

        public static void InitClientData(KPlayer client)
        {

        }


        public static void SetupEvent()
        {
            GlobalEventSource eventSource = GlobalEventSource.getInstance();
            
            // xóa các listener nếu đã tồn tại 
            // do nếu reload thì sẽ bị duplicate listener
            eventSource.removeListener((int)EventTypes.MonsterDead, getInstance());
            eventSource.removeListener((int)EventTypes.Join_BachHoDuong, getInstance());
            eventSource.removeListener((int)EventTypes.Kill_TongKim, getInstance());
            eventSource.removeListener((int)EventTypes.RemoveItemByLua, getInstance());
            eventSource.removeListener((int)EventTypes.Join_TieuDaoCoc, getInstance());
            eventSource.removeListener((int)EventTypes.Join_TuLuyenBiCanh, getInstance());
            eventSource.removeListener((int)EventTypes.FamilyFubenJoint, getInstance());
            eventSource.removeListener((int)EventTypes.Join_EmperorTomb, getInstance());
            
            
            eventSource.registerListener((int)EventTypes.MonsterDead, getInstance());
            eventSource.registerListener((int)EventTypes.Join_BachHoDuong, getInstance());
            eventSource.registerListener((int)EventTypes.Kill_TongKim, getInstance());
            eventSource.registerListener((int)EventTypes.RemoveItemByLua, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TieuDaoCoc, getInstance());
            eventSource.registerListener((int)EventTypes.Join_TuLuyenBiCanh, getInstance());
            eventSource.registerListener((int)EventTypes.FamilyFubenJoint, getInstance());
            eventSource.registerListener((int)EventTypes.Join_EmperorTomb, getInstance());
        }

        void IEventListener.processEvent(EventObject eventObject)
        {
            switch ((EventTypes)eventObject.getEventType())
            {
                case EventTypes.FamilyFubenJoint://
                    if (eventObject is FamilyFubenJoinEventObject familyFubenJoinEvent)
                    {
                        UpdateQuestProgress(familyFubenJoinEvent.player, EventTypes.FamilyFubenJoint, 1);
                    }
                    break;
                case EventTypes.Join_TuLuyenBiCanh://
                    if (eventObject is BiCanhEventObject biCanhEventObject)
                    {
                        UpdateQuestProgress(biCanhEventObject.player, EventTypes.Join_TuLuyenBiCanh, 1);
                    }
                    break;

                case EventTypes.MonsterDead://
                    if (eventObject is MonsterDeadEventObject monsterDeadEvent)
                    {
                        KPlayer attacker = monsterDeadEvent.getAttacker();
                        if (attacker.Teammates == null || attacker.Teammates.Count == 0)
                        {
                            UpdateQuestProgress(attacker, EventTypes.MonsterDead, 1, monsterDeadEvent.getMonster().MonsterInfo.ExtensionID);
                        }
                        else
                        {
                            foreach (KPlayer _client in attacker.Teammates)
                            {
                                UpdateQuestProgress(_client, EventTypes.MonsterDead, 1, monsterDeadEvent.getMonster().MonsterInfo.ExtensionID);
                            }
                        }
                        
                    }

                    break;
                case EventTypes.Join_BachHoDuong://
                    if (eventObject is BachHoDuongJoinEventObject bachHoDuongJoinEvent)
                    {
                        UpdateQuestProgress(bachHoDuongJoinEvent.player, EventTypes.Join_BachHoDuong, 1, bachHoDuongJoinEvent.IsKuaFuServer ? 1 : 0);
                    }
                    break;

                case EventTypes.Kill_TongKim://
                    if (eventObject is TongKimKillEventObject tongKimKillEvent)
                    {
                        UpdateQuestProgress(tongKimKillEvent.player, EventTypes.Kill_TongKim, 1);
                    }
                    break;
                case EventTypes.RemoveItemByLua://
                    if (eventObject is RemoveItemByLuaEventObject removeItemByLuaEvent)
                    {
                        UpdateQuestProgress(removeItemByLuaEvent.player, EventTypes.RemoveItemByLua, removeItemByLuaEvent.amount, removeItemByLuaEvent.itemId);
                    }
                    break;
                case EventTypes.Join_TieuDaoCoc://
                    if (eventObject is TieuDaoCocEventObject tieuDaoCocEvent)
                    {
                        UpdateQuestProgress(tieuDaoCocEvent.player, EventTypes.Join_TieuDaoCoc, 1);
                    }
                    break;
                case EventTypes.Join_EmperorTomb:
                    if (eventObject is JoinEmperorTombEventObject emperorTombEvent)
                    {
                        UpdateQuestProgress(emperorTombEvent.player, EventTypes.Join_EmperorTomb, 1, emperorTombEvent.IsKuaFuServer ? 1 : 0);
                    }
                    break;
            }
        }



        public static Dictionary<int, int> GetQuestProgress(KPlayer player)
        {
            var questProgress = new Dictionary<int, int>();

            for (int i = 0; i < Config.quests.Count; i++)
            {
                int questId = Config.quests[i].ID;
                questProgress.Add(questId, GetQuestProgress(player, questId));
            }

            return questProgress;
        }


        public static void UpdateQuestProgress(KPlayer client, EventTypes eventType, int updateValue, int param = 0)
        {
            var matchingTasks = Config.quests.Where(x => x.eventType == (int)eventType).ToList();  

            var filteredTasks = matchingTasks
                    .Where(task => {
                        if (task.extraParams == "-1")
                            return true;

                        var taskParams = task.extraParams.Split(':');
                        return taskParams.Any(p => Convert.ToInt32(p) == param);
                    })
                    .ToList();

            matchingTasks = filteredTasks;

            foreach (var quest in matchingTasks)
            {

                if (IsQuestDoneByPlayer(client,quest.ID))
                    continue;
                int progress = GetQuestProgress(client, quest.ID) + updateValue;
                if (progress > quest.goal) progress = quest.goal;
                client.SetQuestEventStatusValue(quest.ID, progress);
            }

        }

        public static void SetTaskProgress(KPlayer player, int questId, int setValue)
        {
            var quest = GetQuestById(questId);
            if (quest == null)
                return;


            //Set progress of a task
            player.SetQuestEventStatusValue(quest.ID, setValue);
        }

        private static bool CanUpdateQuestId(KPlayer player, int questId)
        {
            if (!EventTimeManager.IsEventEnabled(Config.timeKey))
                return false;

            try
            {
                var questData = GetQuestById(questId);
                return !IsQuestDoneByPlayer(player, questId);
            }
            catch
            {
                return false;
            }
        }


        public static int GetQuestProgress(KPlayer player, int questId)
        {
            //Task progress for each task

            var quest = GetQuestById(questId);
            int progress = player.GetQuestEventStatusValue(quest.ID);

            return progress;
        }

        private static bool IsQuestDoneByPlayer(KPlayer player, int questId)
        {
            var quest = GetQuestById(questId);
            int progress = GetQuestProgress(player, questId);
            return progress > quest.goal || progress == -1 ;
        }


        private static QuestEvent GetQuestById(int taskId)
        {
            return Config.quests.FirstOrDefault(t => t.ID == taskId);
        }

        public static Dictionary<int, int> GetEventStatus(KPlayer client)
        {

            var status = new Dictionary<int, int>();
            foreach (var quest in Config.quests)
            {
                int progress = GetQuestProgress(client, quest.ID);
                status.Add(quest.ID, progress);
            }
            return status;
        }

        public static TCPProcessCmdResults ProcessQuestEventInfoCmd(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lam nhieu vu loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 1)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }



                Dictionary<int, int> clientData = GetEventStatus(client);
                byte[] _cmdData = DataHelper.ObjectToBytes(clientData);
                tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, _cmdData, nID);
                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

        public static TCPProcessCmdResults ProcessReceiveQuestEventReward(TCPManager tcpMgr, TMSKSocket socket, TCPClientPool tcpClientPool, TCPRandKey tcpRandKey, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
        {
            tcpOutPacket = null;
            string cmdData = null;
            try
            {
                cmdData = new UTF8Encoding().GetString(data, 0, count);
            }
            catch (Exception)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Lam nhieu vu loi , CMD={0}, Client={1}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket)));
                return TCPProcessCmdResults.RESULT_FAILED;
            }

            try
            {
                string[] fields = cmdData.Split(':');

                if (fields.Length != 2)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Error Socket params count not fit CMD={0}, Client={1}, Recv={2}",
                        (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), fields.Length));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                int roleID = Convert.ToInt32(fields[0]);
                int questId = Convert.ToInt32(fields[1]);

                string strcmd = "";

                KPlayer client = GameManager.ClientMgr.FindClient(socket);
                if (null == client || client.RoleID != roleID)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("Can not find player corresponding ID, CMD={0}, Client={1}, RoleID={2}", (TCPGameServerCmds)nID, Global.GetSocketRemoteEndPoint(socket), roleID));
                    return TCPProcessCmdResults.RESULT_FAILED;
                }

                Dictionary<int, int> status = GetEventStatus(client);
                var quest = GetQuestById(questId);
                int progress = status[quest.ID];
                if(progress >= quest.goal)
                {
                    int totalSpackNeed = quest.rewards.Sum(x => ItemManager.TotalSpaceNeed(x.ID, x.Count));
                    if (!KTGlobal.IsHaveSpace(totalSpackNeed, client))
                    {
                        PlayerManager.ShowNotification(client, "Túi đồ không đủ chỗ");
                        return TCPProcessCmdResults.RESULT_OK;
                    }
                    //can get this pack
                    foreach (var rewardItem in quest.rewards)
                    {
                        ItemManager.CreateItem(pool, client, rewardItem.ID, rewardItem.Count, 0, "NHIEM_VU_SU_KIEN",
                            true, 1, false, Global.ConstGoodsEndTime);
                        //PlayerManager.ShowNotification(client, $"Chúc mừng bạn nhận được vật phẩm {rewardItem.ID}");
                    }
                    SetTaskProgress(client, quest.ID, -1);
                    Global.SaveRoleParamsStringToDB(client, RoleParamName.QuestEventRecord, client.QuestEventRecordString, true);
                    tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "1", nID);
                }
               


                return TCPProcessCmdResults.RESULT_DATA;

            }
            catch (Exception ex)
            {
                DataHelper.WriteFormatExceptionLog(ex, Global.GetDebugHelperInfo(socket), false);
            }
            return TCPProcessCmdResults.RESULT_FAILED;
        }

    }




}
