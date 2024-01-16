using System;
using AbstractBot.Operations;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Carespace.Bot.Operations.Info;

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

    protected override async Task ExecuteAsync(PurchaseInfo data, Message message, User sender)
    {
        DateOnly date = _bot.Clock.GetDateTimeFull(message.Date).DateOnly;
        await _manager.AddTransactionsAsync(message.Chat, date, data.ProductIds, data.Email);

        await _manager.GenerateClientMessagesAsync(message.Chat, data.Name, data.Telegram, data.ProductIds);
    }

    private readonly Bot _bot;
    private readonly FinanceManager _manager;
}