using System.Threading.Tasks;
using AbstractBot.Operations.Commands;
using Telegram.Bot.Types;

namespace Carespace.Bot.Operations.Commands;

internal sealed class FeedbackCommand : CommandSimple
{
    protected override byte Order => 6;

    public FeedbackCommand(Bot bot) : base(bot, "feedback", bot.Config.Texts.FeedbackCommandDescription) => _bot = bot;

    protected override Task ExecuteAsync(Message message, User sender)
    {
        return _bot.Config.Texts.FeedbackLink.SendAsync(_bot, message.Chat);
    }

    private readonly Bot _bot;
}