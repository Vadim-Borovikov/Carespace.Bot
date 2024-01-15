using AbstractBot.Operations.Commands;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ScheduleCommand : CommandText
{
    protected override byte Order => 3;

    public ScheduleCommand(Bot bot)
        : base(bot, "schedule", "расписание",
            GryphonUtilities.Helpers.Text.JoinLines(bot.Config.PracticeScheduleLines))
    { }
}