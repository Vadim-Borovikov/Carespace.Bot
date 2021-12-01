using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Config;
using Carespace.Bot.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Carespace.Bot
{
    public static class Utils
    {
        internal static IEnumerable<string> GetDigisellerSellsEmails(int sellerId, int productId,
            DateTime dateStat, DateTime dateFinish, string sellerSecret)
        {
            string start = dateStat.ToString(GoogleDateTimeFormat);
            string end = dateFinish.ToString(GoogleDateTimeFormat);
            int page = 1;
            int totalPages;
            do
            {
                SellsResult dto = DigisellerProvider.GetSells(sellerId, new List<int> { productId }, start, end, page, sellerSecret);
                foreach (SellsResult.Sell sell in dto.Sells)
                {
                    yield return sell.Email.ToLowerInvariant();
                }
                ++page;
                totalPages = dto.Pages;
            } while (page <= totalPages);
        }

        internal static MailAddress AsEmail(this string email)
        {
            try
            {
                return new MailAddress(email);
            }
            catch
            {
                return null;
            }
        }

        public static void LogException(Exception ex)
        {
            File.AppendAllText(ExceptionsLogPath, $"{ex}{Environment.NewLine}");
        }

        internal static Task SendMessageAsync(this ITelegramBotClient client, Link link, ChatId chatId)
        {
            if (string.IsNullOrWhiteSpace(link.PhotoPath))
            {
                string text = $"[{link.Name}]({link.Url})";
                return client.SendTextMessageAsync(chatId, text, ParseMode.Markdown);
            }

            InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
            return PhotoRepository.SendPhotoAsync(client, chatId, link.PhotoPath, replyMarkup: keyboard);
        }

        internal static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

        internal static DateTime GetMonday(TimeManager timeManager)
        {
            DateTime today = timeManager.Now().Date;
            int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        internal static string ShowDate(DateTime date)
        {
            string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
            return $"{day}, {date:d MMMM}";
        }

        private static InlineKeyboardMarkup GetReplyMarkup(Link link)
        {
            var button = new InlineKeyboardButton(link.Name)
            {
                Url = link.Url
            };
            return new InlineKeyboardMarkup(button);
        }

        private const string GoogleDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        internal const string CalendarUriFormat = "{0}/calendar/{1}";

        private const string ExceptionsLogPath = "errors.txt";
        private const string TimersLogPath = "timers.txt";
    }
}
