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
        public override string Name => "exercises";
        public override string Description => "упражнения";

        public ExercisesCommand(string template, IEnumerable<string> links)
        {
            _template = template;
            _links = links;
        }

        public override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            foreach (string text in _links.Select(l => string.Format(_template, WordJoiner, l)))
            {
                await client.SendTextMessageAsync(chatId, text, ParseMode.Markdown);
            }
        }

        private readonly string _template;
        private readonly IEnumerable<string> _links;

        private const string WordJoiner = "\u2060";
    }
}
