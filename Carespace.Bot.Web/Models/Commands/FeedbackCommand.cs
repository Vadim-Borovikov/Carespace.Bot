using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal class FeedbackCommand : Command
    {
        internal override string Name => "feedback";
        internal override string Description => "оставить обратную связь";

        internal override AccessType Type => AccessType.Users;

        public FeedbackCommand(BotConfiguration.Link link)
        {
            _link = link;
        }

        protected override Task ExecuteAsync(Message message, ITelegramBotClient client, bool _)
        {
            return Utils.SendMessage(_link, message.Chat, client);
        }

        private readonly BotConfiguration.Link _link;
    }
}
