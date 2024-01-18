using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models;
using GryphonUtilities;
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
        try
        {
            // Must await here, otherwise exception will not be catch
            return await Post(singleton.Bot, model, form);
        }
        catch (Exception e)
        {
            singleton.Bot.Logger.LogException(e);
            throw;
        }
    }

    private async Task<ActionResult> Post(Bot bot, Submission model, IFormCollection form)
    {
        LogForm(bot.Logger, form);

        if (model.Test == TestString)
        {
            return Ok();
        }

        if ((model.FormId != _config.TildaFormId) || string.IsNullOrWhiteSpace(model.TranId)
                                                  || string.IsNullOrWhiteSpace(model.Name)
                                                  || string.IsNullOrWhiteSpace(model.Email)
                                                  || string.IsNullOrWhiteSpace(model.Telegram)
                                                  || string.IsNullOrWhiteSpace(model.Items))
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

        await bot.OnSubmissionReceivedAsync(model.TranId, model.Name, model.Email, model.Telegram, items, slips);

        return Ok();
    }

    private static void LogForm(Logger logger, IFormCollection form)
    {
        StringBuilder sb = new();
        sb.AppendLine("Webhook form received:");
        foreach (string key in form.Keys)
        {
            sb.AppendLine($"\t{key}: {form[key]}");
        }
        logger.LogTimedMessage(sb.ToString());
    }

    private const string TestString = "test";
    private const string FilePrefix = "file";
    private const char ItemsSeparator = ';';

    private readonly Config _config;
}