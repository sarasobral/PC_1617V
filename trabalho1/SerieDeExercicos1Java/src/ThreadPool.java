import java.util.LinkedList;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class ThreadPool<T> {

    private final int maxThreads;
    private final int keepAlive;
    private final ReentrantLock myLock;
    private Condition shutdownCondition;

    private LinkedList<Item<T>> waitingItems = new LinkedList<>();
    private LinkedList<WorkThread> threadList = new LinkedList<WorkThread>();
    private int waitingThreads = 0;
    private Condition waitingThreadsCondition;
    private boolean shutdown = false;

    public ThreadPool(int maxThreads, int keepAlive, ReentrantLock lock, Condition shutdownCondition) {
        this.maxThreads = maxThreads;
        this.keepAlive = keepAlive;
        this.myLock = lock;
        this.waitingThreadsCondition = lock.newCondition();
        this.shutdownCondition = shutdownCondition;
    }
/*
A gestão, pelo sincronizador, das worker threads deve obedecer aos
seguintes critérios: (a) se o número total de worker threads for inferior ao limite máximo especificado, é criada
uma nova worker thread sempre que for submetida uma função para execução e não existir nenhuma worker
thread disponível; (b) as worker threads deverão terminar após decorrerem mais do que keepAliveTime
nanosegundos sem que sejam mobilizadas para executar uma função; (c) o número de worker threads
existentes no pool em cada momento depende da atividade deste e pode variar entre zero e maxPoolSize.
*/
    public void resolveItem(Item<T> item) {
        // adicionar o item à lista de elementos a resolver
        waitingItems.addLast(item);
        // existem threads à espera de trabalho
        if (waitingThreads > 0) waitingThreadsCondition.signal();
        // o número total de worker threads for inferior ao limite máximo especificado, é criada uma nova worker thread
        else if (threadList.size() < maxThreads) {
            WorkThread thread = new WorkThread();
            threadList.add(thread);
            thread.start();
        }
    }

    public void cancelItem(Item<T> item) {
        waitingItems.remove(item);
    }

    public void shutdown() {
        this.shutdown = true;
        if (waitingThreads > 0) {
            waitingThreadsCondition.signalAll();
        }
    }

    public boolean isFinished() {
        // já não existem itens a resolver nem worker threads
        return waitingItems.size() == 0 && threadList.size() == 0;
    }

    private class WorkThread extends Thread {
        @Override
        public void run() {
            myLock.lock();
            try {
                worker:
                do {
                    long nanos = keepAlive;
                    while (waitingItems.size() > 0) {
                        Item<T> item = waitingItems.removeFirst();
                        item.resolving();
                        myLock.unlock();
                        T result = null;
                        try { // execute the function outside of the lock
                            result = item.function().call();
                        } catch (Exception e) {
                            item.complete(e);
                        }
                        if (result != null) item.complete(result);
                        myLock.lock();
                        item.condition().signal();
                    }
                    do {
                        // tentar sinalizar a condição de shutdown
                        if (shutdown) {
                            this.submitFinish();
                            break worker;
                        }
                        // já não existem itens
                        try {
                            waitingThreads++;
                            nanos = waitingThreadsCondition.awaitNanos(nanos);
                        } catch (InterruptedException e) {
                            waitingThreads--;
                            interrupt();
                        }
                        waitingThreads--;
                        if (waitingItems.size() > 0) break;
                        if (nanos <= 0L) break worker;
                    } while (true);
                } while (true);
                threadList.remove(this); // remove me before exit
                if (shutdown) this.submitFinish();
            } finally {
                myLock.unlock();
            }
        }

        private void submitFinish() {
            if (threadList.size() == 0) {
                shutdownCondition.signal();
            }
        }
    }
}