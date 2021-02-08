using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class StartCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "start";
        protected override string Description => "команды";

        public StartCommand(Bot bot) : base(bot) { }

        public override Task ExecuteAsync(Message message, bool fromChat = false)
        {
            bool fromAdmin = Bot.FromAdmin(message);
            return Bot.Client.SendTextMessageAsync(message.From.Id, Bot.GetDescription(fromAdmin));
        }
    }
}
