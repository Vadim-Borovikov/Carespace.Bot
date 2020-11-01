using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal class ThanksCommand : Command
    {
        internal override string Name => "thanks";
        internal override string Description => "поблагодарить ведущих";

        internal override AccessType Type => AccessType.Users;

        public ThanksCommand(List<BotConfiguration.Payee> payees, Dictionary<string, BotConfiguration.Link> banks)
        {
            _payees = payees;
            _banks = banks;
        }

        protected override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            return;
            foreach (BotConfiguration.Payee payee in _payees)
            {
                await SendMessage(client, payee, chatId);
            }
        }

        private Task SendMessage(ITelegramBotClient client, BotConfiguration.Payee payee, ChatId chatId)
        {
            string caption = Utils.GetCaption(payee.Name, payee.Accounts, _banks);
            return client.SendPhotoAsync(chatId, payee.PhotoPath, caption, ParseMode.Markdown);
        }

        private readonly List<BotConfiguration.Payee> _payees;
        private readonly Dictionary<string, BotConfiguration.Link> _banks;
    }
}
