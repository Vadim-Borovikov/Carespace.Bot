using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot.Bots;
using AbstractBot.Operations.Data;
using Carespace.Bot.Configs;
using Carespace.Bot.Operations.Commands;
using Carespace.Bot.Save;
using Telegram.Bot.Types;
using Carespace.Bot.Operations;
using Telegram.Bot;
using JetBrains.Annotations;
using System.Linq;
using AbstractBot.Extensions;
using AbstractBot.Operations;
using AbstractBot.Operations.Commands;
using Carespace.Bot.Extensions;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config, Texts, Data, CommandDataSimple>
{
    [Flags]
    internal enum AccessType
    {
        [UsedImplicitly]
        Default = 1,
        Admin = 2,
        Duty = 4,

        AdminOnDuty =  Admin | Duty,

        Finance = 8
    }

    public Bot(Config config) : base(config)
    {
        Dictionary<Type, Func<object?, object?>> additionalConverters = new()
        {
            { typeof(Uri), o => o.ToUri() }
        };

        _financeManager = new FinanceManager(this, DocumentsManager, additionalConverters);
        EmailChecker emailChecker = new(this, _financeManager);

        Chat protectedChat = new()
        {
            Id = Config.DiscussGroupId,
            Type = ChatType.Supergroup
        };

        AdminCommand adminCommand = new(this, protectedChat);
        Operations.Add(adminCommand);

        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));

        Operations.Add(new CheckEmailOperation(this, emailChecker));
        Operations.Add(new AcceptPurchase(this, _financeManager));

        _antiSpam = new RestrictionsManager(this, protectedChat);
        WarningCommand warningCommand = new(this, _antiSpam);
        SpamCommand spamCommand = new(this, _antiSpam);
        Operations.Add(warningCommand);
        Operations.Add(spamCommand);

        _chatMenuCommands = new List<BotCommand>
        {
            adminCommand.BotCommand,
            warningCommand.BotCommand,
            spamCommand.BotCommand
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            _antiSpam.Dispose();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await Client.DeleteMyCommandsAsync(BotCommandScope.ChatAdministrators(Config.DiscussGroupId),
            cancellationToken: cancellationToken);

        await Client.SetMyCommandsAsync(_chatMenuCommands,
            BotCommandScope.ChatAdministrators(Config.DiscussGroupId), cancellationToken: cancellationToken);
    }

    protected override async Task<OperationBasic?> UpdateAsync(Message message, User sender,
        string? callbackQueryData = null)
    {
        OperationBasic? operation = await base.UpdateAsync(message, sender, callbackQueryData);

        if (operation is ICommand && message.Chat.IsGroup() && operation is not AdminCommand)
        {
            await DeleteMessageAsync(message.Chat, message.MessageId);
        }

        return operation;
    }

    public Task OnSubmissionReceivedAsync(string id, string name, string email, string telegram, List<string> items,
        List<Uri> slips)
    {
        PurchaseInfo info = new()
        {
            Name = name,
            Email = email,
            Telegram = telegram,
            ProductIds = Config.Products.Where(p => items.Contains(p.Value.Name)).Select(p => p.Key).ToList()
        };

        SaveManager.SaveData.Purchases[id] = info;
        SaveManager.Save();

        return _financeManager.ProcessSubmissionAsync(id, info, slips);
    }

    internal byte? TryGetStrikes(long userId)
    {
        return SaveManager.SaveData.Strikes.ContainsKey(userId) ? SaveManager.SaveData.Strikes[userId] : null;
    }

    internal void UpdateStrikes(long userId, byte strikes)
    {
        SaveManager.SaveData.Strikes[userId] = strikes;
        SaveManager.Save();
    }

    internal void RemovePurchase(string key)
    {
        SaveManager.SaveData.Purchases.Remove(key);
        SaveManager.Save();
    }

    internal PurchaseInfo? TryGetPurchase(string key) => SaveManager.SaveData.Purchases.GetValueOrDefault(key);

    internal List<long> GetAdminOnDutyIds()
    {
        return Accesses.Where(p => p.Value.IsSufficientAgainst(AccessType.AdminOnDuty)).Select(p => p.Key).ToList();
    }

    internal Uri GetMessageUri(Chat chat, int messageId)
    {
        string id = chat.Id.ToString().Replace("-100", "");
        return new Uri(string.Format(Config.ChatMessegeUriFormat, id, messageId));
    }

    private readonly List<BotCommand> _chatMenuCommands;
    private readonly FinanceManager _financeManager;
    private readonly RestrictionsManager _antiSpam;
}