using System.Threading.Tasks;
using AbstractBot.Commands;
using Carespace.Bot.Config;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class LinksCommand : CommandBaseCustom<Bot, Config.Config>
{
    public LinksCommand(Bot bot) : base(bot, "links", "полезные ссылки")  { }

    public override async Task ExecuteAsync(Message message, Chat chat, string? payload)
    {
        foreach (Link link in Bot.Config.Links)
        {
            await Bot.SendMessageAsync(link, chat);
        }
    }
}