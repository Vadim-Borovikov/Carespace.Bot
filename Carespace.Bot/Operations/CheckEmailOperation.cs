using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.FinanceHelper;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations;

internal sealed class CheckEmailOperation : Operation<MailAddress>
{
    protected override byte Order => 9;

    public CheckEmailOperation(Bot bot, EmailChecker checker) : base(bot) => _checker = checker;

    protected override bool IsInvokingBy(Message message, User sender, out MailAddress? data)
    {
        data = null;

        if ((message.Type != MessageType.Text) || string.IsNullOrWhiteSpace(message.Text))
        {
            return false;
        }

        data = message.Text.ToEmail();
        return data is not null;
    }

    protected override Task ExecuteAsync(MailAddress data, Message message, User sender)
    {
        return _checker.CheckEmailAsync(message.Chat, data);
    }

    private readonly EmailChecker _checker;
}