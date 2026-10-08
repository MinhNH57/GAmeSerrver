using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Utilities;
using GameServer.KiemThe.Utilities.Algorithms;
using GameServer.Logic;
using GameServer.Server;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GameServer.KiemThe.Logic
{
    /// <summary>
    /// Đối tượng thực thi dịch chuyển BOT theo dãy đường đi cho trước
    /// </summary>
    public class KTOtherObjectStoryBoard
    {
        #region Singleton - Instance
        /// <summary>
        /// Đối tượng thực thi dịch chuyển BOT theo dãy đường đi cho trước
        /// </summary>
        public static KTOtherObjectStoryBoard Instance { get; private set; }

        private KTOtherObjectStoryBoard() { }
        #endregion

        #region Initialize
        public static void Init()
        {
            KTOtherObjectStoryBoard.Instance = new KTOtherObjectStoryBoard();
        }
        #endregion

        #region Inheritance
        /// <summary>
        /// Luồng quản lý
        /// </summary>
        private class OtherObjectStoryBoardTimer : KTTimer
        {
            /// <summary>
            /// Chủ nhân
            /// </summary>
            public GameObject Owner { get; set; }

            /// <summary>
            /// Đường đi
            /// </summary>
            public ConcurrentQueue<UnityEngine.Vector2> Paths { get; set; }
            public int ClientPathFloor { get; set; }

            /// <summary>
            /// Thời gian thực thi lần trước
            /// </summary>
            public long LastTick { get; set; } = 0;

            /// <summary>
            /// Tọa độ lưới lần tước
            /// </summary>
            public UnityEngine.Vector2 LastGridPos { get; set; }

            /// <summary>
            /// Loại động tác di chuyển là chạy hay đi bộ
            /// </summary>
            public KE_NPC_DOING Action { get; set; }
        }

        /// <summary>
        /// Inner Timer
        /// </summary>
        private class InnerTimer : KTTimerManager<OtherObjectStoryBoardTimer>
        {
            /// <summary>
            /// Thời gian kích hoạt luồng kiểm tra
            /// </summary>
            protected override int PeriodTick
            {
                get
                {
                    return 100;
                }
            }

            /// <summary>
            /// Inner Timer
            /// </summary>
            public InnerTimer() : base()
            {

            }
        }
        #endregion

        /// <summary>
        /// Thời gian Tick kiểm tra
        /// </summary>
        private const float PeriodTick = 0.1f;
        private const int ClientPathNodes = 64;
        private const int ClientPathLookAhead = 24;

        /// <summary>
        /// Luồng quản lý
        /// </summary>
        private readonly InnerTimer timer = new InnerTimer();

        /// <summary>
        /// Danh sách các đối tượng đang quản lý
        /// </summary>
        private readonly ConcurrentDictionary<int, OtherObjectStoryBoardTimer> objectTimers = new ConcurrentDictionary<int, OtherObjectStoryBoardTimer>();

        #region Implements
        /// <summary>
        /// Thêm đối tượng cần dịch chuyển vào danh sách
        /// </summary>
        /// <param name="go"></param>
        /// <param name="fromPos"></param>
        /// <param name="toPos"></param>
        /// <param name="action"></param>
        public void Add(GameObject go, Point fromPos, Point toPos, KE_NPC_DOING action = KE_NPC_DOING.do_run, bool forceMoveBySpecialState = false)
        {
            if (go == null)
            {
                return;
            }
            else if (fromPos == default || toPos == default)
            {
                return;
            }
            /// Nếu dính trạng thái không thể di chuyển
            else if ((!forceMoveBySpecialState && !go.IsCanPositiveMove()) || (forceMoveBySpecialState && !go.IsCanMove()))
            {
                return;
            }

            /// Dừng thực thi StoryBoard trước đó
            this.Remove(go);

            /// Thực hiện tìm đường
            List<UnityEngine.Vector2> nodeList = KTGlobal.FindPath(go, new UnityEngine.Vector2((float) fromPos.X, (float) fromPos.Y), new UnityEngine.Vector2((float) toPos.X, (float) toPos.Y));

            /// Nếu không có đường đi thì bỏ qua
            if (nodeList.Count < 2)
            {
                return;
            }

            /// Hàng đợi chứa kết quả
            ConcurrentQueue<UnityEngine.Vector2> paths = new ConcurrentQueue<UnityEngine.Vector2>();
            /// Nạp tất cả các điểm vừa tìm được vào hàng đợi
            foreach (UnityEngine.Vector2 pos in nodeList)
            {
                paths.Enqueue(pos);
            }


            string pathStr = "";
            foreach (UnityEngine.Vector2 pos in paths)
            {
                pathStr += "|" + string.Format("{0}_{1}", (int) pos.x, (int) pos.y);
            }
            pathStr = pathStr.Substring(1);
            /// Gửi gói tin về Client thông báo
            this.SendObjectMoveToClient(go, pathStr, fromPos, toPos, go.CurrentDir, action);

            //Console.WriteLine("Move from {0} to {1} with paths = {2}", fromPos.ToString(), toPos.ToString(), pathStr);

            GameMap gameMap = GameManager.MapMgr.DictMaps[go.CurrentMapCode];

            /// Vị trí hiện tại của BOT
            UnityEngine.Vector2 currentPos = new UnityEngine.Vector2((int) go.CurrentPos.X, (int) go.CurrentPos.Y);
            /// Vị trí đích đến
            go.ToPos = toPos;

            OtherObjectStoryBoardTimer timer = new OtherObjectStoryBoardTimer()
            {
                Name = "Storyboard - Other Object " + go.RoleID,
                Alive = true,
                Interval = -1,
                PeriodActivation = KTOtherObjectStoryBoard.PeriodTick,
                Owner = go,
                Paths = paths,
                LastGridPos = KTGlobal.WorldPositionToGridPosition(gameMap, currentPos),
                LastTick = KTGlobal.GetCurrentTimeMilis(),
                Action = action,
            };
            timer.Start = () => {
                this.StartStoryBoard(timer);
            };
            timer.Tick = () => {
                this.TickStoryBoard(timer);
            };
            timer.Destroy = () => {
                this.FinishStoryBoard(timer);
            };
            this.timer.AddTimer(timer);

            this.objectTimers[go.RoleID] = timer;
        }

        /// <summary>
        /// Thêm đối tượng cần dịch chuyển vào danh sách
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="pathString"></param>
        /// <param name="action"></param>
        public void Add(GameObject go, string pathString, KE_NPC_DOING action = KE_NPC_DOING.do_run, bool forceMoveBySpecialState = false)
        {
            if (go == null)
            {
                return;
            }
            /// Nếu dính trạng thái không thể di chuyển
            else if ((!forceMoveBySpecialState && !go.IsCanPositiveMove()) || (forceMoveBySpecialState && !go.IsCanMove()))
            {
                return;
            }

            /// Dừng thực thi StoryBoard trước đó
            this.Remove(go);

            ConcurrentQueue<UnityEngine.Vector2> paths = new ConcurrentQueue<UnityEngine.Vector2>();
            string[] points = pathString.Split('|');
            if (points.Length < 2)
            {
                return;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[go.CurrentMapCode];

            Point toPos = new Point(-1, -1);
            /// Nạp tất cả các điểm vừa tìm được vào hàng đợi
            for (int i = 0; i < points.Length; i++)
            {
                string[] pos = points[i].Split('_');
                if (pos.Length < 2)
                {
                    throw new Exception();
                }

                int posX = int.Parse(pos[0]);
                int posY = int.Parse(pos[1]);

                /// Nếu vị trí hiện tại không thể đến được
                if (!GameManager.MapMgr.GetGameMap(go.CurrentMapCode).CanMove(new Point(posX, posY)))
                {
                    if (go is KTBot) return; // Never skip an obstructed BOT waypoint and connect across a wall.
                    continue;
                }

                toPos = new Point(posX, posY);

                paths.Enqueue(new UnityEngine.Vector2(posX, posY));
            }

            if (paths.Count < 2) return;

            /// Vị trí hiện tại của BOT
            UnityEngine.Vector2 currentPos = new UnityEngine.Vector2((int) go.CurrentPos.X, (int) go.CurrentPos.Y);
            /// Vị trí đích đến
            go.ToPos = toPos;

            OtherObjectStoryBoardTimer timer = new OtherObjectStoryBoardTimer()
            {
                Name = "Storyboard - Other Object " + go.RoleID,
                Alive = true,
                Interval = -1,
                PeriodActivation = KTOtherObjectStoryBoard.PeriodTick,
                Owner = go,
                Paths = paths,
                LastGridPos = KTGlobal.WorldPositionToGridPosition(gameMap, currentPos),
                LastTick = KTGlobal.GetCurrentTimeMilis(),
                Action = action,
            };
            timer.Start = () => {
                this.StartStoryBoard(timer);
            };
            timer.Tick = () => {
                this.TickStoryBoard(timer);
            };
            timer.Destroy = () => {
                this.FinishStoryBoard(timer);
            };
            if (go is KTBot) this.SendBotPathWindow(timer);
            this.objectTimers[go.RoleID] = timer;
            this.timer.AddTimer(timer);
        }

        /// <summary>
        /// Xóa đối tượng khỏi danh sách
        /// </summary>
        /// <param name="go"></param>
        /// <param name="synsClient"></param>
        /// <param name="includeElapse"></param>
        /// <param name="additionTick"></param>
        public void Remove(GameObject go, bool synsClient = true/*, bool includeElapse = true, long additionTick = 0*/)
        {
            if (this.objectTimers.TryGetValue(go.RoleID, out OtherObjectStoryBoardTimer objectTimer))
            {
                /*
                /// Thực hiện nốt thời gian deltaT từ lần cuối Tick cho đến hiện tại hủy, nếu khoảng deltaT chênh lệch đáng kể
                if (includeElapse && (KTGlobal.GetCurrentTimeMilis() - objectTimer.LastTick + additionTick) / 1000f >= 0.02f)
                {
                    this.TickStoryBoard(objectTimer, additionTick, false);
                }
                */

                this.timer.KillTimer(objectTimer);
                this.objectTimers.TryRemove(go.RoleID, out _);
                go.ToPos = go.CurrentPos;

                /// Đồng bộ về Client
                if (synsClient)
                {
                    this.SendObjectStopMoveToClient(go);
                }
            }
        }

        /// <summary>
        /// Kiểm tra có StoryBoard của BOT tương ứng đang thực thi không
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public bool HasStoryBoard(GameObject go)
        {
            return this.objectTimers.TryGetValue(go.RoleID, out _);
        }

        /// <summary>
        /// Trả về danh sách đường đi hiện tại của đối tượng
        /// </summary>
        /// <returns></returns>
        public string GetCurrentPathString(GameObject go)
        {
            if (this.objectTimers.TryGetValue(go.RoleID, out OtherObjectStoryBoardTimer objectTimer))
            {
                Queue<UnityEngine.Vector2> paths = new Queue<UnityEngine.Vector2>(
                    go is KTBot ? objectTimer.Paths.Take(ClientPathNodes) : objectTimer.Paths);

                /// Vị trí hiện tại
                UnityEngine.Vector2 currentPos = new UnityEngine.Vector2((int) go.CurrentPos.X, (int) go.CurrentPos.Y);
                string str = string.Format("{0}_{1}", (int) currentPos.x, (int) currentPos.y) + "|";
                while (paths.Count > 0)
                {
                    UnityEngine.Vector2 pos = paths.Dequeue();
                    str += string.Format("{0}_{1}", (int) pos.x, (int) pos.y) + "|";
                }
                if (str.Length > 0)
                {
                    return str.Substring(0, str.Length - 1);
                }
                else
                {
                    return str;
                }
            }
            else
            {
                return "";
            }
        }
        #endregion

        #region Network
        /// <summary>
        /// Gửi gói tin BOT vật di chuyển tới người chơi xung quanh
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="pathString"></param>
        /// <param name="fromPos"></param>
        /// <param name="toPos"></param>
        /// <param name="direction"></param>
        /// <param name="action"></param>
        private void SendBotPathWindow(OtherObjectStoryBoardTimer timer)
        {
            var window = timer.Paths.Take(ClientPathNodes).ToList();
            if (window.Count == 0) return;
            Point from = timer.Owner.CurrentPos;
            string path = ((int)from.X) + "_" + ((int)from.Y) + "|" +
                string.Join("|", window.Select(p => ((int)p.x) + "_" + ((int)p.y)));
            var end = window[window.Count - 1];
            this.SendObjectMoveToClient(timer.Owner, path, from,
                new Point(end.x, end.y), timer.Owner.CurrentDir, timer.Action);
            timer.ClientPathFloor = Math.Max(0, timer.Paths.Count - window.Count);
        }

        private void SendObjectMoveToClient(GameObject go, string pathString, Point fromPos, Point toPos, KiemThe.Entities.Direction direction, KE_NPC_DOING action)
        {
            try
            {
                int fromPosX = (int) fromPos.X;
                int fromPosY = (int) fromPos.Y;
                int toPosX = (int) toPos.X;
                int toPosY = (int) toPos.Y;

                string zipPathString = pathString;

                int moveSpeed = go.GetCurrentRunSpeed();
                GameManager.ClientMgr.NotifyOthersToMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, go, go.CurrentMapCode, go.CurrentCopyMapID, go.RoleID, KTGlobal.GetCurrentTimeMilis(), fromPosX, fromPosY, (int) action, toPosX, toPosY, (int) TCPGameServerCmds.CMD_SPR_MOVE, moveSpeed, zipPathString, null, direction);
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex.ToString());
            }
        }

        /// <summary>
        /// Thông báo cho người chơi khác đối tượng ngừng di chuyển
        /// </summary>
        /// <param name="obj"></param>
        private void SendObjectStopMoveToClient(GameObject go)
        {
            List<object> objsList = Global.GetAll9Clients(go);

            /// Tạo mới gói tin
            byte[] data = DataHelper.ObjectToBytes<SpriteStopMove>(new SpriteStopMove()
            {
                RoleID = go.RoleID,
                PosX = (int) go.CurrentPos.X,
                PosY = (int) go.CurrentPos.Y,
                MoveSpeed = go.GetCurrentRunSpeed(),
            });

            GameManager.ClientMgr.SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, go, objsList, data, (int) TCPGameServerCmds.CMD_SPR_STOPMOVE);
        }
        #endregion

        #region Logic
        /// <summary>
        /// Bắt đầu thực hiện StoryBoard
        /// </summary>
        /// <param name="timer"></param>
        private void StartStoryBoard(OtherObjectStoryBoardTimer timer)
        {
            /// Nếu dính trạng thái không thể di chuyển
            if (!timer.Owner.IsCanMove())
            {
                this.Remove(timer.Owner);
                return;
            }

            ///// Nếu hàng đợi rỗng
            //if (timer.Paths.Count <= 0)
            //{
            //    return;
            //}

            /// Cập nhật động tác tương ứng
            timer.Owner.m_eDoing = timer.Action;

            /// Vị trí hiện tại
            UnityEngine.Vector2 currentPos = new UnityEngine.Vector2((int) timer.Owner.CurrentPos.X, (int) timer.Owner.CurrentPos.Y);
            /// Nếu vị trí hiện tại trùng với vị trí đầu tiên trong hàng đợi thì bỏ qua vị trí đầu
            if (timer.Paths.TryPeek(out UnityEngine.Vector2 firstPos) && currentPos == firstPos)
            {
                timer.Paths.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Tick thực hiện StoryBoard
        /// </summary>
        /// <param name="timer"></param>
        private void TickStoryBoard(OtherObjectStoryBoardTimer timer, long additionTick = 0, bool autoRemoveUnusedStoryBoard = true)
        {
            // Other object kinds retain their existing behavior. BOT quests require
            // authoritative movement, not only a path animation sent to the client.
            if (!(timer.Owner is KTBot questBot)) return;
            // Quarantine takes effect even before the quest worker unwinds.
            if (questBot.QuestExecutionDisabled) return;
            if (!this.objectTimers.TryGetValue(timer.Owner.RoleID, out var active) || !ReferenceEquals(active, timer))
                return;
            if (!timer.Owner.IsCanMove() || timer.Owner.IsDead())
            {
                this.Remove(timer.Owner);
                return;
            }

            long now = KTGlobal.GetCurrentTimeMilis();
            float elapsed = (now - timer.LastTick + additionTick) / 1000f;
            timer.LastTick = now;
            if (elapsed <= 0) return;

            // A newly blocked waypoint invalidates the route instead of being skipped.
            if (timer.Paths.TryPeek(out var nextPoint) &&
                !GameManager.MapMgr.GetGameMap(timer.Owner.CurrentMapCode).CanMove(new Point(nextPoint.x, nextPoint.y)))
            {
                this.Remove(timer.Owner);
                return;
            }
            bool finished = KTStoryBoardLogic.StepMove(timer.Owner, timer.Paths, elapsed, timer.Action);
            // Refresh the client before it reaches its window end. The server queue never stops.
            if (!finished && timer.ClientPathFloor > 0 &&
                timer.Paths.Count <= timer.ClientPathFloor + ClientPathLookAhead)
                this.SendBotPathWindow(timer);
            GameMap map = GameManager.MapMgr.GetGameMap(timer.Owner.CurrentMapCode);
            var position = new UnityEngine.Vector2((float)timer.Owner.CurrentPos.X, (float)timer.Owner.CurrentPos.Y);
            var gridPosition = KTGlobal.WorldPositionToGridPosition(map, position);
            if (gridPosition != timer.LastGridPos)
            {
                MapGrid grid = GameManager.MapGridMgr.GetMapGrid(timer.Owner.CurrentMapCode);
                if (grid == null || !grid.MoveObject(-1, -1, (int)position.x, (int)position.y, timer.Owner))
                {
                    this.Remove(timer.Owner);
                    return;
                }
                timer.Owner.CurrentGrid = new Point(gridPosition.x, gridPosition.y);
                timer.LastGridPos = gridPosition;
            }
            if (finished && autoRemoveUnusedStoryBoard)
                this.Remove(timer.Owner);
        }
        /// <summary>
        /// Hoàn thành StoryBoard
        /// </summary>
        /// <param name="timer"></param>
        private void FinishStoryBoard(OtherObjectStoryBoardTimer timer)
        {
            /// Nếu không có StoryBoard cũ
            if (!this.HasStoryBoard(timer.Owner))
            {
                timer.Owner.m_eDoing = Entities.KE_NPC_DOING.do_stand;
            }
        }
        #endregion
    }
}
