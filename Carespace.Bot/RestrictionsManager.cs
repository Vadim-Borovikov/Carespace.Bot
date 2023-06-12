using System;
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
            CanSendMessages = false
        };
    }

    public Task Strike(TelegramUser user, TelegramUser admin) => Restrict(0, user, admin);
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
                (byte)Math.Max(_saveManager.Data.Strikes[user.Id] + 1, initialStrikes);
        }
        else
        {
            _saveManager.Data.Strikes[user.Id] = initialStrikes;
        }

        _saveManager.Save();

        ushort strikes = _saveManager.Data.Strikes[user.Id];
        string guidelines = AbstractBot.Bots.Bot.EscapeCharacters(_bot.Config.ChatGuidelinesUri.AbsoluteUri);
        if (strikes == 0)
        {
            string message = GryphonUtilities.Text.FormatLines(_bot.Config.RestrictionWarningMessageFormatLines,
                admin.ShortDescriptor, user.ShortDescriptor, guidelines);
            await _bot.SendTextMessageAsync(Chat, message, ParseMode.MarkdownV2);
        }
        else
        {
            TimeSpan period = TimeSpan.FromDays(Math.Pow(2, strikes - 1));
            DateTime until = _bot.TimeManager.Now().UtcDateTime.Add(period);
            uint days = (uint) period.TotalDays;
            await _bot.Client.RestrictChatMemberAsync(Chat, user.Id, _permissions, until);
            string message = GryphonUtilities.Text.FormatLines(_bot.Config.RestrictionMessageFormatLines,
                admin.ShortDescriptor, user.ShortDescriptor, days, guidelines);
            await _bot.SendTextMessageAsync(Chat, message, ParseMode.MarkdownV2);
        }
    }

    private readonly Bot _bot;
    private readonly SaveManager<Data> _saveManager;
    private readonly ChatPermissions _permissions;
}