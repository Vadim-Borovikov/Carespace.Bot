using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using GryphonUtilities;
using Telegram.Bot.Types;

namespace Carespace.Bot.Commands;

internal sealed class FeedbackCommand : CommandBase<Bot, Config.Config>
{
    protected override string Name => "feedback";
    protected override string Description => "Оставить обратную связь";

    public FeedbackCommand(Bot bot) : base(bot) { }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        Link link = Bot.Config.FeedbackLink.GetValue(nameof(Bot.Config.FeedbackLink));
        User user = message.From.GetValue(nameof(message.From));
        return Bot.Client.SendMessageAsync(link, user.Id);
    }
}