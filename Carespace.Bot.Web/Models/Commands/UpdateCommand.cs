using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FileInfo = GoogleDocumentsUnifier.Logic.FileInfo;
using File = System.IO.File;

namespace Carespace.Bot.Web.Models.Commands
{
    internal class UpdateCommand : Command
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

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool _)
        {
            Message checkingMessage = await client.SendTextMessageAsync(message.Chat, "_Проверяю…_",
                ParseMode.Markdown, disableNotification: true);

            await UpdateLocalAsync();

            await UpdateGoogleAsync(checkingMessage, client);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateLocalAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task UpdateLocalAsync()
        {
            List<PdfData> filesToUpdate = await Utils.CheckAsync(_sourceIds, CheckLocalAsync);

            if (filesToUpdate.Any())
            {
                await Utils.CreateOrUpdateAsync(filesToUpdate, CreateOrUpdateLocalAsync);
            }
        }

        private Task<PdfData> CheckLocalAsync(string id)
        {
            return Utils.CheckLocalPdfAsync(id, _googleDataManager, _pdfFolderPath);
        }

        private Task CreateOrUpdateLocalAsync(PdfData data)
        {
            return Utils.CreateOrUpdateLocalAsync(data, _googleDataManager, _pdfFolderPath);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // UpdateGoogleAsync
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task UpdateGoogleAsync(Message checkingMessage, ITelegramBotClient client)
        {
            IEnumerable<string> sources = Directory.EnumerateFiles(_pdfFolderPath);
            List<PdfData> filesToUpdate = await Utils.CheckAsync(sources, CheckGooglePdfAsync);

            if (filesToUpdate.Any())
            {
                await Utils.FinalizeStatusMessageAsync(checkingMessage, client);

                Message updatingMessage = await client.SendTextMessageAsync(checkingMessage.Chat, "_Обновляю…_",
                    ParseMode.Markdown, disableNotification: true);

                await Utils.CreateOrUpdateAsync(filesToUpdate, CreateOrUpdateGoogleAsync);

                await Utils.FinalizeStatusMessageAsync(updatingMessage, client);
            }
            else
            {
                await Utils.FinalizeStatusMessageAsync(checkingMessage, client, " Раздатки уже актуальны.");
            }
        }

        private async Task<PdfData> CheckGooglePdfAsync(string path)
        {
            string pdfName = Path.GetFileName(path);
            FileInfo pdfInfo = await _googleDataManager.FindFileInFolderAsync(_pdfFolderId, pdfName);

            if (pdfInfo == null)
            {
                return PdfData.CreateNoneGoogle(path);
            }

            if (pdfInfo.ModifiedTime < File.GetLastWriteTime(path))
            {
                return PdfData.CreateOutdatedGoogle(path, pdfInfo.Id);
            }

            return PdfData.CreateOk();
        }

        private async Task CreateOrUpdateGoogleAsync(PdfData data)
        {
            switch (data.Status)
            {
                case PdfData.FileStatus.None:
                    await _googleDataManager.CreateAsync(data.Name, _pdfFolderId, data.Path);
                    break;
                case PdfData.FileStatus.Outdated:
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
