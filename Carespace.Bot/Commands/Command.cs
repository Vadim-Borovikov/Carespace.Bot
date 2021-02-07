using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands
{
    internal abstract class Command
    {
        public abstract string Name { get; }
        public virtual string Description => "";
        public virtual bool AdminsOnly => false;

        public bool IsInvokingBy(Message message, bool fromChat, string botName)
        {
            return (message.Type == MessageType.Text)
                   && (message.Text == (fromChat ? $"/{Name}@{botName}" : $"/{Name}"));
        }

        public abstract Task ExecuteAsync(ChatId chatId, ITelegramBotClient client);
    }
}
