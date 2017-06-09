import java.util.concurrent.Callable;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class SynchronousThreadPoolExecutor<T> {

    private ReentrantLock myLock = new ReentrantLock();
    private ThreadPool<T> threadPool;
    private boolean shutdown = false;
    private Condition shutdownCondition = myLock.newCondition();

    public SynchronousThreadPoolExecutor(int maxPoolSize, int keepAliveTime) {
        this.threadPool = new ThreadPool<T>(maxPoolSize, keepAliveTime, myLock, shutdownCondition);
    }

    public T execute(Callable<T> toCall) throws Exception {
        myLock.lock();
        try {
            if (shutdown) throw new IllegalStateException();
            Condition condition = myLock.newCondition();
            Item<T> item = new Item<T>(toCall, condition);
            threadPool.resolveItem(item);
            do {
                try {
                    condition.await();
                } catch (InterruptedException e) {
                    if (item.canBeCanceled()) threadPool.cancelItem(item);
                    throw e;
                }
                if (item.isCompleted()) return item.result();
            } while (true);
        } finally {
            myLock.unlock();
        }
    }

    public void shutdown() {
        myLock.lock();
        try {
            shutdown = true;
            threadPool.shutdown();
            do {
                if (threadPool.isFinished()) return;
                try {
                    shutdownCondition.await();
                } catch (InterruptedException e) { }
            } while (true);
        } finally {
            myLock.unlock();
        }
    }
}
