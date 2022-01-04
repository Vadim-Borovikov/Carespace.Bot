using System;
using System.Globalization;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot
{
    public static class Utils
    {
        internal static MailAddress AsEmail(this string email)
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

        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
        }

        internal static Task SendMessageAsync(this ITelegramBotClient client, Link link, ChatId chatId)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{AbstractBot.Utils.EscapeCharacters(link.Name)}]({link.Url})";
                return client.SendTextMessageAsync(chatId, text, ParseMode.MarkdownV2);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return PhotoRepository.SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
        }

        internal static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

        internal static DateTime GetMonday(TimeManager timeManager)
        {
            DateTime today = timeManager.Now().Date;
            int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        internal static DateTime GetNextThursday(DateTime date)
        {
            int diff = (7 + DayOfWeek.Thursday - date.DayOfWeek) % 7;
            return date.AddDays(diff);
        }

        internal static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:d MMMM}";
        }

        private static InlineKeyboardMarkup GetReplyMarkup(Link link)
        {
            var button = new InlineKeyboardButton(link.Name)
            {
                Url = link.Url
            };
            return new InlineKeyboardMarkup(button);
        }

        internal const string CalendarUriFormat = "{0}/calendar/{1}";

        private const string ExceptionsLogPath = "errors.txt";
        private const string TimersLogPath = "timers.txt";
    }
}
