using System;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Configs;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ExercisesCommand : CommandOperation
{
    protected override byte MenuOrder => 4;

    public ExercisesCommand(Bot bot, Config config) : base(bot, "exercises", "упражнения") => _config = config;

    protected override async Task ExecuteAsync(Message message, long _, string? __)
    {
        foreach (string text in _config.ExerciseUris.Select(GetMessage))
        {
            await Bot.SendTextMessageAsync(message.Chat, text, ParseMode.MarkdownV2);
        }
    }

    private string GetMessage(Uri uri)
    {
        return string.Format(_config.Template, Text.WordJoiner,
            AbstractBot.Bots.Bot.EscapeCharacters(uri.AbsoluteUri));
    }

    private readonly Config _config;
}