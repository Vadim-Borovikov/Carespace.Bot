using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.AntiSpam;

namespace Carespace.Bot.Operations.Commands;

internal sealed class StrikeCommand : RestrictCommand
{
    protected override byte MenuOrder => 9;

    public StrikeCommand(Bot bot, Manager manager) : base(bot, manager, "strike",
        "предупредить автора или ограничить его права")
    {
    }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => Manager.Strike(user, admin);
}