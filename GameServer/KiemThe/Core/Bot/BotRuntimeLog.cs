using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GameServer.Logic
{
    internal static class BotRuntimeLog
    {
        private static readonly BlockingCollection<string> messages = new BlockingCollection<string>(2048);
        private static int dropped;

        static BotRuntimeLog()
        {
            new Thread(() =>
            {
                foreach (string message in messages.GetConsumingEnumerable())
                {
                    try
                    {
                        int count = Interlocked.Exchange(ref dropped, 0);
                        global::Server.Tools.LogManager.WriteLog(global::Server.Tools.LogTypes.Robot,
                            (count == 0 ? "" : "[BotLog dropped=" + count + "] ") + message,
                            bConsole: false, pushToLoki: false);
                    }
                    catch { Interlocked.Increment(ref dropped); }
                }
            }) { IsBackground = true, Name = "BOT log writer" }.Start();
        }

        public static void Write(string message)
        {
            if (!messages.TryAdd(message)) Interlocked.Increment(ref dropped);
        }

        public static void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }
    }
}
