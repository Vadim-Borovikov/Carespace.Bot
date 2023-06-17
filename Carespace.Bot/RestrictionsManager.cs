﻿using System;
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

    private async Task Restrict(byte initialStrikes, TelegramUser user, TelegramUser admin)
    {
        byte strikes = UpdateStrikes(initialStrikes, user.Id);

        string restrictionPart;
        if (strikes > 1)
        {
            uint days = GetDaysFor(strikes);
            DateTime until = _bot.TimeManager.Now().UtcDateTime.AddDays(days);

            await _bot.Client.RestrictChatMemberAsync(Chat, user.Id, _permissions, false, until);

            string daysPart = GryphonUtilities.Text.FormatNumericWithNoun(_bot.Config.DaysFormat, days,
                _bot.Config.DaysForm1, _bot.Config.DaysForm24, _bot.Config.DaysFormAlot);

            restrictionPart = string.Format(_bot.Config.RestrictionPartFormat, user.ShortDescriptor, daysPart);
        }
        else
        {
            restrictionPart = string.Format(_bot.Config.RestrictionWarningPartFormat, user.ShortDescriptor);
        }

        ushort nextDays = GetDaysFor(GetNextStrikes(strikes));
        string comingNext = GryphonUtilities.Text.FormatNumericWithNoun(_bot.Config.DaysFormat, nextDays,
            _bot.Config.DaysForm1, _bot.Config.DaysForm24, _bot.Config.DaysFormAlot);

        string message = GryphonUtilities.Text.FormatLines(_bot.Config.RestrictionMessageFormatLines,
            AbstractBot.Bots.Bot.EscapeCharacters(admin.ShortDescriptor),
            AbstractBot.Bots.Bot.EscapeCharacters(restrictionPart),
            AbstractBot.Bots.Bot.EscapeCharacters(comingNext),
            AbstractBot.Bots.Bot.EscapeCharacters(_bot.Config.ChatGuidelinesUri.AbsoluteUri));
        await _bot.SendTextMessageAsync(Chat, message, ParseMode.MarkdownV2);
    }

    private byte UpdateStrikes(byte initialStrikes, long userId)
    {
        _saveManager.Load();

        _saveManager.Data.Strikes[userId] = _saveManager.Data.Strikes.ContainsKey(userId)
            ? Math.Max(initialStrikes, GetNextStrikes(_saveManager.Data.Strikes[userId]))
            : initialStrikes;

        _saveManager.Save();

        return _saveManager.Data.Strikes[userId];
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
    private readonly SaveManager<Data> _saveManager;
    private readonly ChatPermissions _permissions;
}