using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using FileInfo = GoogleDocumentsUnifier.Logic.FileInfo;

namespace Carespace.Bot.Web
{
    internal static class Utils
    {
        public static async Task<Message> FinalizeStatusMessageAsync(this ITelegramBotClient client,
            Message message, string postfix = "")
        {
            Chat chat = message.Chat;
            string text = $"_{message.Text}_ Готово.{postfix}";
            await client.DeleteMessageAsync(chat, message.MessageId);
            return await client.SendTextMessageAsync(chat, text, ParseMode.Markdown);
        }

        public static async Task<List<PdfData>> CheckAsync(IEnumerable<string> sources,
            Func<string, Task<PdfData>> check)
        {
            PdfData[] datas = await Task.WhenAll(sources.Select(check));
            return datas.Where(d => d.Status != PdfData.FileStatus.Ok).ToList();
        }

        public static Task CreateOrUpdateAsync(IEnumerable<PdfData> sources, Func<PdfData, Task> createOrUpdate)
        {
            List<Task> updateTasks = sources.Select(createOrUpdate).ToList();
            return Task.WhenAll(updateTasks);
        }

        public static async Task<PdfData> CheckLocalPdfAsync(string sourceId, DataManager googleDataManager,
            string pdfFolderPath)
        {
            FileInfo fileInfo = await googleDataManager.GetFileInfoAsync(sourceId);

            string path = Path.Combine(pdfFolderPath, $"{fileInfo.Name}.pdf");
            if (!File.Exists(path))
            {
                return PdfData.CreateNoneLocal(sourceId, path);
            }

            if (File.GetLastWriteTime(path) < fileInfo.ModifiedTime)
            {
                return PdfData.CreateOutdatedLocal(sourceId, path);
            }

            return PdfData.CreateOk();
        }

        public static async Task CreateOrUpdateLocalAsync(PdfData data, DataManager googleDataManager,
            string pdfFolderPath)
        {
            var info = new DocumentInfo(data.SourceId, DocumentType.Document);
            string path = Path.Combine(pdfFolderPath, data.Name);
            switch (data.Status)
            {
                case PdfData.FileStatus.None:
                case PdfData.FileStatus.Outdated:
                    await googleDataManager.DownloadAsync(info, path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data.Status), data.Status, "Unexpected Pdf status!");
            }
        }

        public static Task SendMessageAsync(this ITelegramBotClient client, BotConfiguration.Link link,
            ChatId chatId)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{link.Name}]({link.Url})";
                return client.SendTextMessageAsync(chatId, text, ParseMode.Markdown);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
        }

        public static DateTime GetMonday()
        {
            DateTime today = DateTime.Today;
            int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        public static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:d MMMM}";
        }

        public static void LogException(Exception ex) => File.AppendAllText(LogPath, $"{ex}{Environment.NewLine}");

        private static async Task<Message> SendPhotoAsync(ITelegramBotClient client, ChatId chatId, string photoPath,
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

        private static InlineKeyboardMarkup GetReplyMarkup(BotConfiguration.Link link)
        {
            var button = new InlineKeyboardButton
            {
                Text = link.Name,
                Url = link.Url
            };
            return new InlineKeyboardMarkup(button);
        }

        private static readonly ConcurrentDictionary<string, string> PhotoIds =
            new ConcurrentDictionary<string, string>();

        private const string LogPath = "errors.txt";
    }
}
