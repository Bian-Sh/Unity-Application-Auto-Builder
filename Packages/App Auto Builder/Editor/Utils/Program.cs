using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace zFramework.AppBuilder.Utils
{
    internal class Program : IDisposable
    {
        internal event Action<string> OnStandardOutputReceived;
        internal event Action<string> OnStandardErrorReceived;
        private readonly SynchronizationContext context;
        public Process _process;

        protected Program()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                throw new InvalidOperationException("Program must be created on the main thread");
            }
            context = SynchronizationContext.Current;
            _process = new Process();
        }

        public Program(ProcessStartInfo si) : this()
        {
            _process.StartInfo = si;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;
            //Encoding  is GB2312
            _process.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding("GB2312");
            _process.StartInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding("GB2312");
        }

        public async Task StartAsync()
        {
            void RunProcess()
            {
                _process.Start();
                using var outputReader = new AsyncStreamReader(_process.StandardOutput);
                using var errorReader = new AsyncStreamReader(_process.StandardError);
                outputReader.OnOutputDataReceived += result => context.Post(_ => OnStandardOutputReceived?.Invoke(result), null);
                errorReader.OnOutputDataReceived += result => context.Post(_ => OnStandardErrorReceived?.Invoke(result), null);
                outputReader.Start();
                errorReader.Start();
                WaitForExit();
            }
            await Task.Run(RunProcess);
        }

        public void Dispose()
        {
            if (!_process.HasExited)
            {
                _process.Kill();
                WaitForExit();
            }
            _process.Dispose();
        }

        private int SleepTimeoutMiliseconds => 10;

        public int ExitCode => _process.ExitCode;

        public void WaitForExit()
        {
            // Case 1111601: Process.WaitForExit hangs on OSX platform
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                while (!_process.HasExited)
                {
                    // Don't consume 100% of CPU while waiting for process to exit
                    Thread.Sleep(SleepTimeoutMiliseconds);
                }
            }
            else
            {
                _process.WaitForExit();
            }
        }
    }

}