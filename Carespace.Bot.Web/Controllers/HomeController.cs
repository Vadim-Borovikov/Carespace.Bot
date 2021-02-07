using System.Diagnostics;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers
{
    [Route("")]
    public sealed class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index([FromServices]Models.Bot bot)
        {
            User model = await bot.GetUserAsunc();
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            return View(model);
        }
    }
}
