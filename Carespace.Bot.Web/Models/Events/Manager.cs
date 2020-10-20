using System;
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
    internal sealed class Manager : IDisposable
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRange;
        private readonly BotSaveManager _saveManager;
        private readonly Uri _formUri;
        private readonly ITelegramBotClient _client;
        private readonly ChatId _eventsChatId;
        private readonly ChatId _logsChatId;

        private Dictionary<int, Event> _events;
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

            await PostOrUpdateEvents(weekStart);
            await PostOrUpdateScheduleAsync(weekStart);
            await CreateOrUpdateNotificationsAsync();

            _saveManager.Save();

            await _client.FinalizeStatusMessageAsync(statusMessage);
        }

        public void Dispose() => DisposeEvents();

        private void DisposeEvents()
        {
            if (_events == null)
            {
                return;
            }

            foreach (Event e in _events.Values)
            {
                e.Dispose();
            }
        }

        private async Task PostOrUpdateEvents(DateTime weekStart)
        {
            Dictionary<int, Template> templates = LoadRelevantTemplates(weekStart).ToDictionary(t => t.Id, t => t);

            _saveManager.Load();

            DisposeEvents();
            _events = new Dictionary<int, Event>();

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

                        _events[template.Id] = new Event(template, data);
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

            foreach (Template template in toPost.OrderBy(t => t.Start))
            {
                Data data = await PostEventAsync(template);
                _events[template.Id] = new Event(template, data);
            }

            _saveManager.Data.Events = _events.ToDictionary(e => e.Key, e => e.Value.Data);
        }

        private async Task PostOrUpdateScheduleAsync(DateTime weekStart)
        {
            string text = PrepareWeekSchedule(weekStart);

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
                Utils.DoOnce(ref e.Timer, e.Template.Start - Hour, () => NotifyInAnHourAsync(e));
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
            Utils.DoOnce(ref e.Timer, nextAt, () => nextFunc(e));
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

        private async Task DeleteNotificationAsync(Event e)
        {
            if (!e.Data.NotificationId.HasValue)
            {
                return;
            }

            await DeleteMessageAsync(e.Data.NotificationId.Value);
            e.Data.NotificationId = null;
            _saveManager.Save();
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

        private string PrepareWeekSchedule(DateTime start)
        {
            var scheduleBuilder = new StringBuilder();
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

        private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
        private static readonly TimeSpan Soon = TimeSpan.FromMinutes(15);
    }
}
