using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Email;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations;

internal sealed class PrepareEmailOperation : Operation
{
    protected override byte MenuOrder => 11;

    protected override Access AccessLevel => Access.Admin;

    public PrepareEmailOperation(Bot bot, Manager manager) : base(bot)
    {
        MenuDescription = "*ключ письма* – подготовить письмо с книгой в ответ\\. Можно также указать имя";
        _manager = manager;
    }

    protected override async Task<ExecutionResult> TryExecuteAsync(Message message, long senderId)
    {
        if ((message.Type != MessageType.Text) || string.IsNullOrWhiteSpace(message.Text))
        {
            return ExecutionResult.UnsuitableOperation;
        }

        string[] parts = message.Text.Split();
        if (parts.Length is 0 or > 2 || !_manager.Mail.ContainsKey(parts[0]))
        {
            return ExecutionResult.UnsuitableOperation;
        }

        if (!IsAccessSuffice(senderId))
        {
            return ExecutionResult.InsufficentAccess;
        }

        string key = parts[0];
        string? name = parts.Length == 2 ? parts[1] : null;

        await _manager.PrepareEmailAsync(message.Chat, key, name);
        return ExecutionResult.Success;
    }

    private readonly Manager _manager;
}