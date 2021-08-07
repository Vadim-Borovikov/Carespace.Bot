using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class ConfirmCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "confirm";
        protected override string Description => "Подтвердить отправку событий";

        public override bool AdminsOnly => true;

        public ConfirmCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            await Bot.EventManager.PostOrUpdateWeekEventsAndScheduleAsync();
        }
    }
}
