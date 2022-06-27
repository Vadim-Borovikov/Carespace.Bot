using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Commands;
using Carespace.Bot.Save;
using GoogleSheetsManager;
using GryphonUtilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot.Events;

internal sealed class Manager : IDisposable
{
    private readonly Bot _bot;
    private readonly SaveManager<Data, JsonData> _saveManager;

    private readonly Dictionary<int, Event> _events = new();

    public Manager(Bot bot, SaveManager<Data, JsonData> saveManager)
    {
        _bot = bot;
        _saveManager = saveManager;
    }

    public async Task PostOrUpdateWeekEventsAndScheduleAsync(ChatId chatId, bool shouldConfirm)
    {
        _weekStart = Utils.GetMonday(_bot.TimeManager);
        _weekEnd = _weekStart.AddDays(7);

        IEnumerable<Template> templates = await LoadRelevantTemplatesAsync();
        _templates.Clear();
        foreach (Template template in templates)
        {
            _templates[template.Id.GetValue()] = template;
        }
        _saveManager.Load();

        IEnumerable<int> savedTemplateIds = _saveManager.Data.Events.Keys;
        _toPost.Clear();
        _toPost.AddRange(_templates.Values
                                   .Where(t => !savedTemplateIds.Contains(t.Id.GetValue()))
                                   .OrderBy(t => t.Start));

        shouldConfirm = shouldConfirm && _toPost.Any();

        _confirmationPending = !shouldConfirm;

        if (shouldConfirm)
        {
            await AskForConfirmationAsync(chatId);
        }
        else
        {
            await PostOrUpdateWeekEventsAndScheduleAsync(chatId);
        }
    }

    public async Task PostOrUpdateWeekEventsAndScheduleAsync(ChatId chatId)
    {
        if (!_confirmationPending)
        {
            await _bot.Client.SendTextMessageAsync(chatId, "Обновлений не запланировано.");
            return;
        }

        _confirmationPending = false;

        Message statusMessage =
            await _bot.Client.SendTextMessageAsync(chatId, "_Обновляю расписание…_", ParseMode.MarkdownV2);

        await PostOrUpdateEventsAsync();
        await PostOrUpdateScheduleAsync();
        await CreateOrUpdateNotificationsAsync();

        List<int> toRemove = _saveManager.Data.Messages.Keys.Where(IsExcess).ToList();
        foreach (int id in toRemove)
        {
            _saveManager.Data.Messages.Remove(id);
        }

        _saveManager.Save();

        await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
    }

    public void Dispose() => DisposeEvents();

    private void DisposeEvents()
    {
        foreach (Event e in _events.Values)
        {
            e.Dispose();
        }
        _events.Clear();
    }

    private Task AskForConfirmationAsync(ChatId chatId)
    {
        StringBuilder sb = new();
        sb.AppendLine("Я собираюсь опубликовать события:");
        foreach (Template template in _toPost)
        {
            sb.AppendLine($"• {template.Name}");
        }
        sb.AppendLine();
        sb.AppendLine($"ОК? /{ConfirmCommand.CommandName}");

        _confirmationPending = true;

        return _bot.Client.SendTextMessageAsync(chatId, sb.ToString());
    }

    private async Task PostOrUpdateEventsAsync()
    {
        DisposeEvents();

        ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
        foreach (int savedTemplateId in savedTemplateIds)
        {
            EventData data = _saveManager.Data.Events[savedTemplateId].GetValue();
            if (_templates.ContainsKey(savedTemplateId))
            {
                Template template = _templates[savedTemplateId];

                string messageText = GetMessageText(template);
                InlineKeyboardButton icsButton = GetMessageIcsButton(template);
                int messageId = data.MessageId.GetValue(nameof(data.MessageId));
                await EditMessageTextAsync(messageId, messageText, icsButton: icsButton,
                    keyboard: MessageData.KeyboardType.Ics);
                _bot.Calendars[savedTemplateId] = new Calendar(template, _bot.TimeManager);

                _events[savedTemplateId] = new Event(template, data, _bot.TimeManager);
            }
            else
            {
                await DeleteNotificationAsync(data);
            }
        }

        foreach (Template template in _toPost)
        {
            int id = template.Id.GetValue();
            _bot.Calendars[id] = new Calendar(template, _bot.TimeManager);
            EventData data = await PostEventAsync(template);
            _events[id] = new Event(template, data, _bot.TimeManager);
        }

        _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => e.Value.Data);
    }

    private async Task PostOrUpdateScheduleAsync()
    {
        string text = PrepareWeekSchedule();

        int scheduleId = _saveManager.Data.ScheduleId.GetValue(nameof(_saveManager.Data.ScheduleId));
        if (IsScheduleRelevant())
        {
            await EditMessageTextAsync(scheduleId, text, MessageData.KeyboardType.None,
                disableWebPagePreview: true);
        }
        else
        {
            _saveManager.Data.ScheduleId = await SendTextMessageAsync(text, MessageData.KeyboardType.None, disableWebPagePreview: true);
            await _bot.Client.UnpinChatMessageAsync(_bot.Config.EventsChannelId);
            await _bot.Client.PinChatMessageAsync(_bot.Config.EventsChannelId, scheduleId, true);
        }
    }

    private async Task CreateOrUpdateNotificationsAsync()
    {
        foreach (Event e in _events.Values)
        {
            await CreateOrUpdateNotificationAsync(e, _weekEnd);
        }
    }

    private Task CreateOrUpdateNotificationAsync(Event e, DateTime end)
    {
        DateTime now = _bot.TimeManager.Now();

        if (!e.Template.Active || (e.Template.End <= now) || (e.Template.Start >= end))
        {
            e.DisposeTimer();
            return DeleteNotificationAsync(e);
        }

        TimeSpan startIn = e.Template.Start - now;
        if (startIn > Hour)
        {
            e.Timer.DoOnce(e.Template.Start - Hour, () => NotifyInAnHourAsync(e),
                $"{nameof(NotifyInAnHourAsync)} for event #{e.Template.Id}");
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
        await NotifyAndPlanAsync(e, "*Через час* начнётся", e.Template.Start - Soon, NotifySoonAsync,
            nameof(NotifySoonAsync));
    }

    private async Task NotifySoonAsync(Event e)
    {
        await NotifyAndPlanAsync(e, "*Через 15 минут* начнётся", e.Template.Start, NotifyCurrentAsync,
            nameof(NotifyCurrentAsync));
    }

    private async Task NotifyCurrentAsync(Event e)
    {
        await NotifyAndPlanAsync(e, "*Сейчас* идёт", e.Template.End, DeleteNotificationAsync,
            nameof(DeleteNotificationAsync));
    }

    private async Task NotifyAndPlanAsync(Event e, string prefix, DateTime nextAt, Func<Event, Task> nextFunc,
        string nextFuncName)
    {
        await CreateOrUpdateNotificationAsync(e, prefix);
        e.Timer.DoOnce(nextAt, () => nextFunc(e), $"{nextFuncName} for event #{e.Template.Id}");
    }

    private async Task CreateOrUpdateNotificationAsync(Event e, string prefix)
    {
        string text = $"{prefix} мероприятие [{AbstractBot.Utils.EscapeCharacters(e.Template.Name)}]({e.Template.Uri})\\.";

        if (e.Data.NotificationId.HasValue)
        {
            await EditMessageTextAsync(e.Data.NotificationId.Value, text);
        }
        else
        {
            int messageId = e.Data.MessageId.GetValue(nameof(e.Data.MessageId));
            e.Data.NotificationId = await SendTextMessageAsync(text, replyToMessageId: messageId);
        }

        _saveManager.Save();
    }

    private Task DeleteNotificationAsync(Event e) => DeleteNotificationAsync(e.Data);
    private async Task DeleteNotificationAsync(EventData data)
    {
        if (data.NotificationId is null)
        {
            return;
        }

        await DeleteMessageAsync(data.NotificationId.Value);
        data.NotificationId = null;
        _saveManager.Save();
    }

    private async Task<EventData> PostEventAsync(Template template)
    {
        string text = GetMessageText(template);
        InlineKeyboardButton icsButton = GetMessageIcsButton(template);
        int messageId = await SendTextMessageAsync(text, MessageData.KeyboardType.Ics, icsButton);
        return new EventData(messageId);
    }

    private async Task<int> SendTextMessageAsync(string text,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
    {
        InlineKeyboardMarkup? keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
        Message message = await _bot.Client.SendTextMessageAsync(_bot.Config.EventsChannelId, text,
            ParseMode.MarkdownV2, null, disableWebPagePreview, disableNotification, replyToMessageId, false,
            keyboardMarkup);
        _saveManager.Data.Messages[message.MessageId] = new MessageData(message, text, keyboard);
        return message.MessageId;
    }

    private async Task<IEnumerable<Template>> LoadRelevantTemplatesAsync()
    {
        string range = _bot.Config.GoogleRange.GetValue(nameof(_bot.Config.GoogleRange));
        IList<Template> templates = await DataManager.GetValuesAsync(_bot.GoogleSheetsProvider, Template.Load, range);
        return LoadRelevantTemplates(templates.RemoveNulls());
    }

    private IEnumerable<Template> LoadRelevantTemplates(IEnumerable<Template> templates)
    {
        foreach (Template t in templates)
        {
            if (t.IsWeekly)
            {
                if (t.Start >= _weekEnd)
                {
                    continue;
                }
                t.MoveToWeek(_weekStart);
            }
            else if (t.Start < _weekStart)
            {
                continue;
            }

            yield return t;
        }
    }

    private string PrepareWeekSchedule()
    {
        StringBuilder scheduleBuilder = new();
        scheduleBuilder.AppendLine("🗓 *Расписание* \\(время московское, 🔄 — еженедельные\\)");
        DateTime date = _weekStart.AddDays(-1);
        foreach (Event e in _events.Values
                                   .Where(e => e.Template.Active && (e.Template.Start < _weekEnd))
                                   .OrderBy(e => e.Template.Start))
        {
            if (e.Template.Start.Date > date)
            {
                if (scheduleBuilder.Length > 0)
                {
                    scheduleBuilder.AppendLine();
                }
                date = e.Template.Start.Date;
                scheduleBuilder.AppendLine($"*{Utils.ShowDate(date)}*");
            }
            string name = AbstractBot.Utils.EscapeCharacters(e.Template.Name);
            int messageId = e.Data.MessageId.GetValue(nameof(e.Data.MessageId));
            Uri? messageUri = GetMessageUri(_bot.Config.EventsChannelId, messageId);
            Uri uri = messageUri.GetValue(nameof(messageUri));
            string messageUrl = AbstractBot.Utils.EscapeCharacters(uri.AbsoluteUri);
            string weekly = e.Template.IsWeekly ? " 🔄" : "";
            scheduleBuilder.AppendLine($"{e.Template.Start:HH:mm} [{name}]({messageUrl}){weekly}");
        }
        scheduleBuilder.AppendLine();
        Uri formUri = _bot.Config.EventsFormUri.GetValue(nameof(_bot.Config.EventsFormUri));
        string url = AbstractBot.Utils.EscapeCharacters(formUri.AbsoluteUri);
        scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {url}\\.");
        return scheduleBuilder.ToString();
    }

    private static Uri? GetMessageUri(ChatId chatId, int messageId)
    {
        Uri? chatUri = GetUri(chatId);
        if (chatUri is null)
        {
            return null;
        }
        string uriString = string.Format(ChannelMessageUriFormat, chatUri, messageId);
        return new Uri(uriString);
    }
    private static Uri? GetUri(ChatId chatId)
    {
        string? username = GetUsername(chatId);
        if (username is null)
        {
            return null;
        }
        string uriString = string.Format(ChannelUriFormat, username);
        return new Uri(uriString);
    }
    private static string? GetUsername(ChatId chatId) => chatId.Username?.Remove(0, 1);

    private async Task EditMessageTextAsync(int messageId, string text,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false)
    {
        MessageData? data = GetMessageData(messageId);
        if ((data?.Text == text) && (data.Keyboard == keyboard))
        {
            return;
        }
        InlineKeyboardMarkup? keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
        Message message = await _bot.Client.EditMessageTextAsync(_bot.Config.EventsChannelId, messageId, text,
            ParseMode.MarkdownV2, null, disableWebPagePreview, keyboardMarkup);
        if (data is null)
        {
            _saveManager.Data.Messages[messageId] = new MessageData(message, text, keyboard);
        }
        else
        {
            data.Text = text;
            data.Keyboard = keyboard;
        }
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

    private async Task DeleteMessageAsync(int messageId, DateTime? weekStart = null)
    {
        if (weekStart is null || (_saveManager.Data.Messages[messageId].Date >= weekStart))
        {
            await _bot.Client.DeleteMessageAsync(_bot.Config.EventsChannelId, messageId);
        }
        _saveManager.Data.Messages.Remove(messageId);
    }

    private static string GetMessageText(Template template)
    {
        StringBuilder builder = new();

        builder.Append($"[{WordJoiner}]({AbstractBot.Utils.EscapeCharacters(template.Uri.AbsoluteUri)})");
        builder.AppendLine($"*{AbstractBot.Utils.EscapeCharacters(template.Name)}*");

        builder.AppendLine();
        builder.AppendLine(AbstractBot.Utils.EscapeCharacters(template.Description));

        builder.AppendLine();
        builder.Append("🕰️ *Когда:* ");
        if (template.IsWeekly)
        {
            builder.Append("по ");
            switch (template.Start.DayOfWeek)
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
                    throw new ArgumentOutOfRangeException(nameof(template.Start.DayOfWeek), template.Start.DayOfWeek,
                                                          null);
            }
        }
        else
        {
            builder.Append($"{template.Start:d MMMM}");
        }
        builder.AppendLine($", {template.Start:HH:mm}\\-{template.End:HH:mm} \\(Мск\\)\\.");

        if (!string.IsNullOrWhiteSpace(template.Hosts))
        {
            builder.AppendLine();
            builder.AppendLine($"🎤 *Кто ведёт*: {AbstractBot.Utils.EscapeCharacters(template.Hosts)}\\.");
        }

        builder.AppendLine();
        builder.AppendLine($"💰 *Цена*: {AbstractBot.Utils.EscapeCharacters(template.Price)}\\.");

        builder.AppendLine();
        builder.AppendLine($"🗞️ *Принять участие*: {AbstractBot.Utils.EscapeCharacters(template.Uri.AbsoluteUri)}\\.");

        return builder.ToString();
    }

    private InlineKeyboardButton GetMessageIcsButton(Template template)
    {
        return new InlineKeyboardButton("📅 В календарь")
        {
            Url = string.Format(Utils.CalendarUriFormat, _bot.Config.Host, template.Id)
        };
    }

    private bool IsScheduleRelevant()
    {
        int scheduleId = _saveManager.Data.ScheduleId.GetValue(nameof(_saveManager.Data.ScheduleId));
        MessageData? data = GetMessageData(scheduleId);
        return data?.Date >= _weekStart;
    }

    private MessageData? GetMessageData(int id)
    {
        return _saveManager.Data.Messages.TryGetValue(id, out MessageData? data) ? data : null;
    }

    private bool IsExcess(int id)
    {
        return (id != _saveManager.Data.ScheduleId)
               && _saveManager.Data.Events.Values.All(d => (d.MessageId != id) && (d.NotificationId != id));
    }

    private DateTime _weekStart;
    private DateTime _weekEnd;
    private bool _confirmationPending;

    private readonly Dictionary<int, Template> _templates = new();
    private readonly List<Template> _toPost = new();

    private const string ChannelUriFormat = "https://t.me/{0}";
    private const string ChannelMessageUriFormat = "{0}/{1}";
    private const string WordJoiner = "\u2060";

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
}