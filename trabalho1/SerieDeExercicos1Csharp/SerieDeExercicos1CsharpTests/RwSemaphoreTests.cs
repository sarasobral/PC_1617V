using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerieDeExercicos1Csharp;
using System.Threading;

namespace SerieDeExercicos1CsharpTests {
    [TestClass]
    public class RwSemaphoreTests {
        [TestMethod]
        public void TestSemaphoreReadersFirst()
        {
            RwSemaphore sem = new RwSemaphore();
            sem.DownRead();
            sem.DownRead();
            Assert.AreEqual(sem.getReaders(), 2);

            sem.UpRead();
            sem.UpRead();
            Assert.AreEqual(sem.getReaders(), 0);

            sem.DownWrite();
            sem.UpWrite();
            Assert.AreEqual(sem.getWriters(), 0);
        }

        [TestMethod]
        public void TestSemaphoreWriterssFirst()
        {
            RwSemaphore sem = new RwSemaphore();
            sem.DownWrite();
            sem.UpWrite();
            Assert.AreEqual(sem.getWriters(), 0);
            sem.DownRead();
            sem.DownRead();
            Assert.AreEqual(sem.getReaders(), 2);
            sem.UpRead();
            sem.UpRead();
        }
    }
}