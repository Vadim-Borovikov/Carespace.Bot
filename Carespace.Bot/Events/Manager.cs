using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Commands;
using Carespace.Bot.Save;
using GoogleSheetsManager;
using GoogleSheetsManager.Documents;
using GryphonUtilities;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot.Events;

internal sealed class Manager : IDisposable
{
    public Manager(Bot bot, DocumentsManager documentsManager, SaveManager<Data> saveManager)
    {
        _bot = bot;
        _documentsManager = documentsManager;
        _saveManager = saveManager;
        _eventsChat = new Chat
        {
            Id = _bot.Config.EventsChannelId,
            Type = ChatType.Channel
        };
    }

    public async Task PostOrUpdateWeekEventsAndScheduleAsync(Chat chat, bool shouldConfirm)
    {
        _weekStart = Week.GetMonday(_bot.TimeManager);
        _weekEnd = _weekStart.AddDays(7);

        IEnumerable<Template> templates = await LoadRelevantTemplatesAsync();
        _templates.Clear();
        foreach (Template template in templates)
        {
            _templates[template.Id] = template;
        }
        _saveManager.Load();

        IEnumerable<int> savedTemplateIds = _saveManager.Data.Events.Keys;
        _toPost.Clear();
        _toPost.AddRange(_templates.Values
                                   .Where(t => !savedTemplateIds.Contains(t.Id))
                                   .OrderBy(t => t.StartDate)
                                   .ThenBy(t => t.StartTime));

        if (shouldConfirm && _toPost.Any())
        {
            await AskForConfirmationAsync(chat);
        }
        else
        {
            await PostOrUpdateWeekEventsAndScheduleAsync(chat);
        }
    }

    public Task ConfirmAndPostOrUpdateWeekEventsAndScheduleAsync(Chat chat)
    {
        if (!_waitingForConfirmation)
        {
            return _bot.SendTextMessageAsync(chat, "Обновлений не запланировано.");
        }

        _waitingForConfirmation = false;
        return PostOrUpdateWeekEventsAndScheduleAsync(chat);
    }

    public async Task PostOrUpdateWeekEventsAndScheduleAsync(Chat chat)
    {
        await using (await StatusMessage.CreateAsync(_bot, chat, "Обновляю расписание"))
        {
            await PostOrUpdateEventsAsync();
            await PostOrUpdateScheduleAsync();
            await CreateOrUpdateNotificationsAsync();

            List<int> toRemove = _saveManager.Data.Messages.Keys.Where(IsExcess).ToList();
            foreach (int id in toRemove)
            {
                _saveManager.Data.Messages.Remove(id);
            }

            _saveManager.Save();
        }
    }

    public void Dispose() => DisposeEvents();

    private static Uri GetChannelMessageUri(long channelId, int messageId)
    {
        string channelParameter = channelId.ToString().Remove(0, 4);
        string channelUriPostfix = $"c/{channelParameter}";
        string channelUriString = string.Format(ChannelUriFormat, channelUriPostfix);
        Uri chatUri = new(channelUriString);
        string uriString = string.Format(ChannelMessageUriFormat, chatUri, messageId);
        return new Uri(uriString);
    }

    private static InlineKeyboardMarkup? GetKeyboardMarkup(MessageData.KeyboardType keyboardType,
        InlineKeyboardButton? icsButton)
    {
        switch (keyboardType)
        {
            case MessageData.KeyboardType.None: return null;
            case MessageData.KeyboardType.Ics:
                InlineKeyboardButton button = icsButton.GetValue(nameof(icsButton));
                return new InlineKeyboardMarkup(button);
            default: throw new ArgumentOutOfRangeException(nameof(keyboardType), keyboardType, null);
        }
    }

    private static string ShowDate(DateOnly date)
    {
        string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
        return $"{day}, {date:d MMMM}";
    }

    private void DisposeEvents()
    {
        foreach (Event e in _events.Values)
        {
            e.Dispose();
        }
        _events.Clear();
    }

    private Task AskForConfirmationAsync(Chat chat)
    {
        StringBuilder sb = new();
        sb.AppendLine("Я собираюсь опубликовать события:");
        foreach (Template template in _toPost)
        {
            sb.AppendLine($"• {template.Name}");
        }
        sb.AppendLine();
        sb.Append($"ОК? /{ConfirmCommand.CommandName}");

        _waitingForConfirmation = true;

        return _bot.SendTextMessageAsync(chat, sb.ToString());
    }

    private async Task PostOrUpdateEventsAsync()
    {
        DisposeEvents();

        ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
        foreach (int savedTemplateId in savedTemplateIds)
        {
            EventData data = _saveManager.Data.Events[savedTemplateId];
            if (_templates.ContainsKey(savedTemplateId))
            {
                Template template = _templates[savedTemplateId];

                string messageText = GetMessageText(template);
                InlineKeyboardButton icsButton = GetMessageIcsButton(template);
                await EditMessageTextAsync(data.MessageId, messageText, icsButton: icsButton,
                    keyboard: MessageData.KeyboardType.Ics);
                _bot.Calendars[savedTemplateId] = new Calendar(template, _bot.TimeManager);

                _events[savedTemplateId] = new Event(template, data.MessageId, _bot.Logger, data.NotificationId);
            }
            else
            {
                if (data.NotificationId.HasValue)
                {
                    await DeleteMessageAsync(data.NotificationId.Value);
                }
            }
        }

        foreach (Template template in _toPost)
        {
            _bot.Calendars[template.Id] = new Calendar(template, _bot.TimeManager);
            int messageId = await PostEventAsync(template);
            _events[template.Id] = new Event(template, messageId, _bot.Logger);
        }

        _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => new EventData(e.Value));
    }

    private async Task PostOrUpdateScheduleAsync()
    {
        string text = PrepareWeekSchedule();

        if (_saveManager.Data.ScheduleId is not null)
        {
            int scheduleId = _saveManager.Data.ScheduleId.Value;
            MessageData? data = GetMessageData(scheduleId);
            if (data?.Date >= _weekStart)
            {
                await EditMessageTextAsync(scheduleId, text, data, MessageData.KeyboardType.None,
                    disableWebPagePreview: true);
                return;
            }
        }

        int? oldScheduleId = _saveManager.Data.ScheduleId;
        _saveManager.Data.ScheduleId =
            await SendTextMessageAsync(text, MessageData.KeyboardType.None, disableWebPagePreview: true);
        if (oldScheduleId.HasValue)
        {
            await _bot.UnpinChatMessageAsync(_eventsChat, oldScheduleId.Value);
        }
        await _bot.PinChatMessageAsync(_eventsChat, _saveManager.Data.ScheduleId.Value, true);
    }

    private async Task CreateOrUpdateNotificationsAsync()
    {
        foreach (Event e in _events.Values)
        {
            await CreateOrUpdateNotificationAsync(e);
        }
    }

    private Task CreateOrUpdateNotificationAsync(Event e)
    {
        DateTimeFull now = DateTimeFull.CreateUtcNow();

        if (!e.Template.Active
            || (e.Template.GetEnd(_bot.TimeManager) <= now)
            || (e.Template.StartDate >= _weekEnd))
        {
            e.DisposeTimer();
            return DeleteNotificationAsync(e);
        }

        TimeSpan startIn = e.Template.GetStart(_bot.TimeManager) - now;
        if (startIn > Hour)
        {
            e.Timer.GetValue(nameof(e.Timer)).DoOnce(e.Template.GetStart(_bot.TimeManager) - Hour,
                () => NotifyInAnHourAsync(e), $"{nameof(NotifyInAnHourAsync)} for event #{e.Template.Id}");
            return DeleteNotificationAsync(e);
        }

        if (startIn > Soon)
        {
            return NotifyInAnHourAsync(e);
        }

        return startIn > TimeSpan.Zero ? NotifySoonAsync(e) : NotifyCurrentAsync(e);
    }

    private async Task NotifyInAnHourAsync(Event e)
    {
        await NotifyAndPlanAsync(e, "*Через час* начнётся", e.Template.GetStart(_bot.TimeManager) - Soon,
            NotifySoonAsync, nameof(NotifySoonAsync));
    }

    private async Task NotifySoonAsync(Event e)
    {
        await NotifyAndPlanAsync(e, "*Через 15 минут* начнётся", e.Template.GetStart(_bot.TimeManager),
            NotifyCurrentAsync, nameof(NotifyCurrentAsync));
    }

    private async Task NotifyCurrentAsync(Event e)
    {
        await NotifyAndPlanAsync(e, "*Сейчас* идёт", e.Template.GetEnd(_bot.TimeManager),
            DeleteNotificationAsync, nameof(DeleteNotificationAsync));
    }

    private async Task NotifyAndPlanAsync(Event e, string prefix, DateTimeFull nextAt, Func<Event, Task> nextFunc,
        string nextFuncName)
    {
        await CreateOrUpdateNotificationAsync(e, prefix);
        e.Timer.GetValue(nameof(e.Timer)).DoOnce(nextAt,
            () => nextFunc(e), $"{nextFuncName} for event #{e.Template.Id}");
    }

    private async Task CreateOrUpdateNotificationAsync(Event e, string prefix)
    {
        string text =
            $"{prefix} мероприятие [{AbstractBot.Bots.Bot.EscapeCharacters(e.Template.Name)}]({e.Template.Uri})\\.";

        if (e.NotificationId.HasValue)
        {
            await EditMessageTextAsync(e.NotificationId.Value, text);
        }
        else
        {
            e.NotificationId = await SendTextMessageAsync(text, replyToMessageId: e.MessageId);
            _saveManager.Data.Events[e.Template.Id].NotificationId = e.NotificationId;
        }

        _saveManager.Save();
    }

    private async Task DeleteNotificationAsync(Event e)
    {
        if (e.NotificationId is null)
        {
            return;
        }

        await DeleteMessageAsync(e.NotificationId.Value);
        e.NotificationId = null;

        if (_saveManager.Data.Events.ContainsKey(e.Template.Id))
        {
            _saveManager.Data.Events[e.Template.Id].NotificationId = null;
        }
        _saveManager.Save();
    }

    private Task<int> PostEventAsync(Template template)
    {
        string text = GetMessageText(template);
        InlineKeyboardButton icsButton = GetMessageIcsButton(template);
        return SendTextMessageAsync(text, MessageData.KeyboardType.Ics, icsButton);
    }

    private async Task<int> SendTextMessageAsync(string text,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
    {
        InlineKeyboardMarkup? keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
        Message message = await _bot.SendTextMessageAsync(_eventsChat, text, keyboardMarkup, ParseMode.MarkdownV2,
            disableWebPagePreview: disableWebPagePreview, disableNotification: disableNotification,
            replyToMessageId: replyToMessageId);
        _saveManager.Data.Messages[message.MessageId] = new MessageData(message, _bot.TimeManager)
        {
            Text = text,
            Keyboard = keyboard
        };
        return message.MessageId;
    }

    private async Task<IEnumerable<Template>> LoadRelevantTemplatesAsync()
    {
        if (_sheet is null)
        {
            GoogleSheetsManager.Documents.Document document = _documentsManager.GetOrAdd(_bot.Config.GoogleSheetId);

            Dictionary<Type, Func<object?, object?>> additionalConverters = new()
            {
                { typeof(Uri), o => o.ToUri() }
            };
            additionalConverters[typeof(DateOnly)] = additionalConverters[typeof(DateOnly?)] =
                o => o.ToDateOnly(_bot.TimeManager);
            additionalConverters[typeof(TimeOnly)] = additionalConverters[typeof(TimeOnly?)] =
                o => o.ToTimeOnly(_bot.TimeManager);
            additionalConverters[typeof(TimeSpan)] = additionalConverters[typeof(TimeSpan?)] =
                o => o.ToTimeSpan(_bot.TimeManager);
            _sheet = document.GetOrAddSheet(_bot.Config.GoogleTitle, additionalConverters);
        }

        SheetData<Template> templates = await _sheet.LoadAsync<Template>(_bot.Config.GoogleRange);
        return LoadRelevantTemplates(templates.Instances);
    }

    private IEnumerable<Template> LoadRelevantTemplates(IEnumerable<Template> templates)
    {
        foreach (Template t in templates)
        {
            if (t.StartDate < _weekStart)
            {
                if (t.IsWeekly)
                {
                    t.MoveToWeek(_weekStart);
                }
                else
                {
                    continue;
                }
            }

            yield return t;
        }
    }

    private string PrepareWeekSchedule()
    {
        StringBuilder scheduleBuilder = new();
        scheduleBuilder.AppendLine("🗓 *Расписание* \\(время московское, 🔄 — еженедельные\\)");
        DateOnly date = _weekStart.AddDays(-1);
        foreach (Event e in _events.Values
                                   .Where(e => e.Template.Active && (e.Template.StartDate < _weekEnd))
                                   .OrderBy(e => e.Template.StartDate)
                                   .ThenBy(e => e.Template.StartTime))
        {
            if (e.Template.StartDate > date)
            {
                if (scheduleBuilder.Length > 0)
                {
                    scheduleBuilder.AppendLine();
                }
                date = e.Template.StartDate;
                scheduleBuilder.AppendLine($"*{ShowDate(date)}*");
            }
            string name = AbstractBot.Bots.Bot.EscapeCharacters(e.Template.Name);
            Uri uri = GetChannelMessageUri(_bot.Config.EventsChannelId, e.MessageId);
            string messageUrl = AbstractBot.Bots.Bot.EscapeCharacters(uri.AbsoluteUri);
            string weekly = e.Template.IsWeekly ? " 🔄" : "";
            scheduleBuilder.AppendLine($"{e.Template.StartTime:HH:mm} [{name}]({messageUrl}){weekly}");
        }
        scheduleBuilder.AppendLine();
        string url = AbstractBot.Bots.Bot.EscapeCharacters(_bot.Config.EventsFormUri.AbsoluteUri);
        scheduleBuilder.Append($"Оставить заявку на добавление своего мероприятия можно здесь: {url}\\.");
        return scheduleBuilder.ToString();
    }

    private Task EditMessageTextAsync(int messageId, string text,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false)
    {
        MessageData? data = GetMessageData(messageId);
        return EditMessageTextAsync(messageId, text, data, keyboard, icsButton, disableWebPagePreview);
    }

    private async Task EditMessageTextAsync(int messageId, string text, MessageData? data,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false)
    {
        if ((data?.Text == text) && (data.Keyboard == keyboard))
        {
            UpdateInfo.LogRefused(_eventsChat,  UpdateInfo.Type.EditText, _bot.Logger, messageId, text);
            return;
        }
        InlineKeyboardMarkup? keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
        Message message = await _bot.EditMessageTextAsync(_eventsChat, messageId, text, ParseMode.MarkdownV2, null,
            disableWebPagePreview, keyboardMarkup);
        if (data is null)
        {
            _saveManager.Data.Messages[messageId] = new MessageData(message, _bot.TimeManager)
            {
                Text = text,
                Keyboard = keyboard
            };
        }
        else
        {
            data.Text = text;
            data.Keyboard = keyboard;
        }
    }

    private async Task DeleteMessageAsync(int messageId, DateOnly? weekStart = null)
    {
        if (weekStart is null || (_saveManager.Data.Messages[messageId].Date >= weekStart))
        {
            await _bot.DeleteMessageAsync(_eventsChat, messageId);
        }
        _saveManager.Data.Messages.Remove(messageId);
    }

    private string GetMessageText(Template template)
    {
        StringBuilder builder = new();

        builder.Append($"[{WordJoiner}]({AbstractBot.Bots.Bot.EscapeCharacters(template.Uri.AbsoluteUri)})");
        builder.AppendLine($"*{AbstractBot.Bots.Bot.EscapeCharacters(template.Name)}*");

        builder.AppendLine();
        builder.AppendLine(AbstractBot.Bots.Bot.EscapeCharacters(template.Description));

        builder.AppendLine();
        builder.Append("🕰️ *Когда:* ");
        if (template.IsWeekly)
        {
            builder.Append("по ");
            switch (template.StartDate.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    builder.Append("понедельникам");
                    break;
                case DayOfWeek.Tuesday:
                    builder.Append("вторникам");
                    break;
                case DayOfWeek.Wednesday:
                    builder.Append("средам");
                    break;
                case DayOfWeek.Thursday:
                    builder.Append("четвергам");
                    break;
                case DayOfWeek.Friday:
                    builder.Append("пятницам");
                    break;
                case DayOfWeek.Saturday:
                    builder.Append("субботам");
                    break;
                case DayOfWeek.Sunday:
                    builder.Append("воскресеньям");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(template.StartDate.DayOfWeek),
                        template.StartDate.DayOfWeek, null);
            }

            if (template.StartDate > _weekEnd)
            {
                builder.Append($" c {template.StartDate:d MMMM}");
            }
        }
        else
        {
            builder.Append($"{template.StartDate:d MMMM}");
        }
        builder.AppendLine($", {template.StartTime:HH:mm}\\-{template.GetEnd(_bot.TimeManager):HH:mm} \\(Мск\\)\\.");

        if (!string.IsNullOrWhiteSpace(template.Hosts))
        {
            builder.AppendLine();
            builder.AppendLine($"🎤 *Кто ведёт*: {AbstractBot.Bots.Bot.EscapeCharacters(template.Hosts)}\\.");
        }

        builder.AppendLine();
        builder.AppendLine($"💰 *Цена*: {AbstractBot.Bots.Bot.EscapeCharacters(template.Price)}\\.");

        builder.AppendLine();
        builder.Append($"🗞️ *Принять участие*: {AbstractBot.Bots.Bot.EscapeCharacters(template.Uri.AbsoluteUri)}\\.");

        return builder.ToString();
    }

    private InlineKeyboardButton GetMessageIcsButton(Template template)
    {
        return new InlineKeyboardButton("📅 В календарь")
        {
            Url = string.Format(CalendarUriFormat, _bot.Host, template.Id)
        };
    }

    private MessageData? GetMessageData(int id)
    {
        return _saveManager.Data.Messages.ContainsKey(id) ? _saveManager.Data.Messages[id] : null;
    }

    private bool IsExcess(int id)
    {
        return (id != _saveManager.Data.ScheduleId)
               && _saveManager.Data.Events.Values.All(d => (d.MessageId != id) && (d.NotificationId != id));
    }

    private const string ChannelUriFormat = "https://t.me/{0}";
    private const string ChannelMessageUriFormat = "{0}/{1}";
    private const string WordJoiner = "\u2060";
    private const string CalendarUriFormat = "{0}/calendar/{1}";

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);

    private readonly Dictionary<int, Template> _templates = new();
    private readonly List<Template> _toPost = new();

    private readonly Bot _bot;
    private readonly DocumentsManager _documentsManager;
    private readonly SaveManager<Data> _saveManager;

    private readonly Dictionary<int, Event> _events = new();

    private readonly Chat _eventsChat;

    private Sheet? _sheet;

    private DateOnly _weekStart;
    private DateOnly _weekEnd;
    private bool _waitingForConfirmation;
}