using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class FinanceCommand : CommandBase<Bot, Config.Config>
{
    public override BotBase<Bot, Config.Config>.AccessType Access => BotBase<Bot, Config.Config>.AccessType.SuperAdmin;

    public FinanceCommand(Bot bot, FinanceManager manager) : base(bot, "finance", "обновить финансы")
    {
        _manager = manager;
    }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
        return _manager.UpdateFinances(chat);
    }

    private readonly FinanceManager _manager;
}