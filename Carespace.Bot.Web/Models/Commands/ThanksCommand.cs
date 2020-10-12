using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

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

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool _)
        {
            foreach (BotConfiguration.Payee payee in _payees)
            {
                await Utils.SendMessage(payee, _banks, message.Chat, client);
            }
        }

        private readonly List<BotConfiguration.Payee> _payees;
        private readonly Dictionary<string, BotConfiguration.Link> _banks;
    }
}
