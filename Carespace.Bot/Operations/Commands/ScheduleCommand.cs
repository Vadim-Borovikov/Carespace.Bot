namespace Carespace.Bot.Operations.Commands;

internal sealed class ScheduleCommand : TextCommand
{
    protected override byte MenuOrder => 3;

    public ScheduleCommand(Bot bot) : base(bot, "schedule", "расписание", bot.PracticeSchedule) { }
}