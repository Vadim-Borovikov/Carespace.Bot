﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleSheetsManager;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Manager
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRange;
        private readonly BotSaveManager _saveManager;
        private readonly Uri _formUri;
        private readonly ITelegramBotClient _client;
        private readonly ChatId _eventsChatId;
        private readonly ChatId _logsChatId;

        private Chat _eventsChat;

        public Manager(DataManager googleSheetsDataManager, BotSaveManager saveManager, string googleRange,
            Uri formUri, ITelegramBotClient client, ChatId eventsChatId, ChatId logsChatId)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRange = googleRange;
            _saveManager = saveManager;
            _formUri = formUri;
            _client = client;
            _eventsChatId = eventsChatId;
            _logsChatId = logsChatId;
        }

        public async Task PostOrUpdateWeekEventsAndScheduleAsync()
        {
            Message statusMessage =
                await _client.SendTextMessageAsync(_logsChatId, "_Обновляю расписание…_", ParseMode.Markdown);

            _eventsChat = await _client.GetChatAsync(_eventsChatId);

            DateTime weekStart = Utils.GetMonday();

            Dictionary<int, Event> events = await PostOrUpdateEvents(weekStart);
            await PostOrUpdateScheduleAsync(events.Values, weekStart);
            await CreateOrUpdateNotificationsAsync(events.Values);

            _saveManager.Save();

            await _client.FinalizeStatusMessageAsync(statusMessage);
        }

        private async Task<Dictionary<int, Event>> PostOrUpdateEvents(DateTime weekStart)
        {
            Dictionary<int, Template> templates = LoadRelevantTemplates(weekStart).ToDictionary(t => t.Id, t => t);

            _saveManager.Load();

            var events = new Dictionary<int, Event>();

            IEnumerable<Template> toPost = templates.Values;

            if (IsMessageRelevant(_eventsChat.PinnedMessage, weekStart))
            {
                ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
                foreach (int savedTemplateId in savedTemplateIds)
                {
                    if (templates.ContainsKey(savedTemplateId))
                    {
                        Template template = templates[savedTemplateId];
                        Data data = _saveManager.Data.Events[savedTemplateId];

                        string messageText = GetMessageText(template);
                        await EditMessageTextAsync(data.MessageId, messageText);

                        AddEvent(events, template, data);
                    }
                    else
                    {
                        await DeleteMessageAsync(_saveManager.Data.Events[savedTemplateId].MessageId);
                    }
                }

                toPost = toPost.Where(t => !savedTemplateIds.Contains(t.Id));
            }
            else
            {
                _saveManager.Reset();
            }

            foreach (Template template in toPost)
            {
                Data data = await PostEventAsync(template);
                AddEvent(events, template, data);
            }

            _saveManager.Data.Events = events.ToDictionary(e => e.Key, e => e.Value.Data);

            return events;
        }

        private async Task PostOrUpdateScheduleAsync(IEnumerable<Event> events, DateTime weekStart)
        {
            string text = PrepareWeekSchedule(events, weekStart);

            if (IsMessageRelevant(_eventsChat.PinnedMessage, weekStart))
            {
                await EditMessageTextAsync(_eventsChat.PinnedMessage.MessageId, text, true);
            }
            else
            {
                int messageId = await SendTextMessageAsync(text, true);
                await _client.PinChatMessageAsync(_eventsChatId, messageId, true);
            }
        }

        private async Task CreateOrUpdateNotificationsAsync(IEnumerable<Event> events)
        {
            foreach (Event e in events)
            {
                await CreateOrUpdateNotificationAsync(e);
            }
        }

        private async Task CreateOrUpdateNotificationAsync(Event e)
        {
            string text = GetNotificationText(e.Template);

            if (string.IsNullOrWhiteSpace(text))
            {
                if (!e.Data.NotificationId.HasValue)
                {
                    return;
                }

                await DeleteMessageAsync(e.Data.NotificationId.Value);
                e.Data.NotificationId = null;
                return;
            }

            if (e.Data.NotificationId.HasValue)
            {
                await EditMessageTextAsync(e.Data.NotificationId.Value, text);
            }
            else
            {
                e.Data.NotificationId = await SendTextMessageAsync(text, replyToMessageId: e.Data.MessageId);
            }
        }

        private static string GetNotificationText(Template template)
        {
            DateTime now = DateTime.Now;

            if (template.End <= now)
            {
                return null;
            }

            TimeSpan startIn = template.Start - now;
            if (startIn > TimeSpan.FromHours(1))
            {
                return null;
            }

            if (startIn > TimeSpan.FromMinutes(15))
            {
                return $"*Через час* начнётся мероприятие [{template.Name}]({template.Uri}).";
            }

            if (startIn > TimeSpan.Zero)
            {
                return $"*Через 15 минут* начнётся мероприятие [{template.Name}]({template.Uri}).";
            }

            return $"*Сейчас* идёт мероприятие [{template.Name}]({template.Uri}).";
        }

        private static void AddEvent(IDictionary<int, Event> events, Template template, Data data)
        {
            var e = new Event(template, data);
            events[template.Id] = e;
        }

        private async Task<Data> PostEventAsync(Template template)
        {
            string text = GetMessageText(template);
            int messageId = await SendTextMessageAsync(text);
            return new Data(messageId);
        }

        private async Task<int> SendTextMessageAsync(string text, bool disableWebPagePreview = false,
            bool disableNotification = false, int replyToMessageId = 0)
        {
            Message message = await _client.SendTextMessageAsync(_eventsChatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId);
            _saveManager.Data.Texts[message.MessageId] = text;
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

        private string PrepareWeekSchedule(IEnumerable<Event> events, DateTime start)
        {
            var scheduleBuilder = new StringBuilder();
            DateTime date = start.AddDays(-1);
            foreach (Event e in events.OrderBy(e => e.Template.Start))
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
                var messageUri =
                    new Uri(string.Format(ChannelMessageUriFormat, _eventsChat.Username, e.Data.MessageId));
                scheduleBuilder.AppendLine($"{e.Template.Start:HH:mm} [{e.Template.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {_formUri}.");
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private async Task EditMessageTextAsync(int messageId, string text, bool disableWebPagePreview = false)
        {
            if (_saveManager.Data.Texts.ContainsKey(messageId) && (_saveManager.Data.Texts[messageId] == text))
            {
                return;
            }
            await _client.EditMessageTextAsync(_eventsChatId, messageId, text, ParseMode.Markdown,
                disableWebPagePreview);
            _saveManager.Data.Texts[messageId] = text;
        }

        private async Task DeleteMessageAsync(int messageId)
        {
            await _client.DeleteMessageAsync(_eventsChatId, messageId);
            _saveManager.Data.Texts.Remove(messageId);
        }

        private static string GetMessageText(Template template)
        {
            var builder = new StringBuilder();

            if (template.Uri != null)
            {
                builder.Append($"⁠[{WordJoiner}]({template.Uri})");
            }
            builder.AppendLine($"*{template.Name}*");

            builder.AppendLine();
            builder.AppendLine(template.Description);

            builder.AppendLine();
            builder.AppendLine($"🕰️ *Когда:* {template.Start:dd MMMM, HH:mm}-{template.End:HH:mm}.");

            if (!string.IsNullOrWhiteSpace(template.Hosts))
            {
                builder.AppendLine();
                builder.AppendLine($"🎤 *Кто ведёт*: {template.Hosts}.");
            }

            builder.AppendLine();
            builder.AppendLine($"💰 *Цена*: {template.Price}.");

            if (template.IsWeekly)
            {
                builder.AppendLine();
                builder.AppendLine("📆 Мероприятие проходит каждую неделю.");
            }

            builder.AppendLine();
            builder.AppendLine($"🗞️ *Принять участие*: {template.Uri}.");

            return builder.ToString();
        }

        private static bool IsMessageRelevant(Message message, DateTime start)
        {
            if (message == null)
            {
                return false;
            }

            return message.Date >= start;
        }

        private const string ChannelMessageUriFormat = "https://t.me/{0}/{1}";
        private const string WordJoiner = "\u2060";
    }
}
