using System.Threading.Tasks;
using AbstractBot.Commands;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal sealed class FeedbackCommand : CommandBaseCustom<Bot, Config.Config>
{
    public FeedbackCommand(Bot bot) : base(bot, "feedback", "оставить обратную связь") { }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
        return Bot.SendMessageAsync(Bot.Config.FeedbackLink, chat);
    }
}