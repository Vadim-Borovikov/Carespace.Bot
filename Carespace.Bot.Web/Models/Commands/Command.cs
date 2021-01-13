using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    public abstract class Command
    {
        internal abstract string Name { get; }
        internal abstract string Description { get; }

        internal bool IsInvokingBy(Message message, bool fromChat, string botName)
        {
            return (message.Type == MessageType.Text)
                   && (message.Text == (fromChat ? $"/{Name}@{botName}" : $"/{Name}"));
        }

        internal abstract Task ExecuteAsync(ChatId chatId, ITelegramBotClient client);
    }
}
