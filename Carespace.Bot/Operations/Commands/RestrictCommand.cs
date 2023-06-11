using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Operations;
using Carespace.Bot.AntiSpam;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal abstract class RestrictCommand : CommandOperation
{
    protected override Access AccessLevel => Access.Admin;

    protected override bool EnabledInGroups => true;

    protected RestrictCommand(Bot bot, Manager manager, string command, string description)
        : base(bot, command, description)
    {
        Manager = manager;
    }

    protected override bool IsInvokingBy(Message message, out string? payload)
    {
        bool result = base.IsInvokingBy(message, out payload);
        if (!result)
        {
            return false;
        }

        if (message.ReplyToMessage is null)
        {
            return false;
        }

        if (message.Chat.Id != Manager.Chat.Id)
        {
            return false;
        }

        return true;
    }

    protected override Task ExecuteAsync(Message message, long _, string? __)
    {
        TelegramUser user = new(message.ReplyToMessage.GetValue().From.GetValue());
        TelegramUser admin = new(message.From.GetValue());
        return ExecuteAsync(user, admin);
    }

    protected abstract Task ExecuteAsync(TelegramUser user, TelegramUser admin);

    protected readonly Manager Manager;
}