using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Events;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekCommand : Command
    {
        internal override bool AdminsOnly => true;

        public WeekCommand(Manager eventManager) => _eventManager = eventManager;

        internal override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync();
        }

        private readonly Manager _eventManager;
    }
}
