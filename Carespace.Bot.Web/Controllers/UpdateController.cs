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
            if ((update != null) && (update.Type == UpdateType.Message))
            {
                Message message = update.Message;

                if (message.Chat.Id != message.From.Id)
                {
                    await _bot.Client.DeleteMessageAsync(message.Chat, message.MessageId);
                }

                Command command = _bot.Commands.FirstOrDefault(c => c.Contains(message));
                if (command != null)
                {
                    await command.ExecuteAsync(message.From.Id, _bot.Client);
                }
            }

            return Ok();
        }

        private readonly IBot _bot;
    }
}
