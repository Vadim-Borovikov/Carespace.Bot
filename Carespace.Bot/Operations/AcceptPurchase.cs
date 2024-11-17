using System;
using AbstractBot.Operations;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using System.Collections.Generic;
using System.Net.Mail;
using Carespace.FinanceHelper;
using RestSharp;
using Carespace.Bot.Save;

namespace Carespace.Bot.Operations;

internal sealed class AcceptPurchase : Operation<PurchaseInfo>
{
    protected override byte Order => 10;

    public override Enum AccessRequired => Carespace.Bot.Bot.AccessType.Finance;

    public AcceptPurchase(Bot bot, FinanceManager manager) : base(bot)
    {
        _bot = bot;
        _manager = manager;
    }

    protected override bool IsInvokingBy(Message message, User sender, out PurchaseInfo? data)
    {
        data = null;
        return false;
    }

    protected override bool IsInvokingBy(Message message, User sender, string callbackQueryDataCore,
        out PurchaseInfo? data)
    {
        data = _bot.TryGetPurchase(callbackQueryDataCore);
        _bot.RemovePurchase(callbackQueryDataCore);
        return data is not null;
    }

    protected override async Task ExecuteAsync(PurchaseInfo data, Message message, User sender)
    {
        DateOnly date = _bot.Clock.GetDateTimeFull(message.Date).DateOnly;
        MailAddress email = new(data.Email);
        List<Transaction> transactions =
            await _manager.AddTransactionsAsync(message.Chat, date, data.ProductIds, email);
        await _manager.GenerateClientMessagesAsync(message.Chat, data.Name, data.Telegram, data.ProductIds);

        RestResponse response = await _manager.SendPurchaseAsync(data.Name, date, transactions);
        if (!response.IsSuccessful)
        {
            _bot.Logger.LogError(response.ErrorMessage ?? $"Error in Send Purchase response. Status {response.StatusCode}");
        }
    }

    private readonly Bot _bot;
    private readonly FinanceManager _manager;
}