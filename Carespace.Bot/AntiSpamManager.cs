using System;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Save;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

internal sealed class AntiSpamManager
{
    public readonly Chat Chat;

    public AntiSpamManager(Bot bot, SaveManager<Data> saveManager)
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
            await _bot.SendTextMessageAsync(Chat,
                $"*Админ {admin.ShortDescriptor} предупреждает пользователя {user.ShortDescriptor} пользоваться чатом аккуратнее\\.{Environment.NewLine}Следующая мера — read only на 1 день\\.{Environment.NewLine}Пост о принципах чата: {guidelines}\\.*",
                ParseMode.MarkdownV2);
        }
        else
        {
            TimeSpan period = TimeSpan.FromDays(Math.Pow(2, strikes - 1));
            DateTime until = _bot.TimeManager.Now().UtcDateTime.Add(period);
            await _bot.Client.RestrictChatMemberAsync(Chat, user.Id, _permissions, until);
            await _bot.SendTextMessageAsync(Chat,
                $"*Админ {admin.ShortDescriptor} поставил read only пользователю {user.ShortDescriptor}\\. Дней: {period.TotalDays:G}\\.{Environment.NewLine}В следующий раз срок будет увеличен вдвое\\.{Environment.NewLine}Пост о принципах чата: {guidelines}\\.*",
                ParseMode.MarkdownV2);
        }
    }

    private readonly Bot _bot;
    private readonly SaveManager<Data> _saveManager;
    private readonly ChatPermissions _permissions;
}