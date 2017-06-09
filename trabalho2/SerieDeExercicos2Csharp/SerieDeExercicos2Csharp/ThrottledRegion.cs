using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerieDeExercicos2Csharp {
    internal class ThrottledRegionForKey {
        private readonly int maxWaiting;
        private readonly int waitTimeout;
        private readonly int maxInside;
        // observado e alterado fora do lock
        private volatile int insideCount = 0;
        // observado fora do lock e alterado dentro do lock
        private volatile int waitingCount = 0;
        private readonly object myLock = new object();
        // observado e alterado dentro do lock
        private readonly LinkedList<bool> waitingQueue = new LinkedList<bool>();

        public ThrottledRegionForKey(int maxInside, int maxWaiting, int waitTimeout)
        {
            this.maxInside = maxInside;
            this.maxWaiting = maxWaiting;
            this.waitTimeout = waitTimeout;
        }

        public bool TryEnter()
        {
            //nao há em espera e é possivel entrar
            if (waitingCount == 0 && TryAcquire()) return true;
            lock (myLock)
            {
                // se a espera estiver cheia desiste
                if (waitingCount == maxWaiting) return false;
                // a partir daqui passa a estar em espera
                waitingCount += 1;
                // verificar se já é possivel entrar
                if (waitingCount == 1 && TryAcquire())
                {
                    waitingCount -= 1;
                    return true;
                }
                // adiciono em espera
                LinkedListNode<bool> node = waitingQueue.AddLast(false);
                int timeout = waitTimeout;
                int lastTime = (timeout != Timeout.Infinite) ? Environment.TickCount : 0;
                do
                {
                    try
                    {
                        SyncUtils.Wait(myLock, node, timeout);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        // ocorreu exceção, desito da espera
                        waitingCount -= 1;
                        waitingQueue.Remove(node);
                        if (node.Value)
                        {
                            Thread.CurrentThread.Interrupt();
                            return true;
                        }
                        throw;
                    }
                    if (node.Value) return true;
                    if (SyncUtils.AdjustTimeout(ref lastTime, ref timeout) == 0)
                    {
                        waitingCount -= 1;
                        waitingQueue.Remove(node);
                        return false;
                    }
                } while (true);
            }
        }

        public void Leave()
        {
            bool alreadyDecremented = false;
            //nao há ninguem em espera
            if (waitingCount == 0)
            {
                // reitro
                Interlocked.Decrement(ref insideCount);
                //ainda nao há ninguem À espera, retorno
                if (waitingCount == 0) return;
                alreadyDecremented = true;
            }
            lock (myLock)
            {
                // nao existe thread em espera
                LinkedListNode<bool> first = waitingQueue.First;
                if (first == null)
                {
                    insideCount--;
                    return;
                }
                // nao é possivel adquirir, retorno
                if (alreadyDecremented && !TryAcquire()) return;
                first.Value = true;
                waitingCount -= 1;
                waitingQueue.Remove(first);
                SyncUtils.Notify(myLock, first);
            }
        }

        private bool TryAcquire()
        {
            //inc o contador inside se houver espaço
            do
            {
                int observed = insideCount;
                if (observed >= maxInside) return false;
                if (Interlocked.CompareExchange(ref insideCount, observed + 1, observed) == observed)  return true;
            } while (true);
        }

    }


    public class ThrottledRegion {

        private readonly int maxInside;
        private readonly int maxWaiting;
        private readonly int waitTimeout;
        private readonly ConcurrentDictionary<int, ThrottledRegionForKey> keyToRegion;

        public ThrottledRegion(int maxInside, int maxWaiting, int waitTimeout)
        {
            this.maxInside = maxInside;
            this.maxWaiting = maxWaiting;
            this.waitTimeout = waitTimeout;
            this.keyToRegion = new ConcurrentDictionary<int, ThrottledRegionForKey>();
        }

        public bool TryEnter(int key)
        {
            var region = keyToRegion.GetOrAdd(key,
                new ThrottledRegionForKey(this.maxInside, this.maxWaiting, this.waitTimeout));

            return region.TryEnter();
        }

        public void Leave(int key)
        {
            keyToRegion[key].Leave();
        }

    }
}
