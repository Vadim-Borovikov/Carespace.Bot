using System.Threading.Tasks;
using AbstractBot.Operations.Commands;
using Carespace.Bot.Configs;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class LinksCommand : CommandSimple
{
    protected override byte Order => 3;

    public LinksCommand(Bot bot) : base(bot, "links", bot.Config.Texts.LinksCommandDescription) => _bot = bot;

    protected override async Task ExecuteAsync(Message message, User sender)
    {
        foreach (Link link in _bot.Config.Texts.Links)
        {
            await link.SendAsync(_bot, message.Chat);
        }
    }

    private readonly Bot _bot;
}