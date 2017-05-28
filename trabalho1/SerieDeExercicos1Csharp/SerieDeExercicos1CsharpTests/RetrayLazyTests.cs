using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using SerieDeExercicos1Csharp;
using System.Collections.Generic;

namespace SerieDeExercicos1CsharpTests {
    [TestClass]
    public class RetrayLazyTests {
        static String expected = "some value";
        Func<String> provider = () => expected;
        int maxRetries;

        [TestMethod]
        public void TestSucessellGet()
        {
            maxRetries = 3;
            RetryLazy<String> retryLazy = new RetryLazy<string>(provider, maxRetries);
            // o primeiro consegue calcular o valor
            String res = retryLazy.Value;
            Assert.AreEqual(expected, res);
            // a segunda encontra o valor do primeiro
            res = retryLazy.Value;
            Assert.AreEqual(expected, res);
        }

        [TestMethod]
        public void TestMaxRetries()
        {
            maxRetries = 0;
            RetryLazy<String> retryLazy = new RetryLazy<string>(provider, maxRetries);
            try {
                // nao existem mais tentativas disponiveis
                String res = retryLazy.Value;
            } catch (Exception e) {
                Assert.AreEqual(typeof(InvalidOperationException), e.GetType());
            }
        }
    }
}