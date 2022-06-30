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
    private readonly ChatId _discussChatId;
    private readonly InlineKeyboardButton _discussButton;
    private readonly InlineKeyboardMarkup _discussKeyboard;

    private readonly Dictionary<int, Event> _events = new();

    private readonly Chat _eventsChat;

    public Manager(Bot bot, SaveManager<Data, JsonData> saveManager)
    {
        _bot = bot;

        _eventsChat = new Chat
        {
            Id = _bot.Config.EventsChannelId,
            Type = ChatType.Channel
        };

        _discussChatId = new ChatId($"@{_bot.Config.DiscussGroupLogin}");

        _saveManager = saveManager;

        Uri? chatUri = GetUri(_discussChatId);
        Uri uri = chatUri.GetValue(nameof(chatUri));
        _discussButton = new InlineKeyboardButton("💬 Обсудить")
        {
            Url = uri.AbsoluteUri
        };
        _discussKeyboard = new InlineKeyboardMarkup(_discussButton);
    }

    public async Task PostOrUpdateWeekEventsAndScheduleAsync(Chat chat, bool shouldConfirm)
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
        Message statusMessage =
            await _bot.SendTextMessageAsync(chat, "_Обновляю расписание…_", ParseMode.MarkdownV2);

        await PostOrUpdateEventsAsync();
        await PostOrUpdateScheduleAsync();
        await CreateOrUpdateNotificationsAsync();

        List<int> toRemove = _saveManager.Data.Messages.Keys.Where(IsExcess).ToList();
        foreach (int id in toRemove)
        {
            _saveManager.Data.Messages.Remove(id);
        }

        _saveManager.Save();

        await _bot.FinalizeStatusMessageAsync(statusMessage);
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

    private Task AskForConfirmationAsync(Chat chat)
    {
        StringBuilder sb = new();
        sb.AppendLine("Я собираюсь опубликовать события:");
        foreach (Template template in _toPost)
        {
            sb.AppendLine($"• {template.Name}");
        }
        sb.AppendLine();
        sb.AppendLine($"ОК? /{ConfirmCommand.CommandName}");

        _waitingForConfirmation = true;

        return _bot.SendTextMessageAsync(chat, sb.ToString());
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
                    keyboard: MessageData.KeyboardType.Full);
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

        if (_saveManager.Data.ScheduleId is not null)
        {
            int scheduleId = _saveManager.Data.ScheduleId.Value;
            MessageData? data = GetMessageData(scheduleId);
            if (data?.Date >= _weekStart)
            {
                await EditMessageTextAsync(scheduleId, text, MessageData.KeyboardType.Discuss,
                    disableWebPagePreview: true);
                return;
            }
        }

        int? oldScheduleId = _saveManager.Data.ScheduleId;
        _saveManager.Data.ScheduleId =
            await SendTextMessageAsync(text, MessageData.KeyboardType.Discuss, disableWebPagePreview: true);
        if (oldScheduleId.HasValue)
        {
            await _bot.Client.UnpinChatMessageAsync(_bot.Config.EventsChannelId, oldScheduleId.Value);
        }
        await _bot.Client.PinChatMessageAsync(_bot.Config.EventsChannelId, _saveManager.Data.ScheduleId.Value, true);
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
            if (e.Timer is null)
            {
                throw new NullReferenceException(nameof(e.Timer));
            }
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
        if (e.Timer is null)
        {
            throw new NullReferenceException(nameof(e.Timer));
        }
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
        int messageId = await PostForwardAndAddButtonAsync(text, MessageData.KeyboardType.Ics,
            MessageData.KeyboardType.Full, icsButton);
        return new EventData(messageId);
    }

    private async Task<int> PostForwardAndAddButtonAsync(string text, MessageData.KeyboardType chatKeyboard,
        MessageData.KeyboardType keyboard, InlineKeyboardButton? icsButton = null, bool disableWebPagePreview = false)
    {
        int messageId = await SendTextMessageAsync(text, chatKeyboard, icsButton, disableWebPagePreview);
        await _bot.Client.ForwardMessageAsync(_discussChatId, _bot.Config.EventsChannelId, messageId);
        await EditMessageTextAsync(messageId, text, keyboard, icsButton, disableWebPagePreview);
        return messageId;
    }

    private async Task<int> SendTextMessageAsync(string text,
        MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton? icsButton = null,
        bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
    {
        InlineKeyboardMarkup? keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
        Message message = await _bot.SendTextMessageAsync(_eventsChat, text, ParseMode.MarkdownV2,
            null, disableWebPagePreview, disableNotification, replyToMessageId, false, keyboardMarkup);
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
            Uri uri = GetChannelMessageUri(_bot.Config.EventsChannelId, messageId);
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

    private static Uri GetChannelMessageUri(long channelId, int messageId)
    {
        string channelParameter = channelId.ToString().Remove(0, 4);
        string channelUriPostfix = $"c/{channelParameter}";
        string channelUriString = string.Format(ChannelUriFormat, channelUriPostfix);
        Uri chatUri = new(channelUriString);
        string uriString = string.Format(ChannelMessageUriFormat, chatUri, messageId);
        return new Uri(uriString);
    }

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
        Message message = await _bot.EditMessageTextAsync(_eventsChat, messageId, text, ParseMode.MarkdownV2, null,
            disableWebPagePreview, keyboardMarkup);
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

    private InlineKeyboardMarkup? GetKeyboardMarkup(MessageData.KeyboardType keyboardType,
        InlineKeyboardButton? icsButton)
    {
        switch (keyboardType)
        {
            case MessageData.KeyboardType.None:    return null;
            case MessageData.KeyboardType.Discuss: return _discussKeyboard;
        }

        InlineKeyboardButton button = icsButton.GetValue(nameof(icsButton));

        switch (keyboardType)
        {
            case MessageData.KeyboardType.Ics: return new InlineKeyboardMarkup(button);
            case MessageData.KeyboardType.Full:
                InlineKeyboardButton[] row = { button, _discussButton };
                return new InlineKeyboardMarkup(row);
            default: throw new ArgumentOutOfRangeException(nameof(keyboardType), keyboardType, null);
        }
    }

    private async Task DeleteMessageAsync(int messageId, DateTime? weekStart = null)
    {
        if (weekStart is null || (_saveManager.Data.Messages[messageId].Date >= weekStart))
        {
            await _bot.DeleteMessageAsync(_eventsChat, messageId);
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
    private bool _waitingForConfirmation;

    private readonly Dictionary<int, Template> _templates = new();
    private readonly List<Template> _toPost = new();

    private const string ChannelUriFormat = "https://t.me/{0}";
    private const string ChannelMessageUriFormat = "{0}/{1}";
    private const string WordJoiner = "\u2060";

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
}