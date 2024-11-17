using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Operations.Commands;

internal sealed class SpamCommand : RestrictCommand
{
    protected override byte Order => 7;

    public SpamCommand(Bot bot, RestrictionsManager antiSpam)
        : base(bot, antiSpam, "spam", bot.Config.Texts.SpamCommandDescription)
    { }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => AntiSpam.Destroy(user, admin);
}