using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using AbstractBot.Commands;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using Carespace.FinanceHelper;
using GryphonUtilities;
using Newtonsoft.Json;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot;

public sealed class Bot : BotBaseGoogleSheets<Bot, Config.Config>
{
    public Bot(Config.Config config) : base(config)
    {
        _saveManager = new SaveManager<Data>(Config.SavePath);

        Calendars = new Dictionary<int, Calendar>();

        _weeklyUpdateTimer = new Events.Timer(TimeManager);

        GoogleCredentialJson = string.IsNullOrWhiteSpace(Config.GoogleCredentialJson)
            ? JsonConvert.SerializeObject(Config.GoogleCredential)
            : Config.GoogleCredentialJson;

        if (config.Shares is not null)
        {
            Shares = config.Shares;
        }
        else if (config.SharesJson is not null)
        {
            Dictionary<string, List<Share>>? deserialized =
                JsonConvert.DeserializeObject<Dictionary<string, List<Share>>>(config.SharesJson);
            if (deserialized is not null)
            {
                Shares = deserialized;
            }
        }

        _logsChatId = Config.SuperAdminId.GetValue(nameof(Config.SuperAdminId));
        PracticeIntroduction = string.Join(Environment.NewLine, Config.PracticeIntroduction);
        PracticeSchedule = string.Join(Environment.NewLine, Config.PracticeSchedule);
    }

    internal readonly Dictionary<string, List<Share>> Shares = new();

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Commands.Add(new IntroCommand(this));
        Commands.Add(new ScheduleCommand(this));
        Commands.Add(new ExercisesCommand(this));
        Commands.Add(new LinksCommand(this));
        Commands.Add(new FeedbackCommand(this));
        Commands.Add(new FinanceCommand(this, FinanceManager));
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

            MailAddress? email = textMessage.Text.ToEmail();
            if (email is null)
            {
                await SendStickerAsync(textMessage.Chat, DontUnderstandSticker);
            }
            else
            {
                await EmailChecker.CheckEmailAsync(textMessage.Chat, email);
            }
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
            Id = _logsChatId,
            Type = ChatType.Private
        };
        await EventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, true);
        Schedule(() => EventManager.PostOrUpdateWeekEventsAndScheduleAsync(logsChat, false),
            nameof(EventManager.PostOrUpdateWeekEventsAndScheduleAsync));
    }

    private void Schedule(Func<Task> func, string funcName)
    {
        DateTimeOffset nextUpdateAt =
            DateTimeOffsetHelper.FromOnly(Utils.GetMonday(TimeManager).AddDays(7), Config.EventsUpdateAt);
        _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeeklyAsync(func, funcName), funcName);
    }

    private async Task DoAndScheduleWeeklyAsync(Func<Task> func, string funcName)
    {
        await func();
        _weeklyUpdateTimer.DoWeekly(func, funcName);
    }

    public readonly IDictionary<int, Calendar> Calendars;

    internal readonly string GoogleCredentialJson;
    internal readonly string PracticeIntroduction;
    internal readonly string PracticeSchedule;

    internal Manager EventManager => _eventManager ??= new Manager(this, _saveManager);

    private FinanceManager FinanceManager => _financeManager ??= new FinanceManager(this);
    private EmailChecker EmailChecker => _emailChecker ??= new EmailChecker(this, FinanceManager);

    private Manager? _eventManager;
    private EmailChecker? _emailChecker;
    private FinanceManager? _financeManager;

    private readonly Events.Timer _weeklyUpdateTimer;
    private readonly SaveManager<Data> _saveManager;
    private readonly long _logsChatId;

    private const int MessageToDeleteNotFoundCode = 400;
    private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
    private const int CantInitiateConversationCode = 403;
    private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
}