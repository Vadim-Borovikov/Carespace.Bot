using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands
{
    internal abstract class TextCommand : CommandBase<Bot, Config.Config>
    {
        protected TextCommand(Bot bot, string text) : base(bot) => _text = text;

        public override Task ExecuteAsync(Message message, bool fromChat, string payload)
        {
            return Bot.Client.SendTextMessageAsync(message.From.Id, _text, ParseMode.MarkdownV2);
        }

        private readonly string _text;
    }
}
