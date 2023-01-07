using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Email;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ConfirmEmailCommand : CommandOperation
{
    public const string CommandName = "confirm_email";

    protected override byte MenuOrder => 12;

    protected override Access AccessLevel => Access.Admin;

    public ConfirmEmailCommand(Bot bot, Manager manager, FinanceManager financeManager)
        : base(bot, CommandName, "подтвердить отправку письма")
    {
        _manager = manager;
        _financeManager = financeManager;
    }

    protected override async Task ExecuteAsync(Message message, long _, string? __)
    {
        SellInfo? info = await _manager.SendEmailAsync(message.Chat);
        await _manager.MarkMailAsReadAsync(message.Chat);

        if (info.HasValue)
        {
            await _financeManager.AddEmailTransaction(message.Chat, info.Value);
        }
    }

    private readonly Manager _manager;
    private readonly FinanceManager _financeManager;
}