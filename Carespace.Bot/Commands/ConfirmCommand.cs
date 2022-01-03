using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class ConfirmCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "confirm";
        protected override string Description => "Подтвердить отправку событий";

        public override BotBase<Bot, Config.Config>.AccessType Access => BotBase<Bot, Config.Config>.AccessType.Admins;

        public ConfirmCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat, string payload)
        {
            await Bot.EventManager.PostOrUpdateWeekEventsAndScheduleAsync(message.Chat);
        }
    }
}
