using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Cloudtoid.Interprocess;
using Godot;
using Environment = System.Environment;

namespace Renderite.Godot.Source;

public class Bootstrapper
{
    public readonly string ShmPrefix;
    private readonly ISubscriber _bootstrapperIn;
    private readonly IPublisher _bootstrapperOut;

    private Process _resoniteProcess;
    private readonly Action _onExit;
    public bool ResoniteQuit { get; private set; }

    public Bootstrapper(Action onExit)
    {
        _onExit = onExit;

        const string safeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rnd = new Random();
        ShmPrefix = new string(Enumerable.Range(0, 16)
            .Select(_ => safeChars[rnd.Next(safeChars.Length)]).ToArray());

        _bootstrapperIn = new QueueFactory().CreateSubscriber(
            new QueueOptions($"{ShmPrefix}.bootstrapper_in", 8192L, deleteOnDispose: true)
        );
        _bootstrapperOut = new QueueFactory().CreatePublisher(
            new QueueOptions($"{ShmPrefix}.bootstrapper_out", 8192L, deleteOnDispose: true)
        );

        GD.Print("Bootstrapper queue created.");
    }

    public void LaunchResonite(string resonitePath, string dotnetExecutable, string resoniteArgs)
    {
        var dllPath = System.IO.Path.Combine(resonitePath, "Renderite.Host.dll");

        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetExecutable,
            Arguments = $"{dllPath} -shmprefix {ShmPrefix} {resoniteArgs}",
            WorkingDirectory = resonitePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _resoniteProcess = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        _resoniteProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) GD.Print($"[Resonite] {e.Data}");
        };
        _resoniteProcess.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) GD.PrintErr($"[Resonite] {e.Data}");
        };
        _resoniteProcess.Exited += (sender, e) =>
        {
            ResoniteQuit = true;
            GD.Print($"Resonite process quit with code {_resoniteProcess.ExitCode}.");
            _onExit?.Invoke();
        };

        GD.Print($"Starting Resonite with args {startInfo.Arguments}");
        _resoniteProcess.Start();
        _resoniteProcess.BeginOutputReadLine();
        _resoniteProcess.BeginErrorReadLine();
    }

    public bool TryGetQueueConnection(out string queueName, out long queueCapacity)
    {
        queueName = null;
        queueCapacity = 0;

        if (!_bootstrapperIn.TryDequeue(CancellationToken.None, out var result)) return false;
        var queueArgs = Encoding.UTF8.GetString(result.Span).Split(' ');
        queueName = queueArgs[1];
        queueCapacity = long.Parse(queueArgs[3]);

        _bootstrapperOut.TryEnqueue(Encoding.UTF8.GetBytes("RENDERITE_STARTED:" + Environment.ProcessId));
        StartClipboardHandlerThread();
        return true;
    }

    private void StartClipboardHandlerThread()
    {
        new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (!_bootstrapperIn.TryDequeue(CancellationToken.None, out var result))
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var cmd = Encoding.UTF8.GetString(result.Span);
                    if (cmd == "GETTEXT")
                    {
                        var text = DisplayServer.ClipboardHas() ? DisplayServer.ClipboardGet() : "";
                        _bootstrapperOut.TryEnqueue(Encoding.UTF8.GetBytes(text));
                    }
                    else if (cmd.StartsWith("SETTEXT "))
                    {
                        DisplayServer.ClipboardSet(cmd.Substring("SETTEXT ".Length));
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Clipboard handler exception: {ex.Message}");
                }
            }
        })
        {
            IsBackground = true,
            Name = "ClipboardHandlerThread"
        }.Start();
    }
}