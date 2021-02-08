using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class FeedbackCommand : CommandBase<Config.Config>
    {
        protected override string Name => "feedback";
        protected override string Description => "оставить обратную связь";

        public FeedbackCommand(Bot bot) : base(bot) { }

        public override Task ExecuteAsync(Message message, bool fromChat = false)
        {
            return Bot.Client.SendMessageAsync(Bot.Config.FeedbackLink, message.From.Id);
        }
    }
}
