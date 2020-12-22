using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class StartCommand : Command
    {
        internal override string Name => "start";
        internal override string Description => "список команд";

        public StartCommand(IReadOnlyCollection<Command> commands) => _commands = commands;

        internal override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Привет!");
            builder.AppendLine();
            AppendCommands(builder, _commands);

            return client.SendTextMessageAsync(chatId, builder.ToString());
        }

        private static void AppendCommands(StringBuilder builder, IEnumerable<Command> commands)
        {
            foreach (Command command in commands)
            {
                builder.AppendLine(GetCommandLine(command));
            }
        }
        private static string GetCommandLine(Command command) => $"/{command.Name} – {command.Description}";

        private readonly IReadOnlyCollection<Command> _commands;
    }
}
