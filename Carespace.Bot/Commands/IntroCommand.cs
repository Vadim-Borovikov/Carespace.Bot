namespace Carespace.Bot.Commands
{
    internal sealed class IntroCommand : TextCommand
    {
        public override string Name => "intro";
        public override string Description => "о практикуме";

        public IntroCommand(string text) : base(text) { }
    }
}
