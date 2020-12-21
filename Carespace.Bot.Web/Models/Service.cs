using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Service : IHostedService, IDisposable
    {
        public Service(IBot bot)
        {
            _bot = bot;

            if (string.IsNullOrWhiteSpace(_bot.Config.GoogleCredentialsJson))
            {
                _bot.Config.GoogleCredentialsJson = JsonConvert.SerializeObject(_bot.Config.GoogleCredentials);
            }
            _googleDriveDataManager = new DataManager(_bot.Config.GoogleCredentialsJson);

            _bot.InitCommands(_googleDriveDataManager);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bot.Client.SetWebhookAsync(_bot.Config.Url, cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _bot.Client.DeleteWebhookAsync(cancellationToken);
        }

        public void Dispose()
        {
            _googleDriveDataManager?.Dispose();
        }

        private readonly IBot _bot;
        private readonly DataManager _googleDriveDataManager;
    }
}