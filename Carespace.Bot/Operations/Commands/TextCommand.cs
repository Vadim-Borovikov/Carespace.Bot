﻿using System.Threading.Tasks;
using AbstractBot.Operations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations.Commands;

internal abstract class TextCommand : CommandOperation
{
    protected TextCommand(Bot bot, string command, string description, string text) : base(bot, command, description)
    {
        _bot = bot;
        _text = text;
    }

    protected override Task ExecuteAsync(Message message, long _, string? __)
    {
        return _bot.SendTextMessageAsync(message.Chat, _text, ParseMode.MarkdownV2);
    }

    private readonly Bot _bot;
    private readonly string _text;
}