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
    internal class BotService : IBotService, IHostedService
    {
        public TelegramBotClient Client { get; }
        public IReadOnlyCollection<Command> Commands { get; }
        public IEnumerable<int> AdminIds => _config.AdminIds;

        public BotService(IOptions<BotConfiguration> options)
        {
            _config = options.Value;

            Client = new TelegramBotClient(_config.Token);

            if (string.IsNullOrWhiteSpace(_config.GoogleCredentialsJson))
            {
                _config.GoogleCredentialsJson = JsonConvert.SerializeObject(_config.GoogleCredentials);
            }
            _googleDriveDataManager = new DataManager(_config.GoogleCredentialsJson);
            _googleSheetsDataManager =
                new GoogleSheetsManager.DataManager(_config.GoogleCredentialsJson, _config.GoogleSheetId);

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
                new PostEventsCommand(_googleSheetsDataManager, _config.GoogleEventsRangeAll,
                _config.GoogleEventsRangePin, _config.GoogleEventsRangeWeek, _config.EventsChannelLogin),
                new UpdateScheduleCommand(_googleSheetsDataManager, _config.GoogleEventsRangePin,
                _config.GoogleEventsRangeWeek, _config.EventsChannelLogin)
            };

            Commands = commands.AsReadOnly();
            var startCommand = new StartCommand(Commands);

            commands.Insert(0, startCommand);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.SetWebhookAsync(_config.Url, cancellationToken: cancellationToken);
            User user = await Client.GetMeAsync(cancellationToken);
            await Client.SendTextMessageAsync($"@{_config.DebugChannelLogin}", $"@{user.Username} проснулся!",
                cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _googleDriveDataManager.Dispose();
            _googleSheetsDataManager.Dispose();
            return Client.DeleteWebhookAsync(cancellationToken);
        }

        private readonly BotConfiguration _config;

        private readonly DataManager _googleDriveDataManager;
        private readonly GoogleSheetsManager.DataManager _googleSheetsDataManager;
    }
}