using System.Linq;
using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands
{
    internal sealed class ExercisesCommand : CommandBase<Bot, Config.Config>
    {
        protected override string Name => "exercises";
        protected override string Description => "упражнения";

        public ExercisesCommand(Bot bot) : base(bot) { }

        public override async Task ExecuteAsync(Message message, bool fromChat = false)
        {
            foreach (string text in
                Bot.Config.ExersisesLinks.Select(l => string.Format(Bot.Config.Template, WordJoiner, l)))
            {
                await Bot.Client.SendTextMessageAsync(message.From.Id, text, ParseMode.Markdown);
            }
        }

        private const string WordJoiner = "\u2060";
    }
}
