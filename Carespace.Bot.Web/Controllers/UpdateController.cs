using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers
{
    public sealed class UpdateController : Controller
    {
        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update, [FromServices]BotSingleton singleton)
        {
            await singleton.Bot.UpdateAsync(update);
            return Ok();
        }
    }
}
