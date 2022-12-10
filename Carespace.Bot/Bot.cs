using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GoogleSheetsManager.Providers;
using Carespace.FinanceHelper;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;
using Carespace.Bot.Email;

namespace Carespace.Bot;

public sealed class Bot : BotBaseCustom<Config.Config>, IDisposable
{
    public readonly IDictionary<int, Calendar> Calendars = new Dictionary<int, Calendar>();

    internal readonly Dictionary<string, List<Share>> Shares = new();

    internal readonly SheetsProvider GoogleSheetsProvider;
    internal readonly Dictionary<Type, Func<object?, object?>> AdditionalConverters;

    internal readonly string PracticeIntroduction;
    internal readonly string PracticeSchedule;

    public Bot(Config.Config config) : base(config)
    {
        if (config.Shares is not null)
        {
            Shares = config.Shares;
        }
        else if (config.SharesJson is not null)
        {
            Dictionary<string, List<Share>>? deserialized =
                JsonSerializer.Deserialize<Dictionary<string, List<Share>>>(config.SharesJson,
                    JsonSerializerOptionsProvider.PascalCaseOptions);
            if (deserialized is not null)
            {
                Shares = deserialized;
            }
        }

        _logsChatId = Config.SuperAdminId.GetValue(nameof(Config.SuperAdminId));
        PracticeIntroduction = string.Join(Environment.NewLine, Config.PracticeIntroduction);
        PracticeSchedule = string.Join(Environment.NewLine, Config.PracticeSchedule);

        GoogleSheetsProvider = new SheetsProvider(config, config.GoogleSheetId);

        AdditionalConverters = new Dictionary<Type, Func<object?, object?>>
        {
            { typeof(Uri), Utils.ToUri }
        };
        AdditionalConverters[typeof(DateOnly)] = AdditionalConverters[typeof(DateOnly?)] = o => GetDateOnly(o);
        AdditionalConverters[typeof(TimeOnly)] = AdditionalConverters[typeof(TimeOnly?)] = o => GetTimeOnly(o);
        AdditionalConverters[typeof(TimeSpan)] = AdditionalConverters[typeof(TimeSpan?)] = o => GetTimeSpan(o);

        SaveManager<Data> saveManager = new(Config.SavePath, TimeManager);
        _eventManager = new Manager(this, saveManager);
        FinanceManager financeManager = new(this);
        Checker emailChecker = new(this, financeManager);

        Operations.Add(new WeekCommand(this, _eventManager));
        Operations.Add(new ConfirmCommand(this, _eventManager));
        Operations.Add(new IntroCommand(this));
        Operations.Add(new ScheduleCommand(this));
        Operations.Add(new ExercisesCommand(this, config));
        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));
        Operations.Add(new FinanceCommand(this, financeManager));
        Operations.Add(new WeekCommand(this, _eventManager));
        Operations.Add(new ConfirmCommand(this, _eventManager));
        Operations.Add(new CheckOperation(this, emailChecker));
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        AbstractBot.Utils.FireAndForget(_ => PostOrUpdateWeekEventsAndScheduleAsync(), cancellationToken);
    }

    public void Dispose()
    {
        _weeklyUpdateTimer.Dispose();
        _eventManager.Dispose();
        GoogleSheetsProvider.Dispose();
    }

    protected override Task UpdateAsync(Message message)
    {
        return AbstractBot.Utils.IsGroup(message.Chat) ? Task.CompletedTask : base.UpdateAsync(message);
    }

    private async Task PostOrUpdateWeekEventsAndScheduleAsync()
    {
        Chat logsChat = new()
        {
            Id = _logsChatId,
            Type = ChatType.Private
        };
        await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, true);
        Schedule(() => _eventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, false),
            nameof(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync));
    }

    private void Schedule(Func<Task> func, string funcName)
    {
        DateTimeFull nextUpdateAt =
            TimeManager.GetDateTimeFull(Utils.GetMonday(TimeManager).AddDays(7), Config.EventsUpdateAt);
        _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeeklyAsync(func, funcName), funcName);
    }

    private async Task DoAndScheduleWeeklyAsync(Func<Task> func, string funcName)
    {
        await func();
        _weeklyUpdateTimer.DoWeekly(func, funcName);
    }

    private DateOnly? GetDateOnly(object? o)
    {
        if (o is DateOnly d)
        {
            return d;
        }

        DateTimeFull? dtf = GoogleSheetsManager.Utils.GetDateTimeFull(o, GoogleSheetsProvider.TimeManager);
        return dtf?.DateOnly;
    }

    private TimeOnly? GetTimeOnly(object? o)
    {
        if (o is TimeOnly t)
        {
            return t;
        }

        DateTimeFull? dtf = GoogleSheetsManager.Utils.GetDateTimeFull(o, GoogleSheetsProvider.TimeManager);
        return dtf?.TimeOnly;
    }

    private TimeSpan? GetTimeSpan(object? o)
    {
        if (o is TimeSpan t)
        {
            return t;
        }

        DateTimeFull? dtf = GoogleSheetsManager.Utils.GetDateTimeFull(o, GoogleSheetsProvider.TimeManager);
        return dtf?.DateTimeOffset.TimeOfDay;
    }

    private readonly Manager _eventManager;

    private readonly Events.Timer _weeklyUpdateTimer = new();
    private readonly long _logsChatId;
}