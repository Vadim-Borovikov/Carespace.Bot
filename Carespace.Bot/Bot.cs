using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using GoogleSheetsManager.Providers;
using Carespace.FinanceHelper;
using GryphonUtilities;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot;

public sealed class Bot : BotBaseCustom<Config.Config>, IDisposable
{
    public readonly IDictionary<int, Calendar> Calendars = new Dictionary<int, Calendar>();

    internal readonly Dictionary<string, List<Share>> Shares = new();

    internal readonly SheetsProvider GoogleSheetsProvider;
    internal readonly Dictionary<Type, Func<object?, object?>> AdditionalConverters;

    internal readonly string PracticeIntroduction;
    internal readonly string PracticeSchedule;
    internal readonly Manager EventManager;

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
        EventManager = new Manager(this, saveManager);

        _financeManager = new FinanceManager(this);
        _emailChecker = new EmailChecker(this, _financeManager);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Commands.Add(new IntroCommand(this));
        Commands.Add(new ScheduleCommand(this));
        Commands.Add(new ExercisesCommand(this));
        Commands.Add(new LinksCommand(this));
        Commands.Add(new FeedbackCommand(this));
        Commands.Add(new FinanceCommand(this, _financeManager));
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
                MailAddress? email = textMessage.Text.ToEmail();
                if (email is null)
                {
                    await SendStickerAsync(textMessage.Chat, DontUnderstandSticker);
                }
                else
                {
                    await _emailChecker.CheckEmailAsync(textMessage.Chat, email);
                }
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
            Id = _logsChatId,
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

    private readonly EmailChecker _emailChecker;
    private readonly FinanceManager _financeManager;

    private readonly Events.Timer _weeklyUpdateTimer = new();
    private readonly long _logsChatId;

    private const int MessageToDeleteNotFoundCode = 400;
    private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
    private const int CantInitiateConversationCode = 403;
    private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
}