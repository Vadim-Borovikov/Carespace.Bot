using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.FinanceHelper;
using Carespace.FinanceHelper.Dto.PayMaster;
using GoogleSheetsManager;
using GoogleSheetsManager.Providers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot
{
    internal sealed class FinanceManager
    {
        public FinanceManager(Bot bot)
        {
            _bot = bot;

            FinanceHelper.Utils.PayMasterPaymentUrlFormat = _bot.Config.PayMasterPaymentUrlFormat;

            Transaction.DigisellerSellUrlFormat = _bot.Config.DigisellerSellUrlFormat;
            Transaction.DigisellerProductUrlFormat = _bot.Config.DigisellerProductUrlFormat;

            Transaction.TaxPayerId = _bot.Config.TaxPayerId;

            Transaction.Agents = _bot.Config.Shares.Values
                                     .SelectMany(s => s)
                                     .Select(s => s.Agent)
                                     .Distinct()
                                     .OrderBy(s => s)
                                     .ToList();

            Transaction.Titles.AddRange(Transaction.Agents);
        }

        public async Task UpdateFinances(ChatId chatId)
        {
            await _bot.Client.SendTextMessageAsync(chatId, "Обновляю покупки…");

            await UpdatePurchasesAsync(chatId);

            await _bot.Client.SendTextMessageAsync(chatId, "…покупки обновлены.");

            await _bot.Client.SendTextMessageAsync(chatId, "Обновляю донатики…");

            await UpdateDonationsAsync(chatId);

            await _bot.Client.SendTextMessageAsync(chatId, "…донатики обновлены.");
        }

        private async Task UpdatePurchasesAsync(ChatId chatId)
        {
            using (var provider =
                new SheetsProvider(_bot.Config.GoogleCredentialJson, ApplicationName, _bot.Config.GoogleSheetIdTransactions))
            {
                await LoadGoogleTransactionsAsync(chatId, provider);
            }
        }

        private async Task UpdateDonationsAsync(ChatId chatId)
        {
            using (var provider =
                new SheetsProvider(_bot.Config.GoogleCredentialJson, ApplicationName, _bot.Config.GoogleSheetIdDonations))
            {
                await UpdateDonationsAsync(chatId, provider);
            }
        }

        private async Task LoadGoogleTransactionsAsync(ChatId chatId, SheetsProvider provider)
        {
            var transactions = new List<Transaction>();

            Message statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Загружаю покупки из таблицы…_", ParseMode.MarkdownV2);

            IList<Transaction> oldTransactions =
                await DataManager.GetValuesAsync<Transaction>(provider, _bot.Config.GoogleTransactionsFinalRange);
            transactions.AddRange(oldTransactions);

            IList<Transaction> newCustomTransactions =
                await DataManager.GetValuesAsync<Transaction>(provider, _bot.Config.GoogleTransactionsCustomRange);
            if (newCustomTransactions != null)
            {
                transactions.AddRange(newCustomTransactions);
            }

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Загружаю покупки из Digiseller…_", ParseMode.MarkdownV2);

            DateTime dateStart = transactions.Select(o => o.Date).Min().AddDays(-1);
            DateTime dateEnd = DateTime.Today.AddDays(1);

            List<int> productIds = _bot.Config.Shares.Keys.Where(k => k != "None").Select(int.Parse).ToList();
            List<Transaction> newSells = await FinanceHelper.Utils.GetNewDigisellerSellsAsync(_bot.Config.DigisellerLogin,
                _bot.Config.DigisellerPassword, _bot.Config.DigisellerId, productIds, dateStart, dateEnd,
                _bot.Config.DigisellerApiGuid, oldTransactions);

            transactions.AddRange(newSells);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Считаю доли…_", ParseMode.MarkdownV2);

            FinanceHelper.Utils.CalculateShares(transactions, _bot.Config.TaxFeePercent, _bot.Config.DigisellerFeePercent,
                _bot.Config.PayMasterFeePercents, _bot.Config.Shares);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            List<Transaction> needPayment = transactions.Where(t => t.NeedPaynemt).ToList();
            if (needPayment.Any())
            {
                statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Загружаю платежи…_", ParseMode.MarkdownV2);

                dateStart = needPayment.Select(o => o.Date).Min();
                dateEnd = needPayment.Select(o => o.Date).Max().AddDays(1);
                List<ListPaymentsFilterResult.Response.Payment> payments =
                    await FinanceHelper.Utils.GetPaymentsAsync(_bot.Config.PayMasterSiteAliasDigiseller, dateStart, dateEnd,
                        _bot.Config.PayMasterLogin, _bot.Config.PayMasterPassword);

                foreach (Transaction transaction in needPayment)
                {
                    FinanceHelper.Utils.FindPayment(transaction, payments, _bot.Config.PayMasterPurposesFormats);
                }

                await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
            }

            statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Регистрирую доходы…_", ParseMode.MarkdownV2);

            await FinanceHelper.Utils.RegisterTaxesAsync(transactions, _bot.Config.TaxUserAgent, _bot.Config.TaxSourceDeviceId,
                _bot.Config.TaxSourceType, _bot.Config.TaxAppVersion, _bot.Config.TaxRefreshToken,
                _bot.Config.TaxProductNameFormat);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Заношу покупки в таблицу…_", ParseMode.MarkdownV2);

            await DataManager.UpdateValuesAsync(provider, _bot.Config.GoogleTransactionsFinalRange,
                transactions.OrderBy(t => t.Date).ToList());
            await provider.ClearValuesAsync(_bot.Config.GoogleTransactionsCustomRangeToClear);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        private async Task UpdateDonationsAsync(ChatId chatId, SheetsProvider provider)
        {
            var donations = new List<Donation>();

            Message statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Загружаю донаты из таблицы…_", ParseMode.MarkdownV2);

            IList<Donation> oldDonations =
                await DataManager.GetValuesAsync<Donation>(provider, _bot.Config.GoogleDonationsRange);
            donations.AddRange(oldDonations);

            IList<Donation> newCustomDonations =
                await DataManager.GetValuesAsync<Donation>(provider, _bot.Config.GoogleDonationsCustomRange);
            if (newCustomDonations != null)
            {
                donations.AddRange(newCustomDonations);
            }

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Загружаю платежи…_", ParseMode.MarkdownV2);

            DateTime dateStart = donations.Select(o => o.Date).Min().AddDays(-1);
            DateTime dateEnd = DateTime.Today.AddDays(1);

            List<Donation> newDonations =
                await FinanceHelper.Utils.GetNewPayMasterPaymentsAsync(_bot.Config.PayMasterSiteAliasDonations, dateStart,
                    dateEnd, _bot.Config.PayMasterLogin, _bot.Config.PayMasterPassword, oldDonations);

            donations.AddRange(newDonations);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            DateTime firstThursday = Utils.GetNextThursday(donations.Min(d => d.Date));

            FinanceHelper.Utils.CalculateTotalsAndWeeks(donations, _bot.Config.PayMasterFeePercents, firstThursday);

            statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Заношу донаты в таблицу…_", ParseMode.MarkdownV2);

            await DataManager.UpdateValuesAsync(provider, _bot.Config.GoogleDonationsRange,
                donations.OrderByDescending(d => d.Date).ToList());
            await provider.ClearValuesAsync(_bot.Config.GoogleDonationsCustomRangeToClear);

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);

            statusMessage =
                await _bot.Client.SendTextMessageAsync(chatId, "_Считаю и заношу недельные суммы…_", ParseMode.MarkdownV2);

            List<DonationsSum> sums = donations.GroupBy(d => d.Week)
                                               .Select(g => new DonationsSum(firstThursday, g.Key, g.Sum(d => d.Total)))
                                               .ToList();

            await DataManager.UpdateValuesAsync(provider, _bot.Config.GoogleDonationSumsRange,
                sums.OrderByDescending(s => s.Date).ToList());

            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
        }

        private const string ApplicationName = "Carespace.FinanceHelper";

        private readonly Bot _bot;
    }
}
