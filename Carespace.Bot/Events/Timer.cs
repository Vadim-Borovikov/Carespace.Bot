using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Events;

internal sealed class Timer : IDisposable
{
    public Timer(TimeManager timeManager)
    {
        _timeManager = timeManager;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Stop() => _cancellationTokenSource.Cancel();

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
    }

    public void DoOnce(DateTime at, Func<Task> func, string funcName)
    {
        _at = at;
        _after = _at - _timeManager.Now();

        _doPeriodically = false;
        _funcName = funcName;

        Invoker.DoAfterDelay(_ => DoAndLog(func, _at), _after, _cancellationTokenSource.Token);

        Logs[_at] = ToString();
        UpdateLog();
    }

    public void DoWeekly(Func<Task> func, string funcName)
    {
        _after = TimeSpan.FromDays(7);
        _at = _timeManager.Now() + _after;

        _doPeriodically = true;
        _funcName = funcName;

        Invoker.DoPeriodically(_ => DoUpdateAndLog(func), _after, false, _cancellationTokenSource.Token);

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
        _at = _timeManager.Now() + _after;
        Logs[_at] = ToString();

        UpdateLog();
    }

    private static async Task DoAndLog(Func<Task> func, DateTime at)
    {
        await func();
        Logs.Remove(at);
        UpdateLog();
    }

    private static void UpdateLog()
    {
        StringBuilder sb = new();
        foreach (DateTime at in Logs.Keys.OrderBy(d => d))
        {
            sb.AppendLine(Logs[at]);
        }
        Utils.LogTimers(sb.ToString());
    }

    private static readonly Dictionary<DateTime, string> Logs = new();

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly TimeManager _timeManager;

    private DateTime _at;
    private TimeSpan _after;
    private bool _doPeriodically;
    private string? _funcName;
}
