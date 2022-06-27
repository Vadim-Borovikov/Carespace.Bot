using System.Threading.Tasks;
using AbstractBot;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal abstract class TextCommand : CommandBase<Bot, Config.Config>
{
    protected TextCommand(Bot bot, string? text) : base(bot) => _text = text.GetValue(nameof(text));

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        return Bot.SendTextMessageAsync(user.Id, _text, ParseMode.MarkdownV2);
    }

    private readonly string _text;
}