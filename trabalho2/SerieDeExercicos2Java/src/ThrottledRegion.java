/***
 *
 *  ISEL, LEIC, Concurrent Programming, Verão 2016/17
 *
 *	Carlos Martins
 *
 *  Codigo anexo ao exercício 2 da SE#2
 *
 ***/
import java.util.LinkedList;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class ThrottledRegion {
     private static class Item {
        public final Condition condition;
        public boolean done = false;

        public Item(Condition condition) {
            this.condition = condition;
        }
    }

    private class ThrottledRegionForKey {
        private final AtomicInteger insideCount = new AtomicInteger(0);
        // observado e alterado fora do lock
        private volatile int waitingCount = 0;
        private final Lock myLock = new ReentrantLock();
        // observado e alterado dentro do lock
        private final LinkedList<Item> waitingQueue = new LinkedList<>();

        public boolean tryEnter() throws InterruptedException {
            //nao há em espera e é possivel entrar
            if (waitingCount == 0 && tryAcquire()) {
                return true;
            }

            myLock.lock();
            try {
                // se a espera estiver cheia desiste
                if (waitingCount == _maxWaiting)
                    return false;
                // a partir daqui passa a estar em espera
                waitingCount += 1;
                // verificar se já é possivel entrar
                if (waitingCount == 1 && tryAcquire()) {
                    waitingCount -= 1;
                    return true;
                }
                // adiciono em espera
                Item item = new Item(myLock.newCondition());
                waitingQueue.addLast(item);
                long timeout = waitTimeout;
                do {
                    try {
                        timeout = item.condition.awaitNanos(timeout);
                    } catch (InterruptedException e) {
                        // ocorreu exceção, desito da espera
                        waitingCount -= 1;
                        waitingQueue.remove(item);
                        if (item.done) {
                            Thread.currentThread().interrupt();
                            return true;
                        }
                        throw e;
                    }

                    if (item.done) {
                        return true;
                    }

                    if (timeout <= 0) {
                        waitingCount -= 1;
                        waitingQueue.remove(item);
                        return false;
                    }
                } while (true);
            } finally {
                myLock.unlock();
            }
        }

        public void leave() {
            boolean alreadyDecremented = false;
            //nao há ninguem em espera
            if (waitingCount == 0) {
                // reitro
                insideCount.decrementAndGet();
                //ainda nao há ninguem À espera, retorno
                if (waitingCount == 0) {
                    return;
                }
                alreadyDecremented = true;
            }
            myLock.lock();
            try {
                // nao existe thread em espera
                Item first = waitingQueue.peekFirst();
                if (first == null) {
                    insideCount.decrementAndGet();
                    return;
                }
                // nao é possivel adquirir, retorno
                if (alreadyDecremented && !tryAcquire()) return;
                first.done = true;
                waitingCount -= 1;
                waitingQueue.remove(first);
                first.condition.signal();
            } finally {
                myLock.unlock();
            }
        }

        private boolean tryAcquire() {
            //inc o contador inside se houver espaço
            do {
                int observed = insideCount.get();
                if (observed >= maxInside) return false;
                if (insideCount.compareAndSet(observed, observed + 1)) return true;
            } while (true);
        }
    }

    private final int maxInside;
    private final int _maxWaiting;
    private final int waitTimeout;
    private final ConcurrentMap<Integer, ThrottledRegionForKey> keyToRegion = new ConcurrentHashMap<>();

    public ThrottledRegion(int maxInside, int maxWaiting, int waitTimeout) {
        this.maxInside = maxInside;
        this._maxWaiting = maxWaiting;
        this.waitTimeout = waitTimeout;
    }

    public boolean tryEnter(int key) throws InterruptedException {
        return keyToRegion.computeIfAbsent(key, k -> new ThrottledRegionForKey()).tryEnter();
    }

    public void leave(int key) {
        keyToRegion.get(key).leave();
    }
}
