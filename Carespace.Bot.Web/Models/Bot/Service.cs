using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Bot
{
    internal sealed class Service : IHostedService, IDisposable
    {
        public Service(IBot bot)
        {
            _bot = bot;

            var saveManager = new BotSaveManager(_bot.Config.SavePath);

            if (string.IsNullOrWhiteSpace(_bot.Config.GoogleCredentialsJson))
            {
                _bot.Config.GoogleCredentialsJson = JsonConvert.SerializeObject(_bot.Config.GoogleCredentials);
            }
            _googleDriveDataManager = new DataManager(_bot.Config.GoogleCredentialsJson);
            _googleSheetsDataManager =
                new GoogleSheetsManager.DataManager(_bot.Config.GoogleCredentialsJson, _bot.Config.GoogleSheetId);

            var eventsChatId = new ChatId($"@{_bot.Config.EventsChannelLogin}");
            var discussChatId = new ChatId($"@{_bot.Config.DiscussGroupLogin}");
            _eventManager = new Events.Manager(_googleSheetsDataManager, saveManager, _bot.Config.GoogleRange,
                _bot.Config.EventsFormUri, _bot.Client, eventsChatId, _bot.Config.LogsChatId, discussChatId,
                _bot.Config.Host, _bot.Calendars);

            _bot.InitCommands(_googleDriveDataManager, _eventManager);

            _weeklyUpdateTimer = new Timer();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bot.Client.SetWebhookAsync(_bot.Config.Url, cancellationToken: cancellationToken);
            await DoAndSchedule(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync,
                nameof(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _weeklyUpdateTimer.Stop();
            await _bot.Client.DeleteWebhookAsync(cancellationToken);
        }

        public void Dispose()
        {
            _weeklyUpdateTimer?.Dispose();
            _googleDriveDataManager?.Dispose();
            _googleSheetsDataManager?.Dispose();
            _eventManager?.Dispose();
        }

        private async Task DoAndSchedule(Func<Task> func, string funcName)
        {
            await func();
            DateTime nextUpdateAt = Utils.GetMonday().AddDays(7) + _bot.Config.EventsUpdateAt.TimeOfDay;
            _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeekly(func, funcName), funcName);
        }

        private async Task DoAndScheduleWeekly(Func<Task> func, string funcName)
        {
            await func();
            _weeklyUpdateTimer.DoWeekly(func, funcName);
        }

        private readonly IBot _bot;
        private readonly DataManager _googleDriveDataManager;
        private readonly GoogleSheetsManager.DataManager _googleSheetsDataManager;
        private readonly Events.Manager _eventManager;
        private readonly Timer _weeklyUpdateTimer;
    }
}