using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class ExercisesCommand : CommandBaseCustom<Bot, Config.Config>
{
    public ExercisesCommand(Bot bot) : base(bot, "exercises", "упражнения") { }

    public override async Task ExecuteAsync(Message message, Chat chat, string? payload)
    {
        foreach (string text in Bot.Config.ExercisesLinks.Select(GetMessage))
        {
            await Bot.SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
        }
    }

    private string GetMessage(string link)
    {
        return string.Format(Bot.Config.Template, WordJoiner, AbstractBot.Utils.EscapeCharacters(link));
    }

    private const string WordJoiner = "\u2060";
}