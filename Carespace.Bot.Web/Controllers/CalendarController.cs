using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using UAParser;

namespace Carespace.Bot.Web.Controllers
{
    [Route("calendar")]
    public sealed class CalendarController : Controller
    {
        private readonly IBot _bot;

        public CalendarController(IBot bot) => _bot = bot;

        [HttpGet("{id}")]
        public IActionResult GetCalendar(int id)
        {
            if (IsApple())
            {
                return File(_bot.Calendars[id].IcsContent, ContentType);
            }

            return Redirect(_bot.Calendars[id].GoogleCalendarLink);
        }

        private bool IsApple()
        {
            string userAgent = Request.Headers["User-Agent"].ToString();
            Parser uaParser = Parser.GetDefault();
            ClientInfo clientInfo = uaParser.Parse(userAgent);
            return (clientInfo.OS.Family == "iOS") || clientInfo.OS.Family.Contains("Mac OS");
        }

        private const string ContentType = "text/calendar";
    }
}
