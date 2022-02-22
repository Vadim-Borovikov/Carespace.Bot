using System;
using System.Globalization;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot;

internal static class Utils
{
    public static MailAddress? AsEmail(this string email)
    {
        try
        {
            return new MailAddress(email);
        }
        catch
        {
            return null;
        }
    }

    public static Task SendMessageAsync(this ITelegramBotClient client, Link link, ChatId chatId)
    {
        if (string.IsNullOrWhiteSpace(link.PhotoPath))
        {
            string name = link.Name.GetValue(nameof(link.Name));
            string text = $"[{AbstractBot.Utils.EscapeCharacters(name)}]({link.Uri.AbsoluteUri})";
            return client.SendTextMessageAsync(chatId, text, ParseMode.MarkdownV2);
        }

        InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
        return PhotoRepository.SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
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

    private static InlineKeyboardMarkup GetReplyMarkup(Link link)
    {
        string name = link.Name.GetValue(nameof(link.Name));
        InlineKeyboardButton button = new(name) { Url = link.Uri.AbsoluteUri };
        return new InlineKeyboardMarkup(button);
    }

    internal const string CalendarUriFormat = "{0}/calendar/{1}";

    private const string TimersLogPath = "timers.txt";
}