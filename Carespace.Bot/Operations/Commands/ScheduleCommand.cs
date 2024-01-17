using AbstractBot.Operations.Commands;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ScheduleCommand : CommandText
{
    protected override byte Order => 3;

    public ScheduleCommand(Bot bot)
        : base(bot, "schedule", bot.Config.Texts.ScheduleCommandDescription, bot.Config.Texts.PracticeSchedule) { }
}