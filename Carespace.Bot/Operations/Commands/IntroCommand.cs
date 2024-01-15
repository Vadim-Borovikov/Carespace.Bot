using AbstractBot.Operations.Commands;

namespace Carespace.Bot.Operations.Commands;

internal sealed class IntroCommand : CommandText
{
    protected override byte Order => 2;

    public IntroCommand(Bot bot)
        : base(bot, "intro", "о практикуме", GryphonUtilities.Helpers.Text.JoinLines(bot.Config.PracticeIntroductionLines))
    { }
}