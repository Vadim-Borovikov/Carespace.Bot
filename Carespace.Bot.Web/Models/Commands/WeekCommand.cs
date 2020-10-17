using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekCommand : Command
    {
        private readonly ChannelManager _channelManager;

        public WeekCommand(ChannelManager channelManager) { _channelManager = channelManager; }

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            Message statusMessage = await client.SendTextMessageAsync(message.Chat, "_Обновляю…_", ParseMode.Markdown);

            await _channelManager.PostOrUpdateWeekEventsAndScheduleAsync();

            await client.FinalizeStatusMessageAsync(statusMessage);
        }
    }
}
