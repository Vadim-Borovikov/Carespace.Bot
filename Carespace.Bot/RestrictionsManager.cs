using System;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Configs.MessageTemplates;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class RestrictionsManager : IDisposable
{
    public readonly Chat Chat;

    public RestrictionsManager(Bot bot)
    {
        _bot = bot;

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

        _invoker = new Invoker(bot.Logger);
    }

    public void Dispose() => _invoker.Dispose();

    public Task Strike(TelegramUser user, TelegramUser admin) => Restrict(1, user, admin);
    public Task Destroy(TelegramUser user, TelegramUser admin)
    {
        return Restrict(_bot.Config.InitialStrikesForSpammers, user, admin);
    }

    private async Task Restrict(byte initialStrikes, TelegramUser user, TelegramUser admin)
    {
        byte strikes = UpdateStrikes(initialStrikes, user.Id);

        string restrictionPart;
        if (strikes > 1)
        {
            uint days = GetDaysFor(strikes);
            DateTime until = _bot.Clock.Now().UtcDateTime.AddDays(days);

            await _bot.Client.RestrictChatMemberAsync(Chat, user.Id, _permissions, false, until);

            string daysPart = _bot.Config.Texts.Day.FormatWithNumeric(_bot.Config.Texts.DaysFormat, days);

            restrictionPart = string.Format(_bot.Config.Texts.RestrictionPartFormat, user.ShortDescriptor, daysPart);
        }
        else
        {
            restrictionPart = string.Format(_bot.Config.Texts.RestrictionWarningPartFormat, user.ShortDescriptor);
        }

        ushort nextDays = GetDaysFor(GetNextStrikes(strikes));
        string comingNext = _bot.Config.Texts.Day.FormatWithNumeric(_bot.Config.Texts.DaysFormat, nextDays);

        MessageTemplateText messageTemplate = _bot.Config.Texts.RestrictionMessageFormat.Format(admin.ShortDescriptor,
            restrictionPart, comingNext, _bot.Config.Texts.ChatGuidelinesUri.AbsoluteUri);
        Message restrictionMessage = await messageTemplate.SendAsync(_bot, Chat);
        TimeSpan delay = TimeSpan.FromMinutes(_bot.Config.RestrictionMessagesLifetimeMinutes);
        _invoker.DoAfterDelay(token => _bot.DeleteMessageAsync(Chat, restrictionMessage.MessageId, token), delay);
    }

    private byte UpdateStrikes(byte initialStrikes, long userId)
    {
        byte? oldStrikes = _bot.TryGetStrikes(userId);
        byte strikes =
            oldStrikes.HasValue ? Math.Max(initialStrikes, GetNextStrikes(oldStrikes.Value)) : initialStrikes;
        _bot.UpdateStrikes(userId, strikes);
        return strikes;
    }

    private byte GetNextStrikes(byte strikes)
    {
        byte nextStrikes = (byte) (strikes + 1);
        return GetDaysFor(nextStrikes) == GetDaysFor(strikes) ? strikes : nextStrikes;
    }

    private ushort GetDaysFor(byte strikes)
    {
        return strikes switch
        {
            0 => throw new ArgumentOutOfRangeException(),
            1 => 0,
            _ => Math.Min(_bot.Config.RestrictionsMaxDays, (ushort) Math.Pow(2, strikes - 2))
        };
    }

    private readonly Bot _bot;
    private readonly ChatPermissions _permissions;
    private readonly Invoker _invoker;
}