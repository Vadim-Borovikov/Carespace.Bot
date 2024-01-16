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
using System.Net.Mail;
using System.Linq;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config, Texts, Data, CommandDataSimple>
{
    [Flags]
    internal enum AccessType
    {
        [UsedImplicitly]
        Default = 1,
        Admin = 2,
        Finance = 4
    }

    public Bot(Config config) : base(config)
    {
        Dictionary<Type, Func<object?, object?>> additionalConverters = new()
        {
            { typeof(Uri), o => o.ToUri() }
        };

        _financeManager = new FinanceManager(this, DocumentsManager, additionalConverters);
        EmailChecker emailChecker = new(this, _financeManager);

        RestrictionsManager antiSpam = new(this, SaveManager);

        Operations.Add(new IntroCommand(this));
        Operations.Add(new ScheduleCommand(this));
        Operations.Add(new ExercisesCommand(this));
        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));

        Operations.Add(new CheckEmailOperation(this, emailChecker));
        Operations.Add(new AcceptPurchase(this, _financeManager));

        WarningCommand warningCommand = new(this, antiSpam);
        SpamCommand spamCommand = new(this, antiSpam);
        Operations.Add(warningCommand);
        Operations.Add(spamCommand);

        _restrictCommands = new List<BotCommand>
        {
            warningCommand.BotCommand,
            spamCommand.BotCommand
        };
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await Client.DeleteMyCommandsAsync(BotCommandScope.ChatAdministrators(Config.DiscussGroupId),
            cancellationToken: cancellationToken);

        await Client.SetMyCommandsAsync(_restrictCommands,
            BotCommandScope.ChatAdministrators(Config.DiscussGroupId), cancellationToken: cancellationToken);
    }

    public Task OnSubmissionReceivedAsync(string name, MailAddress email, string telegram, List<string> items,
        List<Uri> slips)
    {
        List<byte> productIds = Config.Products.Where(p => items.Contains(p.Value.Name)).Select(p => p.Key).ToList();
        return _financeManager.ProcessSubmissionAsync(name, email, telegram, productIds, slips);
    }

    private readonly List<BotCommand> _restrictCommands;
    private readonly FinanceManager _financeManager;
}