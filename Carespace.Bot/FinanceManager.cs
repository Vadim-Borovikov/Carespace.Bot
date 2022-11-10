using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Carespace.FinanceHelper;
using Carespace.FinanceHelper.Data.PayMaster;
using GoogleSheetsManager;
using GoogleSheetsManager.Providers;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        using (SheetsProvider provider =
               new(_bot.GoogleCredentialJson, ApplicationName, _bot.Config.GoogleSheetIdTransactions))
        {
            IEnumerable<MailAddress> emails = await LoadGoogleTransactionsAsync(provider, null, productId);
            return emails;
        }
    }

    private async Task UpdatePurchasesAsync(Chat chat)
    {
        using (SheetsProvider provider =
               new(_bot.GoogleCredentialJson, ApplicationName, _bot.Config.GoogleSheetIdTransactions))
        {
            await LoadGoogleTransactionsAsync(provider, chat);
        }
    }

    private async Task UpdateDonationsAsync(Chat chat)
    {
        using (SheetsProvider provider =
               new(_bot.GoogleCredentialJson, ApplicationName, _bot.Config.GoogleSheetIdDonations))
        {
            await UpdateDonationsAsync(chat, provider);
        }
    }

    private async Task<IEnumerable<MailAddress>> LoadGoogleTransactionsAsync(SheetsProvider provider, Chat? chat,
        int? productIdForMails = null)
    {
        List<Transaction> transactions = new();

        Message? statusMessage = chat is null
            ? null
            : await _bot.SendTextMessageAsync(chat, "_Загружаю покупки из таблицы…_", ParseMode.MarkdownV2);

        SheetData<Transaction> oldTransactions = await DataManager<Transaction>.LoadAsync(provider,
            _bot.Config.GoogleTransactionsFinalRange, additionalConverters: AdditionalConverters);
        transactions.AddRange(oldTransactions.Instances);

        SheetData<Transaction> newCustomTransactions = await DataManager<Transaction>.LoadAsync(provider,
            _bot.Config.GoogleTransactionsCustomRange, additionalConverters: AdditionalConverters);
        transactions.AddRange(newCustomTransactions.Instances);

        if (statusMessage is not null)
        {
            await _bot.FinalizeStatusMessageAsync(statusMessage);
        }

        statusMessage = chat is null
            ? null
            : await _bot.SendTextMessageAsync(chat, "_Загружаю покупки из Digiseller…_", ParseMode.MarkdownV2);

        DateTime dateStart = transactions.Select(o => o.Date).Min().AddDays(-1);
        DateTime dateEnd = DateTime.Today.AddDays(1);

        List<int> productIds = _bot.Shares.Keys.Where(k => k != "None").Select(int.Parse).ToList();

        List<Transaction> newSells = await FinanceHelper.Utils.GetNewDigisellerSellsAsync(_bot.Config.DigisellerLogin,
            _bot.Config.DigisellerPassword, _bot.Config.DigisellerId, productIds, dateStart, dateEnd,
            _bot.Config.DigisellerApiGuid, oldTransactions.Instances);

        transactions.AddRange(newSells);

        if (statusMessage is not null)
        {
            await _bot.FinalizeStatusMessageAsync(statusMessage);
        }

        statusMessage = chat is null
            ? null
            : await _bot.SendTextMessageAsync(chat, "_Считаю доли…_", ParseMode.MarkdownV2);

        FinanceHelper.Utils.CalculateShares(transactions, _bot.Config.TaxFeePercent, _bot.Config.DigisellerFeePercent,
            _bot.Config.PayMasterFeePercents, _bot.Shares);

        if (statusMessage is not null)
        {
            await _bot.FinalizeStatusMessageAsync(statusMessage);
        }

        List<Transaction> needPayment = transactions.Where(t => t.NeedPaynemt).ToList();
        if (needPayment.Any())
        {
            statusMessage = chat is null
                ? null
                : await _bot.SendTextMessageAsync(chat, "_Загружаю платежи…_", ParseMode.MarkdownV2);

            dateStart = needPayment.Select(o => o.Date).Min();
            dateEnd = needPayment.Select(o => o.Date).Max().AddDays(1);
            List<PaymentsResult.Item> payments =
                await FinanceHelper.Utils.GetPaymentsAsync(_bot.Config.PayMasterMerchantIdDigiseller, dateStart,
                    dateEnd, _bot.Config.PayMasterToken);

            foreach (Transaction transaction in needPayment)
            {
                FinanceHelper.Utils.FindPayment(transaction, payments, _bot.Config.PayMasterPurposesFormats);
            }

            if (statusMessage is not null)
            {
                await _bot.FinalizeStatusMessageAsync(statusMessage);
            }
        }

        statusMessage = chat is null
            ? null
            : await _bot.SendTextMessageAsync(chat, "_Заношу покупки в таблицу…_", ParseMode.MarkdownV2);

        SheetData<Transaction> data = new(transactions.OrderBy(t => t.Date).ToList(), oldTransactions.Titles);
        await DataManager<Transaction>.SaveAsync(provider, _bot.Config.GoogleTransactionsFinalRange, data,
            AdditionalSavers);

        await provider.ClearValuesAsync(_bot.Config.GoogleTransactionsCustomRangeToClear);

        if (statusMessage is not null)
        {
            await _bot.FinalizeStatusMessageAsync(statusMessage);
        }

        return productIdForMails is null
            ? Enumerable.Empty<MailAddress>()
            : transactions.Where(t => t.DigisellerProductId == productIdForMails).Select(t => t.Email).RemoveNulls();
    }

    private async Task UpdateDonationsAsync(Chat chat, SheetsProvider provider)
    {
        List<Donation> donations = new();

        Message statusMessage =
            await _bot.SendTextMessageAsync(chat, "_Загружаю донаты из таблицы…_", ParseMode.MarkdownV2);

        SheetData<Donation> oldDonations =
            await DataManager<Donation>.LoadAsync(provider, _bot.Config.GoogleDonationsRange);
        donations.AddRange(oldDonations.Instances);

        SheetData<Donation> newCustomDonations =
            await DataManager<Donation>.LoadAsync(provider, _bot.Config.GoogleDonationsCustomRange);
        donations.AddRange(newCustomDonations.Instances);

        await _bot.FinalizeStatusMessageAsync(statusMessage);

        statusMessage = await _bot.SendTextMessageAsync(chat, "_Загружаю платежи…_", ParseMode.MarkdownV2);

        DateTime dateStart = donations.Select(o => o.Date).Min().AddDays(-1);
        DateTime dateEnd = DateTime.Today.AddDays(1);

        List<Donation> newDonations =
            await FinanceHelper.Utils.GetNewPayMasterPaymentsAsync(_bot.Config.PayMasterMerchantIdDonations, dateStart,
            dateEnd, _bot.Config.PayMasterToken, oldDonations.Instances);

        donations.AddRange(newDonations);

        await _bot.FinalizeStatusMessageAsync(statusMessage);

        DateTime firstThursday = Utils.GetNextThursday(donations.Min(d => d.Date));

        FinanceHelper.Utils.CalculateTotalsAndWeeks(donations, _bot.Config.PayMasterFeePercents, firstThursday);

        statusMessage = await _bot.SendTextMessageAsync(chat, "_Заношу донаты в таблицу…_", ParseMode.MarkdownV2);

        SheetData<Donation> data = new(donations.OrderByDescending(d => d.Date).ToList(), oldDonations.Titles);
        await DataManager<Donation>.SaveAsync(provider, _bot.Config.GoogleDonationsRange, data);
        await provider.ClearValuesAsync(_bot.Config.GoogleDonationsCustomRangeToClear);

        await _bot.FinalizeStatusMessageAsync(statusMessage);

        statusMessage =
            await _bot.SendTextMessageAsync(chat, "_Считаю и заношу недельные суммы…_", ParseMode.MarkdownV2);

        List<DonationsSum> sums = donations.GroupBy(d => d.Week)
                                           .Select(g => new DonationsSum(firstThursday, g.Key, g.Sum(d => d.Total)))
                                           .ToList();
        List<string> titles =
            await GoogleSheetsManager.Utils.LoadTitlesAsync(provider, _bot.Config.GoogleDonationSumsRange);
        SheetData<DonationsSum> sumsData = new(sums.OrderByDescending(s => s.Date).ToList(), titles);
        await DataManager<DonationsSum>.SaveAsync(provider, _bot.Config.GoogleDonationSumsRange, sumsData);

        await _bot.FinalizeStatusMessageAsync(statusMessage);
    }

    private static readonly Dictionary<Type, Func<object?, object?>> AdditionalConverters = new()
    {
        { typeof(Transaction.PayMethod), o => o.ToPayMathod() },
        { typeof(Transaction.PayMethod?), o => o.ToPayMathod() }
    };

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers = new()
    {
        Transaction.Save
    };

    private const string ApplicationName = "Carespace.FinanceHelper";

    private readonly Bot _bot;
}