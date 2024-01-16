using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using GryphonUtilities.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Carespace.Bot.Web.Controllers;

[Route("[controller]")]
public class PurchaseController : ControllerBase
{
    public PurchaseController(IOptions<Config> config) => _config = config.Value;

    [HttpPost]
    public async Task<ActionResult> Post([FromServices] BotSingleton singleton, [FromForm] Submission model,
        [FromForm] IFormCollection form)
    {
        if (model.Test == TestString)
        {
            return Ok();
        }

        if ((model.FormId != _config.TildaFormId) || string.IsNullOrWhiteSpace(model.Name) || model.Email is null
            || string.IsNullOrWhiteSpace(model.Telegram) || string.IsNullOrWhiteSpace(model.Items))
        {
            return BadRequest(ModelState);
        }

        List<string> items = model.Items.Split(ItemsSeparator).Select(s => s.Trim()).ToList();

        List<Uri> slips = form.Where(p => p.Key.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase))
                              .SelectMany(p => p.Value)
                              .SkipNulls()
                              .Select(s => new Uri(s))
                              .ToList();
        if (slips.Count == 0)
        {
            return BadRequest(ModelState);
        }

        await singleton.Bot.OnSubmissionReceivedAsync(model.Name, new MailAddress(model.Email), model.Telegram, items,
            slips);

        return Ok();
    }

    private const string TestString = "test";
    private const string FilePrefix = "file";
    private const char ItemsSeparator = ';';

    private readonly Config _config;
}