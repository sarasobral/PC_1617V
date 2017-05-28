using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerieDeExercicos1Csharp {
    public class RwSemaphore {
        // the request node used for enqueue shared lock requests
        private class RdReqNode {
            internal int waiters;
            internal bool done;
        }

        private readonly object mlock;          
        
        // the state of the read/write lock
        private int readers;                    // current number of readers
        private bool writing;               // true when writing

        // all waiting readers same queue node. We do not need a linked list at all
        private RdReqNode waitingReaders;

        /* Os métodos DownRead e DownWrite adquirem a posse do semáforo 
         * para leitura ou escrita, respectivamente.*/
        public void DownRead()
        { // throws ThreadInterruptedException
        }
        public void DownWrite()
        { // throws ThreadInterruptedException
        }

        /* Os métodos UpRead e UpWrite libertam o semáforo depois do 
         * mesmo ter sido adquirido para leitura ou escrita, respectivamente. */
        public void UpRead() 
        { // throws InvalidOperationException
        }
        public void UpWrite()
        { // throws InvalidOperationException
        }
        /* O método DowngradeWriter , que apenas pode ser invocado pelas 
         * threads que tenham adquirido o semáforo para escrita, liberta o 
         * acesso para escrita e, atomicamente, adquire acesso para 
         * leitura.*/
        public void DowngradeWriter()
        { // throws InvalidOperationException
        }
    }
    /*
Se o método UpRead for invocado por threads que não tenham previamente 
adquirido o semáforo para leitura, ou os métodos UpWrite e 
DowngradeWriter forem invocados por threads que não tenham previamente
adquirido o semáforo para escrita, deve ser lançada a excepção 
InvalidOperationException . O acesso para leitura deve ser concedido às 
threads leitoras que se encontrem no início da fila de espera (ou de 
imediato, se a fila de espera estiver vazia quando é invocado o método 
DownRead ) desde que o não tenha sido concedido acesso para escrita a 
outra thread; para ser concedido acesso para escrita à thread que se 
encontra à cabeça da fila (ou de imediato, se a fila estiver vazia quando 
é invocado o método DownWrite ), é necessário que nenhuma outra thread 
tenha adquirido acesso para escrita ou para leitura.
Para que o semáforo seja equitativo na atribuição dos dois tipos de 
acesso (leitura e escrita), deverá ser utilizada uma única fila de espera,
com disciplina FIFO, onde são inseridas por ordem de chegada as
solicitações de aquisição pendentes. A implementação deve suportar o 
cancelamento dos métodos bloqueantes quando são interrompidas as threads 
bloqueadas (lançando ThreadInterruptedException ) e deve optimizar o 
número de comutações de t hread que ocorrem nas várias circunstâncias.
*/
}
