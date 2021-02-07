using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Carespace.Bot.Events
{
    internal sealed class Timer : IDisposable
    {
        public Timer() => _timer = new System.Timers.Timer();

        public void Stop() => _timer.Stop();
        public void Dispose() => _timer.Dispose();

        public void DoOnce(DateTime at, Func<Task> func, string funcName)
        {
            _at = at;
            _after = _at - Utils.Now();

            _funcName = funcName;

            Do(false, func);
        }

        public void DoWeekly(Func<Task> func, string funcName)
        {
            _after = TimeSpan.FromDays(7);
            _at = Utils.Now() + _after;

            _funcName = funcName;

            Do(true, func);
        }

        public override string ToString()
        {
            string result = $"{_at}: {_funcName}";
            if (_timer.AutoReset)
            {
                result += $" with repeat every {_after}";
            }
            return result;
        }

        private void Do(bool autoReset, Func<Task> func)
        {
            _timer.Stop();

            _timer.Interval = _after.TotalMilliseconds;
            _timer.AutoReset = autoReset;
            SetHandlerTo(func);

            _timer.Start();

            Logs[_at] = ToString();
            UpdateLog();
        }

        private void SetHandlerTo(Func<Task> func)
        {
            if (_handler != null)
            {
                _timer.Elapsed -= _handler;
            }
            _handler = CreateHandlerFor(func, _at);
            _timer.Elapsed += _handler;
        }

        private static ElapsedEventHandler CreateHandlerFor(Func<Task> func, DateTime at)
        {
            return (sender, e) => {
                try
                {
                    func().Wait();
                    Logs.Remove(at);
                    UpdateLog();
                }
                catch (Exception ex)
                {
                    Utils.LogException(ex);
                    throw;
                }
            };
        }

        private static void UpdateLog()
        {
            var sb = new StringBuilder();
            foreach (DateTime at in Logs.Keys.OrderBy(d => d))
            {
                sb.AppendLine(Logs[at]);
            }
            Utils.LogTimers(sb.ToString());
        }

        private static readonly Dictionary<DateTime, string> Logs = new Dictionary<DateTime, string>();

        private readonly System.Timers.Timer _timer;
        private ElapsedEventHandler _handler;
        private DateTime _at;
        private TimeSpan _after;
        private string _funcName;
    }
}
