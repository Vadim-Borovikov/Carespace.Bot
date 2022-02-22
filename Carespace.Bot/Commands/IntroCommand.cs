namespace Carespace.Bot.Commands;

internal sealed class IntroCommand : TextCommand
{
    protected override string Name => "intro";
    protected override string Description => "О практикуме";

    public IntroCommand(Bot bot) : base(bot, bot.Config.Introduction) { }
}
