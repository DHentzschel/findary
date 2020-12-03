using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Abstractions;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Process = System.Diagnostics.Process;

namespace Findary.Abstraction
{
    public class ProcessWrapper : IProcess
    {
        private readonly Process _object;

        public ProcessWrapper()
        {
            _object = new Process();
        }

        public void Dispose() => _object?.Dispose();

        public void BeginErrorReadLine() => _object?.BeginErrorReadLine();

        public void BeginOutputReadLine() => _object?.BeginOutputReadLine();

        public void CancelErrorRead() => _object?.CancelErrorRead();

        public void CancelOutputRead() => _object?.CancelOutputRead();

        public void Close() => _object?.Close();

        public bool CloseMainWindow() => _object?.CloseMainWindow() == true;

        public void Kill() => _object?.Kill();

        public void Refresh() => _object?.Refresh();

        public bool Start() => _object?.Start() == true;

        public void WaitForExit() => _object?.WaitForExit();

        public bool WaitForExit(int milliseconds) => _object?.WaitForExit(milliseconds) == true;

        public bool WaitForInputIdle() => _object?.WaitForInputIdle() == true;

        public bool WaitForInputIdle(int milliseconds) => _object?.WaitForInputIdle(milliseconds) == true;

        public int BasePriority => _object.BasePriority;

        public bool EnableRaisingEvents
        {
            get => _object.EnableRaisingEvents;
            set => _object.EnableRaisingEvents = value;
        }

        public int ExitCode => _object.ExitCode;

        public DateTime ExitTime => _object.ExitTime;

        public IntPtr Handle => _object.Handle;

        public int HandleCount => _object.HandleCount;

        public bool HasExited => _object.HasExited;

        public int Id => _object.Id;

        public string MachineName => _object.MachineName;

        public ProcessModule MainModule => _object.MainModule;

        public IntPtr MainWindowHandle => _object.MainWindowHandle;

        public string MainWindowTitle => _object.MainWindowTitle;

        public IntPtr MaxWorkingSet { get => _object.MaxWorkingSet; set => _object.MaxWorkingSet = value; }

        public IntPtr MinWorkingSet { get => _object.MinWorkingSet; set => _object.MinWorkingSet = value; }

        public ProcessModuleCollection Modules => _object.Modules;

        public int NonpagedSystemMemorySize => _object.NonpagedSystemMemorySize;

        public long NonpagedSystemMemorySize64 => _object.NonpagedSystemMemorySize64;

        public int PagedMemorySize => _object.PagedMemorySize;

        public long PagedMemorySize64 => _object.PagedMemorySize64;

        public int PagedSystemMemorySize => _object.PagedSystemMemorySize;

        public long PagedSystemMemorySize64 => _object.PagedSystemMemorySize64;

        public int PeakPagedMemorySize => _object.PeakPagedMemorySize;

        public long PeakPagedMemorySize64 => _object.PeakPagedMemorySize64;

        public int PeakVirtualMemorySize => _object.PeakVirtualMemorySize;

        public long PeakVirtualMemorySize64 => _object.PeakVirtualMemorySize64;

        public int PeakWorkingSet => _object.PeakWorkingSet;

        public long PeakWorkingSet64 => _object.PeakWorkingSet64;

        public bool PriorityBoostEnabled { get => _object.PriorityBoostEnabled; set => _object.PriorityBoostEnabled = value; }

        public ProcessPriorityClass PriorityClass { get => _object.PriorityClass; set => _object.PriorityClass = value; }

        public int PrivateMemorySize => _object.PrivateMemorySize;

        public long PrivateMemorySize64 => _object.PrivateMemorySize64;

        public TimeSpan PrivilegedProcessorTime => _object.PrivilegedProcessorTime;

        public string ProcessName => _object.ProcessName;

        public IntPtr ProcessorAffinity { get => _object.ProcessorAffinity; set => _object.ProcessorAffinity = value; }

        public bool Responding => _object.Responding;

        public SafeProcessHandle SafeHandle => _object.SafeHandle;

        public int SessionId => _object.SessionId;

        public StreamReader StandardError => _object.StandardError;

        public StreamWriter StandardInput => _object.StandardInput;

        public StreamReader StandardOutput => _object.StandardOutput;

        public ProcessStartInfo StartInfo
        {
            get => _object.StartInfo;
            set {
                try
                {
                    _object.StartInfo = value;
                }
                catch (Exception)
                {
                }
            }
        }

        public DateTime StartTime => _object.StartTime;

        public ISynchronizeInvoke SynchronizingObject { get => _object.SynchronizingObject; set => _object.SynchronizingObject = value; }

        public ProcessThreadCollection Threads => _object.Threads;

        public TimeSpan TotalProcessorTime => _object.TotalProcessorTime;

        public TimeSpan UserProcessorTime => _object.UserProcessorTime;

        public int VirtualMemorySize => _object.VirtualMemorySize;

        public long VirtualMemorySize64 => _object.VirtualMemorySize64;

        public int WorkingSet => _object.WorkingSet;

        public long WorkingSet64 => _object.WorkingSet64;

        public event DataReceivedEventHandler ErrorDataReceived;

        public event EventHandler Exited;

        public event DataReceivedEventHandler OutputDataReceived;
    }
}
