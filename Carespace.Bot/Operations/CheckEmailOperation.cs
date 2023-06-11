using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Email;
using Carespace.FinanceHelper;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations;

internal sealed class CheckEmailOperation : Operation
{
    protected override byte MenuOrder => 12;

    public CheckEmailOperation(Bot bot, Checker checker) : base(bot) => _checker = checker;

    protected override async Task<ExecutionResult> TryExecuteAsync(Message message, long senderId)
    {
        if ((message.Type != MessageType.Text) || string.IsNullOrWhiteSpace(message.Text))
        {
            return ExecutionResult.UnsuitableOperation;
        }

        MailAddress? email = message.Text.ToEmail();
        if (email is null)
        {
            return ExecutionResult.UnsuitableOperation;
        }

        if (!IsAccessSuffice(senderId))
        {
            return ExecutionResult.InsufficentAccess;
        }

        await _checker.CheckEmailAsync(message.Chat, email);
        return ExecutionResult.Success;
    }

    private readonly Checker _checker;
}