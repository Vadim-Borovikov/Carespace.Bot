﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using FileInfo = GoogleDocumentsUnifier.Logic.FileInfo;

namespace Carespace.Bot.Web.Models
{
    internal static class Utils
    {
        internal static async Task<Message> FinalizeStatusMessageAsync(this ITelegramBotClient client,
            Message message, string postfix = "")
        {
            Chat chat = message.Chat;
            string text = $"_{message.Text}_ Готово.{postfix}";
            await client.DeleteMessageAsync(chat, message.MessageId);
            return await client.SendTextMessageAsync(chat, text, ParseMode.Markdown);
        }

        internal static async Task<List<PdfData>> CheckAsync(IEnumerable<string> sources,
            Func<string, Task<PdfData>> check)
        {
            PdfData[] datas = await Task.WhenAll(sources.Select(check));
            return datas.Where(d => d.Status != PdfData.FileStatus.Ok).ToList();
        }

        internal static Task CreateOrUpdateAsync(IEnumerable<PdfData> sources, Func<PdfData, Task> createOrUpdate)
        {
            List<Task> updateTasks = sources.Select(createOrUpdate).ToList();
            return Task.WhenAll(updateTasks);
        }

        internal static async Task<PdfData> CheckLocalPdfAsync(string sourceId, DataManager googleDataManager,
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

        internal static async Task CreateOrUpdateLocalAsync(PdfData data, DataManager googleDataManager,
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

        internal static Task SendMessage(this ITelegramBotClient client, BotConfiguration.Link link, Chat chat)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{link.Name}]({link.Url})";
                return client.SendTextMessageAsync(chat, text, ParseMode.Markdown);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return SendPhotoAsync(client, chat, link.PhotoPath, replyMarkup: keyboard);
        }

        internal static Task SendMessage(this ITelegramBotClient client, BotConfiguration.Payee payee,
            IReadOnlyDictionary<string, BotConfiguration.Link> banks, Chat chat)
        {
            string caption = GetCaption(payee.Name, payee.Accounts, banks);
            return client.SendPhotoAsync(chat, payee.PhotoPath, caption, ParseMode.Markdown);
        }

        internal static async Task CreateOrUpdatePinnedMessage(this ITelegramBotClient client,
            IEnumerable<Event> events, string channel)
        {
            var chatId = new ChatId($"@{channel}");
            string text = PrepareWeekSchedule(events, channel);
            Message message = await client.GetPinnedMessage(chatId);
            if (IsMessageRelevant(message))
            {
                await client.EditMessageTextAsync(chatId, message.MessageId, text, ParseMode.Markdown, true);
            }
            else
            {
                message = await client.SendTextMessageAsync(chatId, text, ParseMode.Markdown, true);
                await client.PinChatMessageAsync(chatId, message.MessageId);
            }
        }

        internal static DateTime GetMonday()
        {
            DateTime today = DateTime.Today;
            int diff = (today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        private static string PrepareWeekSchedule(IEnumerable<Event> events, string channel)
        {
            var scheduleBuilder = new StringBuilder();
            DateTime date = GetMonday().AddDays(-1);
            foreach (Event e in events.Where(e => e.DescriptionId.HasValue))
            {
                if (e.Start.Date > date)
                {
                    if (scheduleBuilder.Length > 0)
                    {
                        scheduleBuilder.AppendLine();
                    }
                    date = e.Start.Date;
                    scheduleBuilder.AppendLine($"*{ShowDate(date)}*");
                }
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, channel, e.DescriptionId));
                scheduleBuilder.AppendLine($"{e.Start:HH:mm} [{e.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:dd MMMM}";
        }

        private static bool IsMessageRelevant(Message message)
        {
            if (message == null)
            {
                return false;
            }

            return message.Date < GetMonday();
        }

        private static async Task<Message> GetPinnedMessage(this ITelegramBotClient client, ChatId chatId)
        {
            Message chatMesage = await client.EditMessageReplyMarkupAsync(chatId, ChatMessageId);
            return chatMesage.Chat.PinnedMessage;
        }

        private static async Task<Message> SendPhotoAsync(ITelegramBotClient client, Chat chat, string photoPath,
            string caption = null, ParseMode parseMode = ParseMode.Default, IReplyMarkup replyMarkup = null)
        {
            bool success = PhotoIds.TryGetValue(photoPath, out string fileId);
            if (success)
            {
                var photo = new InputOnlineFile(fileId);
                return await client.SendPhotoAsync(chat, photo, caption, parseMode, replyMarkup: replyMarkup);
            }

            using (var stream = new FileStream(photoPath, FileMode.Open))
            {
                var photo = new InputOnlineFile(stream);
                Message message =
                    await client.SendPhotoAsync(chat, photo, caption, parseMode, replyMarkup: replyMarkup);
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

        private static string GetCaption(string name, IEnumerable<BotConfiguration.Payee.Account> accounts,
            IReadOnlyDictionary<string, BotConfiguration.Link> banks)
        {
            IEnumerable<string> texts = accounts.Select(a => GetText(a, banks[a.BankId]));
            string options = string.Join($" или{Environment.NewLine}", texts);
            return $"{name}:{Environment.NewLine}{options}";
        }

        private static string GetText(BotConfiguration.Payee.Account account, BotConfiguration.Link bank)
        {
            return $"`{account.CardNumber}` в [{bank.Name}]({bank.Url})";
        }

        private static readonly ConcurrentDictionary<string, string> PhotoIds =
            new ConcurrentDictionary<string, string>();
        private const int ChatMessageId = 1;
        private const string ChannelMessageUriFormat = "https://t.me/{0}/{1}";
    }
}
