using Microsoft.AspNetCore.Mvc;

namespace Carespace.Bot.Web.Controllers
{
    [Route("ics")]
    public sealed class IcsController : Controller
    {
        [HttpGet("{id}")]
        public IActionResult GetIcs(int id)
        {
            string path = string.Format(Utils.IcsPathFormat, id);
            byte[] content = System.IO.File.ReadAllBytes(path);
            return File(content, ContentType);
        }

        private const string ContentType = "text/calendar";
    }
}
