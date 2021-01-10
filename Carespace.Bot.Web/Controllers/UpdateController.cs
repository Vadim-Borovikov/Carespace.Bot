using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Controllers
{
    public sealed class UpdateController : Controller
    {
        public UpdateController(IBot bot) => _bot = bot;

        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            if (update?.Type == UpdateType.Message)
            {
                Message message = update.Message;

                string botName = null;
                bool fromChat = message.Chat.Id != message.From.Id;
                if (fromChat)
                {
                    User me = await _bot.Client.GetMeAsync();
                    botName = me.Username;
                }

                Command command = _bot.Commands.FirstOrDefault(c => c.IsInvokingBy(message, fromChat, botName));
                if (command != null)
                {
                    if (fromChat)
                    {
                        await _bot.Client.DeleteMessageAsync(message.Chat, message.MessageId);
                    }

                    await command.ExecuteAsync(message.From.Id, _bot.Client);
                }
            }

            return Ok();
        }

        private readonly IBot _bot;
    }
}
