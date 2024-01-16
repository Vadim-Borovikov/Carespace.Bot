using AbstractBot;
using AbstractBot.Operations.Data;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Info;

internal sealed class RestrictCommandInfo : ICommandData<RestrictCommandInfo>
{
    public readonly TelegramUser User;
    public readonly TelegramUser Admin;

    private RestrictCommandInfo(TelegramUser user, TelegramUser admin)
    {
        User = user;
        Admin = admin;
    }

    public static RestrictCommandInfo? From(Message message, User sender, string[] parameters)
    {
        if (message.ReplyToMessage?.From is null)
        {
            return null;
        }

        if (message.From is null)
        {
            return null;
        }

        TelegramUser user = new(message.ReplyToMessage.From);
        TelegramUser admin = new(message.From);

        return new RestrictCommandInfo(user, admin);
    }
}