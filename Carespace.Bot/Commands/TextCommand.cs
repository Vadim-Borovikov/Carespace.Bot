using System.Threading.Tasks;
using AbstractBot.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Commands;

internal abstract class TextCommand : CommandBaseCustom<Bot, Config.Config>
{
    protected TextCommand(Bot bot, string command, string description, string text) : base(bot, command, description)
    {
        _text = text;
    }

    public override Task ExecuteAsync(Message message, Chat chat, string? payload)
    {
        return Bot.SendTextMessageAsync(chat, _text, ParseMode.MarkdownV2);
    }

    private readonly string _text;
}