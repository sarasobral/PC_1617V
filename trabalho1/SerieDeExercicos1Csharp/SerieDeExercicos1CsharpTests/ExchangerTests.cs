using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using SerieDeExercicos1Csharp;

namespace SerieDeExercicos1CsharpTests {
    [TestClass]
    public class ExchangerTests {
        private Exchanger<String> exchanger = new Exchanger<string>();
        private readonly int timeout = 2000;
        private readonly String message1 = "vim de t1";
        private readonly String message2 = "vim de t2";
        private readonly String message3 = "vim de t3";

        // (a) outra thread invoque o método Exchange , devolvendo o método a mensagem oferecida pela outra thread ; */
        [TestMethod]
        public void TestSucessfullSwitch()
        {
            Thread t1 = new Thread(() => { doWork(message1, message2, timeout); });
            Thread t2 = new Thread(() => { doWork(message2, message1, timeout); });
            t1.Start(); // t1 dá
            t2.Start(); // t2 receber e dá
            // t1 recebe
            t1.Join(); 
            t2.Join();
        }

        // (b) expire o limite do tempo de espera especificado, situação em que o método devolve null , ou;
        [TestMethod]
        public void TestTimeout()
        {
            Thread t1 = new Thread(() => { doWork(message1, message2, timeout); });
            Thread t2 = new Thread(() => { doWork(message2, message1, timeout); });
            Thread t3 = new Thread(() => {
                try
                {
                    Thread.Sleep(1000);
                    doWork(message3, null, timeout);
                    Assert.Fail();
                }
                catch (TimeoutException) { }
            });
            t1.Start(); // t1 dá
            t2.Start(); // t2 receber e dá
            t3.Start(); // t3 pode receber e dá
            // t1 recebe
            // t3 sai por timeout
            t1.Join();
            t2.Join();
            t3.Join();
        }

        // (c) a espera seja interrompida, terminado o método com o lançamento de ThreadInterruptedException. 
        [TestMethod]
        public void TestInterruptedEException()
        {
            Thread t1 = new Thread(() => { doWork(message1, message2, timeout); });
            Thread t2 = new Thread(() => { doWork(message2, message1, timeout); });
            Thread t3 = new Thread(() => { doWork(message3, null, timeout); });
            t1.Start(); // t1 dá
            t2.Start(); // t2 receber e dá
            t3.Start(); // t3 pode receber e dá
            // t1 recebe
            Thread.Sleep(1000); // t3 espera um pouco
            t3.Interrupt(); // t3 é interrumpida e sai por exceção
            t1.Join();
            t2.Join();
            t3.Join();
        }

        private void doWork(String actual, String expected, int timeout)
        {
            try
            {
                actual = exchanger.Exchange(actual, timeout);
                Assert.Equals(actual, expected);
            }
            catch (ThreadInterruptedException e)
            {
                Console.WriteLine(e.Message);
                Assert.Fail();

            }
        }
        
    }
}
