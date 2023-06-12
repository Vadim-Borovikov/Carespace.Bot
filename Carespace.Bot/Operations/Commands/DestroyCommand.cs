using System.Threading.Tasks;
using AbstractBot;

namespace Carespace.Bot.Operations.Commands;

internal sealed class DestroyCommand : RestrictCommand
{
    protected override byte MenuOrder => 10;

    public DestroyCommand(Bot bot, AntiSpamManager antiSpam) : base(bot, antiSpam, "destroy",
        "сразу и сильно ограничить права автора")
    {
    }

    protected override Task ExecuteAsync(TelegramUser user, TelegramUser admin) => AntiSpam.Destroy(user, admin);
}