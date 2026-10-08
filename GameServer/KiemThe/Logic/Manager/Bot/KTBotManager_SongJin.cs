using System;
using System.Windows;
using GameServer.Logic;

namespace GameServer.KiemThe.Logic.Manager
{
    public static partial class KTBotManager
    {
        // Called exclusively by the BOT owner worker. Does not create a player session.
        internal static void MoveSongJinBot(KTBot bot, int mapCode, Point position, int camp)
        {
            bot.CheckQuestCancellation("SongJinTransfer");
            using (new BotMutationLease(bot))
            {
                KTBot current;
                if (!bots.TryGetValue(bot.RoleID, out current) || !ReferenceEquals(current, bot))
                    throw new InvalidOperationException("BOT is no longer registered.");
                if (bot.CurrentCopyMapID != -1)
                    throw new InvalidOperationException("SongJin BOT admission requires a world map.");
                GameMap destination;
                if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out destination))
                    throw new InvalidOperationException("SongJin destination map is unavailable: " + mapCode);
                Point target = new Point(Math.Floor(position.X), Math.Floor(position.Y));
                Func<Point, bool> valid = p => p.X >= 0 && p.Y >= 0 && p.X < destination.MapWidth &&
                    p.Y < destination.MapHeight && destination.CanMove(p);
                if (!valid(target))
                {
                    bool found = false;
                    for (int radius = 20; radius <= 200 && !found; radius += 20)
                        for (int direction = 0; direction < 8 && !found; direction++)
                        {
                            double angle = direction * Math.PI / 4;
                            var candidate = new Point(Math.Floor(position.X + radius * Math.Cos(angle)),
                                Math.Floor(position.Y + radius * Math.Sin(angle)));
                            if (valid(candidate)) { target = candidate; found = true; }
                        }
                    if (!found) throw new InvalidOperationException("No walkable SongJin destination near " + position);
                }
                var sourceGrid = GameManager.MapGridMgr.GetMapGrid(bot.CurrentMapCode);
                var destinationGrid = GameManager.MapGridMgr.GetMapGrid(mapCode);
                if (sourceGrid == null || destinationGrid == null)
                    throw new InvalidOperationException("BOT map grid unavailable.");
                int oldMap = bot.CurrentMapCode, oldCamp = bot.Camp;
                Point oldPos = bot.CurrentPos, oldGrid = bot.CurrentGrid, oldTo = bot.ToPos, oldStart = bot.StartPos;
                KTOtherObjectStoryBoard.Instance.Remove(bot);
                bot.ResetAI();
                bool insertionAttempted = false;
                try
                {
                    sourceGrid.RemoveObject(bot);
                    try { NotifyNearClientsToRemoveSelf(bot); }
                    catch (Exception ex) { BotRuntimeLog.Write("[BotSongJinNotify] Bot=" + bot.RoleID + " Remove=" + ex.Message); }
                    bot.CheckQuestCancellation("SongJinTransferCommit");
                    bot.SetCurrentMapCode(mapCode);
                    bot.CurrentPos = target;
                    bot.CurrentGrid = new Point((int)target.X / destination.MapGridWidth, (int)target.Y / destination.MapGridHeight);
                    bot.StartPos = bot.ToPos = target;
                    bot.Camp = camp;
                    insertionAttempted = true;
                    if (!AddToMap(bot)) throw new InvalidOperationException("Destination rejected BOT.");
                }
                catch (Exception transferError)
                {
                    try
                    {
                        if (insertionAttempted) destinationGrid.RemoveObject(bot);
                        bot.SetCurrentMapCode(oldMap); bot.Camp = oldCamp;
                        bot.CurrentPos = oldPos; bot.CurrentGrid = oldGrid;
                        bot.ToPos = oldTo; bot.StartPos = oldStart;
                        sourceGrid.RemoveObject(bot);
                        if (!AddToMap(bot)) throw new InvalidOperationException("BOT transfer rollback failed.");
                        NotifyNearClientsToAddSelf(bot);
                    }
                    catch (Exception rollbackError)
                    {
                        bot.DisableQuestExecution("SongJin transfer and rollback failed.");
                        throw new AggregateException(transferError, rollbackError);
                    }
                    throw;
                }
                try { NotifyNearClientsToAddSelf(bot); }
                catch (Exception ex) { BotRuntimeLog.Write("[BotSongJinNotify] Bot=" + bot.RoleID + " Add=" + ex.Message); }
            }
        }
    }
}
