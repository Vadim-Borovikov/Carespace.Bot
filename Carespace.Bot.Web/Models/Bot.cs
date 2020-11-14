using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models.Events;
using GoogleDocumentsUnifier.Logic;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Bot : IBot
    {
        public TelegramBotClient Client { get; }

        public IReadOnlyCollection<Command> Commands => _commands.AsReadOnly();
        public IEnumerable<int> AdminIds => Config.AdminIds;
        public IDictionary<int, Calendar> Calendars { get; }

        public Config.Config Config { get; }

        public Bot(IOptions<Config.Config> options)
        {
            Config = options.Value;

            Client = new TelegramBotClient(Config.Token);

            if (string.IsNullOrWhiteSpace(Config.GoogleCredentialsJson))
            {
                Config.GoogleCredentialsJson = JsonConvert.SerializeObject(Config.GoogleCredentials);
            }

            Calendars = new Dictionary<int, Calendar>();
        }

        public void InitCommands(DataManager googleDataManager, Manager eventManager)
        {
            _commands = new List<Command>
            {
                new CustomCommand(Config.DocumentIds, Config.PdfFolderPath, googleDataManager),
                new UpdateCommand(Config.DocumentIds, Config.PdfFolderId, Config.PdfFolderPath, googleDataManager),
                new CheckListCommand(Config.CheckList),
                new ExercisesCommand(Config.Template, Config.ExersisesLinks),
                new LinksCommand(Config.Links),
                new WeekCommand(eventManager)
            };

            var startCommand = new StartCommand(Commands);

            _commands.Insert(0, startCommand);
        }

        private List<Command> _commands;
    }
}