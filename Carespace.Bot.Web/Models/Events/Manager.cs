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

        private async Task LoadChatAsync()
        {
            if (_chat != null)
            {
                return;
            }
            _chat = await _client.GetChatAsync(_chatId);
        }

        public async Task PostOrUpdateWeekEventsAndScheduleAsync()
        {
            await LoadChatAsync();
            _saveManager.Load();

            DateTime weekStart = Utils.GetMonday();
            List<Event> events = LoadWeekEvents(weekStart).ToList();

            if (IsMessageRelevant(_chat.PinnedMessage, weekStart))
            {
                List<int> googleTemplateIds = events.Select(e => e.Template.Id).ToList();

                foreach (Data data in _saveManager.Data.Events)
                {
                    if (googleTemplateIds.Contains(data.TemplateId))
                    {
                        Event toUpdate = events.Single(e => e.Template.Id == data.TemplateId);
                        toUpdate.Data = data;
                        string messageText = GetMessageText(toUpdate);
                        await EditMessageTextAsync(toUpdate.Data.MessageId, messageText);
                    }
                    else
                    {
                        await DeleteMessageAsync(data.MessageId);
                    }
                }

                IEnumerable<int> telegramTemplateIds = _saveManager.Data.Events.Select(d => d.TemplateId);
                IEnumerable<Event> toAdd = events.Where(e => !telegramTemplateIds.Contains(e.Template.Id));
                await PostEventsAsync(toAdd);

                string scheduleText = PrepareWeekSchedule(events, weekStart);

                await EditMessageTextAsync(_chat.PinnedMessage.MessageId, scheduleText, true);
            }
            else
            {
                _saveManager.Reset();

                await PostEventsAsync(events);
                string text = PrepareWeekSchedule(events, weekStart);

                int messageId = await SendTextMessageAsync(text, true);
                await _client.PinChatMessageAsync(_chatId, messageId, true);
            }

            _saveManager.Data.Events = events.Select(e => e.Data).ToList();

            _saveManager.Save();
        }

        private async Task PostEventsAsync(IEnumerable<Event> events)
        {
            foreach (Event e in events)
            {
                await PostEventAsync(e);
            }
        }

        private async Task PostEventAsync(Event e)
        {
            string text = GetMessageText(e);
            int messageId = await SendTextMessageAsync(text);
            e.Data.MessageId = messageId;
        }


        private async Task<int> SendTextMessageAsync(string text, bool disableWebPagePreview = false,
            bool disableNotification = false, int replyToMessageId = 0)
        {
            Message message = await _client.SendTextMessageAsync(_chatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId);
            _saveManager.Data.Texts[message.MessageId] = text;
            return message.MessageId;
        }

        private IEnumerable<Event> LoadWeekEvents(DateTime weekStart)
        {
            IList<Template> templates = _googleSheetsDataManager.GetValues<Template>(_googleRange);
            DateTime weekEnd = weekStart.AddDays(7);
            foreach (Template t in templates.Where(t => t.IsApproved && (t.Start < weekEnd)))
            {
                if (t.Start < weekStart)
                {
                    if (t.IsWeekly)
                    {
                        yield return new Event(t, weekStart);
                    }

                    continue;
                }

                yield return new Event(t);
            }
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

        private static string GetMessageText(Event e)
        {
            Template template = e.Template;

            var builder = new StringBuilder();

            if (template.Uri != null)
            {
                builder.Append($"⁠[{WordJoiner}]({template.Uri})");
            }
            builder.AppendLine($"*{template.Name}*");

            builder.AppendLine();
            builder.AppendLine(template.Description);

            builder.AppendLine();
            builder.AppendLine($"🕰️ *Когда:* {e.Data.Start:dd MMMM, HH:mm}-{e.Data.End:HH:mm}.");

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
