using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Operations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class ExercisesCommand : CommandOperation
{
    protected override byte MenuOrder => 4;

    public ExercisesCommand(Bot bot, Config.Config config) : base(bot, "exercises", "упражнения") => _config = config;

    protected override async Task ExecuteAsync(Message _, Chat chat, string? __)
    {
        foreach (string text in _config.ExercisesLinks.Select(GetMessage))
        {
            await BotBase.SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
        }
    }

    private string GetMessage(string link)
    {
        return string.Format(_config.Template, WordJoiner, AbstractBot.Utils.EscapeCharacters(link));
    }

    private const string WordJoiner = "\u2060";

    private readonly Config.Config _config;
}