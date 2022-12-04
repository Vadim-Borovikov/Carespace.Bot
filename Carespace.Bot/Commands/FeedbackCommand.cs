using System.Threading.Tasks;
using AbstractBot.Commands;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class FeedbackCommand : CommandBaseCustom<Bot, Config.Config>
{
    public FeedbackCommand(Bot bot) : base(bot, "feedback", "оставить обратную связь") { }

    public override Task ExecuteAsync(Message message, Chat chat, string? payload)
    {
        return Bot.SendMessageAsync(Bot.Config.FeedbackLink, chat);
    }
}