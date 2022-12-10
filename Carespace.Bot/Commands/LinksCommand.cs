using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Config;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class LinksCommand : CommandOperation
{
    protected override byte MenuOrder => 5;

    public LinksCommand(Bot bot) : base(bot, "links", "полезные ссылки") => _bot = bot;

    protected override async Task ExecuteAsync(Message _, Chat chat, string? __)
    {
        foreach (Link link in _bot.Config.Links)
        {
            await _bot.SendMessageAsync(link, chat);
        }
    }

    private readonly Bot _bot;
}