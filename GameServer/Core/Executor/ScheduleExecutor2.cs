using GameServer.Logic;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameServer.Core.Executor
{
    public class NormalScheduleTask : ScheduleTask
    {
        private TaskInternalLock _InternalLock = new TaskInternalLock();
        public TaskInternalLock InternalLock { get { return _InternalLock; } }

        private readonly EventHandler TimerCallProc;
        private readonly string Name;
        private readonly Action processing;

        public NormalScheduleTask(string name, EventHandler timerCallProc)
        {
            TimerCallProc = timerCallProc;
            Name = name;
        }

        public NormalScheduleTask(string v, Action processing)
        {
            Name = v;
            this.processing = processing;
        }

        public void run()
        {
            try
            {
                TimerCallProc?.Invoke(this, EventArgs.Empty);
                processing?.Invoke();
            }
            catch
            {
                // ignore
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ScheduleExecutor2
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        public static ScheduleExecutor2 Instance = new ScheduleExecutor2();

        private Dictionary<ScheduleTask, Timer> TimerDict = new Dictionary<ScheduleTask, Timer>();

        public void start()
        {
        }

        public void stop()
        {
            lock (this)
            {
                foreach (var t in TimerDict)
                {
                    t.Value.Dispose();
                }
                TimerDict.Clear();
            }
        }

        public void scheduleCancle(ScheduleTask task)
        {
            lock (this)
            {
                Timer timer;
                if (TimerDict.TryGetValue(task, out timer))
                {
                    timer.Dispose();
                    TimerDict.Remove(task);
                }
            }
        }

        public void scheduleCancle(string name)
        {
            lock (this)
            {
                var task = TimerDict.Keys.ToList().FirstOrDefault(t => t.ToString() == name);
                if (task is null)
                {
                    return;
                }

                if (!TimerDict.TryGetValue(task, out var timer)) 
                    return;
                timer.Dispose();
                TimerDict.Remove(task);
            }
        }

        /// <summary>
        /// 周期性任务
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="delay">延迟开始时间（毫秒）</param>
        /// <param name="periodic">间隔周期时间（毫秒）</param>
        /// <returns></returns>
        public void scheduleExecute(ScheduleTask task, int delay, int periodic)
        {
            if (periodic < 15 || periodic > 86400 * 1000)
            {
                throw new Exception("Incorrect scheduling interval periodic = " + periodic);
            }

            if (delay <= 0)
            {
                delay = periodic;
            }
            lock (this)
            {
                Timer timer;
                if (!TimerDict.TryGetValue(task, out timer))
                {
                    timer = new Timer(OnTimedEvent, task, Global.GetRandomNumber(delay / 2, delay * 3 / 2), periodic);
                    TimerDict.Add(task, timer);
                }
                else
                {
                    timer.Change(periodic, periodic);
                }
            }
        }

        private static void OnTimedEvent(object source)
        {
            ScheduleTask task = source as ScheduleTask;
            if (task.InternalLock.TryEnter())
            {
                bool logRunTime = false;
                long nowTicks = TimeUtil.CurrentTicksInexact;
                try
                {
                    task.run();
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteLog(LogTypes.Error, string.Format("{0}执行时异常,{1}", task.ToString(), ex.ToString()));
                }
                finally
                {
                    logRunTime = task.InternalLock.Leave();
                }

            }
        }
    }
}