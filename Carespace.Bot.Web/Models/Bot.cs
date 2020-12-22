using System.Collections.Generic;
using Carespace.Bot.Web.Models.Commands;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Bot : IBot
    {
        public TelegramBotClient Client { get; }

        public IReadOnlyCollection<Command> Commands => _commands.AsReadOnly();

        public Config.Config Config { get; }

        public Bot(IOptions<Config.Config> options)
        {
            Config = options.Value;

            Client = new TelegramBotClient(Config.Token);
        }

        public void InitCommands()
        {
            _commands = new List<Command>
            {
                new IntroCommand(Config.Introduction),
                new ExercisesCommand(Config.Template, Config.ExersisesLinks),
                new LinksCommand(Config.Links),
                new FeedbackCommand(Config.FeedbackLink),
                new ThanksCommand(Config.Payees, Config.Banks),
            };

            var startCommand = new StartCommand(Commands);

            _commands.Insert(0, startCommand);
        }

        private List<Command> _commands;
    }
}