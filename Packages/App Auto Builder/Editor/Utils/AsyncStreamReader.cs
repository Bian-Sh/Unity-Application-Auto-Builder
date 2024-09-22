using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace zFramework.AppBuilder.Utils
{
    internal class AsyncStreamReader : IDisposable
    {
        private readonly StreamReader reader;
        private readonly CancellationTokenSource cts = new();
        public event Action<string> OnOutputDataReceived;

        public AsyncStreamReader(StreamReader reader)
        {
            this.reader = reader;
        }

        public void Start()
        {
            Task.Run(ReceivingDataFunc);
        }

        private async Task ReceivingDataFunc()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    OnOutputDataReceived?.Invoke(line);
                }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            reader.Dispose();
        }
    }

}