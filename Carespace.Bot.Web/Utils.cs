using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Config;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot.Web
{
    internal static class Utils
    {
        public static Task SendMessageAsync(this ITelegramBotClient client, Link link, ChatId chatId)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{link.Name}]({link.Url})";
                return client.SendTextMessageAsync(chatId, text, ParseMode.Markdown);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
        }

        public static string GetCaption(string name, Payee payee, IReadOnlyDictionary<string, Link> banks)
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

        public static Task<Message> SendStickerAsync(this ITelegramBotClient client, Message message,
            InputOnlineFile sticker)
        {
            return client.SendStickerAsync(message.Chat, sticker, replyToMessageId: message.MessageId);
        }

        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
        }

        public static async Task<Message> SendPhotoAsync(ITelegramBotClient client, ChatId chatId, string photoPath,
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

        private static readonly ConcurrentDictionary<string, string> PhotoIds =
            new ConcurrentDictionary<string, string>();

        private const string ExceptionsLogPath = "errors.txt";
    }
}
