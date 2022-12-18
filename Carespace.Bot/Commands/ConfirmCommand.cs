using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Events;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class ConfirmCommand : CommandOperation
{
    public const string CommandName = "confirm";

    protected override byte MenuOrder => 8;

    protected override Access AccessLevel => Access.Admin;

    public ConfirmCommand(Bot bot, Manager manager) : base(bot, CommandName, "подтвердить отправку событий")
    {
        _manager = manager;
    }

    protected override async Task ExecuteAsync(Message message, long _, string? __)
    {
        await _manager.ConfirmAndPostOrUpdateWeekEventsAndScheduleAsync(message.Chat);
    }

    private readonly Manager _manager;
}