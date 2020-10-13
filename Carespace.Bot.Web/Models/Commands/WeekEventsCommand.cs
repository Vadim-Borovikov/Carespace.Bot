using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleSheetsReader;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class WeekEventsCommand : Command
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRange;
        private readonly string _channel;

        public WeekEventsCommand(DataManager googleSheetsDataManager, string googleRange, string channel)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRange = googleRange;
            _channel = channel;
        }

        internal override string Name => "week";
        internal override string Description => "события на этой неделе";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            DateTime start = Utils.GetMonday(DateTime.Today);
            DateTime end = start.AddDays(7);
            IList<Event> events = _googleSheetsDataManager.GetValues<Event>(_googleRange);
            DateTime date = start.AddDays(-1);
            var scheduleBuilder = new StringBuilder();
            foreach (Event e in events.Where(e => Utils.IsWithin(e.Start, start, end)))
            {
                string text = GetMessageText(e);
                Message eventMessage = await client.SendTextMessageAsync($"@{_channel}", text, ParseMode.Markdown,
                    disableNotification: true);

                DateTime notificationTime = e.Start - NotificationBefore;
                string notificationText = string.Format(NotificationFormat, ShowTitle(e), NotificationBefore);
                SceduleMessage(notificationTime, client, notificationText, eventMessage.MessageId);

                if (e.Start.Date > date)
                {
                    if (scheduleBuilder.Length > 0)
                    {
                        scheduleBuilder.AppendLine();
                    }
                    date = e.Start.Date;
                    scheduleBuilder.AppendLine($"*{ShowDate(date)}*");
                }
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, _channel, eventMessage.MessageId));
                scheduleBuilder.AppendLine($"{e.Start:HH:mm} [{e.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");

            Message scheduleMessage = await client.SendTextMessageAsync($"@{_channel}", scheduleBuilder.ToString(),
                ParseMode.Markdown, true);
            await client.PinChatMessageAsync($"@{_channel}", scheduleMessage.MessageId, true);
        }

        private async void SceduleMessage(DateTime dateTime, ITelegramBotClient client, string text,
            int parentMessageId)
        {
            await Task.Delay(dateTime - DateTime.Now);
            await client.SendTextMessageAsync($"@{_channel}", text, ParseMode.Markdown,
                replyToMessageId: parentMessageId);
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

        private static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:dd MMMM}";
        }

        private const string ChannelMessageUriFormat = "https://t.me/{0}/{1}";
        private const string NotificationFormat = "{0}: начинаем через *{1:%m} минут*";
        private static readonly TimeSpan NotificationBefore = TimeSpan.FromMinutes(15);
    }
}
