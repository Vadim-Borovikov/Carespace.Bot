using System.Diagnostics;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Carespace.Bot.Web.Models.Bot;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers
{
    [Route("")]
    public sealed class HomeController : Controller
    {
        private readonly IBot _bot;

        public HomeController(IBot bot) { _bot = bot; }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            User model = await _bot.Client.GetMeAsync();
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
