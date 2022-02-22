using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class EmailChecker
{
    public EmailChecker(Bot bot) => _bot = bot;

    public async Task CheckEmailAsync(ChatId chatId, MailAddress email)
    {
        Message statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.MarkdownV2);
        bool found = await CheckEmailAsync(email);
        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        if (found)
        {
            await _bot.Client.SendTextMessageAsync(chatId, $"Email найден\\! Твой промокод: `{_bot.Config.BookPromo}`",
                ParseMode.MarkdownV2);
        }
        else
        {
            await _bot.Client.SendTextMessageAsync(chatId, "Email не найден! Напиши @Netris");
        }
    }

    private async Task<bool> CheckEmailAsync(MailAddress email)
    {
        int productId = _bot.Config.ProductId.GetValue(nameof(_bot.Config.ProductId));
        int digisellerId = _bot.Config.DigisellerId.GetValue(nameof(_bot.Config.DigisellerId));
        DateTime sellsStart = _bot.Config.SellsStart.GetValue(nameof(_bot.Config.SellsStart));

        List<int> productIds = new() { productId };
        DateTime finish = DateTime.Today.AddDays(1);
        string guid = _bot.Config.DigisellerApiGuid.GetValue(_bot.Config.DigisellerApiGuid);
        IEnumerable<string> eMails = await FinanceHelper.Utils.GetDigisellerSellsEmailsAsync(digisellerId, productIds,
            sellsStart, finish, guid);
        return eMails.Contains(email.Address, StringComparer.InvariantCultureIgnoreCase);
    }

    private readonly Bot _bot;
}