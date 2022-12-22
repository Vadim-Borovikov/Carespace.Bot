namespace Carespace.Bot.Operations.Commands;

internal sealed class IntroCommand : TextCommand
{
    protected override byte MenuOrder => 2;

    public IntroCommand(Bot bot) : base(bot, "intro", "о практикуме", bot.PracticeIntroduction) { }
}