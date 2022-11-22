using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GryphonUtilities;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot;

public sealed class Bot : BotBaseGoogleSheets<Bot, Config>
{
    public Bot(Config config) : base(config)
    {
        _saveManager = new SaveManager<Data>(Config.SavePath);

        Calendars = new Dictionary<int, Calendar>();

        _weeklyUpdateTimer = new Events.Timer();

        AdditionalConverters[typeof(DateOnly)] = AdditionalConverters[typeof(DateOnly?)] = o => GetDateOnly(o);
        AdditionalConverters[typeof(TimeOnly)] = AdditionalConverters[typeof(TimeOnly?)] = o => GetTimeOnly(o);
        AdditionalConverters[typeof(TimeSpan)] = AdditionalConverters[typeof(TimeSpan?)] = o => GetTimeSpan(o);
        AdditionalConverters[typeof(Uri)] = Utils.ToUri;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Commands.Add(new WeekCommand(this));
        Commands.Add(new ConfirmCommand(this));

        await base.StartAsync(cancellationToken);

        AbstractBot.Utils.FireAndForget(_ => PostOrUpdateWeekEventsAndScheduleAsync(), cancellationToken);
    }

    public override void Dispose()
    {
        _weeklyUpdateTimer.Dispose();
        _eventManager?.Dispose();
        base.Dispose();
    }

    protected override async Task ProcessTextMessageAsync(Message textMessage, bool fromChat,
        CommandBase? command = null, string? payload = null)
    {
        if (command is null)
        {
            if (fromChat)
            {
                return;
            }

            await SendStickerAsync(textMessage.Chat, DontUnderstandSticker);

            return;
        }

        if (fromChat)
        {
            try
            {
                await DeleteMessageAsync(textMessage.Chat, textMessage.MessageId);
            }
            catch (ApiRequestException e)
                when ((e.ErrorCode == MessageToDeleteNotFoundCode) && (e.Message == MessageToDeleteNotFoundText))
            {
                return;
            }
        }

        User user = textMessage.From.GetValue(nameof(textMessage.From));
        bool shouldExecute = IsAccessSuffice(user.Id, command.Access);
        if (!shouldExecute)
        {
            if (!fromChat)
            {
                await SendStickerAsync(textMessage.Chat, ForbiddenSticker);
            }
            return;
        }

        try
        {
            await command.ExecuteAsync(textMessage, fromChat, payload);
        }
        catch (ApiRequestException e)
            when ((e.ErrorCode == CantInitiateConversationCode) && (e.Message == CantInitiateConversationText))
        {
        }
    }

    protected override Task UpdateAsync(Message message, bool fromChat, CommandBase? command = null,
        string? payload = null)
    {
        if (fromChat && (message.Type != MessageType.Text) && (message.Type != MessageType.SuccessfulPayment))
        {
            return Task.CompletedTask;
        }

        return base.UpdateAsync(message, fromChat, command, payload);
    }

    private async Task PostOrUpdateWeekEventsAndScheduleAsync()
    {
        Chat logsChat = new()
        {
            Id = Config.LogsChatId,
            Type = ChatType.Private
        };
        await EventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, true);
        Schedule(() => EventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, false),
            nameof(EventManager.PostOrUpdateWeekEventsAndScheduleAsync));
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

        DateTimeFull? dtf = GetDateTimeFull(o);
        return dtf?.DateOnly;
    }

    private TimeOnly? GetTimeOnly(object? o)
    {
        if (o is TimeOnly t)
        {
            return t;
        }

        DateTimeFull? dtf = GetDateTimeFull(o);
        return dtf?.TimeOnly;
    }

    private TimeSpan? GetTimeSpan(object? o)
    {
        if (o is TimeSpan t)
        {
            return t;
        }

        DateTimeFull? dtf = GetDateTimeFull(o);
        return dtf?.DateTimeOffset.TimeOfDay;
    }

    public readonly IDictionary<int, Calendar> Calendars;

    internal Manager EventManager => _eventManager ??= new Manager(this, _saveManager);

    private Manager? _eventManager;

    private readonly Events.Timer _weeklyUpdateTimer;
    private readonly SaveManager<Data> _saveManager;

    private const int MessageToDeleteNotFoundCode = 400;
    private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
    private const int CantInitiateConversationCode = 403;
    private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
}