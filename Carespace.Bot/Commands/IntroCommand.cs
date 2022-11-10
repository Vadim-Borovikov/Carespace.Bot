namespace Carespace.Bot.Commands;

internal sealed class IntroCommand : TextCommand
{
    public IntroCommand(Bot bot) : base(bot, "intro", "о практикуме", bot.PracticeIntroduction) { }
}