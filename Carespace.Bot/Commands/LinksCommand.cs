using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class LinksCommand : CommandBase<Config.Config>
    {
        protected override string Name => "links";
        protected override string Description => "полезные ссылки";

        public LinksCommand(Bot bot) : base(bot)  { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            foreach (Link link in Bot.Config.Links)
            {
                await Bot.Client.SendMessageAsync(link, message.From.Id);
            }
        }
    }
}
