using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models.Events;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models
{
    public interface IBot
    {
        TelegramBotClient Client { get; }
        IReadOnlyCollection<Command> Commands { get; }
        IEnumerable<int> AdminIds { get; }
        IDictionary<int, Calendar> Calendars { get; }
        Config.Config Config { get; }

        void InitCommands(DataManager googleDataManager, Manager eventManager);
    }
}