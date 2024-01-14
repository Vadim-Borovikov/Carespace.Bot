using System.Threading.Tasks;
using AbstractBot.Operations;
using Carespace.Bot.Configs;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class LinksCommand : CommandOperation
{
    protected override byte MenuOrder => 5;

    public LinksCommand(Bot bot) : base(bot, "links", "полезные ссылки") => _bot = bot;

    protected override async Task ExecuteAsync(Message message, long _, string? __)
    {
        foreach (Link link in _bot.Config.Links)
        {
            await _bot.SendMessageAsync(link, message.Chat);
        }
    }

    private readonly Bot _bot;
}