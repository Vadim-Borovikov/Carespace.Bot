﻿using Microsoft.AspNetCore.Mvc;
using UAParser;

namespace Carespace.Bot.Web.Controllers
{
    [Route("calendar")]
    public sealed class CalendarController : Controller
    {
        [HttpGet("{id}")]
        public IActionResult GetCalendar(int id)
        {
            if (IsApple())
            {
                return File(Utils.Calendars[id].IcsContent, ContentType);
            }

            return Redirect(Utils.Calendars[id].GoogleCalendarLink);
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