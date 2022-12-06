using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Analyzers.Extensions;

public static class DebugExtensions
{
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static readonly ConcurrentQueue<(object source, string message)> LogQueue = new();
    private static readonly object LogLocker = new();

    /// <summary>
    ///     Can be used to test analyzers.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Log(this object analyzer, string message)
    {
        LogQueue.Enqueue((analyzer, message));
        Task.Run(() =>
        {
            while (!LogQueue.IsEmpty)
            {
                if (!LogQueue.TryDequeue(out (object source, string message) pair))
                {
                    continue;
                }

                lock (LogLocker)
                {
                    File.AppendAllText(Path.Combine(DesktopPath, $"{pair.source.GetType().Name}.log"), pair.message + Environment.NewLine);
                }
            }
        });
    }
}
