using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServer.KiemThe;
using ProtoBuf;
using UnityEngine.Assertions.Must;

namespace Server.Data
{
    /// <summary>
    /// Bách Bảo Rương
    /// </summary>
    public class KTSeashellCircle
    {
        /// <summary>
        /// Loại ô quay trong khung Bách bảo rương
        /// </summary>
        public enum SeashellCircleCellType
        {
            /// <summary>
            /// Tinh hoạt lực
            /// </summary>
            GatherMakePoint = 0,

            /// <summary>
            /// Bạc
            /// </summary>
            Money = 1,
            /// <summary>
            /// Đồng khóa
            /// </summary>
            BoundToken = 2,

            /// <summary>
            /// Huyền Tinh
            /// </summary>
            CrystalStone = 3,

            /// <summary>
            /// Phiếu đổi quà sự kiện sinh nhật
            /// </summary>
            BirthdayCoupon = 4,
            
            EventMaterial = 5,
            MoonStone = 6,
            Total = 7
        }

        /// <summary>
        /// Thông tin nhóm ô
        /// </summary>
        public class Cell
        {
            /// <summary>
            /// Loại
            /// </summary>
            public SeashellCircleCellType Type { get; set; }

            /// <summary>
            /// Tên loại
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Giá trị theo tầng
            /// </summary>
            public List<int> ValuesByStage { get; set; }

            /// <summary>
            /// Các vị trí 1 sao
            /// </summary>
            public List<int> Position_1 { get; set; }

            /// <summary>
            /// Các vị trí 2 sao
            /// </summary>
            public List<int> Position_2 { get; set; }

            /// <summary>
            /// Các vị trí 3 sao
            /// </summary>
            public List<int> Position_3 { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static Cell Parse(XElement xmlNode)
            {
                // Tạo mới đối tượng
                var cell = new Cell()
                {
                    Type = (SeashellCircleCellType)KTGlobal.GetXMLAttribute<int>(xmlNode, "Type"),
                    Name = KTGlobal.GetXMLAttribute<string>(xmlNode, "Name"),
                    ValuesByStage = new List<int>(),
                    Position_1 = new List<int>(),
                    Position_2 = new List<int>(),
                    Position_3 = new List<int>(),
                };

                // Duyệt danh sách giá trị theo tầng
                foreach (var valueStr in KTGlobal.GetXMLAttribute<string>(xmlNode, "StageValues").Split(';'))
                {
                    try
                    {
                        var value = int.Parse(valueStr);
                        cell.ValuesByStage.Add(value);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        cell.ValuesByStage.Add(0);
                    }
                }

                // Duyệt danh sách các vị trí 1 sao
                foreach (var valueStr in KTGlobal.GetXMLAttribute<string>(xmlNode, "Position_1").Split(';'))
                {
                    try
                    {
                        var value = int.Parse(valueStr);
                        cell.Position_1.Add(value);
                    }
                    catch (Exception ex)
                    {
                        //  Console.WriteLine(ex.ToString());
                        cell.Position_1.Add(-1);
                    }
                }

                // Duyệt danh sách các vị trí 2 sao
                foreach (var valueStr in KTGlobal.GetXMLAttribute<string>(xmlNode, "Position_2").Split(';'))
                {
                    try
                    {
                        var value = int.Parse(valueStr);
                        cell.Position_2.Add(value);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        cell.Position_2.Add(-1);
                    }
                }

                // Duyệt danh sách các vị trí 1 sao
                foreach (var valueStr in KTGlobal.GetXMLAttribute<string>(xmlNode, "Position_3").Split(';'))
                {
                    try
                    {
                        var value = int.Parse(valueStr);
                        cell.Position_3.Add(value);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        cell.Position_3.Add(-1);
                    }
                }

                // Trả về đối tượng
                return cell;
            }
        }

        /// <summary>
        /// Quy đổi tầng tương ứng sang sò
        /// </summary>
        public class ExchangeSeashellStage
        {
            /// <summary>
            /// Thứ tự tầng
            /// </summary>
            public int Stage { get; set; }

            /// <summary>
            /// Giá trị
            /// </summary>
            public int Value { get; set; }
            
            /// <summary>
            /// điểm quy đổi để tính xếp hạng
            /// </summary>
            public int Score { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static ExchangeSeashellStage Parse(XElement xmlNode)
            {
                return new ExchangeSeashellStage()
                {
                    Stage = KTGlobal.GetXMLAttribute<int>(xmlNode, "ID"),
                    Value = KTGlobal.GetXMLAttribute<int>(xmlNode, "Value"),
                    Score = KTGlobal.GetXMLAttribute<int>(xmlNode, "Score"),
                };
            }
        }

        /// <summary>
        /// Tỷ lệ cược đổi được bao nhiêu lần phần thưởng
        /// </summary>
        public class BetRate
        {
            /// <summary>
            /// Cược 2 sò
            /// </summary>
            public int Bet_2 { get; set; }

            /// <summary>
            /// Cược 10 sò
            /// </summary>
            public int Bet_10 { get; set; }

            /// <summary>
            /// Cược 50 sò
            /// </summary>
            public int Bet_50 { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static BetRate Parse(XElement xmlNode)
            {
                return new BetRate()
                {
                    Bet_2 = KTGlobal.GetXMLAttribute<int>(xmlNode, "Bet_2"),
                    Bet_10 = KTGlobal.GetXMLAttribute<int>(xmlNode, "Bet_10"),
                    Bet_50 = KTGlobal.GetXMLAttribute<int>(xmlNode, "Bet_50"),
                };
            }
        }

        /// <summary>
        /// Tỷ lệ quay các ô cùng loại tương ứng theo tầng
        /// </summary>
        public class TurnRateByStage
        {
            /// <summary>
            /// Thứ tự tầng
            /// </summary>
            public int Stage { get; set; }

            /// <summary>
            /// Tỷ lệ quay vào ô cùng loại lần trước
            /// </summary>
            public int RateToSameType { get; set; }

            /// <summary>
            /// Tỷ lệ quay vào ô cùng loại 1 sao
            /// </summary>
            public int RateToPosition_1 { get; set; }

            /// <summary>
            /// Tỷ lệ quay vào ô cùng loại 2 sao
            /// </summary>
            public int RateToPosition_2 { get; set; }

            /// <summary>
            /// Tỷ lệ quay vào ô cùng loại 3 sao
            /// </summary>
            public int RateToPosition_3 { get; set; }

            /// <summary>
            /// Chuyển đối tượng từ XMLNode
            /// </summary>
            /// <param name="xmlNode"></param>
            /// <returns></returns>
            public static TurnRateByStage Parse(XElement xmlNode)
            {
                return new TurnRateByStage()
                {
                    Stage = KTGlobal.GetXMLAttribute<int>(xmlNode, "ID"),
                    RateToSameType = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateToSameType"),
                    RateToPosition_1 = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateToPosition_1"),
                    RateToPosition_2 = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateToPosition_2"),
                    RateToPosition_3 = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateToPosition_3"),
                };
            }
        }

        /// <summary>
        /// Danh sách nhóm ô
        /// </summary>
        public List<Cell> Cells { get; set; }

        /// <summary>
        /// Danh sách đổi sò theo tầng
        /// </summary>
        public List<ExchangeSeashellStage> ExchangeSeashells { get; set; }

        /// <summary>
        /// Tỷ lệ cược đổi phần thưởng
        /// </summary>
        public BetRate BetRates { get; set; }

        /// <summary>
        /// Danh sách tỷ lệ quay vào ô cùng nhóm theo tầng
        /// </summary>
        public List<TurnRateByStage> TurnRateByStages { get; set; }

        /// <summary>
        /// Thời gian quay tối thiểu (Tick)
        /// </summary>
        public int MinTimeTick { get; set; }

        /// <summary>
        /// Thời gian quay tối đa (Tick)
        /// </summary>
        public int MaxTimeTick { get; set; }

        /// <summary>
        /// Số vòng quay tối thiểu
        /// </summary>
        public int MinRound { get; set; }

        /// <summary>
        /// Số vòng quay tối thiểu
        /// </summary>
        public int MaxRound { get; set; }

        /// <summary>
        /// Tỷ lệ quay vào Rương
        /// </summary>
        public int RateToTreasure { get; set; }

        public int RateTo100Treasure { get; set; }

        /// <summary>
        /// Vị trí có rương
        /// </summary>
        public List<int> TreasurePosition { get; set; }

        public int TreasureID { get; set; }

        public int SeashellID { get; set; }

        // public string StartDate;

        // public string EndDate;

        // public string StartDate_FreeTicket;
        // public string EndDate_FreeTicket;
        public int ShopID;
        public int EventMaterialID { get; set; }
        public int CouponID { get; set; }
        public int MoonStoneID { get; set; }

        /// <summary>
        /// Chuyển đối tượng từ XMLNode
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        public static KTSeashellCircle Parse(XElement xmlNode)
        {
            // Tạo mới đối tượng
            var circle = new KTSeashellCircle()
            {
                MinTimeTick = KTGlobal.GetXMLAttribute<int>(xmlNode, "MinTimeTick"),
                MaxTimeTick = KTGlobal.GetXMLAttribute<int>(xmlNode, "MaxTimeTick"),
                MinRound = KTGlobal.GetXMLAttribute<int>(xmlNode, "MinRound"),
                MaxRound = KTGlobal.GetXMLAttribute<int>(xmlNode, "MaxRound"),
                RateToTreasure = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateToTreasure"),
                RateTo100Treasure = KTGlobal.GetXMLAttribute<int>(xmlNode, "RateTo100Treasure"),
                TreasurePosition = new List<int>(),
                Cells = new List<Cell>(),
                ExchangeSeashells = new List<ExchangeSeashellStage>(),
                BetRates = BetRate.Parse(xmlNode.Element("BetRate")),
                TurnRateByStages = new List<TurnRateByStage>(),
                TreasureID = KTGlobal.GetXMLAttribute<int>(xmlNode, "TreasureID"),
                SeashellID = KTGlobal.GetXMLAttribute<int>(xmlNode, "SeashellID"),
                // StartDate = KTGlobal.GetXMLAttribute<string>(xmlNode, "StartDate"),
                // EndDate = KTGlobal.GetXMLAttribute<string>(xmlNode, "EndDate"),
                // StartDate_FreeTicket = KTGlobal.GetXMLAttribute<string>(xmlNode, "StartDate_FreeTicket"),
                // EndDate_FreeTicket = KTGlobal.GetXMLAttribute<string>(xmlNode, "EndDate_FreeTicket"),
                ShopID = KTGlobal.GetXMLAttribute<int>(xmlNode, "ShopID"),
                EventMaterialID = KTGlobal.GetXMLAttribute<int>(xmlNode, "EventMaterialID"),
                CouponID = KTGlobal.GetXMLAttribute<int>(xmlNode, "CouponID"),
                MoonStoneID = KTGlobal.GetXMLAttribute<int>(xmlNode, "MoonStoneID")
            };

            // Duyệt danh sách các vị trí có rương
            foreach (var valueStr in KTGlobal.GetXMLAttribute<string>(xmlNode, "TreasurePosition").Split(';'))
            {
                try
                {
                    var value = int.Parse(valueStr);
                    circle.TreasurePosition.Add(value);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    circle.TreasurePosition.Add(-1);
                }
            }

            // Duyệt danh sách Cell
            foreach (var node in xmlNode.Element("Cells").Elements("Cell"))
            {
                // Thêm vào danh sách
                circle.Cells.Add(Cell.Parse(node));
            }

            // Duyệt danh sách ExchangeSeashell
            foreach (var node in xmlNode.Element("ExchangeSeashell").Elements("Stage"))
            {
                // Thêm vào danh sách
                circle.ExchangeSeashells.Add(ExchangeSeashellStage.Parse(node));
            }

            // Duyệt danh sách TurnRate
            foreach (var node in xmlNode.Element("TurnRate").Elements("Stage"))
            {
                // Thêm vào danh sách
                circle.TurnRateByStages.Add(TurnRateByStage.Parse(node));
            }

            // Trả về giá trị
            return circle;
        }
    }
}