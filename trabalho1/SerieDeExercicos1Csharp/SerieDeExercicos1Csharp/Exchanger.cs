using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SerieDeExercicos1Csharp {
    class Mail<T> {
        public T firstMessage;
        public T secondMessage;
        public bool completed;
    }
    public class Exchanger<T> where T : class {
        private Mail<T> mail;
        private bool someoneIsWaiting = false;
        private readonly object myLock = new object();

        public T Exchange(T mine, int timeout) { // throws ThreadInterruptedException
            lock (myLock) {
                if (someoneIsWaiting) {
                    this.mail.secondMessage = mine;
                    this.mail.completed = true;
                    someoneIsWaiting = false;
                    Monitor.PulseAll(myLock);
                    return this.mail.firstMessage;
                }
                if (timeout == 0)
                    throw new TimeoutException();

                int lastTime = (timeout != Timeout.Infinite) ? Environment.TickCount : 0;
                mail = new Mail<T>();
                mail.firstMessage = mine;
                mail.secondMessage = default(T);
                someoneIsWaiting = true;
                do {
                    try {
                        Monitor.Wait(myLock, timeout);
                    }
                    /* (c) a espera seja interrompida, terminado o método com o lançamento de ThreadInterruptedException. */
                    catch (ThreadInterruptedException) {
                        if (mail.completed) 
                            return mail.secondMessage;
                        else {
                            someoneIsWaiting = false;
                            // vale a pena retiro a minha mensagem? visto q a proxima a entrar vai fazer default
                            throw;
                        }
                    }
                    /* (a) outra thread invoque o método Exchange , devolvendo o método a mensagem oferecida pela outra thread; */
                    if (mail.completed) 
                        return mail.secondMessage;
                    /* (b) expire o limite do tempo de espera especificado, situação em que o método devolve null , ou; */
                    if (SyncUtils.AdjustTimeout(ref lastTime, ref timeout) == 0) {
                        someoneIsWaiting = false;
                        throw new TimeoutException();
                    }
                } while (true);
            }
        }
    }
}