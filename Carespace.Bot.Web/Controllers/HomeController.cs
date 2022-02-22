using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers;

[Route("")]
public sealed class HomeController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromServices] BotSingleton singleton)
    {
        User model = await singleton.Bot.GetUserAsync();
        return View(model);
    }
}
