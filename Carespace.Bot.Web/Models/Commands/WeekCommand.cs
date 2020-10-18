using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Events;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekCommand : Command
    {
        private readonly Manager _eventManager;

        public WeekCommand(Manager eventManager) { _eventManager = eventManager; }

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            Message statusMessage = await client.SendTextMessageAsync(message.Chat, "_Обновляю…_", ParseMode.Markdown);

            await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync();

            await client.FinalizeStatusMessageAsync(statusMessage);
        }
    }
}
