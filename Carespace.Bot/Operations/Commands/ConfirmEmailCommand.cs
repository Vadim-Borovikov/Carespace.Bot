using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Email;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class ConfirmEmailCommand : CommandOperation
{
    public const string CommandName = "confirm_email";

    protected override byte MenuOrder => 12;

    protected override Access AccessLevel => Access.Admin;

    public ConfirmEmailCommand(Bot bot, Manager manager) : base(bot, CommandName, "подтвердить отправку письма")
    {
        _manager = manager;
    }

    protected override Task ExecuteAsync(Message message, long _, string? __) => _manager.SendEmailAsync(message.Chat);

    private readonly Manager _manager;
}