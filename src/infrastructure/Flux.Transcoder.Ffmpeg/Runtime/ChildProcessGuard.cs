using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

/// <summary>
/// Гарантирует, что ffmpeg не переживёт воркер, даже если тот убит и никакой код выключения не выполнился.
/// <list type="bullet">
/// <item>Windows: воркер входит в Job Object с KILL_ON_JOB_CLOSE, дети наследуют job с момента создания.</item>
/// <item>Linux: запуск через setpriv --pdeathsig KILL. Сигнал приходит при смерти ПОТОКА-родителя,
/// поэтому все запуски идут с одного потока, живущего столько же, сколько процесс.</item>
/// </list>
/// </summary>
public sealed class ChildProcessGuard
{
    private static readonly string[] SetprivPaths = ["/usr/bin/setpriv", "/bin/setpriv"];

    private readonly string? _setpriv;
    private readonly BlockingCollection<Action>? _spawns;

    public ChildProcessGuard(ILogger<ChildProcessGuard> logger)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                JoinKillOnCloseJob();
            }
            catch (Win32Exception ex)
            {
                logger.LogError(ex, "Job Object не создан: при падении воркера ffmpeg останется работать");
            }

            return;
        }

        if (OperatingSystem.IsLinux())
            _setpriv = SetprivPaths.FirstOrDefault(File.Exists);

        if (_setpriv is null)
        {
            logger.LogError("setpriv (util-linux) не найден: при падении воркера ffmpeg останется работать");
            return;
        }

        _spawns = [];
        new Thread(() =>
        {
            foreach (var spawn in _spawns.GetConsumingEnumerable())
                spawn();
        })
        {
            IsBackground = true,
            Name = "ffmpeg-spawner"
        }.Start();
    }

    /// <summary>Запускает процесс, привязав его жизнь к жизни воркера.</summary>
    public bool Start(Process process)
    {
        if (_setpriv is null || _spawns is null)
            return process.Start();

        var info = process.StartInfo;
        string[] prefix = ["--pdeathsig", "KILL", "--", info.FileName];
        for (var i = 0; i < prefix.Length; i++)
            info.ArgumentList.Insert(i, prefix[i]);
        info.FileName = _setpriv;

        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _spawns.Add(() =>
        {
            try { started.SetResult(process.Start()); }
            catch (Exception ex) { started.SetException(ex); }
        });

        return started.Task.GetAwaiter().GetResult();
    }

    private const int JobObjectExtendedLimitInformation = 9;
    private const uint KillOnJobClose = 0x2000;

    private static void JoinKillOnCloseJob()
    {
        var job = CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
            throw new Win32Exception();

        var limits = new ExtendedLimits { Basic = { LimitFlags = KillOnJobClose } };
        if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<ExtendedLimits>())
            || !AssignProcessToJobObject(job, GetCurrentProcess()))
        {
            var error = new Win32Exception();
            CloseHandle(job);
            throw error;
        }

        // handle намеренно не закрывается: его закроет ОС при смерти воркера и этим завершит детей
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public IoCounters Io;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateJobObjectW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref ExtendedLimits info, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
}
