namespace Carespace.Bot.Commands
{
    internal sealed class ScheduleCommand : TextCommand
    {
        protected override string Name => "schedule";
        protected override string Description => "расписание четвергового практикума";

        public ScheduleCommand(Bot bot) : base(bot, bot.Config.Schedule) { }
    }
}