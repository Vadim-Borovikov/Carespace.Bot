using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Save;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class RestrictionsManager
{
    public readonly Chat Chat;

    public RestrictionsManager(Bot bot, SaveManager<Data> saveManager)
    {
        _bot = bot;
        _saveManager = saveManager;

        Chat = new Chat
        {
            Id = _bot.Config.DiscussGroupId,
            Username = $"@{_bot.Config.DiscussGroupLogin}",
            Type = ChatType.Channel
        };

        _permissions = new ChatPermissions
        {
            CanSendMessages = false,
            CanSendAudios = false,
            CanSendDocuments = false,
            CanSendPhotos = false,
            CanSendVideos = false,
            CanSendVideoNotes = false,
            CanSendVoiceNotes = false
        };
    }

    public Task Strike(TelegramUser user, TelegramUser admin) => Restrict(1, user, admin);
    public Task Destroy(TelegramUser user, TelegramUser admin)
    {
        return Restrict(_bot.Config.InitialStrikesForSpammers, user, admin);
    }

    private async Task Restrict(ushort initialStrikes, TelegramUser user, TelegramUser admin)
    {
        _saveManager.Load();

        if (_saveManager.Data.Strikes.ContainsKey(user.Id))
        {
            _saveManager.Data.Strikes[user.Id] =
                (byte) Math.Max(_saveManager.Data.Strikes[user.Id] + 1, initialStrikes);
        }
        else
        {
            _saveManager.Data.Strikes[user.Id] = initialStrikes;
        }

        _saveManager.Save();

        ushort strikes = _saveManager.Data.Strikes[user.Id];

        List<string?> formatLines = _bot.Config.RestrictionWarningMessageFormatLines;
        uint days = 1;
        if (strikes > 1)
        {
            formatLines = _bot.Config.RestrictionMessageFormatLines;

            TimeSpan period = TimeSpan.FromDays(Math.Pow(2, strikes - 2));
            DateTime until = _bot.TimeManager.Now().UtcDateTime.Add(period);
            days = (uint) period.TotalDays;

            await _bot.Client.RestrictChatMemberAsync(Chat, user.Id, _permissions, false, until);
        }

        string daysPart = GryphonUtilities.Text.FormatNumericWithNoun(_bot.Config.DaysFormat, days,
            _bot.Config.DaysForm1, _bot.Config.DaysForm24, _bot.Config.DaysFormAlot);

        string message = GryphonUtilities.Text.FormatLines(formatLines,
            AbstractBot.Bots.Bot.EscapeCharacters(admin.ShortDescriptor),
            AbstractBot.Bots.Bot.EscapeCharacters(user.ShortDescriptor),
            AbstractBot.Bots.Bot.EscapeCharacters(daysPart),
            AbstractBot.Bots.Bot.EscapeCharacters(_bot.Config.ChatGuidelinesUri.AbsoluteUri));
        await _bot.SendTextMessageAsync(Chat, message, ParseMode.MarkdownV2);
    }

    private readonly Bot _bot;
    private readonly SaveManager<Data> _saveManager;
    private readonly ChatPermissions _permissions;
}