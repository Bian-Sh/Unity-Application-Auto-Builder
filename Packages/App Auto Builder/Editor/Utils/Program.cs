using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace zFramework.AppBuilder.Utils
{
    internal class Program : IDisposable
    {
        private ProcessOutputReader _stdout;
        private ProcessOutputReader _stderr;
        private Stream _stdin;
        public Process _process;

        protected Program()
        {
            _process = new Process();
        }

        public Program(ProcessStartInfo si) : this()
        {
            _process.StartInfo = si;
        }

        public void Start()
        {
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;

            _process.Start();
            _stdout = new ProcessOutputReader(_process, _process.StandardOutput);
            _stderr = new ProcessOutputReader(_process, _process.StandardError);
            _stdin = _process.StandardInput.BaseStream;
        }

        public ProcessStartInfo GetProcessStartInfo()
        {
            return _process.StartInfo;
        }

        public string GetAllOutput()
        {
            var sb = new StringBuilder();
            sb.AppendLine("stdout:");
            foreach (var s in GetStandardOutput())
                sb.AppendLine(s);
            sb.AppendLine("stderr:");
            foreach (var s in GetErrorOutput())
                sb.AppendLine(s);
            return sb.ToString();
        }

        public bool HasExited
        {
            get
            {
                if (_process == null)
                    throw new InvalidOperationException("You cannot call HasExited before calling Start");
                try
                {
                    return _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }
        }

        public int ExitCode => _process.ExitCode;

        public int Id => _process.Id;

        public void Dispose()
        {
            Kill();
            _process.Dispose();
            _stdin?.Dispose();
            _stdout?.Dispose();
            _stderr?.Dispose();
        }

        public void Kill()
        {
            if (!HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
            }
        }

        public Stream GetStandardInput()
        {
            return _stdin;
        }

        public string[] GetStandardOutput()
        {
            return _stdout.GetOutput();
        }

        public string GetStandardOutputAsString()
        {
            var output = GetStandardOutput();
            return GetOutputAsString(output);
        }

        public string[] GetErrorOutput()
        {
            return _stderr.GetOutput();
        }

        public string GetErrorOutputAsString()
        {
            var output = GetErrorOutput();
            return GetOutputAsString(output);
        }

        private static string GetOutputAsString(string[] output)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in output)
                sb.AppendLine(t);
            return sb.ToString();
        }

        private int SleepTimeoutMiliseconds
        {
            get { return 10; }
        }

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

        public bool WaitForExit(int milliseconds)
        {
            // Case 1111601: Process.WaitForExit hangs on OSX platform
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var start = DateTime.Now;
                while (!_process.HasExited && (DateTime.Now - start).TotalMilliseconds < milliseconds)
                {
                    // Don't consume 100% of CPU while waiting for process to exit
                    Thread.Sleep(SleepTimeoutMiliseconds);
                }
                return _process.HasExited;
            }
            else
            {
                return _process.WaitForExit(milliseconds);
            }
        }
    }
}