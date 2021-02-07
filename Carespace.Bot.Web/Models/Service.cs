using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Service : IHostedService
    {
        public Service(Bot bot) => _bot = bot;

        public Task StartAsync(CancellationToken cancellationToken) => _bot.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => _bot.StopAsync(cancellationToken);

        private readonly Bot _bot;
    }
}