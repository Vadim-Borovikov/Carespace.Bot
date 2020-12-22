using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Service : IHostedService
    {
        public Service(IBot bot)
        {
            _bot = bot;
            _bot.InitCommands();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bot.Client.SetWebhookAsync(_bot.Config.Url, cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _bot.Client.DeleteWebhookAsync(cancellationToken);
        }

        private readonly IBot _bot;
    }
}