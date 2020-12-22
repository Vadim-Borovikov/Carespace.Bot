using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class FeedbackCommand : Command
    {
        internal override string Name => "feedback";
        internal override string Description => "оставить обратную связь";

        public FeedbackCommand(Link link) => _link = link;

        internal override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            return client.SendMessageAsync(_link, chatId);
        }

        private readonly Link _link;
    }
}
