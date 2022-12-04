using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class FinanceCommand : CommandBaseCustom<Bot, Config.Config>
{
    public override BotBase.AccessType Access => BotBase.AccessType.SuperAdmin;

    public FinanceCommand(Bot bot, FinanceManager manager) : base(bot, "finance", "обновить финансы")
    {
        _manager = manager;
    }

    public override Task ExecuteAsync(Message message, Chat chat, string? payload) => _manager.UpdateFinances(chat);

    private readonly FinanceManager _manager;
}