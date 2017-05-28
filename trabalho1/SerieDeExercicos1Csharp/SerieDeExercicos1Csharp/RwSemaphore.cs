using System;
using System.Collections.Generic;
using System.Threading;
using Utils;

namespace SerieDeExercicos1Csharp {
    public class RwSemaphore {
        private readonly object mlock = new object();
        private int readers = 0; // numero currente de leitores
        private bool writing = false; // indica se um escritor está a escrever
        private class WaitingReaders {
            internal int waiters;
            internal bool done; // se true todos os leitores estão em condição de ter acesso
        }
        private WaitingReaders waitingReaders = null; // leitores em espera (os leitores entram todos no semáforo)
        private readonly LinkedList<bool> waitingWriters = new LinkedList<bool>(); // escritores em espera (entra um de cada vez no semáforo)

        // DownRead adquirem a posse do semáforo para leitura
        public void DownRead() { 
            lock (mlock) {
                // não existem escritores à espera e não há nenhum esritor a escrever,
                // o leitor ganha acesso independentemente dos outros leitores
                if (waitingWriters.Count == 0 && !writing) {
                    readers++;
                    return;
                }
                // o leitor fica em espera
                WaitingReaders rdnode;
                if ((rdnode = waitingReaders) == null)
                    waitingReaders = rdnode = new WaitingReaders();
                rdnode.waiters++;
                do {
                    try {
                        MonitorEx.Wait(mlock, mlock);
                    }
                    catch (ThreadInterruptedException) {
                        // acesso foi garantido, o leitor retira-se
                        if (rdnode.done) {
                            Thread.CurrentThread.Interrupt();
                            return;
                        }
                        // o leitor é removido da espera
                        if (--rdnode.waiters == 0)
                            waitingReaders = null;
                        throw;
                    }
                } while (!rdnode.done);
            }
        }
        
        // DownWrite adquirem a posse do semáforo para escrita
        public void DownWrite() {
            lock (mlock) {
                // nao existem leitores em espera e neinguem está a escrever e a fila de escritores está vazia,
                // o escritor tem acesso ao semáforo
                if (readers == 0 && !writing && waitingWriters.Count == 0) {
                    writing = true;
                    return;
                }
                // o escritor fica em espera
                LinkedListNode<bool> wrnode = waitingWriters.AddLast(false);
                do {
                    try {
                        MonitorEx.Wait(mlock, wrnode);
                    }
                    catch (ThreadInterruptedException) {
                        // acesso foi garantido, o escritor retira-se
                        if (wrnode.Value) {
                            Thread.CurrentThread.Interrupt();
                            return;
                        }
                        // o escritor é removido da espera
                        waitingWriters.Remove(wrnode);
                        // garantir o acesso dos leitores ao semáforo
                        if (!writing && waitingWriters.Count == 0 && waitingReaders != null)
                            if (waitingReaders != null && waitingReaders.waiters > 0) {
                                readers += waitingReaders.waiters;
                                waitingReaders.done = true; // dar acesso aos leitores
                                waitingReaders = null; // retirar os leitores de espera
                                MonitorEx.PulseAll(mlock, mlock); // notificar todos os leitores
                            }
                        throw;
                    }
                } while (!wrnode.Value);
            }
        }

        // UpRead liberta o semáforo depois do mesmo ter sido adquirido para leitura
        public void UpRead() {
            lock (mlock) {
                // decrementar o número de leitores
                readers--;
                // último leitor e esxistem escritores eme espera
                if (readers == 0 && waitingWriters.Count > 0)
                    GrantAccessToOneWritter();
            }
        }
        // UpWrite liberta o semáforo depois do mesmo ter sido adquirido para escrita
        public void UpWrite() { 
            lock (mlock) {
                // cede acesso a todos os leitores, caso não existam é garantido acesso a um escritor
                DowngradeWriter();
            }
        }

        /* apenas pode ser invocado pelas threads que tenham
         * adquirido o semáforo para escrita, liberta o acesso para 
         * escrita e, atomicamente, adquire acesso para leitura.*/
        public void DowngradeWriter() {
            if (waitingReaders != null && waitingReaders.waiters > 0) {
                readers += waitingReaders.waiters; 
                waitingReaders.done = true; // dar acesso aos leitores
                waitingReaders = null; // retirar os leitores de espera
                MonitorEx.PulseAll(mlock, mlock); // notificar todos os leitores
            }
            else
                GrantAccessToOneWritter();
        }

        private void GrantAccessToOneWritter() {
            if (waitingWriters.Count > 0) {
                LinkedListNode<bool> writer = waitingWriters.First;     // get first waiting writer
                waitingWriters.RemoveFirst();       // remove the writer from the wait queue
                writing = true;                     // set exclusive lock as taken
                writer.Value = true;                // mark exclusive lock request as granted;
                MonitorEx.PulseAll(mlock, writer);  // notify the specific writer
            }
        }
    }
}