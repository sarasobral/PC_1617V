using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TAPserver {
    public class Logger {
        private readonly TextWriter writer;
        private readonly Message<string> _messages;
        private readonly int maxNumberOfMessages = 100;
        private readonly Thread worker;

        private volatile int _numRequests;
        private volatile bool stop;

        public Logger() : this(Console.Out) { }
        public Logger(string logfile) : this(new StreamWriter(new FileStream(logfile, FileMode.Append, FileAccess.Write))) { }
        public Logger(TextWriter awriter)
        {
            writer = awriter;
            _messages = new Message<string>(maxNumberOfMessages);
            worker = new Thread(() => doWork());
        }

        private void doWork()
        {
            DateTime startTime = DateTime.Now;
            writer.WriteLine();
            writer.WriteLine("::- LOG STARTED @ {0} -::", DateTime.Now);
            writer.WriteLine();

            while (!stop || _messages.HasMessages())
            {
                try
                {
                    LinkedList<string> msgs = _messages.Take();
                    foreach (string msg in msgs)
                    {
                        writer.WriteLine(string.Format("{0}: {1}", DateTime.Now, msg));
                    }
                }
                catch (ThreadInterruptedException) { }
            }

            long elapsed = DateTime.Now.Ticks - startTime.Ticks;
            writer.WriteLine();
            writer.WriteLine("Running for {0} second(s)", elapsed / 10000000L);
            writer.WriteLine("Number of request(s): {0}", _numRequests);
            writer.WriteLine();
            writer.WriteLine("::- LOG STOPPED @ {0} -::", DateTime.Now);
            writer.Close();
        }

        public void Start()
        {
            worker.Start();
        }

        public void LogMessage(string msg)
        {
            if (stop)
            {
                return;
            }
            _messages.Put(msg);
        }

        public void IncrementRequests()
        {
            Interlocked.Increment(ref _numRequests);
        }

        public void Stop()
        {
            stop = true;
            if (!_messages.HasMessages())
            {
                worker.Interrupt();
            }
            worker.Join();
        }
    }

}
