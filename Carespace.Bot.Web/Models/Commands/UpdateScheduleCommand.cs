using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleSheetsManager;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    internal sealed class UpdateScheduleCommand : Command
    {
        private readonly DataManager _googleSheetsDataManager;
        private readonly string _googleRangePin;
        private readonly string _googleRangeWeek;
        private readonly string _channel;

        public UpdateScheduleCommand(DataManager googleSheetsDataManager, string googleRangePin,
            string googleRangeWeek, string channel)
        {
            _googleSheetsDataManager = googleSheetsDataManager;
            _googleRangePin = googleRangePin;
            _googleRangeWeek = googleRangeWeek;
            _channel = channel;
        }

        internal override string Name => "update_schedule";
        internal override string Description => "обновить расписание событий";

        protected override async Task ExecuteAsync(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            IList<Event> events = _googleSheetsDataManager.GetValues<Event>(_googleRangeWeek);
            string text = PrepareWeekSchedule(events, _channel);
            await UpdateWeekSchedule(text, client);
        }

        private async Task UpdateWeekSchedule(string text, ITelegramBotClient client)
        {
            int? messageId = _googleSheetsDataManager.GetInt(_googleRangePin);
            if (messageId.HasValue)
            {
                await client.EditMessageTextAsync($"@{_channel}", messageId.Value, text, ParseMode.Markdown, true);
            }
        }

        public static string PrepareWeekSchedule(IEnumerable<Event> events, string channel)
        {
            var scheduleBuilder = new StringBuilder();
            DateTime date = Utils.GetMonday(DateTime.Today).AddDays(-1);
            foreach (Event e in events.Where(e => e.DescriptionId.HasValue))
            {
                if (e.Start.Date > date)
                {
                    if (scheduleBuilder.Length > 0)
                    {
                        scheduleBuilder.AppendLine();
                    }
                    date = e.Start.Date;
                    scheduleBuilder.AppendLine($"*{ShowDate(date)}*");
                }
                var messageUri = new Uri(string.Format(ChannelMessageUriFormat, channel, e.DescriptionId));
                scheduleBuilder.AppendLine($"{e.Start:HH:mm} [{e.Name}]({messageUri})");
            }
            scheduleBuilder.AppendLine();
            scheduleBuilder.AppendLine("#расписание");
            return scheduleBuilder.ToString();
        }

        private static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:dd MMMM}";
        }

        private const string ChannelMessageUriFormat = "https://t.me/{0}/{1}";
    }
}
