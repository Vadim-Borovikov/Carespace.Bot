using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers
{
    public sealed class UpdateController : Controller
    {
        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update, [FromServices]Models.Bot bot)
        {
            await bot.UpdateAsync(update);
            return Ok();
        }
    }
}
