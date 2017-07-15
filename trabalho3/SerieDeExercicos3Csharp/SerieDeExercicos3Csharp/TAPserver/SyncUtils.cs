using System;
using System.Threading;

namespace TAPserver {
    public class SyncUtils {
        public static int AdjustTimeout(ref int lastTime, ref int timeout)
        {
            if (timeout != Timeout.Infinite)
            {
                int now = Environment.TickCount;
                int elapsed = (now == lastTime) ? 1 : now - lastTime;
                if (elapsed >= timeout)
                {
                    timeout = 0;
                }
                else
                {
                    timeout -= elapsed;
                    lastTime = now;
                }
            }
            return timeout;
        }

        public static void Wait(object mlock, object condition)
        {
            Wait(mlock, condition, Timeout.Infinite);
        }

        public static void Wait(object mlock, object condition, int tmout)
        {
            if (mlock == condition)
            {
                Monitor.Wait(mlock, tmout);
                return;
            }

            Monitor.Enter(condition);
            Monitor.Exit(mlock);

            try
            {
                Monitor.Wait(condition, tmout);
            }
            finally
            {
                Monitor.Exit(condition);

                bool interrupted;
                EnterUninterruptibly(mlock, out interrupted);
                if (interrupted) throw new ThreadInterruptedException();
            }
        }

        public static void Notify(object mlock, object condition)
        {
            if (mlock == condition)
            {
                Monitor.Pulse(mlock);
                return;
            }

            bool interrupted;
            EnterUninterruptibly(condition, out interrupted);

            Monitor.Pulse(condition);
            Monitor.Exit(condition);

            if (interrupted)
                Thread.CurrentThread.Interrupt();
        }

        public static void Broadcast(object mlock, object condition)
        {
            if (mlock == condition)
            {
                Monitor.PulseAll(mlock);
                return;
            }

            bool interrupted;
            EnterUninterruptibly(condition, out interrupted);

            Monitor.PulseAll(condition);
            Monitor.Exit(condition);

            if (interrupted)
                Thread.CurrentThread.Interrupt();
        }

        private static void EnterUninterruptibly(object mlock, out bool interrupted)
        {
            interrupted = false;
            do
            {
                try
                {
                    Monitor.Enter(mlock);
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    interrupted = true;
                }
            } while (true);
        }

    }
}
