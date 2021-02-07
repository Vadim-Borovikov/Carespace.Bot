using System.Threading.Tasks;
using Carespace.Bot.Events;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands
{
    internal sealed class WeekCommand : Command
    {
        public override string Name => "week";

        public override bool AdminsOnly => true;

        public WeekCommand(Manager eventManager) => _eventManager = eventManager;

        public override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync();
        }

        private readonly Manager _eventManager;
    }
}
