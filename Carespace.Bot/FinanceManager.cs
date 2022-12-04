using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.FinanceHelper;
using Carespace.FinanceHelper.Data.PayMaster;
using GoogleSheetsManager;
using GoogleSheetsManager.Providers;
using GryphonUtilities;
using Telegram.Bot.Types;

namespace Carespace.Bot;

internal sealed class FinanceManager
{
    public FinanceManager(Bot bot)
    {
        _bot = bot;

        FinanceHelper.Utils.PayMasterPaymentUrlFormat = _bot.Config.PayMasterPaymentUrlFormat;

        Transaction.DigisellerSellUrlFormat = _bot.Config.DigisellerSellUrlFormat;
        Transaction.DigisellerProductUrlFormat = _bot.Config.DigisellerProductUrlFormat;

        Transaction.TaxPayerId = _bot.Config.TaxPayerId;

        _additionalConverters = _bot.AdditionalConverters.ToDictionary(p => p.Key, p => p.Value);
        _additionalConverters[typeof(Transaction.PayMethod)] = _additionalConverters[typeof(Transaction.PayMethod?)] =
            o => o.ToPayMathod();
    }

    public async Task UpdateFinances(Chat chat)
    {
        await _bot.SendTextMessageAsync(chat, "Обновляю покупки…");

        await UpdatePurchasesAsync(chat);

        await _bot.SendTextMessageAsync(chat, "…покупки обновлены.");

        await _bot.SendTextMessageAsync(chat, "Обновляю донатики…");

        await UpdateDonationsAsync(chat);

        await _bot.SendTextMessageAsync(chat, "…донатики обновлены.");
    }

    public async Task<IEnumerable<MailAddress>> LoadTransactionEmailsAsync(int productId)
    {
        using (SheetsProvider provider = new(_bot.Config, _bot.Config.GoogleSheetIdTransactions))
        {
            IEnumerable<MailAddress> emails = await LoadGoogleTransactionsAsync(provider, null, productId);
            return emails;
        }
    }

    private async Task UpdatePurchasesAsync(Chat chat)
    {
        using (SheetsProvider provider = new(_bot.Config, _bot.Config.GoogleSheetIdTransactions))
        {
            await LoadGoogleTransactionsAsync(provider, chat);
        }
    }

    private async Task UpdateDonationsAsync(Chat chat)
    {
        using (SheetsProvider provider = new(_bot.Config, _bot.Config.GoogleSheetIdDonations))
        {
            await UpdateDonationsAsync(chat, provider);
        }
    }

    private async Task<IEnumerable<MailAddress>> LoadGoogleTransactionsAsync(SheetsProvider provider, Chat? chat,
        int? productIdForMails = null)
    {
        List<Transaction> transactions = new();

        StatusMessage? statusMessage = chat is null
            ? null
            : await StatusMessage.CreateAsync(_bot, chat, "Загружаю покупки из таблицы");

        SheetData<Transaction> oldTransactions = await DataManager<Transaction>.LoadAsync(provider,
            _bot.Config.GoogleTransactionsFinalRange, additionalConverters: _additionalConverters);
        transactions.AddRange(oldTransactions.Instances);

        SheetData<Transaction> newCustomTransactions = await DataManager<Transaction>.LoadAsync(provider,
            _bot.Config.GoogleTransactionsCustomRange, additionalConverters: _additionalConverters);
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

        List<Transaction> newSells = await FinanceHelper.Utils.GetNewDigisellerSellsAsync(_bot.Config.DigisellerLogin,
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

        FinanceHelper.Utils.CalculateShares(transactions, _bot.Config.TaxFeePercent, _bot.Config.DigisellerFeePercent,
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
                await FinanceHelper.Utils.GetPaymentsAsync(_bot.Config.PayMasterMerchantIdDigiseller, dateStart,
                    dateEnd, _bot.Config.PayMasterToken, _bot.JsonSerializerOptionsProvider.CamelCaseOptions);

            foreach (Transaction transaction in needPayment)
            {
                FinanceHelper.Utils.FindPayment(transaction, payments, _bot.Config.PayMasterPurposesFormats);
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
        await DataManager<Transaction>.SaveAsync(provider, _bot.Config.GoogleTransactionsFinalRange, data,
            AdditionalSavers);

        await provider.ClearValuesAsync(_bot.Config.GoogleTransactionsCustomRangeToClear);

        if (statusMessage is not null)
        {
            await statusMessage.DisposeAsync();
        }

        return productIdForMails is null
            ? Enumerable.Empty<MailAddress>()
            : transactions.Where(t => t.DigisellerProductId == productIdForMails).Select(t => t.Email).RemoveNulls();
    }

    private async Task UpdateDonationsAsync(Chat chat, SheetsProvider provider)
    {
        List<Donation> donations = new();

        SheetData<Donation> oldDonations;
        await using (await StatusMessage.CreateAsync(_bot, chat, "Загружаю донаты из таблицы"))
        {
            oldDonations = await DataManager<Donation>.LoadAsync(provider,
                _bot.Config.GoogleDonationsRange, additionalConverters: _additionalConverters);
            donations.AddRange(oldDonations.Instances);

            SheetData<Donation> newCustomDonations = await DataManager<Donation>.LoadAsync(provider,
                _bot.Config.GoogleDonationsCustomRange, additionalConverters: _additionalConverters);
            donations.AddRange(newCustomDonations.Instances);
        }

        await using (await StatusMessage.CreateAsync(_bot, chat, "Загружаю платежи"))
        {
            DateOnly dateStart = donations.Select(o => o.Date).Min().AddDays(-1);
            DateOnly dateEnd = _bot.TimeManager.Now().DateOnly.AddDays(1);

            List<Donation> newDonations =
                await FinanceHelper.Utils.GetNewPayMasterPaymentsAsync(_bot.Config.PayMasterMerchantIdDonations,
                    dateStart, dateEnd, _bot.Config.PayMasterToken, oldDonations.Instances,
                    _bot.JsonSerializerOptionsProvider.CamelCaseOptions);

            donations.AddRange(newDonations);
        }

        DateOnly firstThursday = Utils.GetNextThursday(donations.Min(d => d.Date));

        FinanceHelper.Utils.CalculateTotalsAndWeeks(donations, _bot.Config.PayMasterFeePercents, firstThursday);

        await using (await StatusMessage.CreateAsync(_bot, chat, "Заношу донаты в таблицу"))
        {
            SheetData<Donation> data = new(donations.OrderByDescending(d => d.Date).ToList(), oldDonations.Titles);
            await DataManager<Donation>.SaveAsync(provider, _bot.Config.GoogleDonationsRange, data);
            await provider.ClearValuesAsync(_bot.Config.GoogleDonationsCustomRangeToClear);
        }

        await using (await StatusMessage.CreateAsync(_bot, chat, "Считаю и заношу недельные суммы"))
        {
            List<DonationsSum> sums = donations.GroupBy(d => d.Week)
                                               .Select(g => new DonationsSum(firstThursday, g.Key, g.Sum(d => d.Total)))
                                               .ToList();
            List<string> titles =
                await GoogleSheetsManager.Utils.LoadTitlesAsync(provider, _bot.Config.GoogleDonationSumsRange);
            SheetData<DonationsSum> sumsData = new(sums.OrderByDescending(s => s.Date).ToList(), titles);
            await DataManager<DonationsSum>.SaveAsync(provider, _bot.Config.GoogleDonationSumsRange, sumsData);
        }
    }

    private readonly Dictionary<Type, Func<object?, object?>> _additionalConverters;

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers =
        EnumerableHelper.WrapWithList(Transaction.Save);

    private readonly Bot _bot;
}