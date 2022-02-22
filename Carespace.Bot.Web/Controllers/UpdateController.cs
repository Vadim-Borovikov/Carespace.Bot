using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers;

public sealed class UpdateController : Controller
{
    public async Task<OkResult> Post([FromServices] BotSingleton singleton, [FromBody] Update update)
    {
        await singleton.Bot.UpdateAsync(update);
        return Ok();
    }
}
