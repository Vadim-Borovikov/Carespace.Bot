using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.FinanceHelper;
using Carespace.FinanceHelper.Data.PayMaster;
using GoogleSheetsManager;
using GoogleSheetsManager.Documents;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;

namespace Carespace.Bot;

internal sealed class FinanceManager
{
    public FinanceManager(Bot bot, DocumentsManager manager,
        Dictionary<Type, Func<object?, object?>> additionalConverters)
    {
        _bot = bot;

        FinanceHelper.PayMaster.Manager.PaymentUrlFormat = _bot.Config.PayMasterPaymentUrlFormat;

        Transaction.DigisellerSellUrlFormat = _bot.Config.DigisellerSellUrlFormat;
        Transaction.DigisellerProductUrlFormat = _bot.Config.DigisellerProductUrlFormat;

        Transaction.TaxPayerId = _bot.Config.TaxPayerId;

        additionalConverters = new Dictionary<Type, Func<object?, object?>>(additionalConverters);
        additionalConverters[typeof(Transaction.PayMethod)] = additionalConverters[typeof(Transaction.PayMethod?)] =
            o => o.ToPayMathod();

        GoogleSheetsManager.Documents.Document transactions = manager.GetOrAdd(bot.Config.GoogleSheetIdTransactions);
        _allTransactions = transactions.GetOrAddSheet(_bot.Config.GoogleAllTransactionsTitle, additionalConverters);
        _customTransactions =
            transactions.GetOrAddSheet(_bot.Config.GoogleCustomTransactionsTitle, additionalConverters);
    }

    public async Task UpdateFinances(Chat chat)
    {
        await _bot.SendTextMessageAsync(chat, "Обновляю покупки…");

        await LoadGoogleTransactionsAsync(chat);

        await _bot.SendTextMessageAsync(chat, "…покупки обновлены.");
    }

    public async Task<IEnumerable<MailAddress>> LoadGoogleTransactionsAsync(Chat? chat, int? productIdForMails = null)
    {
        List<Transaction> transactions = new();

        StatusMessage? statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, "Загружаю покупки из таблицы");

        SheetData<Transaction> oldTransactions =
            await _allTransactions.LoadAsync<Transaction>(_bot.Config.GoogleAllTransactionsFinalRange);
        transactions.AddRange(oldTransactions.Instances);

        SheetData<Transaction> newCustomTransactions =
            await _customTransactions.LoadAsync<Transaction>(_bot.Config.GoogleCustomTransactionsRange);
        transactions.AddRange(newCustomTransactions.Instances);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, "Загружаю покупки из Digiseller");

        DateOnly dateStart = transactions.Select(o => o.Date).Min().AddDays(-1);
        DateOnly dateEnd = _bot.TimeManager.Now().DateOnly.AddDays(1);

        List<int> productIds = _bot.Shares.Keys.Where(k => k != "None").Select(int.Parse).ToList();

        List<Transaction> newSells =
            await FinanceHelper.Digiseller.Manager.GetNewSellsAsync(_bot.Config.DigisellerLogin,
                _bot.Config.DigisellerPassword, _bot.Config.DigisellerId, productIds, dateStart, dateEnd,
                _bot.Config.DigisellerApiGuid, oldTransactions.Instances, _bot.TimeManager,
                _bot.JsonSerializerOptionsProvider.SnakeCaseOptions);

        transactions.AddRange(newSells);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, "Считаю доли");

        Calculator.CalculateShares(transactions, _bot.Config.TaxFeePercent, _bot.Config.DigisellerFeePercent,
            _bot.Config.PayMasterFeePercents, _bot.Shares);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        List<Transaction> needPayment = transactions.Where(t => t.NeedPaynemt).ToList();
        if (needPayment.Any())
        {
            statusMessage = chat is null
                ? null
                : await StatusMessage.CreateAsync(_bot, chat, "Загружаю платежи");

            dateStart = needPayment.Select(o => o.Date).Min();
            dateEnd = needPayment.Select(o => o.Date).Max().AddDays(1);
            List<PaymentsResult.Item> payments =
                await FinanceHelper.PayMaster.Manager.GetPaymentsAsync(_bot.Config.PayMasterMerchantIdDigiseller,
                    dateStart, dateEnd, _bot.Config.PayMasterToken,
                    _bot.JsonSerializerOptionsProvider.CamelCaseOptions);

            foreach (Transaction transaction in needPayment)
            {
                FinanceHelper.PayMaster.Manager.FindPayment(transaction, payments,
                    _bot.Config.PayMasterPurposesFormats);
            }

            if (statusMessage is not null)
            {
                await statusMessage.DisposeAsync();
            }
        }

        statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, "Заношу покупки в таблицу");

        SheetData<Transaction> data = new(transactions.OrderBy(t => t.Date).ToList(), oldTransactions.Titles);
        await _allTransactions.SaveAsync(_bot.Config.GoogleAllTransactionsFinalRange, data, AdditionalSavers);

        await _customTransactions.ClearAsync(_bot.Config.GoogleCustomTransactionsRangeToClear);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        return productIdForMails is null
            ? Enumerable.Empty<MailAddress>()
            : transactions.Where(t => t.DigisellerProductId == productIdForMails).Select(t => t.Email).RemoveNulls();
    }

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers =
        WrapExtensions.WrapWithList(Transaction.Save);

    private readonly Bot _bot;

    private readonly Sheet _allTransactions;
    private readonly Sheet _customTransactions;
}