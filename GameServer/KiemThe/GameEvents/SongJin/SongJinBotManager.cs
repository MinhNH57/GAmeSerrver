using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GameServer.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.KiemThe.Logic.Manager.Battle;

namespace GameServer.KiemThe.GameEvents.SongJin
{
    // Metadata only. Movement and quest changes always run on the BOT owner worker.
    internal static class SongJinBotManager
    {
        internal sealed class Admission
        {
            public Battle_SongJin Battle;
            public long Round;
            public string Name;
            public int Level, Map, SongHumans, JinHumans;
            public Point SongPosition, JinPosition;
            public BattelStatus State;
        }

        internal sealed class Ticket
        {
            public Admission Admission;
            public KTBot Bot;
            public int Camp;
            public bool Active;

            // Vị trí riêng đã giữ chỗ cho BOT.
            public Point Position;
        }

        private static readonly object gate = new object();
        private static readonly Dictionary<Battle_SongJin, Admission> battles = new Dictionary<Battle_SongJin, Admission>();
        private static readonly Dictionary<int, Ticket> tickets = new Dictionary<int, Ticket>();
        private static long nextRound;

        internal static void Publish(Battle_SongJin battle, BattleConfig config, bool guildEvent)
        {
            lock (gate)
            {
                Admission previous;
                battles.TryGetValue(battle, out previous);
                var state = battle.GetBattelState();
                long round = previous == null ? 0 : previous.Round;
                if (state == BattelStatus.STATUS_PREPARE &&
                    (previous == null || previous.State != BattelStatus.STATUS_PREPARE))
                    round = ++nextRound;
                var song = config.Region == null ? null : config.Region.FirstOrDefault(x => x.RegionCamp == 10);
                var jin = config.Region == null ? null : config.Region.FirstOrDefault(x => x.RegionCamp == 20);
                // Guild-only events have separate eligibility rules and are not Song/Jin admission.
                bool eligible = !guildEvent && song != null && jin != null &&
                    (config.DayOfMonth == null || config.DayOfMonth.Count == 0);
                battles[battle] = new Admission {
                    Battle = battle, Round = round, Name = config.BattleName,
                    Level = battle.BattleLevel, Map = config.MapID,
                    SongHumans = battle.SongCampRegister.Count, JinHumans = battle.JinCampRegister.Count,
                    SongPosition = song == null ? new Point() : new Point(song.PosX, song.PosY),
                    JinPosition = jin == null ? new Point() : new Point(jin.PosX, jin.PosY),
                    State = eligible ? state : BattelStatus.STATUS_NULL
                };
            }
        }

        private const int WaitingSpacing = 120;

        // Thử tối đa 4 vòng quanh điểm xuất hiện của phe.
        // Cần kiểm tra thực tế để các điểm vẫn nằm trong khu chờ.
        private const int WaitingRings = 4;

        // Chỉ gọi hàm này bên trong lock (gate).
        private static bool TryFindWaitingPosition(
            Admission admission,
            int camp,
            out Point position)
        {
            position = new Point();

            GameMap map;
            if (!GameManager.MapMgr.DictMaps.TryGetValue(
                admission.Map, out map))
            {
                return false;
            }

            Point origin = camp == 10
                ? admission.SongPosition
                : admission.JinPosition;

            // Lấy cả vị trí đang giữ chỗ, chưa chuyển map xong.
            // Không chỉ lấy BOT Active, tránh hai BOT nhận cùng điểm.
            var occupied = tickets.Values
                .Where(x => x.Admission.Map == admission.Map)
                .Select(x => x.Position)
                .ToArray();

            // Duyệt từ điểm giữa ra từng vòng vuông.
            for (int ring = 0; ring <= WaitingRings; ring++)
            {
                for (int row = -ring; row <= ring; row++)
                {
                    for (int column = -ring; column <= ring; column++)
                    {
                        // Chỉ xét viền vòng hiện tại.
                        if (Math.Max(Math.Abs(row), Math.Abs(column)) != ring)
                            continue;

                        Point candidate = new Point(
                            Math.Floor(origin.X) + column * WaitingSpacing,
                            Math.Floor(origin.Y) + row * WaitingSpacing
                        );

                        // Điểm phải nằm trong bản đồ.
                        if (candidate.X < 0 || candidate.Y < 0 ||
                            candidate.X >= map.MapWidth ||
                            candidate.Y >= map.MapHeight)
                        {
                            continue;
                        }

                        // Điểm phải đi được.
                        if (!map.CanMove(candidate))
                            continue;

                        bool tooClose = occupied.Any(other =>
                        {
                            double dx = other.X - candidate.X;
                            double dy = other.Y - candidate.Y;

                            return dx * dx + dy * dy
                                < WaitingSpacing * WaitingSpacing;
                        });

                        if (tooClose)
                            continue;

                        position = candidate;
                        return true;
                    }
                }
            }

            // Không tìm được chỗ thì chưa cho BOT vào.
            return false;
        }

        internal static Ticket TryReserve(KTBot bot)
        {
            lock (gate)
            {
                if (tickets.ContainsKey(bot.RoleID))
                    return null;

                int level = Battel_SonJin_Manager.GetBattleLevel(bot.m_Level);

                var admission = battles.Values.FirstOrDefault(x =>
                    x.Level == level &&
                    x.Round > 0 &&
                    x.State == BattelStatus.STATUS_PREPARE &&
                    x.Battle.GetBattelState() == BattelStatus.STATUS_PREPARE);

                if (admission == null)
                    return null;

                var members = tickets.Values.Where(x =>
                    ReferenceEquals(x.Admission.Battle, admission.Battle) &&
                    x.Admission.Round == admission.Round &&
                    !x.Bot.QuestExecutionDisabled
                ).ToArray();

                int song = admission.Battle.SongCampRegister.Count
                    + members.Count(x => x.Camp == 10);

                int jin = admission.Battle.JinCampRegister.Count
                    + members.Count(x => x.Camp == 20);

                int camp = song <= jin ? 10 : 20;

                Point waitingPosition;
                if (!TryFindWaitingPosition(
                    admission, camp, out waitingPosition))
                {
                    // Chưa có vị trí hợp lệ; tick sau sẽ thử lại.
                    return null;
                }

                var ticket = new Ticket
                {
                    Admission = admission,
                    Bot = bot,
                    Camp = camp,
                    Position = waitingPosition
                };

                // Giữ chỗ ngay trong lock để BOT khác không lấy lại.
                tickets.Add(bot.RoleID, ticket);

                return ticket;
            }
        }

        internal static BattelStatus GetState(Ticket ticket)
        {
            lock (gate)
            {
                Admission current;
                if (!battles.TryGetValue(ticket.Admission.Battle, out current) || current.Round != ticket.Admission.Round)
                    return BattelStatus.STATUS_NULL;
                return current.State == BattelStatus.STATUS_NULL ? current.State : current.Battle.GetBattelState();
            }
        }

        internal static bool Activate(Ticket ticket)
        {
            lock (gate)
            {
                Ticket registered;
                if (!tickets.TryGetValue(ticket.Bot.RoleID, out registered) || !ReferenceEquals(registered, ticket) ||
                    GetState(ticket) != BattelStatus.STATUS_PREPARE) return false;
                ticket.Active = true;
                return true;
            }
        }

        internal static void Release(Ticket ticket)
        {
            if (ticket == null) return;
            lock (gate)
            {
                Ticket current;
                if (tickets.TryGetValue(ticket.Bot.RoleID, out current) && ReferenceEquals(current, ticket))
                    tickets.Remove(ticket.Bot.RoleID);
            }
        }

        internal static int ActiveCount(Battle_SongJin battle, int camp)
        {
            lock (gate)
            {
                Admission current;
                if (!battles.TryGetValue(battle, out current)) return 0;
                return tickets.Values.Count(x => ReferenceEquals(x.Admission.Battle, battle) &&
                    x.Admission.Round == current.Round && x.Camp == camp && x.Active &&
                    !x.Bot.QuestExecutionDisabled && !x.Bot.IsDead() &&
                    x.Bot.CurrentMapCode == current.Map && x.Bot.CurrentCopyMapID == -1);
            }
        }
    }
}
