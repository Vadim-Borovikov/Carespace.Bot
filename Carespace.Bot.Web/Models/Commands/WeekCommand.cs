using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Events;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekCommand : Command
    {
        private readonly Manager _eventManager;

        public WeekCommand(Manager eventManager) => _eventManager = eventManager;

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(ChatId chat, ITelegramBotClient client, bool _)
        {
            await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync();
        }
    }
}
