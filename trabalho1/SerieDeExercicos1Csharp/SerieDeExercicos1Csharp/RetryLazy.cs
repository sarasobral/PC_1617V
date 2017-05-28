using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerieDeExercicos1Csharp {
    public class RetryLazy<T> where T : class {

        private readonly object myLock = new object();
        public Func<T> provider;
        private int maxRetries;

        public RetryLazy(Func<T> provider, int maxRetries) {
            this.maxRetries = maxRetries;
            this.provider = provider;
        }

        private T value;
        private bool someoneIsTryingToSetValue = false;
        private LinkedList<int> queue = new LinkedList<int>();

        public T Value
        {// throws InvalidOperationException, ThreadInterruptedException
            get {
                lock (myLock) {
                    // (a)caso o valor já tenha sido calculado, retorna esse valor;
                    if (value != null) return value;
                    if (maxRetries == 0) throw new InvalidOperationException();
                    /*(c)caso já existe outra thread a realizar esse cálculo, espera até que o valor esteja
                    calculado ou o número de tentativas seja excedido;*/
                    if (someoneIsTryingToSetValue) {
                        LinkedListNode<int> node = queue.AddLast(0);
                        do {
                            try {
                                Monitor.Wait(myLock);
                            }
                            catch (ThreadInterruptedException) {
                                /*(d)lança ThreadInterruptedException se a espera da thread for interrompida.*/
                                queue.Remove(node);
                                PulseAll();
                                throw;
                            }
                            /*(b) se o número de tentativas ainda não tiver sido excedido e existirem threads em espera, deve 
                            ser seleccionada a mais antiga para a retentativa do cálculo através da função provider*/
                        } while (node != queue.First && someoneIsTryingToSetValue);
                        queue.Remove(node);
                        if (value != null) {
                            PulseAll();
                            return value;
                        }
                        /*(c) quando o número de tentativas é excedido, todas as threads à espera na propriedade 
                         Value , ou que chamem essa propriedade no futuro, devem retornar lançando
                         excepção InvalidOperationException .*/
                        if (maxRetries == 0) {
                            PulseAll();
                            throw new InvalidOperationException();
                        }
                    }
                    someoneIsTryingToSetValue = true;
                    T res;
                    /*(b) caso o valor ainda não tenha sido calculado, e o número máximo de tentativas 
                    (especificado com maxRetries ) não tenha sido excedido, inicia esse cálculo chamando 
                    provider na própria thread invocante e retorna o valor resultante; */
                    try
                    {
                        res = provider();
                    }
                    catch (Exception) {
                        /*(a) a chamada a Value nessa thread deve resultar no lançamento dessa excepção; */
                        maxRetries--;
                        TryingIsOver();
                        throw;
                    }
                    value = res;
                    TryingIsOver();
                    return value;
                }
            }
        }

        private void TryingIsOver() {
            someoneIsTryingToSetValue = false;
            PulseAll();
        }
        private void PulseAll() {
            if (queue.Count > 0)
                Monitor.PulseAll(myLock);
        }
    }
}