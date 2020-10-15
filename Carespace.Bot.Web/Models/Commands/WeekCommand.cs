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
        private readonly string _googleRangeAll;
        private readonly string _googleRangeWeek;
        private readonly string _channelLogin;

        public WeekCommand(DataManager googleSheetsDataManager, string googleRangeAll, string googleRangeWeek,
            string channelLogin)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRangeAll = googleRangeAll;
            _googleRangeWeek = googleRangeWeek;
            _channelLogin = channelLogin;
        }

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            Message statusMessage = await client.SendTextMessageAsync(message.Chat, "_Обновляю…_", ParseMode.Markdown);

            await PostOrUpdateWeekEventsAndSchedule(client);

            await client.FinalizeStatusMessageAsync(statusMessage);
        }

        private async Task PostOrUpdateWeekEventsAndSchedule(ITelegramBotClient client)
        {
            Chat channel = await client.GetChatAsync($"@{_channelLogin}");
            DateTime start = Utils.GetMonday();
            if (IsMessageRelevant(channel.PinnedMessage, start))
            {
                IList<Event> events = _googleSheetsDataManager.GetValues<Event>(_googleRangeWeek);
                string text = PrepareWeekSchedule(events, start);

                await client.EditMessageTextAsync(channel, channel.PinnedMessage.MessageId, text, ParseMode.Markdown,
                    true);
            }
            else
            {
                List<Event> events = await PostWeekEvents(client, start, channel);
                string text = PrepareWeekSchedule(events, start);

                Message message = await client.SendTextMessageAsync(channel, text, ParseMode.Markdown, true);
                await client.PinChatMessageAsync(channel, message.MessageId, true);
            }
        }

        private async Task<List<Event>> PostWeekEvents(ITelegramBotClient client, DateTime start, ChatId chatId)
        {
            DateTime end = start.AddDays(7);
            IList<Event> events = _googleSheetsDataManager.GetValues<Event>(_googleRangeAll);
            var weekEvents = new List<Event>();
            foreach (Event e in events.Where(e => e.Start < end))
            {
                if (e.Start < start)
                {
                    if (!e.IsWeekly)
                    {
                        continue;
                    }

                    e.PlaceOnWeek(start);
                }

                string text = GetMessageText(e);
                Message eventMessage = await client.SendTextMessageAsync(chatId, text, ParseMode.Markdown,
                    disableNotification: true);
                e.DescriptionId = eventMessage.MessageId;
                weekEvents.Add(e);
            }
            _googleSheetsDataManager.UpdateValues(_googleRangeWeek, weekEvents);
            return weekEvents;
        }

        private string PrepareWeekSchedule(IEnumerable<Event> events, DateTime start)
        {
            var scheduleBuilder = new StringBuilder();
            DateTime date = start.AddDays(-1);
            foreach (Event e in events.Where(e => e.DescriptionId.HasValue))
            {
                if (e.Start.Date > date)
                {
                    if (scheduleBuilder.Length > 0)
                    {
                        scheduleBuilder.AppendLine();
                    }
                    date = e.Start.Date;
                    scheduleBuilder.AppendLine($"*{Utils.ShowDate(date)}*");
                }
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, _channelLogin, e.DescriptionId));
                scheduleBuilder.AppendLine($"{e.Start:HH:mm} [{e.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private static string GetMessageText(Event e)
        {
            var builder = new StringBuilder();

            string title = e.Uri != null ? $"[{e.Name}]({e.Uri})" : $"*{e.Name}*";
            builder.AppendLine(title);

            builder.AppendLine();
            builder.AppendLine(e.Description);

            builder.AppendLine();
            builder.AppendLine($"🕰️ *Когда:* {e.Start:dddd dd MMMM}, {e.Start:HH:mm}-{e.End:HH:mm}");
            if (!string.IsNullOrWhiteSpace(e.Hosts))
            {
                builder.AppendLine();
                string form = e.Hosts.Contains(',') ? "Ведущие" : "Ведущий";
                builder.AppendLine($"🎤 *{form}:* {e.Hosts}");
            }
            if ((e.Tags != null) && (e.Tags.Count > 0))
            {
                builder.AppendLine();
                foreach (string tag in e.Tags)
                {
                    builder.Append($"#{tag}");
                }
                builder.AppendLine();
            }
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
    }
}
