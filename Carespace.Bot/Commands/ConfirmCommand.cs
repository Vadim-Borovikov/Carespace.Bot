using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Events;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class ConfirmCommand : CommandOperation
{
    public const string CommandName = "confirm";

    protected override byte MenuOrder => 8;

    protected override Access AccessLevel => Access.Admins;

    public ConfirmCommand(Bot bot, Manager manager) : base(bot, CommandName, "подтвердить отправку событий")
    {
        _manager = manager;
    }

    protected override async Task ExecuteAsync(Message _, Chat chat, string? __)
    {
        await _manager.ConfirmAndPostOrUpdateWeekEventsAndScheduleAsync(chat);
    }

    private readonly Manager _manager;
}