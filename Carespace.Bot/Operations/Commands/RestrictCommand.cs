using System;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Operations.Commands;
using Carespace.Bot.Operations.Info;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal abstract class RestrictCommand : Command<RestrictCommandInfo>
{
    public override Enum AccessRequired => Carespace.Bot.Bot.AccessType.Admin;

    protected override bool EnabledInGroups => true;

    protected RestrictCommand(Bot bot, RestrictionsManager antiSpam, string command, string description)
        : base(bot, command, description)
    {
        AntiSpam = antiSpam;
    }

    protected override bool IsInvokingBy(Message message, User sender, out RestrictCommandInfo? data)
    {
        return base.IsInvokingBy(message, sender, out data)
               && message.ReplyToMessage is not null
               && (message.Chat.Id == AntiSpam.Chat.Id);
    }

    protected override Task ExecuteAsync(RestrictCommandInfo data, Message message, User sender)
    {
        return ExecuteAsync(data.User, data.Admin);
    }

    protected abstract Task ExecuteAsync(TelegramUser user, TelegramUser admin);

    protected readonly RestrictionsManager AntiSpam;
}