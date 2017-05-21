using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using SerieDeExercicos1Csharp;
using System.Collections.Generic;

namespace SerieDeExercicos1CsharpTests {
    [TestClass]
    public class RetrayLazyTests         {

        [TestMethod]
        public void TestSucessellGet()
        {
            String expected = "value";
            Func<String> provider = () => expected;
            RetryLazy<String> retryLazy = new RetryLazy<string>(provider, 1);
            Assert.AreEqual(expected, retryLazy.Value);
        }

        [TestMethod]
        public void TestMaxRetries()
        {
            int retries = 3, doneRetries = 0;
            String message = "Can't retry";
            Func<String> provider = () => { throw new Exception(message); };
            RetryLazy<String> retryLazy = new RetryLazy<string>(provider, retries);

            for (int i = 0; i < retries; i++)
            {
                try {
                    String res = retryLazy.Value;
                } catch (Exception e) {
                    Assert.AreEqual(e.Message, message);
                    ++doneRetries;
                }
            }
            Assert.AreEqual(retries, doneRetries);

            try {
                String res = retryLazy.Value;
                Assert.Fail("InvalidOperationException");
            }  catch (Exception e) {
                Assert.AreEqual(typeof(InvalidOperationException), e.GetType());
            }
        }

        [TestMethod]
        public void TestWithDifferentThreads()
        {
            int  threads = 50, count = 0, retries = 10, doneRetries = 0;
            RetryLazy<String> retrieLazy = new RetryLazy<string>(() => {
                Thread.Sleep(200);
                if (count++ < retries) // first tries should fail
                    throw new Exception("I failed.");
                return "done";
            }, retries + 1);

            List<Thread> thList = new List<Thread>(threads);
            for (int i = 0; i < threads; i++)
                thList.Add(new Thread(() => {
                    try {
                        Assert.AreEqual("done", retrieLazy.Value);
                    } catch (Exception e) {
                        ++doneRetries;
                    }
                }));

            thList.ForEach((t) => t.Start());
            thList.ForEach((t) => t.Join());

            Assert.AreEqual(retries, doneRetries);
        }

    }
}
