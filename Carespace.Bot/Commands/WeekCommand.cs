using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Events;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class WeekCommand : CommandBase<Config.Config>
    {
        protected override string Name => "week";
        protected override string Description => "обновить расписание";

        public override bool AdminsOnly => true;

        public WeekCommand(Bot bot, Manager eventManager) : base(bot) => _eventManager = eventManager;

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync();
        }

        private readonly Manager _eventManager;
    }
}
