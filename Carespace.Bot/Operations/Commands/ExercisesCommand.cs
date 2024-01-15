using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Extensions;
using AbstractBot.Operations.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ExercisesCommand : CommandSimple
{
    protected override byte Order => 4;

    public ExercisesCommand(Bot bot) : base(bot, "exercises", bot.Config.Texts.ExercisesCommandDescription)
    {
        _messages = bot.Config.Texts.ExerciseUris.Select(u => GetMessage(u, bot.Config.InstantViewFormat)).ToList();
    }

    protected override async Task ExecuteAsync(Message message, User sender)
    {
        foreach (string text in _messages)
        {
            await Bot.SendTextMessageAsync(message.Chat, text, parseMode: ParseMode.MarkdownV2);
        }
    }

    private static string GetMessage(Uri uri, string format)
    {
        return string.Format(format, Text.WordJoiner, uri.AbsoluteUri.Escape());
    }

    private readonly List<string> _messages;
}