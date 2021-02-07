using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        internal static string GetCaption(string name, Payee payee, IReadOnlyDictionary<string, Link> banks)
        {
            string options;
            if (payee.Accounts?.Count > 0)
            {
                IEnumerable<string> texts = payee.Accounts.Select(a => GetText(a, banks[a.BankId]));
                options = string.Join($" или{Environment.NewLine}", texts);
            }
            else
            {
                options = payee.ThanksString;
            }
            return $"{name}: {options}";
        }

        internal static Task<Message> SendStickerAsync(this ITelegramBotClient client, Message message,
            InputOnlineFile sticker)
        {
            return client.SendStickerAsync(message.Chat, sticker, replyToMessageId: message.MessageId);
        }

        internal static async Task<string> GetNameAsync(this ITelegramBotClient client)
        {
            User me = await client.GetMeAsync();
            return me.Username;
        }

        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
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

        internal static Task<Message> FinalizeStatusMessageAsync(this ITelegramBotClient client, Message message,
            string postfix = "")
        {
            string text = $"_{message.Text}_ Готово.{postfix}";
            return client.EditMessageTextAsync(message.Chat, message.MessageId, text, ParseMode.Markdown);
        }

        internal static DateTime GetMonday()
        {
            DateTime today = Now().Date;
            int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        internal static void SetupTimeZoneInfo(string id) => _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(id);

        internal static DateTime Now() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZoneInfo);

        internal static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:d MMMM}";
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

        private static string GetText(Account account, Link bank)
        {
            return $"{account.CardNumber} в [{bank.Name}]({bank.Url})";
        }

        internal const string CalendarUriFormat = "{0}/calendar/{1}";

        private const string ExceptionsLogPath = "errors.txt";
        private const string TimersLogPath = "timers.txt";

        private static readonly ConcurrentDictionary<string, string> PhotoIds =
            new ConcurrentDictionary<string, string>();

        private static TimeZoneInfo _timeZoneInfo;
    }
}
