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
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class FinanceManager
{
    public FinanceManager(Bot bot)
    {
        _bot = bot;

        FinanceHelper.Utils.PayMasterPaymentUrlFormat =
            _bot.Config.PayMasterPaymentUrlFormat.GetValue(nameof(_bot.Config.PayMasterPaymentUrlFormat));

        Transaction.DigisellerSellUrlFormat =
            _bot.Config.DigisellerSellUrlFormat.GetValue(nameof(_bot.Config.DigisellerSellUrlFormat));
        Transaction.DigisellerProductUrlFormat =
            _bot.Config.DigisellerProductUrlFormat.GetValue(nameof(_bot.Config.DigisellerProductUrlFormat));

        Transaction.TaxPayerId = _bot.Config.TaxPayerId.GetValue(nameof(_bot.Config.TaxPayerId));

        Transaction.Agents.Clear();
        Transaction.Agents.AddRange(_bot.Config.Shares.Values
                                        .SelectMany(s => s)
                                        .Select(s => s.Agent)
                                        .Distinct()
                                        .OrderBy(a => a));

        Transaction.Titles.AddRange(Transaction.Agents);
    }

    public async Task UpdateFinances(ChatId chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "Обновляю покупки…");

        await UpdatePurchasesAsync(chatId);

        await _bot.SendTextMessageAsync(chatId, "…покупки обновлены.");

        await _bot.SendTextMessageAsync(chatId, "Обновляю донатики…");

        await UpdateDonationsAsync(chatId);

        await _bot.SendTextMessageAsync(chatId, "…донатики обновлены.");
    }

    public async Task<IEnumerable<MailAddress>> LoadTransactionEmailsAsync(int productId)
    {
        string sheetId = _bot.Config.GoogleSheetIdTransactions.GetValue(nameof(_bot.Config.GoogleSheetIdTransactions));
        using (SheetsProvider provider = new(_bot.Config.GoogleCredentialJson, ApplicationName, sheetId))
        {
            IEnumerable<MailAddress> emails = await LoadGoogleTransactionsAsync(provider, null, productId);
            return emails;
        }
    }

    private async Task UpdatePurchasesAsync(ChatId chatId)
    {
        string sheetId = _bot.Config.GoogleSheetIdTransactions.GetValue(nameof(_bot.Config.GoogleSheetIdTransactions));
        using (SheetsProvider provider = new(_bot.Config.GoogleCredentialJson, ApplicationName, sheetId))
        {
            await LoadGoogleTransactionsAsync(provider, chatId);
        }
    }

    private async Task UpdateDonationsAsync(ChatId chatId)
    {
        string sheetId = _bot.Config.GoogleSheetIdDonations.GetValue(nameof(_bot.Config.GoogleSheetIdDonations));
        using (SheetsProvider provider = new(_bot.Config.GoogleCredentialJson, ApplicationName, sheetId))
        {
            await UpdateDonationsAsync(chatId, provider);
        }
    }

    private async Task<IEnumerable<MailAddress>> LoadGoogleTransactionsAsync(SheetsProvider provider, ChatId? chatId,
        int? productIdForMails = null)
    {
        List<Transaction> transactions = new();

        Message? statusMessage = chatId is null
            ? null
            : await _bot.SendTextMessageAsync(chatId, "_Загружаю покупки из таблицы…_", ParseMode.MarkdownV2);

        string finalRange =
            _bot.Config.GoogleTransactionsFinalRange.GetValue(nameof(_bot.Config.GoogleTransactionsFinalRange));
        IList<Transaction> oldTransactions = await DataManager.GetValuesAsync(provider, Transaction.Load, finalRange);
        transactions.AddRange(oldTransactions);

        string customRange =
            _bot.Config.GoogleTransactionsCustomRange.GetValue(nameof(_bot.Config.GoogleTransactionsCustomRange));
        IList<Transaction> newCustomTransactions =
            await DataManager.GetValuesAsync(provider, Transaction.Load, customRange);
        transactions.AddRange(newCustomTransactions);

        if (statusMessage is not null)
        {
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        statusMessage = chatId is null
            ? null
            : await _bot.SendTextMessageAsync(chatId, "_Загружаю покупки из Digiseller…_", ParseMode.MarkdownV2);

        DateTime dateStart = transactions.Select(o => o.Date).Min().AddDays(-1);
        DateTime dateEnd = DateTime.Today.AddDays(1);

        int digisellerId = _bot.Config.DigisellerId.GetValue(nameof(_bot.Config.DigisellerId));

        string digisellerLogin = _bot.Config.DigisellerLogin.GetValue(nameof(_bot.Config.DigisellerLogin));
        string digisellerPassword = _bot.Config.DigisellerPassword.GetValue(nameof(_bot.Config.DigisellerPassword));
        string guid = _bot.Config.DigisellerApiGuid.GetValue(nameof(_bot.Config.DigisellerApiGuid));

        List<int> productIds = _bot.Config.Shares.Keys.Where(k => k != "None").Select(int.Parse).ToList();

        List<Transaction> newSells = await FinanceHelper.Utils.GetNewDigisellerSellsAsync(digisellerLogin,
            digisellerPassword, digisellerId, productIds, dateStart, dateEnd, guid, oldTransactions);

        transactions.AddRange(newSells);

        if (statusMessage is not null)
        {
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        statusMessage = chatId is null
            ? null
            : await _bot.SendTextMessageAsync(chatId, "_Считаю доли…_", ParseMode.MarkdownV2);

        decimal taxFeePercent = _bot.Config.TaxFeePercent.GetValue(nameof(_bot.Config.TaxFeePercent));
        decimal digisellerFeePercent =
            _bot.Config.DigisellerFeePercent.GetValue(nameof(_bot.Config.DigisellerFeePercent));

        FinanceHelper.Utils.CalculateShares(transactions, taxFeePercent, digisellerFeePercent,
            _bot.Config.PayMasterFeePercents, _bot.Config.Shares);

        if (statusMessage is not null)
        {
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        List<Transaction> needPayment = transactions.Where(t => t.NeedPaynemt).ToList();
        if (needPayment.Any())
        {
            statusMessage = chatId is null
                ? null
                : await _bot.SendTextMessageAsync(chatId, "_Загружаю платежи…_", ParseMode.MarkdownV2);

            string alias =
                _bot.Config.PayMasterSiteAliasDigiseller.GetValue(nameof(_bot.Config.PayMasterSiteAliasDigiseller));
            dateStart = needPayment.Select(o => o.Date).Min();
            dateEnd = needPayment.Select(o => o.Date).Max().AddDays(1);
            string payMasterLogin = _bot.Config.PayMasterLogin.GetValue(nameof(_bot.Config.PayMasterLogin));
            string payMasterPassword = _bot.Config.PayMasterPassword.GetValue(nameof(_bot.Config.PayMasterPassword));
            List<ListPaymentsFilterResult.ResponseInfo.Payment> payments =
                await FinanceHelper.Utils.GetPaymentsAsync(alias, dateStart, dateEnd, payMasterLogin,
                    payMasterPassword);

            List<string> formats =
                _bot.Config.PayMasterPurposesFormats.GetValue(nameof(_bot.Config.PayMasterPurposesFormats));
            foreach (Transaction transaction in needPayment)
            {
                FinanceHelper.Utils.FindPayment(transaction, payments, formats);
            }

            if (statusMessage is not null)
            {
                await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
            }
        }

        statusMessage = chatId is null
            ? null
            : await _bot.SendTextMessageAsync(chatId, "_Заношу покупки в таблицу…_", ParseMode.MarkdownV2);


        await DataManager.UpdateValuesAsync(provider, finalRange, transactions.OrderBy(t => t.Date).ToList());

        string rangeToClear =
            _bot.Config.GoogleTransactionsCustomRangeToClear.GetValue(nameof(_bot.Config.GoogleTransactionsCustomRangeToClear));
        await provider.ClearValuesAsync(rangeToClear);

        if (statusMessage is not null)
        {
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        return productIdForMails is null
            ? Enumerable.Empty<MailAddress>()
            : transactions.Where(t => t.DigisellerProductId == productIdForMails).Select(t => t.Email).RemoveNulls();
    }

    private async Task UpdateDonationsAsync(ChatId chatId, SheetsProvider provider)
    {
        List<Donation> donations = new();

        Message statusMessage =
            await _bot.SendTextMessageAsync(chatId, "_Загружаю донаты из таблицы…_", ParseMode.MarkdownV2);

        string range = _bot.Config.GoogleDonationsRange.GetValue(nameof(_bot.Config.GoogleDonationsRange));
        IList<Donation> oldDonations = await DataManager.GetValuesAsync(provider, Donation.Load, range);
        donations.AddRange(oldDonations);

        string customRange =
            _bot.Config.GoogleDonationsCustomRange.GetValue(nameof(_bot.Config.GoogleDonationsCustomRange));
        IList<Donation> newCustomDonations = await DataManager.GetValuesAsync(provider, Donation.Load, customRange);
        donations.AddRange(newCustomDonations);

        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

        statusMessage = await _bot.SendTextMessageAsync(chatId, "_Загружаю платежи…_", ParseMode.MarkdownV2);

        DateTime dateStart = donations.Select(o => o.Date).Min().AddDays(-1);
        DateTime dateEnd = DateTime.Today.AddDays(1);

        string alias =
            _bot.Config.PayMasterSiteAliasDonations.GetValue(nameof(_bot.Config.PayMasterSiteAliasDonations));
        string login = _bot.Config.PayMasterLogin.GetValue(nameof(_bot.Config.PayMasterLogin));
        string password = _bot.Config.PayMasterPassword.GetValue(nameof(_bot.Config.PayMasterPassword));
        List<Donation> newDonations = await FinanceHelper.Utils.GetNewPayMasterPaymentsAsync(alias, dateStart, dateEnd,
            login, password, oldDonations);

        donations.AddRange(newDonations);

        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

        DateTime firstThursday = Utils.GetNextThursday(donations.Min(d => d.Date));

        FinanceHelper.Utils.CalculateTotalsAndWeeks(donations, _bot.Config.PayMasterFeePercents, firstThursday);

        statusMessage = await _bot.SendTextMessageAsync(chatId, "_Заношу донаты в таблицу…_", ParseMode.MarkdownV2);

        string donationsRange = _bot.Config.GoogleDonationsRange.GetValue(nameof(_bot.Config.GoogleDonationsRange));
        string clearRange =
            _bot.Config.GoogleDonationsCustomRangeToClear.GetValue(nameof(_bot.Config.GoogleDonationsCustomRangeToClear));
        await DataManager.UpdateValuesAsync(provider, donationsRange,
            donations.OrderByDescending(d => d.Date).ToList());
        await provider.ClearValuesAsync(clearRange);

        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

        statusMessage =
            await _bot.SendTextMessageAsync(chatId, "_Считаю и заношу недельные суммы…_", ParseMode.MarkdownV2);

        List<DonationsSum> sums = donations.GroupBy(d => d.Week)
                                           .Select(g => new DonationsSum(firstThursday, g.Key, g.Sum(d => d.Total)))
                                           .ToList();

        string sumsRange = _bot.Config.GoogleDonationSumsRange.GetValue(nameof(_bot.Config.GoogleDonationSumsRange));
        await DataManager.UpdateValuesAsync(provider, sumsRange, sums.OrderByDescending(s => s.Date).ToList());

        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
    }

    private const string ApplicationName = "Carespace.FinanceHelper";

    private readonly Bot _bot;
}