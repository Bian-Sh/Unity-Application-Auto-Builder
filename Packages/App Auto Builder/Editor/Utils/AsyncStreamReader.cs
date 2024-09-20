using System;
using System.IO;
using System.Threading;

namespace zFramework.AppBuilder.Utils
{
    internal class AsyncStreamReader : IDisposable 
    {
        private readonly Thread thread;
        internal event Action<string> OnOutputDataReceived;
        private readonly StreamReader reader;

        public AsyncStreamReader(StreamReader reader)
        {
            this.reader = reader;
            thread = new Thread(ReceivingDataFunc);
        }

        private void ReceivingDataFunc()
        {
            while ( reader?.BaseStream != null)
            {
                lock (reader)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        OnOutputDataReceived?.Invoke(line);
                    }
                }
                Thread.Sleep(10);
            }
        }

        public void Start()
        {
            thread.Start();
        }

        public void Dispose()
        {
            lock (reader)
            {
                reader.Dispose();
            }
            thread.Join();
        }
    }

}