using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
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
            if (command != null)
            {
                if (fromChat)
                {
                    await _bot.Client.DeleteMessageAsync(message.Chat, message.MessageId);
                }

                await command.ExecuteAsync(message.From.Id, _bot.Client);
                return;
            }

            if (!fromChat)
            {
                await _bot.Client.SendStickerAsync(message, _dontUnderstandSticker);
            }
        }

        private readonly IBot _bot;
        private readonly InputOnlineFile _dontUnderstandSticker;
    }
}
