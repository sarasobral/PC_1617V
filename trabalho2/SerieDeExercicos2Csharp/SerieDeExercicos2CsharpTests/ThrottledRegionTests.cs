using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerieDeExercicos2Csharp;
using System.Threading;

namespace SerieDeExercicos2CsharpTests {
    [TestClass]
    public class ThrottledRegionTests {
        public void TryEnterSuccess(ThrottledRegion region, int key)
        {
            Assert.IsTrue(region.TryEnter(key));
        }

        public void TryEnterFail(ThrottledRegion region, int key)
        {
            Assert.IsFalse(region.TryEnter(key));
        }

        [TestMethod]
        public void TestSucess()
        {
            var region = new ThrottledRegion(2, 2, Timeout.Infinite);
            var key = 1;
            Thread t1 = new Thread(() => { TryEnterSuccess(region, key); }); // enter
            Thread t2 = new Thread(() => { TryEnterSuccess(region, key); }); // enter
            Thread t3 = new Thread(() => { TryEnterSuccess(region, key); }); // wait

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            t3.Start();
            Thread.Sleep(1000); // make sure t3 is waiting
            region.Leave(key); // t3 can enter
            t3.Join();
        }

        [TestMethod]
        public void TestTimeout()
        {
            var region = new ThrottledRegion(2, 2, 1000);
            var key = 1;
            Thread t1 = new Thread(() => { TryEnterSuccess(region, key); }); // enter
            Thread t2 = new Thread(() => { TryEnterSuccess(region, key); }); // enter
            Thread t3 = new Thread(() => { TryEnterFail(region, key); }); // wait

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            t3.Start();
            Thread.Sleep(3000); // make sure t3 gets the timeout
            t3.Join();
        }
        
        [TestMethod]
        public void TestMaxInsideInTwoRegions()
        {
            var region = new ThrottledRegion(1, 1, Timeout.Infinite);
            var key = 1;
            var key2 = 2;
            Thread t1 = new Thread(() => { TryEnterSuccess(region, key); }); // enter region1
            Thread t2 = new Thread(() => { TryEnterSuccess(region, key); }); // wait region1
            Thread t3 = new Thread(() => { TryEnterFail(region, key); }); // Fail region1

            Thread t4 = new Thread(() => { TryEnterSuccess(region, key2); }); // enter region2
            Thread t5 = new Thread(() => { TryEnterSuccess(region, key2); }); // wait region2
            Thread t6 = new Thread(() => { TryEnterFail(region, key2); }); // fail region2

            t1.Start();
            t1.Join();
            t2.Start(); // will wait
            Thread.Sleep(100); //give some time to run before the t3
            t3.Start();
            t3.Join();
            region.Leave(key);
            t2.Join();

            t4.Start();
            t4.Join();
            t5.Start(); // will wait
            Thread.Sleep(100); //give some time to run before the t6
            t6.Start();
            t6.Join();
            region.Leave(key2);
            t5.Join();
        }
    }
}
