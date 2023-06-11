using System.Threading.Tasks;
using AbstractBot.Operations;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class FinanceCommand : CommandOperation
{
    protected override byte MenuOrder => 11;

    protected override Access AccessLevel => Access.SuperAdmin;

    public FinanceCommand(Bot bot, FinanceManager manager) : base(bot, "finance", "обновить финансы")
    {
        _manager = manager;
    }

    protected override Task ExecuteAsync(Message message, long _, string? __) => _manager.UpdateFinances(message.Chat);

    private readonly FinanceManager _manager;
}