using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleSheetsManager;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Manager : IDisposable
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRange;
        private readonly BotSaveManager _saveManager;
        private readonly Uri _formUri;
        private readonly ITelegramBotClient _client;
        private readonly ChatId _eventsChatId;
        private readonly ChatId _logsChatId;
        private readonly ChatId _discussChatId;
        private readonly string _host;
        private readonly InlineKeyboardButton _discussButton;
        private readonly InlineKeyboardMarkup _discussKeyboard;

        private readonly Dictionary<int, Event> _events = new Dictionary<int, Event>();

        public Manager(DataManager googleSheetsDataManager, BotSaveManager saveManager, string googleRange,
            Uri formUri, ITelegramBotClient client, ChatId eventsChatId, ChatId logsChatId, ChatId discussChatId,
            string host)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRange = googleRange;
            _saveManager = saveManager;
            _formUri = formUri;
            _client = client;
            _eventsChatId = eventsChatId;
            _logsChatId = logsChatId;
            _discussChatId = discussChatId;
            _host = host;

            _discussButton = new InlineKeyboardButton
            {
                Text = "💬 Обсудить",
                Url = GetUri(_discussChatId).AbsoluteUri
            };
            _discussKeyboard = new InlineKeyboardMarkup(_discussButton);
        }

        public async Task PostOrUpdateWeekEventsAndScheduleAsync()
        {
            Message statusMessage =
                await _client.SendTextMessageAsync(_logsChatId, "_Обновляю расписание…_", ParseMode.Markdown);

            DateTime weekStart = Utils.GetMonday();

            await PostOrUpdateEvents(weekStart);
            await PostOrUpdateScheduleAsync(weekStart);
            await CreateOrUpdateNotificationsAsync();

            List<int> toRemove = _saveManager.Data.Messages.Keys.Where(IsExcess).ToList();
            foreach (int id in toRemove)
            {
                _saveManager.Data.Messages.Remove(id);
            }

            _saveManager.Save();

            await _client.FinalizeStatusMessageAsync(statusMessage);
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

        private async Task PostOrUpdateEvents(DateTime weekStart)
        {
            Dictionary<int, Template> templates = LoadRelevantTemplates(weekStart).ToDictionary(t => t.Id, t => t);

            DisposeEvents();

            _saveManager.Load();
            ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
            foreach (int savedTemplateId in savedTemplateIds)
            {
                EventData data = _saveManager.Data.Events[savedTemplateId];
                if (templates.ContainsKey(savedTemplateId))
                {
                    Template template = templates[savedTemplateId];

                    string messageText = GetMessageText(template);
                    InlineKeyboardButton icsButton = GetMessageIcsButton(template);
                    await EditMessageTextAsync(data.MessageId, messageText, icsButton: icsButton,
                        keyboard: MessageData.KeyboardType.Full);
                    Utils.AddCalendars(template);

                    _events[template.Id] = new Event(template, data);
                }
                else
                {
                    await DeleteNotificationAsync(data);
                    await DeleteMessageAsync(data.MessageId, weekStart);
                }
            }

            IOrderedEnumerable<Template> toPost = templates.Values
                .Where(t => !savedTemplateIds.Contains(t.Id))
                .OrderBy(t => t.Start);
            foreach (Template template in toPost)
            {
                Utils.AddCalendars(template);
                EventData data = await PostEventAsync(template);
                _events[template.Id] = new Event(template, data);
            }

            _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => e.Value.Data);
        }

        private async Task PostOrUpdateScheduleAsync(DateTime weekStart)
        {
            string text = PrepareWeekSchedule(weekStart);

            if (IsScheduleRelevant(weekStart))
            {
                await EditMessageTextAsync(_saveManager.Data.ScheduleId, text, MessageData.KeyboardType.Discuss,
                    disableWebPagePreview: true);
            }
            else
            {
                _saveManager.Data.ScheduleId = await PostForwardAndAddButton(text, MessageData.KeyboardType.None,
                    MessageData.KeyboardType.Discuss, disableWebPagePreview: true);
                await _client.UnpinChatMessageAsync(_eventsChatId);
                await _client.PinChatMessageAsync(_eventsChatId, _saveManager.Data.ScheduleId, true);
            }
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
            DateTime now = DateTime.Now;

            if (e.Template.End <= now)
            {
                e.DisposeTimer();
                return DeleteNotificationAsync(e);
            }

            TimeSpan startIn = e.Template.Start - now;
            if (startIn > Hour)
            {
                e.Timer.DoOnce(e.Template.Start - Hour, () => NotifyInAnHourAsync(e),
                    $"{nameof(NotifyInAnHourAsync)} for {e.Template.Id}");
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
            e.Timer.DoOnce(nextAt, () => nextFunc(e), $"{nextFuncName} for {e.Template.Id}");
        }

        private async Task CreateOrUpdateNotificationAsync(Event e, string prefix)
        {
            string text = $"{prefix} мероприятие [{e.Template.Name}]({e.Template.Uri}).";

            if (e.Data.NotificationId.HasValue)
            {
                await EditMessageTextAsync(e.Data.NotificationId.Value, text);
            }
            else
            {
                e.Data.NotificationId = await SendTextMessageAsync(text, replyToMessageId: e.Data.MessageId);
            }

            _saveManager.Save();
        }

        private Task DeleteNotificationAsync(Event e) => DeleteNotificationAsync(e.Data);
        private async Task DeleteNotificationAsync(EventData data)
        {
            if (!data.NotificationId.HasValue)
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
            int messageId = await PostForwardAndAddButton(text, MessageData.KeyboardType.Ics,
                MessageData.KeyboardType.Full, icsButton);
            return new EventData(messageId);
        }

        private async Task<int> PostForwardAndAddButton(string text, MessageData.KeyboardType chatKeyboard,
            MessageData.KeyboardType keyboard, InlineKeyboardButton icsButton = null,
            bool disableWebPagePreview = false)
        {
            int messageId = await SendTextMessageAsync(text, chatKeyboard, icsButton, disableWebPagePreview);
            await _client.ForwardMessageAsync(_discussChatId, _eventsChatId, messageId);
            await EditMessageTextAsync(messageId, text, keyboard, icsButton, disableWebPagePreview);
            return messageId;
        }

        private async Task<int> SendTextMessageAsync(string text,
            MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton icsButton = null,
            bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
        {
            InlineKeyboardMarkup keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
            Message message = await _client.SendTextMessageAsync(_eventsChatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId, keyboardMarkup);
            _saveManager.Data.Messages[message.MessageId] = new MessageData(message, text, keyboard);
            return message.MessageId;
        }

        private IEnumerable<Template> LoadRelevantTemplates(DateTime weekStart)
        {
            IList<Template> templates = _googleSheetsDataManager.GetValues<Template>(_googleRange);
            DateTime weekEnd = weekStart.AddDays(7);
            foreach (Template t in templates.Where(t => t.IsApproved && (t.Start < weekEnd)))
            {
                if (t.IsWeekly)
                {
                    t.MoveToWeek(weekStart);
                }
                else if (t.Start < weekStart)
                {
                    continue;
                }

                yield return t;
            }
        }

        private string PrepareWeekSchedule(DateTime start)
        {
            var scheduleBuilder = new StringBuilder();
            scheduleBuilder.AppendLine("🗓 *Расписание* (время московское, 🔄 — еженедельные)");
            DateTime date = start.AddDays(-1);
            foreach (Event e in _events.Values.OrderBy(e => e.Template.Start))
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
                Uri messageUri = GetMessageUri(_eventsChatId, e.Data.MessageId);
                string weekly = e.Template.IsWeekly ? " 🔄" : "";
                scheduleBuilder.AppendLine($"{e.Template.Start:HH:mm} [{e.Template.Name}]({messageUri}){weekly}");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {_formUri}.");
            return scheduleBuilder.ToString();
        }

        private static Uri GetMessageUri(ChatId chatId, int messageId)
        {
            Uri chatUri = GetUri(chatId);
            string uriString = string.Format(ChannelMessageUriFormat, chatUri, messageId);
            return new Uri(uriString);
        }
        private static Uri GetUri(ChatId chatId)
        {
            string username = GetUsername(chatId);
            string uriString = string.Format(ChannelUriFormat, username);
            return new Uri(uriString);
        }
        private static string GetUsername(ChatId chatId) => chatId.Username.Remove(0, 1);

        private async Task EditMessageTextAsync(int messageId, string text,
            MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton icsButton = null,
            bool disableWebPagePreview = false)
        {
            MessageData data = GetMessageData(messageId);
            if ((data?.Text == text) && (data?.Keyboard == keyboard))
            {
                return;
            }
            InlineKeyboardMarkup keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
            Message message = await _client.EditMessageTextAsync(_eventsChatId, messageId, text, ParseMode.Markdown,
                disableWebPagePreview, keyboardMarkup);
            if (data == null)
            {
                _saveManager.Data.Messages[messageId] = new MessageData(message, text, keyboard);
            }
            else
            {
                data.Text = text;
                data.Keyboard = keyboard;
            }
        }

        private InlineKeyboardMarkup GetKeyboardMarkup(MessageData.KeyboardType keyboardType,
            InlineKeyboardButton icsButton)
        {
            var icsKeyboard = new InlineKeyboardMarkup(icsButton);

            var row = new[] { icsButton, _discussButton };
            var fullKeyboard = new InlineKeyboardMarkup(row);

            switch (keyboardType)
            {
                case MessageData.KeyboardType.None:
                    return null;
                case MessageData.KeyboardType.Ics:
                    return icsKeyboard;
                case MessageData.KeyboardType.Discuss:
                    return _discussKeyboard;
                case MessageData.KeyboardType.Full:
                    return fullKeyboard;
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyboardType), keyboardType, null);
            }
        }

        private async Task DeleteMessageAsync(int messageId, DateTime? weekStart = null)
        {
            if (!weekStart.HasValue || (_saveManager.Data.Messages[messageId].Date >= weekStart))
            {
                await _client.DeleteMessageAsync(_eventsChatId, messageId);
            }
            _saveManager.Data.Messages.Remove(messageId);
        }

        private static string GetMessageText(Template template)
        {
            var builder = new StringBuilder();

            builder.Append($"[{WordJoiner}]({template.Uri})");
            builder.AppendLine($"*{template.Name}*");

            builder.AppendLine();
            builder.AppendLine(template.Description);

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
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.Append($"{template.Start:d MMMM}");
            }
            builder.AppendLine($", {template.Start:HH:mm}-{template.End:HH:mm} (Мск).");

            if (!string.IsNullOrWhiteSpace(template.Hosts))
            {
                builder.AppendLine();
                builder.AppendLine($"🎤 *Кто ведёт*: {template.Hosts}.");
            }

            builder.AppendLine();
            builder.AppendLine($"💰 *Цена*: {template.Price}.");

            string uriString = $"{template.Uri}".Replace("_", "\\_");
            builder.AppendLine();
            builder.AppendLine($"🗞️ *Принять участие*: {uriString}.");

            return builder.ToString();
        }

        private InlineKeyboardButton GetMessageIcsButton(Template template)
        {
            return new InlineKeyboardButton
            {
                Text = "📅 В календарь",
                Url = string.Format(Utils.CalendarUriFormat, _host, template.Id)
            };
        }

        private bool IsScheduleRelevant(DateTime start)
        {
            MessageData data = GetMessageData(_saveManager.Data.ScheduleId);
            return data?.Date >= start;
        }

        private MessageData GetMessageData(int id)
        {
            return _saveManager.Data.Messages.TryGetValue(id, out MessageData data) ? data : null;
        }

        private bool IsExcess(int id)
        {
            return (id != _saveManager.Data.ScheduleId)
                && _saveManager.Data.Events.Values.All(d => (d.MessageId != id) && (d.NotificationId != id));
        }

        private const string ChannelUriFormat = "https://t.me/{0}";
        private const string ChannelMessageUriFormat = "{0}/{1}";
        private const string WordJoiner = "\u2060";

        private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
        private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
    }
}
