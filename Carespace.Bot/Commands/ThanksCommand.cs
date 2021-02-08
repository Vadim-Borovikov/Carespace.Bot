using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands
{
    internal sealed class ThanksCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "thanks";
        protected override string Description => "поблагодарить ведущих";

        public ThanksCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            foreach (Payee payee in Bot.Config.Payees)
            {
                string caption = GetCaption(payee, Bot.Config.Banks);
                await Utils.SendPhotoAsync(Bot.Client, message.From.Id, payee.PhotoPath, caption, ParseMode.Markdown);
            }
        }

        private static string GetCaption(Payee payee, IReadOnlyDictionary<string, Link> banks)
        {
            string options = payee.ThanksString;
            if (payee.Accounts?.Count > 0)
            {
                IEnumerable<string> texts = payee.Accounts.Select(a => Utils.GetText(a, banks[a.BankId]));
                options = string.Join($" или{Environment.NewLine}", texts);
            }
            return $"{payee.Name}: {options}";
        }
    }
}
