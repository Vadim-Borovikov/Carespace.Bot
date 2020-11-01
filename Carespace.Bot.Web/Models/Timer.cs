using System;
using System.Threading.Tasks;
using System.Timers;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Timer : IDisposable
    {
        public Timer() { _timer = new System.Timers.Timer(); }

        public void Stop() { _timer.Stop(); }
        public void Dispose() { _timer.Dispose(); }

        public void DoOnce(DateTime at, Func<Task> func)
        {
            TimeSpan after = at - DateTime.Now;
            Do(after, false, func);
        }

        public void DoWeekly(Func<Task> func) => Do(TimeSpan.FromDays(7), true, func);

        private void Do(TimeSpan interval, bool autoReset, Func<Task> func)
        {
            _timer.Stop();

            _timer.Interval = interval.TotalMilliseconds;
            _timer.AutoReset = autoReset;
            SetHandlerTo(func);

            _timer.Start();
        }

        private void SetHandlerTo(Func<Task> func)
        {
            if (_handler != null)
            {
                _timer.Elapsed -= _handler;
            }
            _handler = CreateHandlerFor(func);
            _timer.Elapsed += _handler;
        }

        private static ElapsedEventHandler CreateHandlerFor(Func<Task> func)
        {
            return (sender, e) => {
                try
                {
                    func().Wait();
                }
                catch (Exception ex)
                {
                    Utils.LogException(ex);
                    throw;
                }
            };
        }

        private readonly System.Timers.Timer _timer;
        private ElapsedEventHandler _handler;
    }
}
