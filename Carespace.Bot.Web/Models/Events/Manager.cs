﻿using System;
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
        private readonly InlineKeyboardMarkup _discussKeyboard;

        private readonly Dictionary<int, Event> _events = new Dictionary<int, Event>();

        public Manager(DataManager googleSheetsDataManager, BotSaveManager saveManager, string googleRange,
            Uri formUri, ITelegramBotClient client, ChatId eventsChatId, ChatId logsChatId, Uri discussUri)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRange = googleRange;
            _saveManager = saveManager;
            _formUri = formUri;
            _client = client;
            _eventsChatId = eventsChatId;
            _logsChatId = logsChatId;

            var discussButton = new InlineKeyboardButton
            {
                Text = "💬 Обсудить",
                Url = discussUri.ToString()
            };
            _discussKeyboard = new InlineKeyboardMarkup(discussButton);
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
                    await EditMessageTextAsync(data.MessageId, messageText, keyboardMarkup: _discussKeyboard);

                    _events[template.Id] = new Event(template, data);
                }
                else
                {
                    await DeleteNotificationAsync(data);
                    await DeleteMessageAsync(data.MessageId);
                }
            }

            IOrderedEnumerable<Template> toPost = templates.Values
                .Where(t => !savedTemplateIds.Contains(t.Id))
                .OrderBy(t => t.Start);
            foreach (Template template in toPost)
            {
                EventData data = await PostEventAsync(template);
                _events[template.Id] = new Event(template, data);
            }

            _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => e.Value.Data);
        }

        private async Task PostOrUpdateScheduleAsync(DateTime weekStart)
        {
            Chat eventsChat = await _client.GetChatAsync(_eventsChatId);
            string text = PrepareWeekSchedule(weekStart, eventsChat.Username);

            if (IsScheduleRelevant(weekStart))
            {
                await EditMessageTextAsync(_saveManager.Data.ScheduleId, text, true, _discussKeyboard);
            }
            else
            {
                _saveManager.Data.ScheduleId =
                    await SendTextMessageAsync(text, true, keyboardMarkup: _discussKeyboard);
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
                e.Timer.DoOnce(e.Template.Start - Hour, () => NotifyInAnHourAsync(e));
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
            await NotifyAndPlanAsync(e, "*Через час* начнётся", e.Template.Start - Soon, NotifySoonAsync);
        }

        private async Task NotifySoonAsync(Event e)
        {
            await NotifyAndPlanAsync(e, "*Через 15 минут* начнётся", e.Template.Start, NotifyCurrentAsync);
        }

        private async Task NotifyCurrentAsync(Event e)
        {
            await NotifyAndPlanAsync(e, "*Сейчас* идёт", e.Template.End, DeleteNotificationAsync);
        }

        private async Task NotifyAndPlanAsync(Event e, string prefix, DateTime nextAt, Func<Event, Task> nextFunc)
        {
            await CreateOrUpdateNotificationAsync(e, prefix);
            e.Timer.DoOnce(nextAt, () => nextFunc(e));
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
            int messageId = await SendTextMessageAsync(text, keyboardMarkup: _discussKeyboard);
            return new EventData(messageId);
        }

        private async Task<int> SendTextMessageAsync(string text, bool disableWebPagePreview = false,
            bool disableNotification = false, int replyToMessageId = 0, IReplyMarkup keyboardMarkup = null)
        {
            Message message = await _client.SendTextMessageAsync(_eventsChatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId, keyboardMarkup);
            _saveManager.Data.Messages[message.MessageId] = new MessageData(message, text);
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

        private string PrepareWeekSchedule(DateTime start, string eventsChatUsername)
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
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, eventsChatUsername, e.Data.MessageId));
                string weekly = e.Template.IsWeekly ? " 🔄" : "";
                scheduleBuilder.AppendLine($"{e.Template.Start:HH:mm} [{e.Template.Name}]({messageUri}){weekly}");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {_formUri}.");
            return scheduleBuilder.ToString();
        }

        private async Task EditMessageTextAsync(int messageId, string text, bool disableWebPagePreview = false,
            InlineKeyboardMarkup keyboardMarkup = null)
        {
            MessageData data = GetMessageData(messageId);
            if (data?.Text == text)
            {
                return;
            }
            Message message = await _client.EditMessageTextAsync(_eventsChatId, messageId, text, ParseMode.Markdown,
                disableWebPagePreview, keyboardMarkup);
            if (data == null)
            {
                _saveManager.Data.Messages[messageId] = new MessageData(message, text);
            }
            else
            {
                data.Text = text;
            }
        }

        private async Task DeleteMessageAsync(int messageId)
        {
            await _client.DeleteMessageAsync(_eventsChatId, messageId);
            _saveManager.Data.Messages.Remove(messageId);
        }

        private static string GetMessageText(Template template)
        {
            var builder = new StringBuilder();

            builder.Append($"⁠[{WordJoiner}]({template.Uri})");
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
                builder.Append($"{template.Start:dd MMMM}");
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

        private const string ChannelMessageUriFormat = "https://t.me/{0}/{1}";
        private const string WordJoiner = "\u2060";

        private static readonly TimeSpan Hour = TimeSpan.FromMinutes(1);
        // private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
        // private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan Soon = TimeSpan.FromSeconds(15);
    }
}
