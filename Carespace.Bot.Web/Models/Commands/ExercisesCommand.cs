using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class ExercisesCommand : Command
    {
        internal override string Name => "exercises";
        internal override string Description => "упражнения";

        public ExercisesCommand(string template, IEnumerable<string> links)
        {
            _template = template;
            _links = links;
        }

        protected override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            foreach (string text in _links.Select(l => string.Format(_template, l)))
            {
                await client.SendTextMessageAsync(chatId, text, ParseMode.Html);
            }
        }

        private readonly string _template;
        private readonly IEnumerable<string> _links;
    }
}
