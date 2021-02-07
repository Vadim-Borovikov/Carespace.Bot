namespace Carespace.Bot.Commands
{
    internal sealed class ScheduleCommand : TextCommand
    {
        public override string Name => "schedule";
        public override string Description => "расписание четвергового практикума";

        public ScheduleCommand(string text) : base(text) { }
    }
}