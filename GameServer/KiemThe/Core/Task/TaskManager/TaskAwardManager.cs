using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Task
{
    public class TaskAwardManager
    {
        private static string KTTaskAward = "Config/KT_Task/TaskAward.xml";

        public static List<Task> _ListTaskAward = new List<Task>();

        public static void SetUp()
        {
            string Files = Global.GameResPath(KTTaskAward);
            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(List<Task>));

                _ListTaskAward = serializer.Deserialize(stream) as List<Task>;
            }
        }

        public static void SendAwardTask(KPlayer client, int levelAward, int taskID, int typeTask)
        {
            LogManager.WriteLog(LogTypes.task, $"{client.RoleName}, nhận thưởng loại {typeTask}");
            var taskAwardInfo = TaskAwardManager._ListTaskAward.Where(x => x.Type == typeTask && x.Count == levelAward + 1).FirstOrDefault();
            if (taskAwardInfo != null)
            {
                int ran = KTGlobal.GetRandomNumber(0, 99);
                LogManager.WriteLog(LogTypes.task, $"{client.RoleName}, nhận thưởng Rate= {taskAwardInfo.Rate}, ran = {ran}");
                if (taskAwardInfo.Rate >= ran)
                {
                    if (taskAwardInfo.Exp > 0)
                    {
                        GameManager.ClientMgr.ProcessRoleExperience(client, 2000000, true, false, true, "EXP");
                    }

                    if (taskAwardInfo.Prestige > 0)
                    {
                        int oldPretige = client.Prestige;
                        //+ 2 uy danh bạo văn đồng
                        client.Prestige = client.Prestige + taskAwardInfo.Prestige;
                        LogManager.WriteLog(LogTypes.UyDanh, String.Format("PlayerID: {0} nhận được {1} uy danh ở nhiệm vụ Nghĩa Quân, uy danh tăng từ {2} lên {3}", client.RoleName, taskAwardInfo.Prestige, oldPretige, client.Prestige));
                    }

                    if (taskAwardInfo.BoundMoney > 0)
                    {
                        GameManager.ClientMgr.AddUserBoundMoney(client, taskAwardInfo.BoundMoney, "QUEST|" + taskID);
                    }

                    if (taskAwardInfo.Money > 0)
                    {
                        GameManager.ClientMgr.AddMoney(client, taskAwardInfo.Money, "QUEST|" + taskID);
                    }

                    if (taskAwardInfo.BoundToken > 0)
                    {
                        GameManager.ClientMgr.AddBoundToken(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, taskAwardInfo.BoundToken, "QUEST|" + taskID);
                    }

                    if (taskAwardInfo.Token > 0)
                    {
                        GameManager.ClientMgr.AddToken(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, taskAwardInfo.BoundToken, "QUEST|" + taskID);
                    }

                    if (taskAwardInfo.Items != null && taskAwardInfo.Items.Count > 0)
                    {
                        foreach (var item in taskAwardInfo.Items)
                        {
                            if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, client, item.ID, item.Number, 0, "QUEST | " + taskID, false, item.Binding, true, Global.ConstGoodsEndTime))
                            {
                                PlayerManager.ShowNotification(client, "Có lỗi khi nhận vật phẩm chế tạo");
                            }
                        }
                    }
                }    
                
            }
        }


        public class Task
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ID { get; set; }

            [XmlAttribute(AttributeName = "Type")]
            public int Type { get; set; }

            [XmlAttribute(AttributeName = "Count")]
            public int Count { get; set; }

            [XmlAttribute(AttributeName = "Rate")]
            public int Rate { get; set; }

            [XmlAttribute(AttributeName = "Money")]
            public int Money { get; set; }

            [XmlAttribute(AttributeName = "BoundMoney")]
            public int BoundMoney { get; set; }

            [XmlAttribute(AttributeName = "Token")]
            public int Token { get; set; }

            [XmlAttribute(AttributeName = "BoundToken")]
            public int BoundToken { get; set; }

            [XmlAttribute(AttributeName = "Prestige")]
            public int Prestige { get; set; }

            [XmlAttribute(AttributeName = "Exp")]
            public int Exp { get; set; }

            [XmlElement(ElementName = "Item")]
            public List<ItemAwardInfo> Items { get; set; }

        }

        public class ItemAwardInfo
        {
            [XmlAttribute(AttributeName = "ID")]
            public int ID { get; set; }

            [XmlAttribute(AttributeName = "Name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "Binding")]
            public int Binding { get; set; }

            [XmlAttribute(AttributeName = "Number")]
            public int Number { get; set; }
        }
    }
}
