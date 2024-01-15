using System;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Extensions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Configs;

[PublicAPI]
public sealed class Link
{
    [Required]
    [MinLength(1)]
    public string Caption { get; init; } = null!;

    [Required]
    public Uri Uri { get; init; } = null!;

    public string? PhotoPath { get; init; }

    internal Task SendAsync(Bot bot, Chat chat)
    {
        if (string.IsNullOrWhiteSpace(PhotoPath))
        {
            string text = $"[{Caption.Escape()}]({Uri.AbsoluteUri})";
            return bot.SendTextMessageAsync(chat, text, parseMode: ParseMode.MarkdownV2);
        }

        InlineKeyboardMarkup keyboard = GetReplyMarkup();
        return bot.SendPhotoAsync(chat, PhotoPath, keyboard);
    }

    private InlineKeyboardMarkup GetReplyMarkup()
    {
        InlineKeyboardButton button = new(Caption) { Url = Uri.AbsoluteUri };
        return new InlineKeyboardMarkup(button);
    }

}