using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class StartCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "start";
    protected override string Description => "Список команд";

    public StartCommand(Bot bot) : base(bot) { }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
        return Bot.SendTextMessageAsync(chat, Bot.GetDescriptionFor(user.Id));
    }
}