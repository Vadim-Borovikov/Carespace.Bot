using AbstractBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot.Configs;
using Telegram.Bot.Types;

namespace Carespace.Bot;

internal sealed class EmailChecker
{
    public EmailChecker(Bot bot, FinanceManager financeManager)
    {
        _bot = bot;
        _financeManager = financeManager;
    }

    public async Task CheckEmailAsync(Chat chat, MailAddress email)
    {
        bool found;
        await using (await StatusMessage.CreateAsync(_bot, chat, _bot.Config.Texts.CheckingEmail))
        {
            found = await CheckEmailAsync(email);
        }

        MessageTemplate formatted = found
            ? _bot.Config.Texts.EmailFoundFormat.Format(_bot.Config.BookPromo)
            : _bot.Config.Texts.EmailNotFoundFormat.Format(_bot.Config.Texts.EmailNotFoundHelp);

        await formatted.SendAsync(_bot, chat);
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