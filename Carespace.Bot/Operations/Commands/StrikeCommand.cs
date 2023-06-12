using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Operations.Commands;

internal sealed class StrikeCommand : RestrictCommand
{
    protected override byte MenuOrder => 9;

    public StrikeCommand(Bot bot, AntiSpamManager antiSpam) : base(bot, antiSpam, "strike",
        "предупредить автора или ограничить его права")
    {
    }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => AntiSpam.Strike(user, admin);
}