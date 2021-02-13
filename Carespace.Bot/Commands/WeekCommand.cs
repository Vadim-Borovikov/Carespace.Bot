using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class WeekCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "week";
        protected override string Description => "обновить расписание";

        public override bool AdminsOnly => true;

        public WeekCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            await Bot.EventManager.PostOrUpdateWeekEventsAndScheduleAsync(true);
        }
    }
}
