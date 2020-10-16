using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleSheetsManager;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekCommand : Command
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly BotSaveManager _saveManager;
        private readonly string _googleRange;
        private readonly string _channelLogin;

        public WeekCommand(DataManager googleSheetsDataManager, BotSaveManager saveManager, string googleRange,
            string channelLogin)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _saveManager = saveManager;
            _googleRange = googleRange;
            _channelLogin = channelLogin;
        }

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            _saveManager.Load();

            Message statusMessage = await client.SendTextMessageAsync(message.Chat, "_Обновляю…_", ParseMode.Markdown);

            await PostOrUpdateWeekEventsAndScheduleAsync(client);

            await client.FinalizeStatusMessageAsync(statusMessage);

            _saveManager.Save();
        }

        private async Task PostOrUpdateWeekEventsAndScheduleAsync(ITelegramBotClient client)
        {
            Chat channel = await client.GetChatAsync($"@{_channelLogin}");
            DateTime weekStart = Utils.GetMonday();
            List<Event> events = LoadWeekEvents(weekStart).ToList();

            if (IsMessageRelevant(channel.PinnedMessage, weekStart))
            {
                List<int> googleTemplateIds = events.Select(e => e.Template.Id).ToList();

                foreach (EventData data in _saveManager.Data.Events)
                {
                    if (googleTemplateIds.Contains(data.TemplateId))
                    {
                        Event toUpdate = events.Single(e => e.Template.Id == data.TemplateId);
                        toUpdate.Data = data;
                        string messageText = GetMessageText(toUpdate);
                        await EditMessageTextAsync(client, channel, toUpdate.Data.MessageId, messageText);
                    }
                    else
                    {
                        await DeleteMessageAsync(client, channel, data.MessageId);
                    }
                }

                IEnumerable<int> telegramTemplateIds = _saveManager.Data.Events.Select(d => d.TemplateId);
                IEnumerable<Event> toAdd = events.Where(e => !telegramTemplateIds.Contains(e.Template.Id));
                await PostEventsAsync(client, toAdd, channel);

                string scheduleText = PrepareWeekSchedule(events, weekStart);

                await EditMessageTextAsync(client, channel, channel.PinnedMessage.MessageId, scheduleText, true);
            }
            else
            {
                _saveManager.Reset();

                await PostEventsAsync(client, events, channel);
                string text = PrepareWeekSchedule(events, weekStart);

                int messageId = await SendTextMessageAsync(client, channel, text, true);
                await client.PinChatMessageAsync(channel, messageId, true);
            }

            _saveManager.Data.Events = events.Select(e => e.Data).ToList();
        }

        private async Task PostEventsAsync(ITelegramBotClient client, IEnumerable<Event> events, ChatId chatId)
        {
            foreach (Event e in events)
            {
                await PostEventAsync(client, chatId, e);
            }
        }

        private async Task PostEventAsync(ITelegramBotClient client, ChatId chatId, Event e)
        {
            string text = GetMessageText(e);
            int messageId = await SendTextMessageAsync(client, chatId, text);
            e.Data.MessageId = messageId;
        }

        private async Task<int> SendTextMessageAsync(ITelegramBotClient client, ChatId chatId, string text,
            bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0)
        {
            Message message = await client.SendTextMessageAsync(chatId, text, ParseMode.Markdown,
                disableWebPagePreview, disableNotification, replyToMessageId);
            _saveManager.Data.Texts[message.MessageId] = text;
            return message.MessageId;
        }

        private IEnumerable<Event> LoadWeekEvents(DateTime weekStart)
        {
            IList<EventTemplate> templates = _googleSheetsDataManager.GetValues<EventTemplate>(_googleRange);
            DateTime weekEnd = weekStart.AddDays(7);
            foreach (EventTemplate t in templates.Where(t => t.Start < weekEnd))
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
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, _channelLogin, e.Data.MessageId));
                scheduleBuilder.AppendLine($"{e.Data.Start:HH:mm} [{e.Template.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private async Task EditMessageTextAsync(ITelegramBotClient client, ChatId chatId, int messageId,
            string text, bool disableWebPagePreview = false)
        {
            if (text == _saveManager.Data.Texts[messageId])
            {
                return;
            }
            await client.EditMessageTextAsync(chatId, messageId, text, ParseMode.Markdown, disableWebPagePreview);
            _saveManager.Data.Texts[messageId] = text;
        }

        private async Task DeleteMessageAsync(ITelegramBotClient client, ChatId chatId, int messageId)
        {
            await client.DeleteMessageAsync(chatId, messageId);
            _saveManager.Data.Texts.Remove(messageId);
        }

        private static string GetMessageText(Event e)
        {
            EventTemplate t = e.Template;

            var builder = new StringBuilder();

            if (t.Uri != null)
            {
                builder.Append($"⁠[{WordJoiner}]({t.Uri})");
            }
            builder.AppendLine($"*{t.Name}*");

            builder.AppendLine();
            builder.AppendLine(t.Description);

            builder.AppendLine();
            builder.AppendLine($"🕰️ *Когда:* {e.Data.Start:dd MMMM, HH:mm}-{e.Data.End:HH:mm}.");

            if (!string.IsNullOrWhiteSpace(t.Hosts))
            {
                builder.AppendLine();
                builder.AppendLine($"🎤 *Кто ведёт*: {t.Hosts}.");
            }

            builder.AppendLine();
            builder.AppendLine($"💰 *Цена*: {t.Price}.");

            if (t.IsWeekly)
            {
                builder.AppendLine();
                builder.AppendLine("📆 Мероприятие проходит каждую неделю.");
            }

            builder.AppendLine();
            builder.AppendLine($"🗞️ *Принять участие*: {t.Uri}.");

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
