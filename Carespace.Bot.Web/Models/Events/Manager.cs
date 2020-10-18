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
    internal sealed class Manager
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRange;
        private readonly BotSaveManager _saveManager;
        private readonly Uri _formUri;
        private readonly ITelegramBotClient _client;
        private readonly ChatId _chatId;

        private Chat _chat;

        public Manager(DataManager googleSheetsDataManager, BotSaveManager saveManager, string googleRange,
            Uri formUri, ITelegramBotClient client, ChatId chatId)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRange = googleRange;
            _saveManager = saveManager;
            _formUri = formUri;
            _client = client;
            _chatId = chatId;
        }

        public async Task PostOrUpdateWeekEventsAndScheduleAsync()
        {
            _chat = await _client.GetChatAsync(_chatId);

            DateTime weekStart = Utils.GetMonday();

            Dictionary<int, Event> events = await PostOrUpdateEvents(weekStart);
            await PostOrUpdateScheduleAsync(events.Values, weekStart);

            _saveManager.Save();
        }

        private async Task<Dictionary<int, Event>> PostOrUpdateEvents(DateTime weekStart)
        {
            Dictionary<int, Template> templates = LoadRelevantTemplates(weekStart);

            _saveManager.Load();

            var events = new Dictionary<int, Event>();

            IEnumerable<Template> toPost = templates.Values;

            if (IsMessageRelevant(_chat.PinnedMessage, weekStart))
            {
                ICollection<int> savedTemplateIds = _saveManager.Data.Events.Keys;
                foreach (int savedTemplateId in savedTemplateIds)
                {
                    if (templates.ContainsKey(savedTemplateId))
                    {
                        Template template = templates[savedTemplateId];
                        Data data = _saveManager.Data.Events[savedTemplateId];

                        string messageText = GetMessageText(template, data.Start, data.End);
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
                Data data = await PostEventAsync(template, weekStart);
                AddEvent(events, template, data);
            }

            _saveManager.Data.Events = events.ToDictionary(e => e.Key, e => e.Value.Data);

            return events;
        }

        private static void AddEvent(IDictionary<int, Event> events, Template template, Data data)
        {
            var e = new Event(template, data);
            events[template.Id] = e;
        }

        private async Task PostOrUpdateScheduleAsync(IEnumerable<Event> events, DateTime weekStart)
        {
            string text = PrepareWeekSchedule(events, weekStart);

            if (IsMessageRelevant(_chat.PinnedMessage, weekStart))
            {
                await EditMessageTextAsync(_chat.PinnedMessage.MessageId, text, true);
            }
            else
            {
                int messageId = await SendTextMessageAsync(text, true);
                await _client.PinChatMessageAsync(_chatId, messageId, true);
            }
        }

        private async Task<Data> PostEventAsync(Template template, DateTime weekStart)
        {
            DateTime start = template.Start;
            DateTime end = template.End;
            if (template.IsWeekly)
            {
                int weeks = (int)Math.Ceiling((weekStart - template.Start).TotalDays / 7);
                start = template.Start.AddDays(7 * weeks);
                end = template.End.AddDays(7 * weeks);
            }

            string text = GetMessageText(template, start, end);
            int messageId = await SendTextMessageAsync(text);
            return new Data(messageId, start, end);
        }

        private async Task<int> SendTextMessageAsync(string text, bool disableWebPagePreview = false,
            bool disableNotification = false, int replyToMessageId = 0)
        {
            Message message = await _client.SendTextMessageAsync(_chatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId);
            _saveManager.Data.Texts[message.MessageId] = text;
            return message.MessageId;
        }

        private Dictionary<int, Template> LoadRelevantTemplates(DateTime weekStart)
        {
            IList<Template> templates = _googleSheetsDataManager.GetValues<Template>(_googleRange);
            DateTime weekEnd = weekStart.AddDays(7);
            return templates
                .Where(t => t.IsApproved && (t.Start < weekEnd) && (t.IsWeekly || (t.Start >= weekStart)))
                .ToDictionary(t => t.Id, t => t);
        }

        private string PrepareWeekSchedule(IEnumerable<Event> events, DateTime start)
        {
            var scheduleBuilder = new StringBuilder();
            DateTime date = start.AddDays(-1);
            foreach (Event e in events.OrderBy(e => e.Data.Start))
            {
                if (e.Data.Start.Date > date)
                {
                    if (scheduleBuilder.Length > 0)
                    {
                        scheduleBuilder.AppendLine();
                    }
                    date = e.Data.Start.Date;
                    scheduleBuilder.AppendLine($"*{Utils.ShowDate(date)}*");
                }
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, _chat.Username, e.Data.MessageId));
                scheduleBuilder.AppendLine($"{e.Data.Start:HH:mm} [{e.Template.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine($"Оставить заявку на добавление своего мероприятия можно здесь: {_formUri}.");
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private async Task EditMessageTextAsync(int messageId, string text, bool disableWebPagePreview = false)
        {
            if (text == _saveManager.Data.Texts[messageId])
            {
                return;
            }
            await _client.EditMessageTextAsync(_chatId, messageId, text, ParseMode.Markdown, disableWebPagePreview);
            _saveManager.Data.Texts[messageId] = text;
        }

        private async Task DeleteMessageAsync(int messageId)
        {
            await _client.DeleteMessageAsync(_chatId, messageId);
            _saveManager.Data.Texts.Remove(messageId);
        }

        private static string GetMessageText(Template template, DateTime start, DateTime end)
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
            builder.AppendLine($"🕰️ *Когда:* {start:dd MMMM, HH:mm}-{end:HH:mm}.");

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
