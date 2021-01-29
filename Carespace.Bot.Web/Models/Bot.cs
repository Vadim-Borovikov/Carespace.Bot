using System.Collections.Generic;
using System.Globalization;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models.Events;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;
using Calendar = Carespace.Bot.Web.Models.Events.Calendar;

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

            Utils.SetupTimeZoneInfo(Config.SystemTimeZoneId);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(Config.CultureInfoName);

            Client = new TelegramBotClient(Config.Token);

            if (string.IsNullOrWhiteSpace(Config.GoogleCredentialsJson))
            {
                Config.GoogleCredentialsJson = JsonConvert.SerializeObject(Config.GoogleCredentials);
            }

            Calendars = new Dictionary<int, Calendar>();
        }

        public void InitCommands(Manager eventManager)
        {
            _commands = new List<Command>
            {
                new IntroCommand(Config.Introduction),
                new ScheduleCommand(Config.Schedule),
                new ExercisesCommand(Config.Template, Config.ExersisesLinks),
                new LinksCommand(Config.Links),
                new FeedbackCommand(Config.FeedbackLink),
                new ThanksCommand(Config.Payees, Config.Banks),
                new WeekCommand(eventManager)
            };

            var startCommand = new StartCommand(Commands);

            _commands.Insert(0, startCommand);
        }

        private List<Command> _commands;
    }
}