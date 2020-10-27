using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using GoogleDocumentsUnifier.Logic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Timer = System.Timers.Timer;

namespace Carespace.Bot.Web.Models.Services
{
    internal sealed class BotService : IBotService, IHostedService
    {
        public TelegramBotClient Client { get; }
        public IReadOnlyCollection<Command> Commands { get; }
        public IEnumerable<int> AdminIds => _config.AdminIds;

        public BotService(IOptions<BotConfiguration> options)
        {
            _config = options.Value;

            var saveManager = new BotSaveManager(_config.SavePath);

            Client = new TelegramBotClient(_config.Token);

            if (string.IsNullOrWhiteSpace(_config.GoogleCredentialsJson))
            {
                _config.GoogleCredentialsJson = JsonConvert.SerializeObject(_config.GoogleCredentials);
            }
            _googleDriveDataManager = new DataManager(_config.GoogleCredentialsJson);
            _googleSheetsDataManager =
                new GoogleSheetsManager.DataManager(_config.GoogleCredentialsJson, _config.GoogleSheetId);

            var chatId = new ChatId($"@{_config.EventsChannelLogin}");
            _eventManager = new Events.Manager(_googleSheetsDataManager, saveManager, _config.GoogleRange,
                _config.EventsFormUri, Client, chatId, _config.LogsChatId, _config.DiscussUri);

            var commands = new List<Command>
            {
                new CustomCommand(_config.DocumentIds, _config.PdfFolderPath, _googleDriveDataManager),
                new UpdateCommand(_config.DocumentIds, _config.PdfFolderId, _config.PdfFolderPath,
                    _googleDriveDataManager),
                new CheckListCommand(_config.CheckList),
                new ExercisesCommand(_config.Template, _config.ExersisesLinks),
                new LinksCommand(_config.Links),
                new FeedbackCommand(_config.FeedbackLink),
                new ThanksCommand(_config.Payees, _config.Banks),
                new WeekCommand(_eventManager)
            };

            Commands = commands.AsReadOnly();
            var startCommand = new StartCommand(Commands);

            commands.Insert(0, startCommand);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.SetWebhookAsync(_config.Url, cancellationToken: cancellationToken);
            await DoAndSchedule(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Client.DeleteWebhookAsync(cancellationToken);
            _weeklyUpdateTimer?.Stop();
            _weeklyUpdateTimer?.Dispose();
            _googleDriveDataManager.Dispose();
            _googleSheetsDataManager.Dispose();
            _eventManager.Dispose();
        }

        private async Task DoAndSchedule(Func<Task> func)
        {
            await func();
            DateTime nextUpdateAt = Utils.GetMonday().AddDays(7) + _config.EventsUpdateAt.TimeOfDay;
            Utils.DoOnce(ref _weeklyUpdateTimer, nextUpdateAt, () => DoAndScheduleWeekly(func));
        }

        private async Task DoAndScheduleWeekly(Func<Task> func)
        {
            await func();
            Utils.DoWeekly(ref _weeklyUpdateTimer, func);
        }

        private readonly BotConfiguration _config;

        private readonly DataManager _googleDriveDataManager;
        private readonly GoogleSheetsManager.DataManager _googleSheetsDataManager;
        private readonly Events.Manager _eventManager;
        private Timer _weeklyUpdateTimer;
    }
}