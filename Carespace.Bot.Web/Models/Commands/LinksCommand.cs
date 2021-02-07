using System.Collections.Generic;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class LinksCommand : Command
    {
        public override string Name => "links";
        public override string Description => "полезные ссылки";

        public LinksCommand(IEnumerable<Link> links) => _links = links;

        public override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            foreach (Link link in _links)
            {
                await client.SendMessageAsync(link, chatId);
            }
        }

        private readonly IEnumerable<Link> _links;
    }
}
