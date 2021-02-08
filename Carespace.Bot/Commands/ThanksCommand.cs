using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands
{
    internal sealed class ThanksCommand : CommandBase<Config.Config>
    {
        protected override string Name => "thanks";
        protected override string Description => "поблагодарить ведущих";

        public ThanksCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            foreach (Payee payee in Bot.Config.Payees)
            {
                string caption = Utils.GetCaption(payee.Name, payee, Bot.Config.Banks);
                await Utils.SendPhotoAsync(Bot.Client, message.From.Id, payee.PhotoPath, caption, ParseMode.Markdown);
            }
        }
    }
}
