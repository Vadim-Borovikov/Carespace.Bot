using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using UAParser;

namespace Carespace.Bot.Web.Controllers
{
    [Route("calendar")]
    public sealed class CalendarController : Controller
    {
        [HttpGet("{id}")]
        public IActionResult GetCalendar(int id, [FromServices]BotSingleton singleton)
        {
            if (!singleton.Bot.Calendars.ContainsKey(id) || singleton.Bot.Calendars[id].IsOver)
            {
                return Redirect(singleton.Bot.Config.ErrorPageUrl);
            }

            if (IsApple())
            {
                return File(singleton.Bot.Calendars[id].IcsContent, ContentType);
            }

            return Redirect(singleton.Bot.Calendars[id].GoogleCalendarLink);
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
