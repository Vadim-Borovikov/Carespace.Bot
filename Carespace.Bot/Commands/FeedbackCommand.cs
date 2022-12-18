using System.Threading.Tasks;
using AbstractBot.Operations;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class FeedbackCommand : CommandOperation
{
    protected override byte MenuOrder => 6;

    public FeedbackCommand(Bot bot) : base(bot, "feedback", "оставить обратную связь") => _bot = bot;

    protected override Task ExecuteAsync(Message message, long _, string? __)
    {
        return _bot.SendMessageAsync(_bot.Config.FeedbackLink, message.Chat);
    }

    private readonly Bot _bot;
}