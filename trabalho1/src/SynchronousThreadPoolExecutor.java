import java.util.concurrent.Callable;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;
/*
O número máximo de w orker threads ( m axPoolSize ) e o tempo máximo que uma w orker thread pode estar
inativa antes de terminar ( k eepAliveTime ) são passados com argumentos para o construtor da classe
SynchronousThreadPoolExecutor . A gestão, pelo sincronizador, das w orker threads deve obedecer aos
seguintes critérios: (a) se o número total de w orker threads for inferior ao limite máximo especificado, é criada
uma nova w orker thread sempre que for submetida uma função para execução e não existir nenhuma w orker
thread disponível; (b) as w orker threads deverão terminar após decorrerem mais do que k eepAliveTime
nanosegundos sem que sejam mobilizadas para executar uma função; (c) o número de w orker threads
existentes no p ool em cada momento depende da atividade deste e pode variar entre zero e m axPoolSize .
As t hreads que pretendem executar funções através do t hread pool executor invocam o método e xecute ,
especificando a função a executar através do parâmetro t oCall . Este método bloqueia sempre a t hread
normalmente, devolvendo a instância do tipo T devolvida pela função, ou; (b) excepcionalmente, lançando a
mesma excepção que foi lançada aquando da chamada à função. Até ao momento em que a t hread dedicada
considerar uma função para execução, é possível interromper a execução do método e xecute; contudo, se a
interrupção ocorrer depois da função ser aceite para execução, o método e xecute deve ser processado
normalmente, sendo a interrupção memorizada de forma a que possa vir a ser lançada pela t hread mais tarde.
A chamada ao método s hutdown coloca o executor em modo s hutdown . Neste modo, todas as chamadas ao
método e xecute deverão lançar a excepção I llegalStateException . Contudo, todas as submissões para
execução feitas antes da chamada ao método s hutdown devem ser processadas normalmente. O método
shutdown deverá bloquear a t hread invocante até que sejam executados todos os itens de trabalho aceites
pelo executor e que tenham terminado todas as w orker threads ativas.
A implementação do sincronizador deve optimizar o número de comutações de t hread que ocorrem nas várias
circunstâncias.
*/
public class SynchronousThreadPoolExecutor<T> {

    private ReentrantLock _lock = new ReentrantLock();

    private ThreadPool<T> _threadPool;
    private boolean shutdown = false;
    private Condition _shutdownCondition = _lock.newCondition();

    public SynchronousThreadPoolExecutor(int maxPoolSize, int keepAliveTime) {
        this._threadPool = new ThreadPool<T>(maxPoolSize, keepAliveTime, _lock, _shutdownCondition);
    }

    public T execute(Callable<T> toCall) throws Exception {
        _lock.lock();

        try {
            if (shutdown)
                throw new IllegalStateException();

            Condition condition = _lock.newCondition();

            Item<T> item = new Item<T>(toCall, condition);
            _threadPool.resolveItem(item);

            do {
                try {
                    condition.await();
                } catch (InterruptedException e) {

                    if (item.canBeCanceled()) {
                        _threadPool.cancelItem(item);
                    }

                    throw e;
                }

                if (item.isCompleted()) {
                    return item.result();
                }

            } while (true);

        } finally {
            _lock.unlock();
        }
    }

    public void shutdown() {
        _lock.lock();
        try {
            shutdown = true;

            _threadPool.shutdown();

            if (_threadPool.isFinished()) // can be already finished
                return;

            do {
                try {
                    _shutdownCondition.await();
                } catch (InterruptedException e) {
                    // ignore interrupt?!
                }

                if (_threadPool.isFinished())
                    return;
            } while (true);

        } finally {
            _lock.unlock();
        }
    }
}
