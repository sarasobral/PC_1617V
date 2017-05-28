using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using SerieDeExercicos1Csharp;

namespace SerieDeExercicos1CsharpTests {
    [TestClass]
    public class ThrottledRegionTests {
        private readonly int key = 1;
        private int maxInside;
        private int maxWaiting;
        private int waitTimeout;

        // retornar true se a entrada foi feita com sucesso. 
        [TestMethod]
        public void TestSuccess()
        {
            maxInside = 2;
            maxWaiting = 2;
            waitTimeout = Timeout.Infinite; // para nunca sairem por timeout
            ThrottledRegion region = new ThrottledRegion(maxInside, maxWaiting, waitTimeout);
            Thread t1 = new Thread(() => { doWork(region, 1, true); });
            Thread t2 = new Thread(() => { doWork(region, 2, true); });
            Thread t3 = new Thread(() => { doWork(region, 3, true); });

            t1.Start(); // t1 entra
            t2.Start(); // t2 entra
            t1.Join();
            t2.Join();
            t3.Start(); 
            Thread.Sleep(1000); // t3 fica em espera
            region.Leave(key); // t3 entra
            t3.Join();
        }
        
        // retornar false se a entrada não tiver sucesso devido à ocorrência de timeout.
        [TestMethod]
        public void TestTimeout()
        {
            maxInside = 2;
            maxWaiting = 2;
            waitTimeout = 1000;
            ThrottledRegion region = new ThrottledRegion(maxInside, maxWaiting, waitTimeout);
            Thread t1 = new Thread(() => { doWork(region, 1, true); });
            Thread t2 = new Thread(() => { doWork(region, 2, true); });
            Thread t3 = new Thread(() => { doWork(region, 3, false); });

            t1.Start(); // t1 entra
            t2.Start(); // t2 entra
            t1.Join();
            t2.Join();
            t3.Start(); 
            Thread.Sleep(2000); // para t3 sair por timeout
            t3.Join();
        }

        // retornar false se foi excedido o número máximo de threads em espera 
        [TestMethod]
        public void TestMaxInsideFull()
        {
            maxInside = 1;
            maxWaiting = 1;
            waitTimeout = Timeout.Infinite;
            ThrottledRegion region = new ThrottledRegion(maxInside, maxWaiting, waitTimeout);
            Thread t1 = new Thread(() => { doWork(region, 1, true); }); 
            Thread t2 = new Thread(() => { doWork(region, 2, true); }); 
            Thread t3 = new Thread(() => { doWork(region, 3, false); });

            t1.Start(); // t1 entra
            t1.Join();
            t2.Start(); // t2 fica em espera
            Thread.Sleep(100); 
            t3.Start(); // sai pq nao há espaço
            t3.Join();
            region.Leave(key); // t2 entra
            t2.Join();
        }

        // throw ThreadInterruptedExceptio se a thread foi interrompida  
        [TestMethod]
        public void TestInterruptedEException()
        {
            maxInside = 1;
            maxWaiting = 1;
            waitTimeout = Timeout.Infinite;
            ThrottledRegion region = new ThrottledRegion(maxInside, maxWaiting, waitTimeout);
            Thread t1 = new Thread(() => { doWork(region, 1, true); });
            Thread t2 = new Thread(() => { doWork(region, 2, true); });
            Thread t3 = new Thread(() => { doWork(region, 3, false); });

            t1.Start(); // t1 entra
            t1.Join();
            t2.Start(); // t2 fica em espera
            Thread.Sleep(100);
            t2.Interrupt();
            t2.Join();
        }

        private void doWork(ThrottledRegion tregion, int threadId, bool expected)
        {
            try
            {
                bool res = tregion.TryEnter(key);
                Console.WriteLine("thread " + threadId + " returned " + res);
                if (expected) Assert.IsTrue(res);
                else Assert.IsFalse(res);
            } catch(ThreadInterruptedException e)
            {
                Console.WriteLine("thread " + threadId + " returned " +e.Message);
                Assert.Fail();
            }
        }

    }
}
