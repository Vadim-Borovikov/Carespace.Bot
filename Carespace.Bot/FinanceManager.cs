﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Configs.MessageTemplates;
using Carespace.Bot.Operations;
using Carespace.Bot.Save;
using Carespace.FinanceHelper;
using GoogleSheetsManager.Documents;
using GryphonUtilities;
using GryphonUtilities.Extensions;
using RestSharp;
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

    public async Task ProcessSubmissionAsync(string id, PurchaseInfo info, IReadOnlyList<Uri> slips)
    {
        List<MessageTemplateText> productLines =
            info.ProductIds.Select(p => _bot.Config.Texts.ListItemFormat.Format(_bot.Config.Products[p].Name))
                .ToList();
        MessageTemplateText productMessages = MessageTemplateText.JoinTexts(productLines);

        MessageTemplateText comfirmation = _bot.Config.Texts.PaymentConfirmationFormat.Format(productMessages);

        comfirmation.KeyboardProvider = CreateConfirmationKeyboard(id, slips);
        await comfirmation.SendAsync(_bot, _itemVendorChat);
    }

    public async Task<List<Transaction>> AddTransactionsAsync(Chat chat, DateOnly date, List<byte> productIds,
        MailAddress email)
    {
        List<Transaction> transactions = new();
        foreach (byte id in productIds)
        {
            Product product = _bot.Config.Products[id];
            Transaction t = new()
            {
                Name = product.Name,
                Date = date,
                Amount = product.Price,
                ProductId = id,
                Email = email
            };
            transactions.Add(t);
        }

        Calculator.CalculateShares(transactions, _bot.Config.Products, _bot.Config.FallbackAgent);

        await using (await StatusMessage.CreateAsync(_bot, chat, _bot.Config.Texts.AddingPurchases))
        {
            transactions = transactions.OrderBy(t => t.Date).ToList();
            await _transactions.AddAsync(_bot.Config.GoogleRange, transactions, additionalSavers: AdditionalSavers);
        }

        return transactions;
    }

    public async Task GenerateClientMessagesAsync(Chat chat, string name, string telegram, List<byte> productIds)
    {
        MessageTemplateText namePart = _bot.Config.Texts.CopyableFormat.Format(name);
        telegram = telegram.Replace(" ", "");
        MessageTemplateText formatted = _bot.Config.Texts.MessageForClientFormat.Format(namePart, telegram);
        await formatted.SendAsync(_bot, chat);

        foreach (byte id in productIds)
        {
            await _bot.Config.Texts.ProductMessages[id].SendAsync(_bot, chat);
        }

        await _bot.Config.Texts.ThankYou.SendAsync(_bot, chat);
    }

    public Task<RestResponse> SendPurchaseAsync(string clientName, DateOnly date, IEnumerable<Transaction> transactions)
    {
        Purchase purchase = new()
        {
            ClientName = clientName,
            Date = date
        };
        purchase.Items.AddRange(transactions.Select(t => new Item(t, _bot.Config.FallbackAgent)));
        return RestManager.PostAsync(_bot.Config.PostPurchaseUri.AbsoluteUri, _bot.Config.PostPurchaseResource,
            obj: purchase);
    }

    public async Task<IEnumerable<MailAddress>> LoadEmailsWithAsync(byte productIdForMails)
    {
        List<Transaction> transactions = await _transactions.LoadAsync<Transaction>(_bot.Config.GoogleRange);

        return transactions.Where(t => t.ProductId == productIdForMails).Select(t => t.Email).SkipNulls();
    }

    private KeyboardProvider CreateConfirmationKeyboard(string id, IReadOnlyList<Uri> slips)
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
            CreateCallbackButton<AcceptPurchase>(_bot.Config.Texts.PaymentConfirmationButton, id);
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

    private const string QuerySeparator = "_";
}