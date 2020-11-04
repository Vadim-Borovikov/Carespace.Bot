﻿using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models.Events;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models.Bot
{
    public interface IBot
    {
        TelegramBotClient Client { get; set; }
        IReadOnlyCollection<Command> Commands { get; }
        IEnumerable<int> AdminIds { get; }
        IDictionary<int, Calendar> Calendars { get; }
        Configuration Config { get; }

        void InitCommands(DataManager googleDataManager, Manager eventManager);
    }
}