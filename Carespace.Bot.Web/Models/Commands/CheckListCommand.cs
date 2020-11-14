using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class CheckListCommand : Command
    {
        internal override string Name => "checklist";
        internal override string Description => "инструкция после вступления";

        public CheckListCommand(string text) { _text = text; }

        protected override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            return client.SendTextMessageAsync(chatId, _text);
        }

        private readonly string _text;
    }
}
