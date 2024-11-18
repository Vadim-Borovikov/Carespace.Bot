using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbstractBot.Configs.MessageTemplates;
using AbstractBot.Operations.Commands;
using AbstractBot.Operations.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Operations.Commands;

internal sealed class AdminCommand : CommandSimple
{
    protected override byte Order => 2;

    protected override bool EnabledInGroups => true;

    public AdminCommand(Bot bot, Chat protectedChat) : base(bot, "admin", bot.Config.Texts.AdminCommandDescription)
    {
        _bot = bot;
        _protectedChat = protectedChat;
        _adminChat = new Chat
        {
            Id = _bot.Config.AdminGroupId,
            Type = ChatType.Supergroup
        };
    }

    protected override bool IsInvokingBy(Message message, User sender, out CommandDataSimple? data)
    {
        return base.IsInvokingBy(message, sender, out data)
               && (message.Chat.Id == _protectedChat.Id);
    }

    protected override async Task ExecuteAsync(Message message, User sender)
    {
        _bot.Config.Texts.AdminCommandReaction.ReplyToMessageId = message.MessageId;
        await _bot.Config.Texts.AdminCommandReaction.SendAsync(_bot, message.Chat);

        Uri messageUri = _bot.GetMessageUri(message.Chat, message.MessageId);

        List<User> admins = await GetAdminsAsync();
        string adminsLine = string.Join(' ', admins.Select(a => $"@{a.Username}"));
        MessageTemplateText messageTemplate = _bot.Config.Texts.AdminCommandPingFormat.Format(messageUri, adminsLine);

        await messageTemplate.SendAsync(_bot, _adminChat);
    }

    private async Task<List<User>> GetAdminsAsync()
    {
        List<User> admins = new();
        foreach (long id in _bot.GetAdminIds())
        {
            Chat chat = await _bot.Client.GetChatAsync(id);
            User user = new()
            {
                Id = id,
                Username = chat.Username
            };
            admins.Add(user);
        }
        return admins;
    }

    private readonly Bot _bot;
    private readonly Chat _protectedChat;
    private readonly Chat _adminChat;
}