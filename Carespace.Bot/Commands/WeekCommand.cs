using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class WeekCommand : CommandBase<Bot, Config.Config>
{
    public override BotBase<Bot, Config.Config>.AccessType Access => BotBase<Bot, Config.Config>.AccessType.Admins;

    public WeekCommand(Bot bot) : base(bot, "week", "обновить расписание") { }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        return Bot.EventManager.PostOrUpdateWeekEventsAndScheduleAsync(message.Chat, true);
    }
}