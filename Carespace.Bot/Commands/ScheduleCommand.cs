namespace Carespace.Bot.Commands;

internal sealed class ScheduleCommand : TextCommand
{
    public ScheduleCommand(Bot bot) : base(bot, "schedule", "расписание", bot.PracticeSchedule) { }
}