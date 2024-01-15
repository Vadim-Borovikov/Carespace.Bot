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

        Transaction.DigisellerProductUrlFormat = _bot.Config.DigisellerProductUrlFormat;

        additionalConverters = new Dictionary<Type, Func<object?, object?>>(additionalConverters);

        GoogleSheetsManager.Documents.Document transactions = documentsManager.GetOrAdd(bot.Config.GoogleSheetIdTransactions);
        _allTransactions = transactions.GetOrAddSheet(_bot.Config.GoogleAllTransactionsTitle, additionalConverters);
        _customTransactions =
            transactions.GetOrAddSheet(_bot.Config.GoogleCustomTransactionsTitle, additionalConverters);
    }

    public Task ProcessSubmissionAsync(string name, MailAddress email, string telegram, List<string> items,
        List<Uri> slips)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<MailAddress>> LoadGoogleTransactionsAsync(Chat? chat, int? productIdForMails = null)
    {
        List<Transaction> transactions = new();

        StatusMessage? statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, new MessageTemplate("Загружаю покупки из таблицы"));

        List<Transaction> oldTransactions =
            await _allTransactions.LoadAsync<Transaction>(_bot.Config.GoogleAllTransactionsFinalRange);
        transactions.AddRange(oldTransactions);

        List<Transaction> newCustomTransactions =
            await _customTransactions.LoadAsync<Transaction>(_bot.Config.GoogleCustomTransactionsRange);
        transactions.AddRange(newCustomTransactions);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, new MessageTemplate("Считаю доли"));

        Calculator.CalculateShares(transactions, _bot.Shares);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, new MessageTemplate("Заношу покупки в таблицу"));

        List<Transaction> data = transactions.OrderBy(t => t.Date).ToList();
        await _allTransactions.SaveAsync(_bot.Config.GoogleAllTransactionsFinalRange, data,
            additionalSavers: AdditionalSavers);

        await _customTransactions.ClearAsync(_bot.Config.GoogleCustomTransactionsRangeToClear);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        return productIdForMails is null
            ? Enumerable.Empty<MailAddress>()
            : transactions.Where(t => t.DigisellerProductId == productIdForMails).Select(t => t.Email).SkipNulls();
    }

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers =
        WrapExtensions.WrapWithList(Transaction.Save);

    private readonly Bot _bot;

    private readonly Sheet _allTransactions;
    private readonly Sheet _customTransactions;
}