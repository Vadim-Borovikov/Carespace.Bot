using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class CustomCommand : Command
    {
        internal override string Name => "custom";
        internal override string Description => "обновить, выбрать и объединить раздатки";

        public CustomCommand(List<string> sourceIds, string pdfFolderPath, DataManager googleDataManager)
        {
            _sourceIds = sourceIds;
            _pdfFolderPath = pdfFolderPath;
            _googleDataManager = googleDataManager;
            Directory.CreateDirectory(_pdfFolderPath);
        }

        protected override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            await UpdateLocalAsync(chatId, client);
            await SelectAsync(chatId, client);
        }

        protected override async Task InvokeAsync(Message message, ITelegramBotClient client, string data)
        {
            long chatId = message.Chat.Id;
            bool success = ChatData.TryGetValue(chatId, out CustomCommandData commandData);
            if (!success)
            {
                throw new Exception("Couldn't get data from ConcurrentDictionary!");
            }

            if (data == "")
            {
                bool shouldCleanup = await GenerateAndSendAsync(client, chatId, commandData);
                if (shouldCleanup)
                {
                    await commandData.Clear(client, message.Chat.Id);
                }
            }
            else
            {
                success = uint.TryParse(data, out uint amount);
                if (!success)
                {
                    throw new Exception("Couldn't get amount from query.Data!");
                }

                await UpdateAmountAsync(client, chatId, message, commandData, amount);
            }
        }

        internal override async Task HandleExceptionAsync(Exception exception, long chatId, ITelegramBotClient client)
        {
            bool success = ChatData.TryGetValue(chatId, out CustomCommandData data);
            if (success)
            {
                await data.Clear(client, chatId);
            }

            await base.HandleExceptionAsync(exception, chatId, client);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Update
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task UpdateLocalAsync(ChatId chatId, ITelegramBotClient client)
        {
            Message checkingMessage = await client.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.Markdown,
                disableNotification: true);

            List<PdfData> filesToUpdate = await Utils.CheckAsync(_sourceIds, CheckLocal);

            if (filesToUpdate.Any())
            {
                await client.FinalizeStatusMessageAsync(checkingMessage);
                Message updatingMessage = await client.SendTextMessageAsync(chatId, "_Обновляю…_", ParseMode.Markdown,
                    disableNotification: true);

                await Utils.CreateOrUpdateAsync(filesToUpdate, CreateOrUpdateLocal);

                await client.FinalizeStatusMessageAsync(updatingMessage);
            }
            else
            {
                await client.FinalizeStatusMessageAsync(checkingMessage, " Раздатки уже актуальны.");
            }
        }

        private Task<PdfData> CheckLocal(string id)
        {
            return Utils.CheckLocalPdfAsync(id, _googleDataManager, _pdfFolderPath);
        }

        private Task CreateOrUpdateLocal(PdfData data)
        {
            return Utils.CreateOrUpdateLocalAsync(data, _googleDataManager, _pdfFolderPath);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Select
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task SelectAsync(ChatId chatId, ITelegramBotClient client)
        {
            Message firstMessage = await client.SendTextMessageAsync(chatId, "Выбери раздатки:", ParseMode.Markdown,
                disableNotification: true);

            string[] paths = Directory.GetFiles(_pdfFolderPath);

            CustomCommandData data = await CreateOrClearDataAsync(client, chatId.Identifier);
            string last = paths.Last();

            data.AddMessage(firstMessage);

            foreach (string path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                data.AddPdf(name);

                bool isLast = path == last;
                InlineKeyboardMarkup keyboard = GetKeyboard(0, isLast);
                Message chatMessage =
                    await client.SendTextMessageAsync(chatId, name, disableNotification: !isLast, replyMarkup: keyboard);
                data.AddMessage(chatMessage);
            }
        }

        private static async Task<CustomCommandData> CreateOrClearDataAsync(ITelegramBotClient client, long chatId)
        {
            bool found = ChatData.TryGetValue(chatId, out CustomCommandData data);
            if (found)
            {
                await data.Clear(client, chatId);
            }
            else
            {
                data = new CustomCommandData();
            }

            ChatData.AddOrUpdate(chatId, data, (l, d) => d);
            return data;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GenerateAndSendAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task<bool> GenerateAndSendAsync(ITelegramBotClient client, long chatId, CustomCommandData data)
        {
            IReadOnlyList<DocumentRequest> requests = data.GetRequestedPdfs(_pdfFolderPath);
            if (!requests.Any())
            {
                await client.SendTextMessageAsync(chatId, "Ничего не выбрано!");
                return false;
            }

            Message unifyingMessage = await client.SendTextMessageAsync(chatId, "_Объединяю…_", ParseMode.Markdown,
                disableNotification: true);

            using (TempFile temp = DataManager.Unify(requests))
            {
                await client.FinalizeStatusMessageAsync(unifyingMessage);
                await client.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                using (var fileStream = new FileStream(temp.Path, FileMode.Open))
                {
                    var pdf = new InputOnlineFile(fileStream, "Раздатки.pdf");
                    await client.SendDocumentAsync(chatId, pdf);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateAmountAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Task<Message> UpdateAmountAsync(ITelegramBotClient client, long chatId, Message message,
            CustomCommandData data, uint amount)
        {
            data.UpdatePdfAmount(message.Text, amount);

            bool isLast = message.ReplyMarkup.InlineKeyboard.Count() == 2;
            InlineKeyboardMarkup keyboard = GetKeyboard(amount, isLast);
            return client.EditMessageReplyMarkupAsync(chatId, message.MessageId, keyboard);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GetKeyboard
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private InlineKeyboardMarkup GetKeyboard(uint amount, bool isLast)
        {
            IEnumerable<InlineKeyboardButton> amountRow = Amounts.Select(a => GetAmountButton(a, amount == a));
            if (!isLast)
            {
                return new InlineKeyboardMarkup(amountRow);
            }

            IEnumerable<InlineKeyboardButton> readyRow =
                new[] { InlineKeyboardButton.WithCallbackData("Готово!", $"{Name}") };
            var rows = new List<IEnumerable<InlineKeyboardButton>> { amountRow, readyRow };
            return new InlineKeyboardMarkup(rows);
        }

        private InlineKeyboardButton GetAmountButton(uint amount, bool selected)
        {
            string text = selected ? $"• {amount} •" : $"{amount}";
            string callBackData = $"{Name}{amount}";
            return InlineKeyboardButton.WithCallbackData(text, callBackData);
        }

        private static readonly uint[] Amounts = { 0, 1, 5, 10, 20 };

        private static readonly ConcurrentDictionary<long, CustomCommandData> ChatData =
            new ConcurrentDictionary<long, CustomCommandData>();

        private readonly List<string> _sourceIds;
        private readonly string _pdfFolderPath;
        private readonly DataManager _googleDataManager;
    }
}
