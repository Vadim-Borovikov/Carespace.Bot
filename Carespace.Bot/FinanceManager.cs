using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Configs;
using Carespace.Bot.Operations;
using Carespace.FinanceHelper;
using GoogleSheetsManager.Documents;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot;

internal sealed class FinanceManager
{
    public FinanceManager(Bot bot, Manager documentsManager,
        Dictionary<Type, Func<object?, object?>> additionalConverters)
    {
        _bot = bot;

        _itemVendorChat = new Chat
        {
            Id = bot.Config.ItemVendorId,
            Type = ChatType.Private
        };

        additionalConverters = new Dictionary<Type, Func<object?, object?>>(additionalConverters);

        GoogleSheetsManager.Documents.Document transactions =
            documentsManager.GetOrAdd(bot.Config.GoogleSheetIdTransactions);
        _transactions = transactions.GetOrAddSheet(_bot.Config.GoogleTitle, additionalConverters);
    }

    public async Task ProcessSubmissionAsync(string name, MailAddress email, string telegram, IList<byte> productIds,
        IReadOnlyList<Uri> slips)
    {
        List<MessageTemplate> productLines =
            productIds.Select(p => _bot.Config.Texts.ListItemFormat.Format(_bot.Config.Products[p].Name)).ToList();
        MessageTemplate productMessages = MessageTemplate.JoinTexts(productLines)!;

        MessageTemplate comfirmation = _bot.Config.Texts.PaymentConfirmationFormat.Format(productMessages);

        KeyboardProvider keyboard = CreateConfirmationKeyboard(name, email, telegram, productIds, slips);

        await comfirmation.SendAsync(_bot, _itemVendorChat, keyboard);
    }

    public async Task AddTransactionsAsync(Chat chat, List<Transaction> transactions)
    {
        Calculator.CalculateShares(transactions, _bot.Config.Products);

        await using (await StatusMessage.CreateAsync(_bot, chat, _bot.Config.Texts.AddingPurchases))
        {
            transactions = transactions.OrderBy(t => t.Date).ToList();
            await _transactions.AddAsync(_bot.Config.GoogleRange, transactions, additionalSavers: AdditionalSavers);
        }
    }

    public async Task<IEnumerable<MailAddress>> LoadEmailsWithAsync(byte productIdForMails)
    {
        List<Transaction> transactions = await _transactions.LoadAsync<Transaction>(_bot.Config.GoogleRange);

        return transactions.Where(t => t.ProductId == productIdForMails).Select(t => t.Email).SkipNulls();
    }

    private KeyboardProvider CreateConfirmationKeyboard(string name, MailAddress email, string telegram,
        IEnumerable<byte> productIds, IReadOnlyList<Uri> slips)
    {
        List<List<InlineKeyboardButton>> rows = new();

        if (slips.Count == 1)
        {
            InlineKeyboardButton button = CreateUriButton(_bot.Config.Texts.PaymentSlipButtonCaption, slips.Single());
            rows.Add(button.WrapWithList());
        }
        else
        {
            for (int i = 0; i < slips.Count; ++i)
            {
                string caption = string.Format(_bot.Config.Texts.PaymentSlipButtonFormat,
                    _bot.Config.Texts.PaymentSlipButtonCaption, i + 1);
                InlineKeyboardButton button = CreateUriButton(caption, slips[i]);
                rows.Add(button.WrapWithList());
            }
        }

        InlineKeyboardButton confirm =
            CreateCallbackButton<AcceptPurchase>(_bot.Config.Texts.PaymentConfirmationButton, name, email, telegram,
                string.Join(BytesSeparator, productIds));
        rows.Add(confirm.WrapWithList());

        return new InlineKeyboardMarkup(rows);
    }

    private static InlineKeyboardButton CreateUriButton(string caption, Uri uri)
    {
        return new InlineKeyboardButton(caption)
        {
            Url = uri.AbsoluteUri
        };
    }

    private static InlineKeyboardButton CreateCallbackButton<TCallback>(string caption, params object[]? args)
    {
        string data = typeof(TCallback).Name;
        if (args is not null)
        {
            data += string.Join(QuerySeparator, args.Select(o => o.ToString()!));
        }
        return new InlineKeyboardButton(caption)
        {
            CallbackData = data
        };
    }

    private static readonly List<Action<Transaction, IDictionary<string, object?>>> AdditionalSavers =
        WrapExtensions.WrapWithList(Transaction.Save);

    private readonly Bot _bot;
    private readonly Chat _itemVendorChat;
    private readonly Sheet _transactions;

    public const string QuerySeparator = "_";
    public const string BytesSeparator = ";";
}