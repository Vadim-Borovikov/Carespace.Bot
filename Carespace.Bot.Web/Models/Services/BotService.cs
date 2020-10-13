using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using GoogleDocumentsUnifier.Logic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;

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
                new GoogleSheetsReader.DataManager(_config.GoogleCredentialsJson, _config.GoogleSheetId);

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
                new WeekEventsCommand(_googleSheetsDataManager, _config.GoogleEventsRange, _config.EventsChannelLogin)
            };

            Commands = commands.AsReadOnly();
            var startCommand = new StartCommand(Commands);

            commands.Insert(0, startCommand);

            var uri = new Uri(_config.Url);
            _pingUrl = uri.Host;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicCancellationSource = new CancellationTokenSource();
            _ping = new Ping();
            StartPeriodicPing(_periodicCancellationSource.Token);

            return Client.SetWebhookAsync(_config.Url, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _googleDriveDataManager.Dispose();
            _googleSheetsDataManager.Dispose();
            _periodicCancellationSource.Cancel();
            _ping.Dispose();
            _periodicCancellationSource.Dispose();
            return Client.DeleteWebhookAsync(cancellationToken);
        }

        private void StartPeriodicPing(CancellationToken cancellationToken)
        {
            IObservable<long> observable = Observable.Interval(_config.PingPeriod);
            observable.Subscribe(PingSite, cancellationToken);
        }

        private void PingSite(long _) => _ping.Send(_pingUrl);

        private readonly BotConfiguration _config;

        private readonly DataManager _googleDriveDataManager;
        private readonly GoogleSheetsReader.DataManager _googleSheetsDataManager;

        private CancellationTokenSource _periodicCancellationSource;
        private Ping _ping;
        private readonly string _pingUrl;
    }
}