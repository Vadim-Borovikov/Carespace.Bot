namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class IntroCommand : TextCommand
    {
        internal override string Name => "intro";
        internal override string Description => "о практикуме";

        public IntroCommand(string text) : base(text) { }
    }
}
