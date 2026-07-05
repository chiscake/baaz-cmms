using System.Diagnostics;

namespace BAAZ.CMMS.Core.Diagnostics;

/// <summary>Таймстемпы и длительности этапов для отладки зависаний (Output → Debug).</summary>
public static class PerfDebug
{
    public static string Timestamp() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static void Mark(string tag, string? detail = null)
    {
        var thread = Environment.CurrentManagedThreadId;
        var suffix = detail is null ? string.Empty : $" | {detail}";
        Debug.WriteLine($"[{Timestamp()}] [T{thread}] {tag}{suffix}");
    }

    public static PerfStep Step(string tag, string? detail = null)
    {
        Mark(tag, detail is null ? "START" : $"START {detail}");
        return new PerfStep(tag);
    }

    public readonly struct PerfStep : IDisposable
    {
        private readonly string _tag;
        private readonly Stopwatch _sw;

        public PerfStep(string tag)
        {
            _tag = tag;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            Mark(_tag, $"END +{_sw.ElapsedMilliseconds}ms");
        }

        public long ElapsedMilliseconds => _sw.ElapsedMilliseconds;
    }
}
