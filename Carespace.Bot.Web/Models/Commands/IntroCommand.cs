namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class IntroCommand : TextCommand
    {
        internal override string Name => "intro";
        internal override string Description => "инструкция после вступления";

        public IntroCommand(string text) : base(text) { }
    }
}
