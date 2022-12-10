using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.FinanceHelper;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Email;

internal sealed class CheckOperation : Operation
{
    protected override byte MenuOrder => 10;

    public CheckOperation(Bot bot, Checker checker) : base(bot) => _checker = checker;

    protected override async Task<ExecutionResult> TryExecuteAsync(Message message, Chat sender)
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

        if (!IsAccessSuffice(sender.Id))
        {
            return ExecutionResult.InsufficentAccess;
        }

        Chat chat = BotBase.GetReplyChatFor(message, sender);
        await _checker.CheckEmailAsync(chat, email);
        return ExecutionResult.Success;
    }

    private readonly Checker _checker;
}