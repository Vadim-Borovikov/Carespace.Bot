using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GoogleSheetsManager.Providers;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot;

public sealed class Bot : BotBaseCustom<Config>, IDisposable
{
    internal readonly SheetsProvider GoogleSheetsProvider;
    internal readonly Dictionary<Type, Func<object?, object?>> AdditionalConverters;

    public Bot(Config config) : base(config)
    {
        Calendars = new Dictionary<int, Calendar>();

        _weeklyUpdateTimer = new Events.Timer();

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

        Operations.Add(new WeekCommand(this, _eventManager));
        Operations.Add(new ConfirmCommand(this, _eventManager));
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
            Id = Config.LogsChatId,
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

    public readonly IDictionary<int, Calendar> Calendars;

    private readonly Manager _eventManager;

    private readonly Events.Timer _weeklyUpdateTimer;
}