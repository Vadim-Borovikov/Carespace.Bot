using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GoogleSheetsManager.Providers;
using GryphonUtilities;
using Telegram.Bot.Exceptions;
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
        EventManager = new Manager(this, saveManager);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Commands.Add(new WeekCommand(this));
        Commands.Add(new ConfirmCommand(this));

        await base.StartAsync(cancellationToken);

        AbstractBot.Utils.FireAndForget(_ => PostOrUpdateWeekEventsAndScheduleAsync(), cancellationToken);
    }

    public void Dispose()
    {
        _weeklyUpdateTimer.Dispose();
        EventManager.Dispose();
        GoogleSheetsProvider.Dispose();
    }

    protected override Task UpdateAsync(Message message, Chat senderChat, CommandBase? command = null,
        string? payload = null)
    {
        if (AbstractBot.Utils.IsGroup(message.Chat) && (message.Type != MessageType.Text)
                                                    && (message.Type != MessageType.SuccessfulPayment))
        {
            return Task.CompletedTask;
        }

        return base.UpdateAsync(message, senderChat, command, payload);
    }

    protected override async Task ProcessTextMessageAsync(Message textMessage, Chat senderChat,
        CommandBase? command = null, string? payload = null)
    {
        bool fromPrivateChat = textMessage.Chat.Type == ChatType.Private;
        if (command is null)
        {
            if (fromPrivateChat)
            {
                await SendStickerAsync(textMessage.Chat, DontUnderstandSticker);
            }
            return;
        }

        if (!fromPrivateChat)
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

        if (senderChat.Type != ChatType.Private)
        {
            return;
        }

        if (GetMaximumAccessFor(senderChat.Id) < command.Access)
        {
            if (fromPrivateChat)
            {
                await SendStickerAsync(textMessage.Chat, ForbiddenSticker);
            }
            return;
        }

        try
        {
            await command.ExecuteAsync(textMessage, senderChat, payload);
        }
        catch (ApiRequestException e)
            when ((e.ErrorCode == CantInitiateConversationCode) && (e.Message == CantInitiateConversationText))
        {
        }
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

    internal readonly Manager EventManager;

    private readonly Events.Timer _weeklyUpdateTimer;

    private const int MessageToDeleteNotFoundCode = 400;
    private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
    private const int CantInitiateConversationCode = 403;
    private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
}