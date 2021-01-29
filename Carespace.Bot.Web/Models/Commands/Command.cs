﻿using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    public abstract class Command
    {
        internal virtual string Name => "";
        internal virtual string Description => "";
        internal virtual bool AdminsOnly => false;

        internal bool IsInvokingBy(Message message, bool fromChat, string botName)
        {
            return (message.Type == MessageType.Text)
                   && (message.Text == (fromChat ? $"/{Name}@{botName}" : $"/{Name}"));
        }

        internal abstract Task ExecuteAsync(ChatId chatId, ITelegramBotClient client);
    }
}
