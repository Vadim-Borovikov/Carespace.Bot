using AbstractBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Email;

internal sealed class Checker
{
    public Checker(Bot bot, FinanceManager financeManager)
    {
        _bot = bot;
        _financeManager = financeManager;
    }

    public async Task CheckEmailAsync(Chat chat, MailAddress email)
    {
        bool found;
        await using (await StatusMessage.CreateAsync(_bot, chat, "Проверяю"))
        {
            found = await CheckEmailAsync(email);
        }
        if (found)
        {
            await _bot.SendTextMessageAsync(chat, $"Email найден\\! Твой промокод: `{_bot.Config.BookPromo}`",
                ParseMode.MarkdownV2);
        }
        else
        {
            await _bot.SendTextMessageAsync(chat, "Email не найден! Напиши @Netris");
        }
    }

    private async Task<bool> CheckEmailAsync(MailAddress email)
    {
        IEnumerable<MailAddress> mailAddresses =
            await _financeManager.LoadGoogleTransactionsAsync(null, _bot.Config.ProductId);
        return mailAddresses.Select(m => m.Address)
                            .Contains(email.Address, StringComparer.InvariantCultureIgnoreCase);
    }

    private readonly Bot _bot;
    private readonly FinanceManager _financeManager;
}