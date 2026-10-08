using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace GameServer.Logic
{
    internal static class BotPathSimplifier
    {
        // rawPath must already be a valid A* route. On budget exhaustion retain its suffix.
        public static List<Point> Simplify(byte[,] grid, int cellWidth, int cellHeight,
            Point start, IList<Point> rawPath, CancellationToken cancellation, int budgetMs = 25)
        {
            var watch = Stopwatch.StartNew();
            var result = new List<Point>();
            Point anchor = start;
            int index = 0;
            while (index < rawPath.Count)
            {
                cancellation.ThrowIfCancellationRequested();
                if (watch.ElapsedMilliseconds >= budgetMs)
                {
                    for (; index < rawPath.Count; index++) result.Add(rawPath[index]);
                    break;
                }
                int chosen = index;
                for (int candidate = rawPath.Count - 1; candidate > index; candidate--)
                {
                    if (watch.ElapsedMilliseconds >= budgetMs) break;
                    if (CanWalkStraight(grid, cellWidth, cellHeight, anchor, rawPath[candidate],
                        cancellation, () => watch.ElapsedMilliseconds >= budgetMs))
                    { chosen = candidate; break; }
                }
                anchor = rawPath[chosen];
                if (result.Count == 0 || result[result.Count - 1] != anchor) result.Add(anchor);
                index = chosen + 1;
            }
            return result;
        }

        // Traverse every crossed grid cell, including both side cells at a diagonal corner.
        internal static bool CanWalkStraight(byte[,] grid, int width, int height, Point from, Point to,
            CancellationToken cancellation, Func<bool> expired = null)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
            double x0 = from.X / width, y0 = from.Y / height;
            double x1 = to.X / width, y1 = to.Y / height;
            if (double.IsNaN(x0) || double.IsNaN(y0) || double.IsNaN(x1) || double.IsNaN(y1) ||
                double.IsInfinity(x0) || double.IsInfinity(y0) || double.IsInfinity(x1) || double.IsInfinity(y1)) return false;
            int x = (int)Math.Floor(x0), y = (int)Math.Floor(y0);
            int endX = (int)Math.Floor(x1), endY = (int)Math.Floor(y1);
            double dx = x1 - x0, dy = y1 - y0;
            int sx = Math.Sign(dx), sy = Math.Sign(dy);
            bool verticalBoundary = sx == 0 && x0 == Math.Floor(x0);
            bool horizontalBoundary = sy == 0 && y0 == Math.Floor(y0);
            Func<int, int, bool> free = (cx, cy) =>
                cx >= 0 && cy >= 0 && cx < grid.GetLength(0) && cy < grid.GetLength(1) && grid[cx, cy] == 1;
            Func<int, int, bool> clear = (cx, cy) => free(cx, cy) &&
                (!verticalBoundary || free(cx - 1, cy)) &&
                (!horizontalBoundary || free(cx, cy - 1)) &&
                (!(verticalBoundary && horizontalBoundary) || free(cx - 1, cy - 1));
            if (!clear(x, y) || !clear(endX, endY)) return false;
            double deltaX = sx == 0 ? double.PositiveInfinity : 1 / Math.Abs(dx);
            double deltaY = sy == 0 ? double.PositiveInfinity : 1 / Math.Abs(dy);
            double nextX = sx == 0 ? double.PositiveInfinity : ((sx > 0 ? x + 1 - x0 : x0 - x) / Math.Abs(dx));
            double nextY = sy == 0 ? double.PositiveInfinity : ((sy > 0 ? y + 1 - y0 : y0 - y) / Math.Abs(dy));
            int maxSteps = Math.Abs(endX - x) + Math.Abs(endY - y) + 2;
            for (int step = 0; step <= maxSteps; step++)
            {
                cancellation.ThrowIfCancellationRequested();
                if (expired != null && expired()) return false;
                if (x == endX && y == endY) return true;
                // Once an axis reaches its destination cell it need not cross another boundary.
                double tx = x == endX ? double.PositiveInfinity : nextX;
                double ty = y == endY ? double.PositiveInfinity : nextY;
                if (Math.Abs(tx - ty) < 1e-10)
                {
                    if (!clear(x + sx, y) || !clear(x, y + sy)) return false;
                    x += sx; y += sy; nextX += deltaX; nextY += deltaY;
                }
                else if (tx < ty) { x += sx; nextX += deltaX; }
                else { y += sy; nextY += deltaY; }
                if (!clear(x, y)) return false;
            }
            return false;
        }
    }
}
