using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Operations.Commands;

internal sealed class WarningCommand : RestrictCommand
{
    protected override byte Order => 9;

    public WarningCommand(Bot bot, RestrictionsManager antiSpam) : base(bot, antiSpam, "warning",
        "предупредить автора или ограничить его права")
    {
    }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => AntiSpam.Strike(user, admin);
}