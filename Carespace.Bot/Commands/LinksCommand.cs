using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class LinksCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "links";
    protected override string Description => "Полезные ссылки";

    public LinksCommand(Bot bot) : base(bot)  { }

    public override async Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
        foreach (Link link in Bot.Config.Links)
        {
            await Bot.SendMessageAsync(link, chat);
        }
    }
}