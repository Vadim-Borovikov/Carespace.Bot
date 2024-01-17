﻿using System;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Configs.MessageTemplates;
using AbstractBot.Extensions;
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

        MessageTemplateText messageTemplate =
               _bot.Config.Texts.RestrictionMessageFormat.Format(admin.ShortDescriptor.Escape(),
                   restrictionPart.Escape(), comingNext.Escape(),
                    _bot.Config.Texts.ChatGuidelinesUri.AbsoluteUri.Escape());
        await messageTemplate.SendAsync(_bot, Chat);
    }

    private byte UpdateStrikes(byte initialStrikes, long userId)
    {
        _saveManager.Load();

        _saveManager.SaveData.Strikes[userId] = _saveManager.SaveData.Strikes.ContainsKey(userId)
            ? Math.Max(initialStrikes, GetNextStrikes(_saveManager.SaveData.Strikes[userId]))
            : initialStrikes;

        _saveManager.Save();

        return _saveManager.SaveData.Strikes[userId];
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