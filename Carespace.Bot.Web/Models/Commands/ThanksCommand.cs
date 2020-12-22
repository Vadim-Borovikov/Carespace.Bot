using System.Collections.Generic;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class ThanksCommand : Command
    {
        internal override string Name => "thanks";
        internal override string Description => "поблагодарить ведущих";

        public ThanksCommand(List<Payee> payees, Dictionary<string, Link> banks)
        {
            _payees = payees;
            _banks = banks;
        }

        internal override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            foreach (Payee payee in _payees)
            {
                await SendMessage(client, payee, chatId);
            }
        }

        private Task SendMessage(ITelegramBotClient client, Payee payee, ChatId chatId)
        {
            string caption = Utils.GetCaption(payee.Name, payee, _banks);
            return Utils.SendPhotoAsync(client, chatId, payee.PhotoPath, caption, ParseMode.Markdown);
        }

        private readonly List<Payee> _payees;
        private readonly Dictionary<string, Link> _banks;
    }
}
