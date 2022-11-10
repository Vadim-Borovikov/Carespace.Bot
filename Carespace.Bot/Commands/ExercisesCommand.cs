using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Commands;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class ExercisesCommand : CommandBase<Bot, Config.Config>
{
    public ExercisesCommand(Bot bot) : base(bot, "exercises", "упражнения") { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
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