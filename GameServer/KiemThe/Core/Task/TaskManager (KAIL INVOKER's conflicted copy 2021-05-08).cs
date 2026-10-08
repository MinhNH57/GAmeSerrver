using GameServer.KiemThe.Logic;
using GameServer.KiemThe.LuaSystem;
using GameServer.KiemThe.LuaSystem.Entities;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Task
{
    public class TaskManager
    {
        public static Config _TotalConfig = new Config();

        public static string TaskConfgi = "Config/KT_Task/SystemTasks.xml";

        public static void Setup()
        {
            LoadItemFromPath(TaskConfgi);
        }

        public static void LoadItemFromPath(string FilesPath)
        {
            string Files = Global.GameResPath(FilesPath);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(Config));
                _TotalConfig = serializer.Deserialize(stream) as Config;
            }

            NPCTasksManager.LoadNPCTasks(_TotalConfig);
        }

        public static int CountAward(int TaskID)
        {
            int Count = 0;

            Task _Task = FindTaskById(TaskID);

            if (_Task.Taskaward.Length > 0)
            {
                string[] TotalItem = _Task.Taskaward.Split('#');

                Count = TotalItem.Length;
            }

            return Count;
        }

        public static bool IsQuestComplete(int TaskID, int TaskValue)
        {
            bool IsComplete = false;

            Task _Task = FindTaskById(TaskID);

            if (_Task.TargetType == (int)TaskTypes.Talk || _Task.TargetType == (int)TaskTypes.AnswerQest)
            {
                if (TaskValue >= 1)
                {
                    IsComplete = true;
                }
            }

            if (_Task.TargetType == (int)TaskTypes.KillMonster || _Task.TargetType == (int)TaskTypes.MonsterSomething || _Task.TargetType == (int)TaskTypes.BuySomething || _Task.TargetType == (int)TaskTypes.UseSomething || _Task.TargetType == (int)TaskTypes.TransferSomething || _Task.TargetType == (int)TaskTypes.GetSomething)
            {
                int NumberRequest = _Task.TargetNum;

                if (TaskValue >= NumberRequest)
                {
                    IsComplete = true;
                }
            }

            return IsComplete;
        }

        public static bool JugeTaskTargetComplete(SystemXmlItem systemTask, int num, int taskVal)
        {
            if (systemTask.GetIntValue(string.Format("TargetNPC{0}", num)) < 0)
            {
                return true;
            }

            int targetNum = systemTask.GetIntValue(string.Format("TargetNum{0}", num));
            if (targetNum <= 0) targetNum = 1;
            return (taskVal >= targetNum);
        }

        public static void AppcepTask(GameMap map, NPC npc, KPlayer client, int TaskID)
        {
            Task _FindTask = TaskManager.FindTaskById(TaskID);

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            if (_FindTask == null)
            {
                // Thực hiện đóng cửa sổ nhiệm vụ và thông báo tới người chơi
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Nhiệm vụ bạn vừa chọn không tồn tại!");
                return;
            }

            int taskClass = Int32.Parse(_FindTask.TaskClass);

            if (taskClass >= (int)TaskClasses.CircleTaskStart && taskClass <= (int)TaskClasses.CircleTaskEnd)
            {
                // Kiểm tra xem có take được nữa ko
                bool canTaskPaoHuanTask = Global.CanTaskPaoHuanTask(client, taskClass);

                //Nếu ko nhận được
                if (!canTaskPaoHuanTask)
                {
                    PlayerManager.ShowNotification(client, "Nhiệm vụ tuần hoàn từ từ hãy xử lý");
                    return;
                }
            }

            //Kiểm tra xem có đủ điều kiện nhân nhiệm vụ mới không
            if (!Global.CanTakeNewTask(client, TaskID, _FindTask))
            {
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Bạn không thể nhận nhiệm vụ này");

                return;
            }

            string strcmd = "";

            // Add Data
            TaskData taskData = new TaskData()
            {
                DbID = -1,
                DoingTaskID = TaskID,
            };

            if (_FindTask.PropsName != "")
            {
                bool IsHaveSpaceGetQuest = KTGlobal.IsHaveSpace(1, client);

                if (!IsHaveSpaceGetQuest)
                {
                    KT_TCPHandler.CloseDialog(client);

                    PlayerManager.ShowNotification(client, "Hành trang không đủ chỗ trống để nhận nhiệm vụ");

                    return;
                }
            }

            int focus = 1;

            if (Global.GetFocusTaskCount(client) >= Data.TaskMaxFocusCount)
            {
                focus = 0;
            }

            // Nếu là nhiệm vụ tuần hoàn
            int nStarLevel = 1;
            if (taskClass == (int)TaskClasses.DailyTask)
            {
                int randPercent = Global.GetRandomNumber(1, 10001);

                int nNum = 0;
                int[] nArrPercent = new int[Data.TaskStarInfo.Count];

                for (int i = 0; i < Data.TaskStarInfo.Count; ++i)
                {
                    nArrPercent[i] = Data.TaskStarInfo[i].Probability + nNum;
                    nNum = nArrPercent[i];
                }

                for (int j = 0; j < nArrPercent.Length; ++j)
                {
                    if (randPercent <= nArrPercent[j])
                    {
                        nStarLevel = Data.TaskStarInfo[j].ID;
                        break;
                    }
                }
            }

            // Viết CMD lưu vào DB
            strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", client.RoleID, npc.ResID, TaskID, focus, nStarLevel);

            // Thực hiện ghi lại quest vào DB
            string[] fieldsData = null;

            if (TCPProcessCmdResults.RESULT_FAILED == Global.RequestToDBServer(tcpClientPool, pool, (int)TCPGameServerCmds.CMD_SPR_NEWTASK, strcmd, out fieldsData, client.ServerId))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Toang khi kết nối với CSDL, CMD={0}", (int)TCPGameServerCmds.CMD_SPR_NEWTASK));

                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Có lỗi khi nhận nhiệm vụ! ErrorCode : 1");

                return;
            }

            strcmd = "";
            if (Convert.ToInt32(fieldsData[3]) < 0) //Nếu Pram gửi về mà lỗi thì toang
            {
                taskData.DbID = Convert.ToInt32(fieldsData[3]);

                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Có lỗi khi nhận nhiệm vụ! ErrorCode : " + taskData.DbID);
            }

            //Nếu mà mọi thứ đều ổn
            // Check xem list task client có null không nếu null thì tạo mới
            if (null == client.TaskDataList)
            {
                client.TaskDataList = new List<TaskData>();
            }

            taskData.DbID = Convert.ToInt32(fieldsData[3]);
            taskData.DoingTaskVal1 = 0;
            taskData.DoingTaskVal2 = 0;
            taskData.DoingTaskFocus = focus;
            taskData.AddDateTime = Convert.ToInt64(fieldsData[2]);
            taskData.DoneCount = 0;
            taskData.StarLevel = nStarLevel;

            OldTaskData oldTaskData = Global.FindOldTaskByTaskID(client, TaskID);
            if (null != oldTaskData)
            {
                taskData.DoneCount = oldTaskData.DoCount;
            }

            //Trả về dữ liệu phần thưởng cho nhiệm vụ tuần hoàn
            Global.ProcessTaskData(client, taskData);

            lock (client.TaskDataList)
            {
                client.TaskDataList.Add(taskData);
            }

            // Thực hiện send DATA TASK VỀ CLLIENT
            byte[] DataSend = DataHelper.ObjectToBytes<TaskData>(taskData);
            GameManager.ClientMgr.SendToClient(client, DataSend, (int)TCPGameServerCmds.CMD_SPR_NEWTASK);

            int state = 0;
            int sourceNPC = _FindTask.SourceNPC;
            if (sourceNPC >= 0)
            {
                // Update STATE CỦA CLIENT
                state = Global.ComputeNPCTaskState(client, client.TaskDataList, sourceNPC);

                GameManager.ClientMgr.NotifyUpdateNPCTaskSate(Global._TCPManager.MySocketListener, pool, client, sourceNPC + SpriteBaseIds.NpcBaseId, state);
            }

            // Thực hiện trao vật phẩm cho người chơi nếu nó là nhiệm vụ nhận cái j đó
            ProcessTask.Process(Global._TCPManager.MySocketListener, pool, client, npc.ResID, npc.ResID, -1, TaskTypes.GetSomething);

            // NPC TRẢ NHIỆM VỤ
            int destNPC = _FindTask.DestNPC;

            // NẾU NHIỆM VỤ CÓ NPC TRẢ THÌ UPDATE TRẠNG THÁI CHO THẰNG NPC TRẢ NHIỆM VỤ
            if (-1 != destNPC)
            {
                state = Global.ComputeNPCTaskState(client, client.TaskDataList, destNPC);
                GameManager.ClientMgr.NotifyUpdateNPCTaskSate(Global._TCPManager.MySocketListener, pool, client, destNPC, state);
            }
        }


        public static void CompleteTask(GameMap map, NPC npc, KPlayer client, int TaskID)
        {

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            Task _TaskFind = TaskManager.FindTaskById(TaskID);

            // Nếu nhiệm vụ không tồn tại thì send packet về là toang
            if (_TaskFind == null)
            {

                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Có lỗi khi nhận nhiệm vụ! ErrorCode : " + TaskID);

                return;
            }

            TaskData taskData = GetTaskData(TaskID,client);
            
            if (null == taskData || taskData.DoingTaskID != TaskID) // NẾu task ko tồn tại trọng người
            {
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Nhiệm vụ bạn muốn trả không tồn tại : " + TaskID);
                return;
            }

            /// Nếu nhiệm chưa hoàn thành thì thông báo về là task chauw xong
            if (!TaskManager.IsQuestComplete(TaskID, taskData.DoingTaskVal1))
            {
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Nhiệm vụ của bạn vẫn chưa hoàn thành.Hãy hoàn thành rồi quay lại");

                return;
            }


            int CountAward = TaskManager.CountAward(TaskID);


            // Kiểm tra số ô đồ trống xem có đủ để add thưởng không
            if (!KTGlobal.IsHaveSpace(CountAward, client))
            {
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Túi đồ của bạn không đủ để nhận thưởng");

                return;
            }

       

            int taskClass = Int32.Parse(_TaskFind.TaskClass);



            int isMainTask = ((int)TaskClasses.MainTask == taskClass ? 1 : 0);

            // Đọc cái j đó từ DB ra
            byte[] sendBytesCmd = new UTF8Encoding().GetBytes(string.Format("{0}:{1}:{2}:{3}:{4}", client.RoleID, npc.ResID, _TaskFind.ID, taskData.DbID, isMainTask));
            byte[] bytesData = null;


            
            if (TCPProcessCmdResults.RESULT_FAILED == Global.ReadDataFromDb((int)TCPGameServerCmds.CMD_SPR_COMPTASK, sendBytesCmd, sendBytesCmd.Length, out bytesData, client.ServerId))
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("Không kết nối được với DBServer, CMD={0}", (int)TCPGameServerCmds.CMD_SPR_COMPTASK));
               
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Không kết nối được với DBServer");

                return;
            }

            Int32 length = BitConverter.ToInt32(bytesData, 0);
            string strData = new UTF8Encoding().GetString(bytesData, 6, length - 2);

            //Kết quả từ DB server trả về
            string[] fieldsData = strData.Split(':');

            // Nếu mà kết quả lấy từ DB Về mà toang thì set là toang
            if (fieldsData.Length < 3 || fieldsData[2] == "-1")
            {
              
                KT_TCPHandler.CloseDialog(client);

                PlayerManager.ShowNotification(client, "Có lỗi xảy ra khi trả nhiệm vụ ERROR CODE :" + fieldsData[2]);

                return;

            }
            else
            {
                //Nếu mà ổn thì ta sẽ
                if (ProcessTask.Complete(Global._TCPManager.MySocketListener, pool, client, npc.ResID, npc.ResID, _TaskFind.ID, taskData.DbID, false))
                {
                    // Nếu là main task
                    if (isMainTask > 0 && _TaskFind.ID > client.MainTaskID)
                    {
                        client.MainTaskID = _TaskFind.ID;

                        GlobalNew.RefreshGongNeng(client);
                    }



                    // nếu không phải task
                    if (isMainTask > 0)
                    {
                        ChengJiuManager.ProcessCompleteMainTaskForChengJiu(client, _TaskFind.ID);
                    }



                    KT_TCPHandler.CloseDialog(client);

                    PlayerManager.ShowNotification(client, "Trả nhiệm vụ :" + _TaskFind.Title + " thành công!");

                    return;
                }
                else
                {
                    KT_TCPHandler.CloseDialog(client);

                    PlayerManager.ShowNotification(client, "Trả nhiệm vụ :" + _TaskFind.Title + " thất bại!");

                    return;
                }

            }

        }

        public static bool IsDoingTask(int TaskID, KPlayer client)
        {
            bool IsDoingTask = false;

            lock (client.TaskDataList)
            {
                for (int i = 0; i < client.TaskDataList.Count; i++)
                {
                    if (client.TaskDataList[i].DoingTaskID == TaskID)
                    {
                        IsDoingTask = true;
                        break;
                    }
                }
            }

            return IsDoingTask;
        }

        public static TaskData GetTaskData(int TaskID, KPlayer client)
        {
            TaskData _TaskData = null;

            lock (client.TaskDataList)
            {
                for (int i = 0; i < client.TaskDataList.Count; i++)
                {
                    if (client.TaskDataList[i].DoingTaskID == TaskID)
                    {
                        _TaskData = client.TaskDataList[i];
                        break;
                    }
                }
            }

            return _TaskData;
        }

        public static void GenTaskDialog(GameMap map, NPC npc, KPlayer client)
        {

            TCPOutPacketPool pool = Global._TCPManager.TcpOutPacketPool;
            TCPClientPool tcpClientPool = Global._TCPManager.tcpClientPool;

            List<int> tasksList = null;

            Lua_NPCDialog _NpcDialog = new Lua_NPCDialog();

            Dictionary<int, string> Selections = new Dictionary<int, string>();

            _NpcDialog.Text = "Ta không giúp được gì cho người cả!";

            if (GameManager.NPCTasksMgr.SourceNPCTasksDict.TryGetValue(npc.ResID, out tasksList))
            {
                

                for (int i = 0; i < tasksList.Count; i++)
                {
                    Task _TaskGet = FindTaskById(tasksList[i]);
                    if (_TaskGet != null)
                    {
                        if (!IsDoingTask(_TaskGet.ID, client))
                        {
                            Selections.Add(_TaskGet.ID, "[Nhận Nhiệm Vụ]" + _TaskGet.Title);
                        }
                        else
                        {
                            _NpcDialog.Text = "Việc người hứa với ta vẫn chưa hoàn thành.<br>Hãy hoàn thành nó rồi quay lại gặp ta";
                        }
                    }
                }
            }

            List<int> ListDoneQuest = null;

            if (GameManager.NPCTasksMgr.DestNPCTasksDict.TryGetValue(npc.ResID, out ListDoneQuest))
            {
               


                for (int i = 0; i < ListDoneQuest.Count; i++)
                {
                    Task _TaskGet = FindTaskById(ListDoneQuest[i]);
                    if (_TaskGet != null)
                    {
                        TaskData _FindTaskData = GetTaskData(_TaskGet.ID, client);

                        if (!IsQuestComplete(_TaskGet.ID, _FindTaskData.DoingTaskVal1) && Int32.Parse(_TaskGet.TaskClass) == (int)TaskTypes.Talk)
                        {

                            ProcessTask.Process(Global._TCPManager.MySocketListener, pool, client, npc.ResID, npc.ResID, -1, TaskTypes.Talk);

                            if(IsQuestComplete(_TaskGet.ID, _FindTaskData.DoingTaskVal1))
                            {
                                _NpcDialog.Text = "Có vẻ như người đã hoàn thành nhiệm vụ :" + _TaskGet.Title + ".Hãy xem ta giúp được người điều gì?";

                                Selections.Add(_TaskGet.ID, "[Trả nhiệm vụ]" + _TaskGet.Title);
                            }
                            else
                            {

                                Selections.Add(_TaskGet.ID, "Ta sẽ quay lại sau");
                            }
                          
                        }
                        else
                        {
                            Selections.Add(_TaskGet.ID, "[Trả nhiệm vụ]" + _TaskGet.Title);
                        }
                    }
                }
              
            }

            if(Selections.Count>0)
            {
                Selections.Add(99999, "Ta muốn xem những thứ khác..");

                _NpcDialog.Selections = Selections;

                _NpcDialog.ShowQuestDialog(npc, client);
            }        
            else
            {
                KTLuaEnvironment.ExecuteNPCScript_Open(map, npc, client, npc.ScriptID);
            }
        }

        public static bool IsHaveQuest(int NpcID)
        {
            List<int> tasksList = null;

            if (!GameManager.NPCTasksMgr.SourceNPCTasksDict.TryGetValue(NpcID, out tasksList))

                return false;   //Nếu thằng nPC này không có nhiệm vụ này thì chim cút

            if (0 == tasksList.Count)

                return false;   /// Nếu mà số lượng task trả về ==0 thì cũng chim cút

            return true;
        }

        public static bool IsHaveCompleteQuest(int NpcID)
        {
            List<int> tasksList = null;

            if (!GameManager.NPCTasksMgr.DestNPCTasksDict.TryGetValue(NpcID, out tasksList))

                return false;   //Nếu thằng nPC này không có nhiệm vụ này thì chim cút

            if (0 == tasksList.Count)

                return false;   /// Nếu mà số lượng task trả về ==0 thì cũng chim cút

            return true;
        }

        public static Task FindTaskById(int TaskID)
        {
            var find = _TotalConfig.Tasks.Task.Where(x => x.ID == TaskID).FirstOrDefault();
            if (find != null)
            {
                return find;
            }
            else
            {
                return null;
            }
        }
    }
}