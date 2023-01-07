using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot.Bots;
using AbstractBot.Extensions;
using Carespace.Bot.Operations.Commands;
using Carespace.Bot.Config;
using Carespace.Bot.Events;
using Carespace.Bot.Save;
using Carespace.FinanceHelper;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Carespace.Bot.Operations;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types.ReplyMarkups;
using Carespace.Bot.Email;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config.Config>
{
    public readonly IDictionary<int, Calendar> Calendars = new Dictionary<int, Calendar>();

    internal readonly Dictionary<string, List<Share>> Shares = new();

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

        long logsChatId = Config.SuperAdminId.GetValue(nameof(Config.SuperAdminId));
        _logsChat = new Chat
        {
            Id = logsChatId,
            Type = ChatType.Private
        };
        PracticeIntroduction = string.Join(Environment.NewLine, Config.PracticeIntroduction);
        PracticeSchedule = string.Join(Environment.NewLine, Config.PracticeSchedule);

        Dictionary<Type, Func<object?, object?>> additionalConverters = new()
        {
            { typeof(Uri), o => o.ToUri() }
        };
        additionalConverters[typeof(DateOnly)] = additionalConverters[typeof(DateOnly?)] =
            o => o.ToDateOnly(TimeManager);
        additionalConverters[typeof(TimeOnly)] = additionalConverters[typeof(TimeOnly?)] =
            o => o.ToTimeOnly(TimeManager);
        additionalConverters[typeof(TimeSpan)] = additionalConverters[typeof(TimeSpan?)] =
            o => o.ToTimeSpan(TimeManager);

        SaveManager<Data> saveManager = new(Config.SavePath, TimeManager);
        _eventManager = new Events.Manager(this, DocumentsManager, additionalConverters, saveManager);
        FinanceManager financeManager = new(this, DocumentsManager, additionalConverters);
        Checker emailChecker = new(this, financeManager);
        _weeklyUpdateTimer = new Events.Timer(Logger);
        _emailManager = new Email.Manager(this, _logsChat);

        Operations.Add(new IntroCommand(this));
        Operations.Add(new ScheduleCommand(this));
        Operations.Add(new ExercisesCommand(this, config));
        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));

        Operations.Add(new WeekCommand(this, _eventManager));
        Operations.Add(new ConfirmCommand(this, _eventManager));

        Operations.Add(new FinanceCommand(this, financeManager));
        Operations.Add(new BookCommand(this, _emailManager));
        Operations.Add(new PrepareEmailOperation(this, _emailManager));
        Operations.Add(new ConfirmEmailCommand(this, _emailManager, financeManager));
        Operations.Add(new CheckEmailOperation(this, emailChecker));
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await _emailManager.StartAsync(cancellationToken);

        AbstractBot.Invoker.FireAndForget(_ => PostOrUpdateWeekEventsAndScheduleAsync(), Logger, cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken) => _emailManager.StopAsync(cancellationToken);

    internal Task SendMessageAsync(Link link, Chat chat)
    {
        if (string.IsNullOrWhiteSpace(link.PhotoPath))
        {
            string text = $"[{EscapeCharacters(link.Name)}]({link.Uri.AbsoluteUri})";
            return SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
        }

        InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
        return PhotoRepository.SendPhotoAsync(this, chat, link.PhotoPath, replyMarkup: keyboard);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            _weeklyUpdateTimer.Dispose();
            _eventManager.Dispose();
            _emailManager.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override Task UpdateAsync(Message message)
    {
        return message.Chat.IsGroup() ? Task.CompletedTask : base.UpdateAsync(message);
    }

    private static InlineKeyboardMarkup GetReplyMarkup(Link link)
    {
        InlineKeyboardButton button = new(link.Name) { Url = link.Uri.AbsoluteUri };
        return new InlineKeyboardMarkup(button);
    }

    private async Task PostOrUpdateWeekEventsAndScheduleAsync()
    {
        await _eventManager.PostOrUpdateWeekEventsAndScheduleAsync(_logsChat, true);
        Schedule(() => _eventManager.PostOrUpdateWeekEventsAndScheduleAsync(_logsChat, false),
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

    private readonly Events.Manager _eventManager;
    private readonly Email.Manager _emailManager;

    private readonly Events.Timer _weeklyUpdateTimer;
    private readonly Chat _logsChat;
}