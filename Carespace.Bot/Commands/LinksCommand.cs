using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using GryphonUtilities;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class LinksCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "links";
    protected override string Description => "Полезные ссылки";

    public LinksCommand(Bot bot) : base(bot)  { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        foreach (Link link in Bot.Config.Links)
        {
            await Bot.Client.SendMessageAsync(link, user.Id);
        }
    }
}