using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using ProtoBuf;
using Server.Tools;

namespace GameServer.KiemThe.GameEvents.LotteryEvent
{
    public class LotteryRecord
    {
        // private readonly List<int> Pool;
        private int numberWin;

        public int NumberWin
        {
            get => numberWin;
        }

        private List<int> TicketPool;

        private readonly ConcurrentDictionary<int, int> LookUp;

        private ConcurrentDictionary<int, int> win6;

        private ConcurrentDictionary<int, int> win4;

        private ConcurrentDictionary<int, int> win2;

        public LotteryRecord()
        {
            // Pool = new List<int>();
            LookUp = new ConcurrentDictionary<int, int>();
            win6 = new ConcurrentDictionary<int, int>();
            win4 = new ConcurrentDictionary<int, int>();
            win2 = new ConcurrentDictionary<int, int>();
            TicketPool = Enumerable.Range(100000, 900000).ToList();
        }

        public void AddTicket(int ticket, int roleID)
        {
            // Pool.Add(ticket);
            if (LookUp.TryAdd(ticket, roleID))
            {
                TicketPool.Remove(ticket);
            }
        }

        public int GetNewTicket()
        {
            if (TicketPool.Count == 0)
                return -1;
            var index = KTGlobal.GetRandomNumber(0, TicketPool.Count - 1);
            return TicketPool[index];
        }

        public void GetWinners()
        {
            var winnerTicketId = KTGlobal.GetRandomNumber(100000, 999999);
            LogManager.WriteLog(LogTypes.Lottery, $"win:{winnerTicketId}");
            numberWin = winnerTicketId;
            //CheckTicket(winnerTicketId);
        }

        /// <summary>
        /// Lấy ra người trúng 6 số để thông báo
        /// </summary>
        public int GetPlayerWinner6()
        {
            if (win6.Count == 0)
                return 0;
            int RoleID = 0;
            foreach (var role in win6.Keys)
            {
                RoleID = role;
            }

            return RoleID;
        }

        public int GetNumberPlayerWin(int number)
        {
            switch (number)
            {
                case 2:
                    return win2.Count;
                case 4:
                    return win4.Count;
                case 6:
                    return win6.Count;
                default:
                    return 0;
            }
        }

        private void CheckTicket(int ticketWin)
        {
            win2 = new ConcurrentDictionary<int, int>();
            win4 = new ConcurrentDictionary<int, int>();
            win6 = new ConcurrentDictionary<int, int>();
            foreach (var ticket in LookUp.Keys)
            {
                if (ticketWin == ticket)
                {
                    win6.TryAdd(LookUp[ticket], ticket);
                }

                if (ticketWin % 10000 == ticket % 10000 && !win6.ContainsKey(LookUp[ticket]) &&
                    !win4.ContainsKey(LookUp[ticket]))
                {
                    win4.TryAdd(LookUp[ticket], ticket);
                }

                if (ticketWin % 100 == ticket % 100 && !win6.ContainsKey(LookUp[ticket]) &&
                    !win4.ContainsKey(LookUp[ticket]) && !win2.ContainsKey(LookUp[ticket]))
                {
                    win2.TryAdd(LookUp[ticket], ticket);
                }
            }
        }

        public void SetWinnerTicket(int numberWin)
        {
            this.numberWin = numberWin;
            CheckTicket(numberWin);
        }

        /// <summary>
        /// kiểm tra trúng mấy số
        /// </summary>
        public byte CheckAwardTicket(int roleID)
        {
            if (win6.ContainsKey(roleID))
            {
                return 6;
            }

            if (win4.ContainsKey(roleID))
            {
                return 4;
            }

            if (win2.ContainsKey(roleID))
            {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Trả về con số trúng thưởng 
        /// </summary>
        public int TicketWin(int roleID)
        {
            int award = CheckAwardTicket(roleID);
            if (award == 2)
            {
                return win2[roleID];
            }

            if (award == 4)
            {
                return win4[roleID];
            }

            if (award == 6)
            {
                return win6[roleID];
            }

            return 0;
        }

        public string GetTickedUsed(int RoleId)
        {
            var strcmd = string.Empty;
            int maxTicketShow = 10;
            foreach (var ticket in LookUp.Keys)
            {
                if (maxTicketShow <= 0)
                    break;
                if (LookUp[ticket] == RoleId)
                {
                    if (strcmd != string.Empty)
                        strcmd += "-";
                    strcmd += ticket.ToString();
                    maxTicketShow--;
                }
            }

            return strcmd;
        }

        public void SetWinnerTicketGM(int numberWin)
        {
            this.numberWin = numberWin;
            LogManager.WriteLog(LogTypes.Lottery, $"win:{numberWin}");
            //CheckTicket(numberWin);
        }

        /// <summary>
        /// đã được quay số chưa
        /// </summary>
        public bool EventIsOpen()
        {
            if (numberWin < 100000 || numberWin > 999999)
            {
                return false;
            }

            return true;
        }
    }

    public static class LotteryManager
    {
        private static ConcurrentDictionary<int, LotteryRecord> Records;
        private static PlayerInfos _PlayerInfos = new PlayerInfos();

        [ProtoContract]
        [XmlRoot(ElementName = "Info")]
        public class PlayerInfo
        {
            [ProtoMember(1)]
            [XmlAttribute(AttributeName = "ID")]
            public int ID;
            [ProtoMember(2)]
            [XmlAttribute(AttributeName = "Name")]
            public string Name;
        }
        [ProtoContract]
        [XmlRoot(ElementName = "Lottery")]
        public class PlayerInfos
        {
            [ProtoMember(1)]
            [XmlElement(ElementName = "Info")]
            public List<PlayerInfo> playerInfos = new List<PlayerInfo>();

        }
        /// <summary>
        /// Đọc từ file xml RoleName theo RoleID
        /// </summary>
        private static void ReadNamePlayerLottery()
        {
            var path = "NameLookUp.xml";
            if (!System.IO.File.Exists(path))
            {
                return;
            }
            using (var stream = new StreamReader(path, Encoding.UTF8))
            {
                var serializer = new XmlSerializer(typeof(PlayerInfos));
                _PlayerInfos = serializer.Deserialize(stream) as PlayerInfos;
            }
        }

        /// <summary>
        /// setup sự kiện
        /// lấy danh sách các vé của người chơi
        /// </summary>
        public static void Setup()
        {
            var currentDate = DateTime.Now.DayOfYear;

            var tommorowDate = currentDate + 1;

            var yesterdayDate = currentDate - 1;

            Records = new ConcurrentDictionary<int, LotteryRecord>
            {
                [currentDate] = new LotteryRecord(),
                [yesterdayDate] = new LotteryRecord()
            };

            for (var i = 0; i < 20; ++i)
            {
                // tạo trước 20 ngày record tiếp theo, đảm bảo qua ngày không bị lỗi
                Records[currentDate + i] = new LotteryRecord();
            }

            var dataToday = LogManager.ReadLog(LogTypes.Lottery, DateTime.Now);
            var dataYesterday = LogManager.ReadLog(LogTypes.Lottery, DateTime.Now.AddDays(-1));

            if (dataToday != null)
            {
                ReadDataFromFileLog(currentDate, dataToday);
            }

            if (dataYesterday != null)
            {
                ReadDataFromFileLog(yesterdayDate, dataYesterday);
            }
        }

        public static void StartEvent(int activityID)
        {
            var date = DateTime.Now.DayOfYear;
            //Thông báo số trúng thưởng 
            if (activityID == 500)
            {
                ReadNamePlayerLottery();
                
                // Nếu ko có dữ liệu thì ko thông báo
                if (Records[date - 1].NumberWin < 100000 || Records[date - 1].NumberWin > 999999)
                    return;

                KTGlobal.SendSystemEventNotification(
                    $"Con số trúng thưởng ngày hôm qua là {Records[date - 1].NumberWin}. Hãy đến Cổ Phong Hà để nhận phần thưởng!");

                if (_PlayerInfos == null)
                    return;
                
                var playerID = Records[date - 1].GetPlayerWinner6();
                if (playerID != 0)
                {
                    var info = _PlayerInfos.playerInfos.FirstOrDefault(x => x.ID == playerID);
                    if (info == null)
                        return;
                    var playerName = KTGlobal.CreateStringByColor(info.Name, ColorType.AppleGreen);
                    var TitleName = KTGlobal.CreateStringByColor("100 vạn KNB (khóa)", ColorType.VanillaIce);
                    KTGlobal.SendSystemEventNotification(
                        $"Chúc mừng {playerName} đã may mắn trúng thưởng 6 số ngày hôm qua nhận được {TitleName}");
                }
            }

            //Quay số trúng thưởng chỉ cần quay trong ngày vì cần đến qua ngày hôm sau mới đc nhận thưởng đang set quay lúc 18h30
            if (activityID == 501)
            {
                LotteryManager.DrawWinner(date);
            }

            //Kiểm tra các vé trúng thưởng không, đang để 0h01 đêm ngày hôm sau
            if (activityID == 502)
            {
                Records[date - 1].SetWinnerTicket(Records[date - 1].NumberWin);
                LogManager.WriteLog(LogTypes.LotteryAward, $"{GetNumberPlayerWin(2)} trúng 2 số");
                LogManager.WriteLog(LogTypes.LotteryAward, $"{GetNumberPlayerWin(4)} trúng 4 số");
                LogManager.WriteLog(LogTypes.LotteryAward, $"{GetNumberPlayerWin(6)} trúng 6 số");
            }
        }

        /// <summary>
        /// rút một vé cho người chơi
        /// </summary>
        /// <param name="player"></param>
        public static void DrawTicket(KPlayer player)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date];
            // rút một vé số cho người chơi

            var ticket = currentRecord.GetNewTicket();

            currentRecord.AddTicket(ticket, player.RoleID);

            PlayerManager.ShowNotification(player, $"Bạn đã nhận được mã số {ticket}");

            LogManager.WriteLog(LogTypes.Lottery, $"{player.RoleID} get {ticket}");
        }

        /// <summary>
        /// rút thưởng (trong pool ngày hôm trước)
        /// </summary>
        /// <param name="player"></param>
        public static void DrawWinner(int date)
        {
            Records[date].GetWinners();
        }

        public static void ReadDataFromFileLog(int date, string[] data)
        {
            int numberWin = 0;
            foreach (var record in data)
            {
                if (!record.Contains("win"))
                {
                    string[] fields = record.Split(':');
                    int RoleID = int.Parse(fields[fields.Length - 1].Split(' ')[1]);
                    int ticketID = int.Parse(fields[fields.Length - 1].Split(' ')[3]);
                    Records[date].AddTicket(ticketID, RoleID);
                }
                else
                {
                    string[] fields = record.Split(':');
                    numberWin = int.Parse(fields[fields.Length - 1]);
                }
            }
            Records[date].SetWinnerTicket(numberWin);
        }

        /// <summary>
        /// Trả thưởng cho người chơi nếu trúng
        /// </summary>
        //public static void AddGiftToPlayer(KPlayer client, int money)
        //{
        //    PlayerManager.ShowNotification(client, $"Bạn đã nhận thưởng giải trúng {CheckAwardTicket(client)} số thành công");
        //    LogManager.WriteLog(LogTypes.LotteryAward, $"Player({client.RoleID}/{client.RoleName}) nhận thưởng {money} KNB khóa trúng {CheckAwardTicket(client)} số");
        //}

        /// <summary>
        /// Lấy ra số người trúng thưởng
        /// </summary>
        public static int GetNumberPlayerWin(int number)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date - 1];
            return currentRecord.GetNumberPlayerWin(number);
        }

        /// <summary>
        /// kiểm tra xem đã quay số ngày hôm qua chưa
        /// </summary>
        public static bool HadLotteryYesterday()
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date - 1];
            return currentRecord.EventIsOpen();
        }

        /// <summary>
        /// Hiển thị con số trúng thưởng hôm qua
        /// </summary>
        public static int NumberWinLotteryYesterday()
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date - 1];
            return currentRecord.NumberWin;
        }

        /// <summary>
        /// Hiển thị con số đang sở hữu theo ngày
        /// </summary>
        public static string GetTickedUsed(KPlayer player, int date)
        {
            var currentRecord = Records[date];
            string strcmd = currentRecord.GetTickedUsed(player.RoleID);
            return strcmd;
        }

        public static int GetTicketWin(KPlayer player)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date - 1];
            return currentRecord.TicketWin(player.RoleID);
        }

        public static int CheckAwardTicket(KPlayer player)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date - 1];
            return currentRecord.CheckAwardTicket(player.RoleID);
        }

        #region GM

        public static void AddNumberLottery(KPlayer player, int number)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date];
            currentRecord.AddTicket(number, player.RoleID);
        }

        public static void SetNumberWin(int number)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date];
            currentRecord.SetWinnerTicketGM(number);
        }

        public static void AddRandomNumber(KPlayer player, int number)
        {
            var date = DateTime.Now.DayOfYear;
            var currentRecord = Records[date];
            for (int i = 0; i < number; i++)
            {
                DrawTicket(player);
            }
        }

        public static void FakeRecords()
        {
            var currentDate = DateTime.Now.DayOfYear;
            foreach (var r in Records.Keys)
            {
                Records.TryRemove(r, out var record);
                if (r >= currentDate)
                    Records.TryAdd(r - 1, record);
                Records.TryAdd(currentDate + 1, new LotteryRecord());
            }
        }

        #endregion
    }
}