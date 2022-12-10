using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Events;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class WeekCommand : CommandOperation
{
    protected override byte MenuOrder => 7;

    protected override Access AccessLevel => Access.Admins;

    public WeekCommand(Bot bot, Manager manager) : base(bot, "week", "обновить расписание") => _manager = manager;

    protected override Task ExecuteAsync(Message _, Chat chat, string? __)
    {
        return _manager.PostOrUpdateWeekEventsAndScheduleAsync(chat, true);
    }

    private readonly Manager _manager;
}