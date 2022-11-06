using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class ConfirmCommand : CommandBase<Bot, Config>
{
    public override BotBase<Bot, Config>.AccessType Access => BotBase<Bot, Config>.AccessType.Admins;

    public ConfirmCommand(Bot bot) : base(bot, CommandName, "подтвердить отправку событий") { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        await Bot.EventManager.ConfirmAndPostOrUpdateWeekEventsAndScheduleAsync(message.Chat);
    }

    public const string CommandName = "confirm";
}