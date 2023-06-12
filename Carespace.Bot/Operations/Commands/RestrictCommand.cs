using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Operations;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal abstract class RestrictCommand : CommandOperation
{
    protected override Access AccessLevel => Access.Admin;

    protected override bool EnabledInGroups => true;

    protected RestrictCommand(Bot bot, AntiSpamManager antiSpam, string command, string description)
        : base(bot, command, description)
    {
        AntiSpam = antiSpam;
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

        if (message.Chat.Id != AntiSpam.Chat.Id)
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

    protected readonly AntiSpamManager AntiSpam;
}