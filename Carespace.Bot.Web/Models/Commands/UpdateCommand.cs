﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Pdf;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FileInfo = GoogleDocumentsUnifier.Logic.FileInfo;
using File = System.IO.File;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class UpdateCommand : Command
    {
        internal override string Name => "update";
        internal override string Description => "обновить раздатки на Диске";

        public UpdateCommand(IEnumerable<string> sourceIds, string pdfFolderId, string pdfFolderPath,
            DataManager googleDataManager)
        {
            _sourceIds = sourceIds;
            _pdfFolderId = pdfFolderId;
            _pdfFolderPath = pdfFolderPath;
            _googleDataManager = googleDataManager;
        }

        protected override async Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool _)
        {
            Message checkingMessage = await client.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.Markdown,
                disableNotification: true);

            await UpdateLocalAsync();

            await UpdateGoogleAsync(checkingMessage, client);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateLocalAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task UpdateLocalAsync()
        {
            List<Data> filesToUpdate = await Utils.CheckAsync(_sourceIds, CheckLocalAsync);

            if (filesToUpdate.Any())
            {
                await Utils.CreateOrUpdateAsync(filesToUpdate, CreateOrUpdateLocalAsync);
            }
        }

        private Task<Data> CheckLocalAsync(string id)
        {
            return Utils.CheckLocalPdfAsync(id, _googleDataManager, _pdfFolderPath);
        }

        private Task CreateOrUpdateLocalAsync(Data data)
        {
            return Utils.CreateOrUpdateLocalAsync(data, _googleDataManager, _pdfFolderPath);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateGoogleAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task UpdateGoogleAsync(Message checkingMessage, ITelegramBotClient client)
        {
            IEnumerable<string> sources = Directory.EnumerateFiles(_pdfFolderPath);
            List<Data> filesToUpdate = await Utils.CheckAsync(sources, CheckGooglePdfAsync);

            if (filesToUpdate.Any())
            {
                await client.FinalizeStatusMessageAsync(checkingMessage);

                Message updatingMessage = await client.SendTextMessageAsync(checkingMessage.Chat, "_Обновляю…_",
                    ParseMode.Markdown, disableNotification: true);

                await Utils.CreateOrUpdateAsync(filesToUpdate, CreateOrUpdateGoogleAsync);

                await client.FinalizeStatusMessageAsync(updatingMessage);
            }
            else
            {
                await client.FinalizeStatusMessageAsync(checkingMessage, " Раздатки уже актуальны.");
            }
        }

        private async Task<Data> CheckGooglePdfAsync(string path)
        {
            string pdfName = Path.GetFileName(path);
            FileInfo pdfInfo = await _googleDataManager.FindFileInFolderAsync(_pdfFolderId, pdfName);

            if (pdfInfo == null)
            {
                return Data.CreateNoneGoogle(path);
            }

            if (pdfInfo.ModifiedTime < File.GetLastWriteTime(path))
            {
                return Data.CreateOutdatedGoogle(path, pdfInfo.Id);
            }

            return Data.CreateOk();
        }

        private async Task CreateOrUpdateGoogleAsync(Data data)
        {
            switch (data.Status)
            {
                case Data.FileStatus.None:
                    await _googleDataManager.CreateAsync(data.Name, _pdfFolderId, data.Path);
                    break;
                case Data.FileStatus.Outdated:
                    await _googleDataManager.UpdateAsync(data.Id, data.Path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data.Status), data.Status,
                        "Unexpected Pdf status!");
            }
        }

        private readonly IEnumerable<string> _sourceIds;
        private readonly string _pdfFolderId;
        private readonly string _pdfFolderPath;
        private readonly DataManager _googleDataManager;
    }
}
