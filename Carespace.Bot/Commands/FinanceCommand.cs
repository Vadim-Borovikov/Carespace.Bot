using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class FinanceCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "finance";
    protected override string Description => "Обновить финансы";

    public override BotBase<Bot, Config.Config>.AccessType Access => BotBase<Bot, Config.Config>.AccessType.SuperAdmin;

    public FinanceCommand(Bot bot, FinanceManager manager) : base(bot) => _manager = manager;

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        return _manager.UpdateFinances(user.Id);
    }

    private readonly FinanceManager _manager;
}