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

        RequestInfo? info = RequestInfo.Parse(message.Text);
        if (info is null || !_manager.Mail.ContainsKey(info.Value.Key))
        {
            return ExecutionResult.UnsuitableOperation;
        }

        if (!IsAccessSuffice(senderId))
        {
            return ExecutionResult.InsufficentAccess;
        }

        await _manager.PrepareEmailAsync(message.Chat, info.Value);
        return ExecutionResult.Success;
    }

    private readonly Manager _manager;
}