using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models.Services
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
        IReadOnlyCollection<Command> Commands { get; }
        IEnumerable<int> AdminIds { get; }
    }
}