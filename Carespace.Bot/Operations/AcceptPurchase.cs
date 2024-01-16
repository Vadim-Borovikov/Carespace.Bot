using System;
using System.Collections.Generic;
using AbstractBot.Operations;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Carespace.Bot.Operations.Info;
using Carespace.FinanceHelper;

namespace Carespace.Bot.Operations;

internal sealed class AcceptPurchase : Operation<PurchaseInfo>
{
    protected override byte Order => 13;

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
        data = PurchaseInfo.TryParse(callbackQueryDataCore);
        return data is not null;
    }

    protected override Task ExecuteAsync(PurchaseInfo data, Message message, User sender)
    {
        DateOnly date = _bot.Clock.GetDateTimeFull(message.Date).DateOnly;
        List<Transaction> transactions = new();
        foreach (byte id in data.ProductIds)
        {
            Product product = _bot.Config.Products[id];
            Transaction t = new()
            {
                Name = product.Name,
                Date = date,
                Amount = product.Price,
                ProductId = id,
                Email = data.Email
            };
            transactions.Add(t);
        }
        return _manager.AddTransactionsAsync(message.Chat, transactions);
    }

    private readonly Bot _bot;
    private readonly FinanceManager _manager;
}