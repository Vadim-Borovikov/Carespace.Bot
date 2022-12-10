using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Carespace.Bot.Web.Controllers;

[Route("")]
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index([FromServices] BotSingleton singleton) => View(singleton.Bot.User);
}
