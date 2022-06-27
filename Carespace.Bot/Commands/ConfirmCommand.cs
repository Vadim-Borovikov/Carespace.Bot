using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class ConfirmCommand : CommandBase<Bot, Config>
{
    protected override string Name => CommandName;
    protected override string Description => "Подтвердить отправку событий";

    public override BotBase<Bot, Config>.AccessType Access => BotBase<Bot, Config>.AccessType.Admins;

    public ConfirmCommand(Bot bot) : base(bot) { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        await Bot.EventManager.ConfirmAndPlanToPostOrUpdateWeekEventsAndScheduleAsync(message.Chat.Id);
    }

    public const string CommandName = "confirm";
}