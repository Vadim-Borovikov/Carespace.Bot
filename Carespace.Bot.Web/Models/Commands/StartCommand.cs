using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class StartCommand : Command
    {
        public override string Name => "start";
        public override string Description => "список команд";

        public StartCommand(IReadOnlyCollection<Command> commands) => _commands = commands;

        public override Task ExecuteAsync(ChatId chatId, ITelegramBotClient client)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Привет!");
            builder.AppendLine();
            AppendCommands(builder, _commands);

            return client.SendTextMessageAsync(chatId, builder.ToString());
        }

        private static void AppendCommands(StringBuilder builder, IEnumerable<Command> commands)
        {
            foreach (Command command in commands.Where(c => !c.AdminsOnly))
            {
                builder.AppendLine(GetCommandLine(command));
            }
        }
        private static string GetCommandLine(Command command) => $"/{command.Name} — {command.Description}";

        private readonly IReadOnlyCollection<Command> _commands;
    }
}
