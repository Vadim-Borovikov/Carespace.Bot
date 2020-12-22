using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models
{
    public interface IBot
    {
        TelegramBotClient Client { get; }
        IReadOnlyCollection<Command> Commands { get; }
        Config.Config Config { get; }

        void InitCommands();
    }
}