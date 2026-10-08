using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace GameServer.Logic
{
    // One owner thread for game mutations. Watchdog never touches map/quest state.
    internal sealed class BotWorkScheduler<T> where T : class
    {
        private sealed class Entry
        {
            public T Item;
            public int State; // 0 idle, 1 queued, 2 running
            public long Due, Started;
            public bool Removed, TimeoutReported;
            public string Snapshot;
        }
        private readonly object gate = new object();
        private readonly Dictionary<T, Entry> entries = new Dictionary<T, Entry>();
        private readonly BlockingCollection<Entry> queue;
        private readonly Action<T> tick, cleanup;
        private readonly Action<T, string> cancel;
        private readonly Func<T, string> describe;
        private readonly Func<bool> stopping;
        private readonly Action<string> log;
        private readonly int capacity, timeoutMs, intervalMs;
        private readonly Stopwatch clock = Stopwatch.StartNew();

        public BotWorkScheduler(Action<T> tick, Action<T> cleanup,
            Action<T, string> cancel, Func<T, string> describe,
            Action<string> log, Func<bool> stopping,
            int capacity = 512, int timeoutMs = 5000, int intervalMs = 1000)
        {
            this.tick = tick; this.cleanup = cleanup; this.cancel = cancel;
            this.describe = describe; this.log = log; this.stopping = stopping;
            this.capacity = capacity; this.timeoutMs = timeoutMs; this.intervalMs = intervalMs;
            queue = new BlockingCollection<Entry>(capacity);
            new Thread(Work) { IsBackground = true, Name = "BOT quest owner" }.Start();
            new Thread(Supervise) { IsBackground = true, Name = "BOT watchdog" }.Start();
        }

        public bool Add(T item)
        {
            lock (gate)
            {
                if (entries.ContainsKey(item)) return true;
                if (entries.Count >= capacity) return false;
                entries.Add(item, new Entry { Item = item,
                    Due = clock.ElapsedMilliseconds + entries.Count % intervalMs });
                return true;
            }
        }

        public void Remove(T item)
        {
            lock (gate)
                if (entries.TryGetValue(item, out Entry entry))
                { entry.Removed = true; entry.Due = 0; }
        }

        private void Supervise()
        {
            while (!stopping())
            {
                var expired = new List<Entry>();
                lock (gate)
                {
                    long now = clock.ElapsedMilliseconds;
                    foreach (Entry e in entries.Values)
                    {
                        if (e.State == 2 && !e.TimeoutReported && now - e.Started >= timeoutMs)
                        {
                            e.TimeoutReported = true;
                            e.Removed = true;
                            cancel(e.Item, "TickTimeout after " + timeoutMs + " ms; " + e.Snapshot);
                            expired.Add(e);
                        }
                        if (e.State == 0 && now >= e.Due)
                        {
                            e.State = 1;
                            if (!queue.TryAdd(e)) e.State = 0;
                        }
                    }
                }
                foreach (Entry e in expired)
                {
                    // Callback must only set flags/cancel a token, never call game managers.
                    log("[BotWatchdog] Cancellation requested; " + e.Snapshot);
                }
                Thread.Sleep(100);
            }
        }

        private void Work()
        {
            while (!stopping())
            {
                if (!queue.TryTake(out Entry e, 100)) continue;
                lock (gate)
                {
                    e.State = 2;
                    e.Started = clock.ElapsedMilliseconds;
                    e.TimeoutReported = false;
                    e.Snapshot = "Preparing BOT tick";
                }
                try
                {
                    string snapshot = describe(e.Item);
                    bool removed;
                    lock (gate) { e.Snapshot = snapshot; removed = e.Removed; }
                    if (!removed) tick(e.Item);
                }
                catch (Exception ex)
                {
                    cancel(e.Item, ex.ToString());
                    lock (gate) e.Removed = true;
                    log("[BotWorkFailed] " + e.Snapshot + " " + ex);
                }
                bool shouldRemove;
                lock (gate) shouldRemove = e.Removed;
                bool cleaned = false;
                if (shouldRemove)
                {
                    try
                    {
                        lock (gate) e.Snapshot = "Cleanup: " + e.Snapshot;
                        cleanup(e.Item);
                        cleaned = true;
                        log("[BotCleanupCompleted] " + e.Snapshot);
                    }
                    catch (Exception ex) { log("[BotCleanupFailed] " + e.Snapshot + " " + ex); }
                }
                lock (gate)
                {
                    if (cleaned) entries.Remove(e.Item);
                    else { e.State = 0; e.Due = clock.ElapsedMilliseconds + intervalMs; }
                }
            }
        }
    }
}
