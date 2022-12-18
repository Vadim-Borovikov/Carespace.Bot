using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;

namespace Carespace.Bot.Events;

internal sealed class Timer : IDisposable
{
    public Timer(Logger logger)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public void DoOnce(DateTimeFull at, Func<Task> func, string funcName)
    {
        _at = at;
        _after = _at - DateTimeFull.CreateUtcNow();

        _doPeriodically = false;
        _funcName = funcName;

        Invoker.DoAfterDelay(_ => DoAndLog(func, _at), _after, _logger, _cancellationTokenSource.Token);

        Logs[_at] = ToString();
        UpdateLog();
    }

    public void DoWeekly(Func<Task> func, string funcName)
    {
        _after = TimeSpan.FromDays(7);
        _at = DateTimeFull.CreateUtcNow() + _after;

        _doPeriodically = true;
        _funcName = funcName;

        Invoker.DoPeriodically(_ => DoUpdateAndLog(func), _after, false, _logger, _cancellationTokenSource.Token);

        Logs[_at] = ToString();
        UpdateLog();
    }

    public override string ToString()
    {
        string result = $"{_at}: {_funcName}";
        if (_doPeriodically)
        {
            result += $" with repeat every {_after}";
        }
        return result;
    }

    private async Task DoUpdateAndLog(Func<Task> func)
    {
        await func();

        Logs.Remove(_at);
        _at = DateTimeFull.CreateUtcNow() + _after;
        Logs[_at] = ToString();

        UpdateLog();
    }

    private static async Task DoAndLog(Func<Task> func, DateTimeFull at)
    {
        await func();
        Logs.Remove(at);
        UpdateLog();
    }

    private static void UpdateLog()
    {
        StringBuilder sb = new();
        foreach (DateTimeFull at in Logs.Keys.Order())
        {
            sb.AppendLine(Logs[at]);
        }
        LogTimers(sb.ToString());
    }

    private static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

    private const string TimersLogPath = "timers.txt";

    private static readonly Dictionary<DateTimeFull, string> Logs = new();

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Logger _logger;

    private DateTimeFull _at;
    private TimeSpan _after;
    private bool _doPeriodically;
    private string? _funcName;
}