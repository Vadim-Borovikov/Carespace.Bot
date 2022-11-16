using System.Threading.Tasks;
using AbstractBot.Commands;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal abstract class TextCommand : CommandBaseCustom<Bot, Config.Config>
{
    protected TextCommand(Bot bot, string command, string description, string text) : base(bot, command, description)
    {
        _text = text;
    }

    public override Task ExecuteAsync(Message message, bool fromChat, string? payload)
    {
        User user = message.From.GetValue(nameof(message.From));
        Chat chat = new()
        {
            Id = user.Id,
            Type = ChatType.Private
        };
        return Bot.SendTextMessageAsync(chat, _text, ParseMode.MarkdownV2);
    }

    private readonly string _text;
}