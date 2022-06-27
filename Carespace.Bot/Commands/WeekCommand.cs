using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class WeekCommand : CommandBase<Bot, Config>
{
    protected override string Name => "week";
    protected override string Description => "Обновить расписание";

    public override BotBase<Bot, Config>.AccessType Access => BotBase<Bot, Config>.AccessType.Admins;

    public WeekCommand(Bot bot) : base(bot) { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        await Bot.EventManager.PlanToPostOrUpdateWeekEventsAndScheduleAsync(message.Chat.Id, true);
    }
}
