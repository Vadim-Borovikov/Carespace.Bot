using System.Linq;
using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class ExercisesCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "exercises";
    protected override string Description => "Упражнения";

    public ExercisesCommand(Bot bot) : base(bot) { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        foreach (string text in Bot.Config.ExercisesLinks.Select(GetMessage))
        {
            await Bot.SendTextMessageAsync(user.Id, text, ParseMode.MarkdownV2);
        }
    }

    private string GetMessage(string link)
    {
        string template = Bot.Config.Template.GetValue(nameof(Bot.Config.Template));
        return string.Format(template, WordJoiner, AbstractBot.Utils.EscapeCharacters(link));
    }

    private const string WordJoiner = "\u2060";
}