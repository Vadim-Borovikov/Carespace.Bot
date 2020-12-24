namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class ScheduleCommand : TextCommand
    {
        internal override string Name => "schedule";
        internal override string Description => "расписание четвергового практикума";

        public ScheduleCommand(string text) : base(text) { }
    }
}