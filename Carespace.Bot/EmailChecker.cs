using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class EmailChecker
{
    public EmailChecker(Bot bot, FinanceManager financeManager)
    {
        _bot = bot;
        _financeManager = financeManager;
    }

    public async Task CheckEmailAsync(ChatId chatId, MailAddress email)
    {
        Message statusMessage = await _bot.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.MarkdownV2);
        bool found = await CheckEmailAsync(email);
        await _bot.FinalizeStatusMessageAsync(statusMessage);
        if (found)
        {
            await _bot.SendTextMessageAsync(chatId, $"Email найден\\! Твой промокод: `{_bot.Config.BookPromo}`",
                ParseMode.MarkdownV2);
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, "Email не найден! Напиши @Netris");
        }
    }

    private async Task<bool> CheckEmailAsync(MailAddress email)
    {
        int productId = _bot.Config.ProductId.GetValue(nameof(_bot.Config.ProductId));
        IEnumerable<MailAddress> mailAddresses = await _financeManager.LoadTransactionEmailsAsync(productId);
        return mailAddresses.Select(m => m.Address)
                            .Contains(email.Address, StringComparer.InvariantCultureIgnoreCase);
    }

    private readonly Bot _bot;
    private readonly FinanceManager _financeManager;
}