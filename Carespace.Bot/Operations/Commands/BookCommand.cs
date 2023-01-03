using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Email;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class BookCommand : CommandOperation
{
    protected override byte MenuOrder => 10;

    protected override Access AccessLevel => Access.Admin;

    public BookCommand(Bot bot, Manager manager) : base(bot, "book", "проверить почту") => _manager = manager;

    protected override Task ExecuteAsync(Message message, long _, string? __) => _manager.Check(message.Chat);

    private readonly Manager _manager;
}