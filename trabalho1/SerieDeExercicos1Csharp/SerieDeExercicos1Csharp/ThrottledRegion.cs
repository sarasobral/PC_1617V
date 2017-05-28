using System;
using System.Collections.Generic;
using System.Threading;
using Utils;

namespace SerieDeExercicos1Csharp {
    public class ThrottledRegion {
        private readonly int maxInside;
        private readonly int maxWaiting;
        private readonly int waitTimeout;
        private readonly object myLock = new object();
        // <int key, Region value>
        private Dictionary<int, Region> regions = new Dictionary<int, Region>(); 

        public ThrottledRegion(int maxInside, int maxWaiting, int waitTimeout) {
            this.maxInside = maxInside;
            this.maxWaiting = maxWaiting;
            this.waitTimeout = waitTimeout;
        }

        public bool TryEnter(int key) { // throws ThreadInterruptedException
            lock (myLock) {
                Region region = null;
                // procurar a região que esteja associada à key
                if (regions.TryGetValue(key, out region)) 
                    return region.Enter();
                // a região ainda não existe, vou criar uma
                region = new Region(maxInside, maxWaiting, waitTimeout, myLock);
                regions.Add(key, region);
                return region.Enter();
            }
        }

        public void Leave(int key) {
            lock (myLock) {
                Region region = null;
                // procurar a região que esteja associada à key
                if (regions.TryGetValue(key, out region)) { 
                    region.Leave();
                   /* A política de entrada nas zonas protegidas é FIFO, por isso
                      vou buscar o 1º em espera da região ssociada à key para o acordar */
                   LinkedListNode<bool> first = region.getFirstOnWaitingQueue();
                    if (first != null) {
                        first.Value = true; // com true pode avançar
                        Monitor.Pulse(myLock);
                    }
                }
            }
        }

        private class Region {
            private int maxInside;
            private int maxWaiting;
            private int waitTimeout;
            private readonly object myLock = new object();
            private LinkedList<bool> waitingQueue = new LinkedList<bool>();

            public Region(int maxInside, int maxWaiting, int timeout, Object myLock) {
                this.maxInside = maxInside;
                this.maxWaiting = maxWaiting;
                this.waitTimeout = timeout;
                this.myLock = myLock;
            }

            public bool Enter()  {
                // ainda há espaço
                if (!IsRegionFull()) { 
                    if (maxInside > 0) { 
                        this.maxInside--;
                        return true; // a entrada foi feita com sucesso
                    }
                }
                /* não há espaço, vou ver se posso ficar em espera na waitingQueue
                   não podem estar mais do que maxWaiting threads à espera de entrar na 
                   zona protegida pela mesma chave */
                if (IsWaitingQueueFull()) return false;
                else {
                    LinkedListNode<bool> node = waitingQueue.AddLast(false);
                    int timeout = waitTimeout;
                    int lastTime = (timeout != Timeout.Infinite) ? Environment.TickCount : 0;
                    do {
                        try  {
                            Monitor.Wait(myLock, timeout);
                        }
                        // interrompido o bloqueio da thread
                        catch (ThreadInterruptedException e) {
                            waitingQueue.Remove(node);
                            throw;
                        }
                        // verificar se já fui sinalizado
                        if (node.Value) { 
                            waitingQueue.Remove(node);
                            if (maxInside > 0) {
                                this.maxInside--;
                                return true;
                            }
                        }
                        /* verificar se ocorreu timeout, uma thread não poderá esperar mais 
                           do que waitTimeout milésimos de segundo para entrar na zona 
                           protegida */
                        if (SyncUtils.AdjustTimeout(ref lastTime, ref timeout) == 0) {
                            waitingQueue.Remove(node);
                            return false;
                        }
                    } while (true);
                }
            }

            public void Leave() {
                // não podem estar mais do que maxInside threads dentro da zona protegida pela mesma chave; 
                this.maxInside++;
            }
            public LinkedListNode<bool> getFirstOnWaitingQueue() { return waitingQueue.First; }
            public bool IsRegionFull() { return maxInside == 0; }
            public bool IsWaitingQueueFull() { return this.waitingQueue.Count >= maxWaiting; }
        }
    }
}
