using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot
{
    public static class Utils
    {
        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
        }

        internal static Task SendMessageAsync(this ITelegramBotClient client, Link link, ChatId chatId)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{link.Name}]({link.Url})";
                return client.SendTextMessageAsync(chatId, text, ParseMode.Markdown);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
        }

        internal static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

        internal static async Task<Message> SendPhotoAsync(ITelegramBotClient client, ChatId chatId, string photoPath,
            string caption = null, ParseMode parseMode = ParseMode.Default, IReplyMarkup replyMarkup = null)
        {
            bool success = PhotoIds.TryGetValue(photoPath, out string fileId);
            if (success)
            {
                var photo = new InputOnlineFile(fileId);
                return await client.SendPhotoAsync(chatId, photo, caption, parseMode, replyMarkup: replyMarkup);
            }

            using (var stream = new FileStream(photoPath, FileMode.Open))
            {
                var photo = new InputOnlineFile(stream);
                Message message =
                    await client.SendPhotoAsync(chatId, photo, caption, parseMode, replyMarkup: replyMarkup);
                fileId = message.Photo.First().FileId;
                PhotoIds.TryAdd(photoPath, fileId);
                return message;
            }
        }

        internal static DateTime GetMonday(TimeManager timeManager)
        {
            DateTime today = timeManager.Now().Date;
            int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        internal static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:d MMMM}";
        }

        internal static string GetText(Account account, Link bank)
        {
            return $"{account.CardNumber} в [{bank.Name}]({bank.Url})";
        }

        private static InlineKeyboardMarkup GetReplyMarkup(Link link)
        {
            var button = new InlineKeyboardButton
            {
                Text = link.Name,
                Url = link.Url
            };
            return new InlineKeyboardMarkup(button);
        }

        internal const string CalendarUriFormat = "{0}/calendar/{1}";

        private const string ExceptionsLogPath = "errors.txt";
        private const string TimersLogPath = "timers.txt";

        private static readonly ConcurrentDictionary<string, string> PhotoIds =
            new ConcurrentDictionary<string, string>();
    }
}
