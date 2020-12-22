using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class IntroCommand : Command
    {
        internal override string Name => "intro";
        internal override string Description => "инструкция после вступления";

        public IntroCommand(string text) => _text = text;

        internal override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            return client.SendTextMessageAsync(chatId, _text, ParseMode.Markdown);
        }

        private readonly string _text;
    }
}
