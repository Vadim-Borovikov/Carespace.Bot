using System.Threading.Tasks;
using Carespace.Bot.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class FeedbackCommand : Command
    {
        public override string Name => "feedback";
        public override string Description => "оставить обратную связь";

        public FeedbackCommand(Link link) => _link = link;

        public override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            return client.SendMessageAsync(_link, chatId);
        }

        private readonly Link _link;
    }
}
