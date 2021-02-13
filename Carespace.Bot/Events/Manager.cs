using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbstractBot;
using GoogleSheetsManager;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot.Events
{
    internal sealed class Manager : IDisposable
    {
        private readonly Bot _bot;
        private readonly SaveManager<SaveData> _saveManager;
        private readonly ChatId _eventsChatId;
        private readonly ChatId _logsChatId;
        private readonly ChatId _discussChatId;
        private readonly InlineKeyboardButton _discussButton;
        private readonly InlineKeyboardMarkup _discussKeyboard;

        private readonly Dictionary<int, Event> _events = new Dictionary<int, Event>();

        public Manager(Bot bot, SaveManager<SaveData> saveManager)
        {
            _bot = bot;

            _eventsChatId = new ChatId($"@{_bot.Config.EventsChannelLogin}");
            _discussChatId = new ChatId($"@{_bot.Config.DiscussGroupLogin}");

            _saveManager = saveManager;
            _logsChatId = _bot.Config.LogsChatId;

            _discussButton = new InlineKeyboardButton
            {
                Text = "💬 Обсудить",
                Url = GetUri(_discussChatId).AbsoluteUri
            };
            _discussKeyboard = new InlineKeyboardMarkup(_discussButton);
        }

        public Task PostOrUpdateWeekEventsAndScheduleAsync(bool shouldConfirm)
        {
            _weekStart = Utils.GetMonday(_bot.TimeManager);
            _weekEnd = _weekStart.AddDays(7);

            _templates = LoadRelevantTemplates().ToDictionary(t => t.Id, t => t);
            _saveManager.Load();

            ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
            _toPost = _templates.Values.Where(t => !savedTemplateIds.Contains(t.Id)).OrderBy(t => t.Start).ToList();

            shouldConfirm = shouldConfirm && _toPost.Any();

            _confirmationPending = !shouldConfirm;

            return shouldConfirm
                ? AskForConfirmationAsync()
                : PostOrUpdateWeekEventsAndScheduleAsync();
        }

        public async Task PostOrUpdateWeekEventsAndScheduleAsync()
        {
            if (!_confirmationPending)
            {
                await _bot.Client.SendTextMessageAsync(_logsChatId, "Обновлений не запланировано.");
                return;
            }

            _confirmationPending = false;

            Message statusMessage =
                await _bot.Client.SendTextMessageAsync(_logsChatId, "_Обновляю расписание…_", ParseMode.Markdown);

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

        private Task AskForConfirmationAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Я собираюсь опубликовать события:");
            foreach (Template template in _toPost)
            {
                sb.AppendLine($"    {template.Name}");
            }
            sb.AppendLine();
            sb.AppendLine("ОК?");

            _confirmationPending = true;

            return _bot.Client.SendTextMessageAsync(_logsChatId, sb.ToString());
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
                        keyboard: MessageData.KeyboardType.Full);
                    _bot.Calendars[template.Id] = new Calendar(template);

                    _events[template.Id] = new Event(template, data, _bot.TimeManager);
                }
                else
                {
                    await DeleteNotificationAsync(data);
                    await DeleteMessageAsync(data.MessageId);
                }
            }

            foreach (Template template in _toPost)
            {
                _bot.Calendars[template.Id] = new Calendar(template);
                EventData data = await PostEventAsync(template);
                _events[template.Id] = new Event(template, data, _bot.TimeManager);
            }

            _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => e.Value.Data);
        }

        private async Task PostOrUpdateScheduleAsync()
        {
            string text = PrepareWeekSchedule();

            if (IsScheduleRelevant())
            {
                await EditMessageTextAsync(_saveManager.Data.ScheduleId, text, MessageData.KeyboardType.Discuss,
                    disableWebPagePreview: true);
            }
            else
            {
                _saveManager.Data.ScheduleId = await PostForwardAndAddButtonAsync(text, MessageData.KeyboardType.None,
                    MessageData.KeyboardType.Discuss, disableWebPagePreview: true);
                await _bot.Client.UnpinChatMessageAsync(_eventsChatId);
                await _bot.Client.PinChatMessageAsync(_eventsChatId, _saveManager.Data.ScheduleId, true);
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
            int messageId = await PostForwardAndAddButtonAsync(text, MessageData.KeyboardType.Ics,
                MessageData.KeyboardType.Full, icsButton);
            return new EventData(messageId);
        }

        private async Task<int> PostForwardAndAddButtonAsync(string text, MessageData.KeyboardType chatKeyboard,
            MessageData.KeyboardType keyboard, InlineKeyboardButton icsButton = null,
            bool disableWebPagePreview = false)
        {
            int messageId = await SendTextMessageAsync(text, chatKeyboard, icsButton, disableWebPagePreview);
            await _bot.Client.ForwardMessageAsync(_discussChatId, _eventsChatId, messageId);
            await EditMessageTextAsync(messageId, text, keyboard, icsButton, disableWebPagePreview);
            return messageId;
        }

        private async Task<int> SendTextMessageAsync(string text,
            MessageData.KeyboardType keyboard = MessageData.KeyboardType.None, InlineKeyboardButton icsButton = null,
            bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
        {
            InlineKeyboardMarkup keyboardMarkup = GetKeyboardMarkup(keyboard, icsButton);
            Message message = await _bot.Client.SendTextMessageAsync(_eventsChatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId, keyboardMarkup);
            _saveManager.Data.Messages[message.MessageId] = new MessageData(message, text, keyboard);
            return message.MessageId;
        }

        private IEnumerable<Template> LoadRelevantTemplates()
        {
            IList<Template> templates = DataManager.GetValues<Template>(_bot.GoogleSheetsProvider, _bot.Config.GoogleRange);
            foreach (Template t in templates.Where(t => t.IsApproved))
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
            var scheduleBuilder = new StringBuilder();
            scheduleBuilder.AppendLine("🗓 *Расписание* (время московское, 🔄 — еженедельные)");
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
                Uri messageUri = GetMessageUri(_eventsChatId, e.Data.MessageId);
                string weekly = e.Template.IsWeekly ? " 🔄" : "";
                scheduleBuilder.AppendLine($"{e.Template.Start:HH:mm} [{e.Template.Name}]({messageUri}){weekly}");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {_bot.Config.EventsFormUri}.");
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
            Message message = await _bot.Client.EditMessageTextAsync(_eventsChatId, messageId, text, ParseMode.Markdown,
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
                await _bot.Client.DeleteMessageAsync(_eventsChatId, messageId);
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

            builder.AppendLine();
            builder.AppendLine($"🗞️ *Принять участие*: {template.Uri}.");

            return builder.ToString().Replace("_", "\\_");
        }

        private InlineKeyboardButton GetMessageIcsButton(Template template)
        {
            return new InlineKeyboardButton
            {
                Text = "📅 В календарь",
                Url = string.Format(Utils.CalendarUriFormat, _bot.Config.Host, template.Id)
            };
        }

        private bool IsScheduleRelevant()
        {
            MessageData data = GetMessageData(_saveManager.Data.ScheduleId);
            return data?.Date >= _weekStart;
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

        private DateTime _weekStart;
        private DateTime _weekEnd;
        private Dictionary<int, Template> _templates;
        private ICollection<Template> _toPost;
        private bool _confirmationPending;

        private const string ChannelUriFormat = "https://t.me/{0}";
        private const string ChannelMessageUriFormat = "{0}/{1}";
        private const string WordJoiner = "\u2060";

        private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
        private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
    }
}
