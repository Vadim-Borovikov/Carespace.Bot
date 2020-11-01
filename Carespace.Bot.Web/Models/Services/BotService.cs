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

            var eventsChatId = new ChatId($"@{_config.EventsChannelLogin}");
            var discussChatId = new ChatId($"@{_config.DiscussGroupLogin}");
            _eventManager = new Events.Manager(_googleSheetsDataManager, saveManager, _config.GoogleRange,
                _config.EventsFormUri, Client, eventsChatId, _config.LogsChatId, discussChatId);

            var commands = new List<Command>
            {
                new CustomCommand(_config.DocumentIds, _config.PdfFolderPath, _googleDriveDataManager),
                new UpdateCommand(_config.DocumentIds, _config.PdfFolderId, _config.PdfFolderPath,
                    _googleDriveDataManager),
                new CheckListCommand(_config.CheckList),
                new ExercisesCommand(_config.Template, _config.ExersisesLinks),
                new LinksCommand(_config.Links),
                new WeekCommand(_eventManager)
            };

            Commands = commands.AsReadOnly();
            var startCommand = new StartCommand(Commands);

            commands.Insert(0, startCommand);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.SetWebhookAsync(_config.Url, cancellationToken: cancellationToken);
            _weeklyUpdateTimer = new Timer();
            await DoAndSchedule(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Client.DeleteWebhookAsync(cancellationToken);
            _weeklyUpdateTimer.Stop();
            _weeklyUpdateTimer.Dispose();
            _googleDriveDataManager.Dispose();
            _googleSheetsDataManager.Dispose();
            _eventManager.Dispose();
        }

        private async Task DoAndSchedule(Func<Task> func)
        {
            await func();
            DateTime nextUpdateAt = Utils.GetMonday().AddDays(7) + _config.EventsUpdateAt.TimeOfDay;
            _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeekly(func));
        }

        private async Task DoAndScheduleWeekly(Func<Task> func)
        {
            await func();
            _weeklyUpdateTimer.DoWeekly(func);
        }

        private readonly BotConfiguration _config;

        private readonly DataManager _googleDriveDataManager;
        private readonly GoogleSheetsManager.DataManager _googleSheetsDataManager;
        private readonly Events.Manager _eventManager;
        private Timer _weeklyUpdateTimer;
    }
}