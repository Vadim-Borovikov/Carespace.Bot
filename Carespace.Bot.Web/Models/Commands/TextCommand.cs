using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal abstract class TextCommand : Command
    {
        protected TextCommand(string text) => _text = text;

        public override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            return client.SendTextMessageAsync(chatId, _text, ParseMode.Markdown);
        }

        private readonly string _text;
    }
}
