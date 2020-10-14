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
    internal sealed class PostEventsCommand : Command
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRangeAll;
        private readonly string _googleRangePin;
        private readonly string _googleRangeWeek;
        private readonly string _channel;

        public PostEventsCommand(DataManager googleSheetsDataManager, string googleRangeAll, string googleRangePin,
            string googleRangeWeek, string channel)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRangeAll = googleRangeAll;
            _googleRangePin = googleRangePin;
            _googleRangeWeek = googleRangeWeek;
            _channel = channel;
        }

        internal override string Name => "post_events";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            DateTime start = Utils.GetMonday();
            List<Event> events = await PostWeekEvents(start, client);
            await PostWeekSchedule(events, client);
        }

        private async Task<List<Event>> PostWeekEvents(DateTime start, ITelegramBotClient client)
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
                Message eventMessage = await client.SendTextMessageAsync($"@{_channel}", text, ParseMode.Markdown,
                    disableNotification: true);
                e.DescriptionId = eventMessage.MessageId;
                weekEvents.Add(e);
            }
            _googleSheetsDataManager.UpdateValues(_googleRangeWeek, weekEvents);
            return weekEvents;
        }

        private async Task PostWeekSchedule(IEnumerable<Event> events, ITelegramBotClient client)
        {
            string text = UpdateScheduleCommand.PrepareWeekSchedule(events, _channel);

            Message message = await client.SendTextMessageAsync($"@{_channel}", text, ParseMode.Markdown, true);
            await client.PinChatMessageAsync($"@{_channel}", message.MessageId, true);
            _googleSheetsDataManager.UpdateValue(_googleRangePin, message.MessageId);
        }

        private static string GetMessageText(Event e)
        {
            var builder = new StringBuilder();

            builder.AppendLine(ShowTitle(e));

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

        private static string ShowTitle(Event e) => e.Uri != null ? $"[{e.Name}]({e.Uri})" : $"*{e.Name}*";
    }
}
