using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Carespace.Bot.Web.Controllers
{
    public sealed class UpdateController : Controller
    {
        public UpdateController(IBot bot)
        {
            _bot = bot;
            _dontUnderstandSticker = new InputOnlineFile(_bot.Config.DontUnderstandStickerFileId);
            _forbiddenSticker = new InputOnlineFile(_bot.Config.ForbiddenStickerFileId);
        }

        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            await ProcessAsync(update);
            return Ok();
        }

        private async Task ProcessAsync(Update update)
        {
            if (update?.Type != UpdateType.Message)
            {
                return;
            }

            Message message = update.Message;
            bool fromChat = message.Chat.Id != message.From.Id;
            string botName = fromChat ? await _bot.Client.GetNameAsync() : null;
            Command command = _bot.Commands.FirstOrDefault(c => c.IsInvokingBy(message, fromChat, botName));

            if (command == null)
            {
                if (!fromChat)
                {
                    await _bot.Client.SendStickerAsync(message, _dontUnderstandSticker);
                }
                return;
            }

            if (fromChat)
            {
                try
                {
                    await _bot.Client.DeleteMessageAsync(message.Chat, message.MessageId);
                }
                catch (ApiRequestException e)
                    when ((e.ErrorCode == MessageToDeleteNotFoundCode)
                            && (e.Message == MessageToDeleteNotFoundText))
                {
                    return;
                }
            }

            if (command.AdminsOnly)
            {
                bool isAdmin = _bot.AdminIds.Contains(message.From.Id);
                if (!isAdmin)
                {
                    if (!fromChat)
                    {
                        await _bot.Client.SendStickerAsync(message, _forbiddenSticker);
                    }
                    return;
                }
            }

            try
            {
                await command.ExecuteAsync(message.From.Id, _bot.Client);
            }
            catch (ApiRequestException e)
                when ((e.ErrorCode == CantInitiateConversationCode) && (e.Message == CantInitiateConversationText))
            {
            }
        }

        private readonly IBot _bot;
        private readonly InputOnlineFile _dontUnderstandSticker;
        private readonly InputOnlineFile _forbiddenSticker;
        private const int MessageToDeleteNotFoundCode = 400;
        private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
        private const int CantInitiateConversationCode = 403;
        private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
    }
}
