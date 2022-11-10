using System;
using System.Globalization;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot;

internal static class Utils
{
    public static Task SendMessageAsync(this Bot bot, Link link, Chat chat)
    {
        if (string.IsNullOrWhiteSpace(link.PhotoPath))
        {
            string text = $"[{AbstractBot.Utils.EscapeCharacters(link.Name)}]({link.Uri.AbsoluteUri})";
            return bot.SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
        }

        InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
        return PhotoRepository.SendPhotoAsync(bot, chat, link.PhotoPath, replyMarkup: keyboard);
    }

    public static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

    public static DateTime GetMonday(TimeManager timeManager)
    {
        DateTime today = timeManager.Now().Date;
        int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
        return today.AddDays(-diff);
    }

    public static string ShowDate(DateTime date)
    {
        string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
        return $"{day}, {date:d MMMM}";
    }

    public static DateTime GetNextThursday(DateTime date)
    {
        int diff = (7 + DayOfWeek.Thursday - date.DayOfWeek) % 7;
        return date.AddDays(diff);
    }

    public static Uri? ToUri(this object? o)
    {
        if (o is Uri uri)
        {
            return uri;
        }
        string? uriString = o?.ToString();
        return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
    }

    private static InlineKeyboardMarkup GetReplyMarkup(Link link)
    {
        InlineKeyboardButton button = new(link.Name) { Url = link.Uri.AbsoluteUri };
        return new InlineKeyboardMarkup(button);
    }

    public const string CalendarUriFormat = "{0}/calendar/{1}";

    private const string TimersLogPath = "timers.txt";
}