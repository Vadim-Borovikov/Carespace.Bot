using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Operations.Commands;

internal sealed class WarningCommand : RestrictCommand
{
    protected override byte Order => 7;

    public WarningCommand(Bot bot, RestrictionsManager antiSpam)
        : base(bot, antiSpam, "warning", bot.Config.Texts.WarningCommandDescription)
    { }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => AntiSpam.Strike(user, admin);
}