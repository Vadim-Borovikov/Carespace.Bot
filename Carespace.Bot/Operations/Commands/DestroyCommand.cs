using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.AntiSpam;

namespace Carespace.Bot.Operations.Commands;

internal sealed class DestroyCommand : RestrictCommand
{
    protected override byte MenuOrder => 10;

    public DestroyCommand(Bot bot, Manager manager) : base(bot, manager, "destroy",
        "сразу и сильно ограничить права автора")
    {
    }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => Manager.Destroy(user, admin);
}