using System.Diagnostics;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using Carespace.Bot.Web.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly IBotService _botService;

        public HomeController(IBotService botService) { _botService = botService; }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            User model = await _botService.Client.GetMeAsync();
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
