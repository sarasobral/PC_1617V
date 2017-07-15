using System;
using System.Collections.Generic;
using System.Threading;

namespace APMserver {
    public class Message<T> where T : class {

        private readonly object obj = new object();
        private LinkedList<T> _messages = new LinkedList<T>();
        private Taker _taker;

        private readonly long _maxNumberOfMsgs;

        public Message(long maxNumberOfMsgs)
        {
            this._maxNumberOfMsgs = maxNumberOfMsgs;
        }

        public void Put(T msg)
        {
            lock (obj)
            {
                if (_messages.Count >= _maxNumberOfMsgs)
                    return;

                if (_taker != null)
                {
                    _taker.Messages.AddLast(msg);
                    if (!_taker._hasMessages)
                    {
                        _taker._hasMessages = true;
                        Monitor.Pulse(obj);
                    }
                    return;
                }
                _messages.AddLast(msg);
            }
        }

        public LinkedList<T> Take()
        {
            lock (obj)
            {
                if (_taker != null)
                    throw new InvalidOperationException();

                if (_messages.Count > 0)
                {
                    LinkedList<T> ret = _messages;
                    _messages = new LinkedList<T>();
                    return ret;
                }

                Taker t = new Taker();
                _taker = t;
                do
                {
                    try
                    {
                        Monitor.Wait(obj);
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (t._hasMessages)
                        {
                            Thread.CurrentThread.Interrupt();
                            return t.Messages;
                        }
                        _taker = null;
                        throw;
                    }

                } while (!t._hasMessages);
                _taker = null;
                return t.Messages;
            }
        }

        public bool HasMessages()
        {
            lock (obj)
            {
                return _messages.Count > 0;
            }
        }

        private class Taker {
            public readonly LinkedList<T> Messages = new LinkedList<T>();
            public bool _hasMessages;
        }
    }
}
