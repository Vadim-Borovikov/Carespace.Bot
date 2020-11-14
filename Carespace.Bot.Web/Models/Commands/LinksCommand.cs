using System.Collections.Generic;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Bot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class LinksCommand : Command
    {
        internal override string Name => "links";
        internal override string Description => "полезные ссылки";

        internal override AccessType Type => AccessType.Users;

        public LinksCommand(IEnumerable<Configuration.Link> links)
        {
            _links = links;
        }

        protected override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            foreach (Configuration.Link link in _links)
            {
                await client.SendMessageAsync(link, chatId);
            }
        }

        private readonly IEnumerable<Configuration.Link> _links;
    }
}
