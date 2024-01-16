using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Configs;
using Carespace.FinanceHelper;
using GoogleSheetsManager.Documents;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;

namespace Carespace.Bot;

internal sealed class FinanceManager
{
    public FinanceManager(Bot bot, Manager documentsManager,
        Dictionary<Type, Func<object?, object?>> additionalConverters)
    {
        _bot = bot;

        additionalConverters = new Dictionary<Type, Func<object?, object?>>(additionalConverters);

        GoogleSheetsManager.Documents.Document transactions =
            documentsManager.GetOrAdd(bot.Config.GoogleSheetIdTransactions);
        _transactions = transactions.GetOrAddSheet(_bot.Config.GoogleAllTransactionsTitle, additionalConverters);
    }

    public async Task AddTransactionsAsync(Chat chat, List<Transaction> transactions)
    {
        await using (await StatusMessage.CreateAsync(_bot, chat, new MessageTemplate("Считаю доли")))
        {
            Calculator.CalculateShares(transactions, _bot.Config.Products);
        }

        await using (await StatusMessage.CreateAsync(_bot, chat, new MessageTemplate("Заношу покупки в таблицу")))
        {
            transactions = transactions.OrderBy(t => t.Date).ToList();
            await _transactions.AddAsync(_bot.Config.GoogleAllTransactionsFinalRange, transactions,
                additionalSavers: AdditionalSavers);
        }
    }

    public async Task<IEnumerable<MailAddress>> LoadEmailsWithAsync(byte productIdForMails)
    {
        List<Transaction> transactions =
            await _transactions.LoadAsync<Transaction>(_bot.Config.GoogleAllTransactionsFinalRange);

        return transactions.Where(t => t.ProductId == productIdForMails).Select(t => t.Email).SkipNulls();
    }

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers =
        WrapExtensions.WrapWithList(Transaction.Save);

    private readonly Bot _bot;
    private readonly Sheet _transactions;
}