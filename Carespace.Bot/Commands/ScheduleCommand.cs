namespace Carespace.Bot.Commands
{
    internal sealed class ScheduleCommand : TextCommand
    {
        protected override string Name => "schedule";
        protected override string Description => "Расписание";

        public ScheduleCommand(Bot bot) : base(bot, bot.Config.Schedule) { }
    }
}