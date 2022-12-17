using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot.Bots;
using AbstractBot.Extensions;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config>
{
    public Bot(Config config) : base(config)
    {
        Calendars = new Dictionary<int, Calendar>();

        _weeklyUpdateTimer = new Events.Timer(Logger);

        SaveManager<Data> saveManager = new(Config.SavePath, TimeManager);
        _eventManager = new Manager(this, DocumentsManager, saveManager);

        Operations.Add(new WeekCommand(this, _eventManager));
        Operations.Add(new ConfirmCommand(this, _eventManager));
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        AbstractBot.Invoker.FireAndForget(_ => PostOrUpdateWeekEventsAndScheduleAsync(), Logger, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            _weeklyUpdateTimer.Dispose();
            _eventManager.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override Task UpdateAsync(Message message)
    {
        return message.Chat.IsGroup() ? Task.CompletedTask : base.UpdateAsync(message);
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
            TimeManager.GetDateTimeFull(Week.GetMonday(TimeManager).AddDays(7), Config.EventsUpdateAt);
        _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeeklyAsync(func, funcName), funcName);
    }

    private async Task DoAndScheduleWeeklyAsync(Func<Task> func, string funcName)
    {
        await func();
        _weeklyUpdateTimer.DoWeekly(func, funcName);
    }

    public readonly IDictionary<int, Calendar> Calendars;

    private readonly Manager _eventManager;

    private readonly Events.Timer _weeklyUpdateTimer;
}