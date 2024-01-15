using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot.Bots;
using AbstractBot.Operations.Data;
using Carespace.Bot.Configs;
using Carespace.Bot.Operations.Commands;
using Carespace.Bot.Save;
using Carespace.FinanceHelper;
using Telegram.Bot.Types;
using Carespace.Bot.Operations;
using Telegram.Bot;
using JetBrains.Annotations;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config, Texts, Data, CommandDataSimple>
{
    [Flags]
    internal enum AccessType
    {
        [UsedImplicitly]
        Default = 1,
        Admin = 2
    }

    internal readonly Dictionary<string, List<Share>> Shares;

    public Bot(Config config) : base(config)
    {
        Shares =
            config.GetShares(JsonSerializerOptionsProvider.PascalCaseOptions) ?? new Dictionary<string, List<Share>>();

        Dictionary<Type, Func<object?, object?>> additionalConverters = new()
        {
            { typeof(Uri), o => o.ToUri() }
        };

        FinanceManager financeManager = new(this, DocumentsManager, additionalConverters);
        EmailChecker emailChecker = new(this, financeManager);

        RestrictionsManager antiSpam = new(this, SaveManager);

        Operations.Add(new IntroCommand(this));
        Operations.Add(new ScheduleCommand(this));
        Operations.Add(new ExercisesCommand(this));
        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));

        Operations.Add(new CheckEmailOperation(this, emailChecker));

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

    private readonly List<BotCommand> _restrictCommands;
}