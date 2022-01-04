using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot
{
    internal sealed class EmailChecker
    {
        public EmailChecker(Bot bot) => _bot = bot;

        public async Task CheckEmailAsync(ChatId chatId, MailAddress email)
        {
            Message statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.MarkdownV2);
            bool found = await CheckEmailAsync(email);
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
            if (found)
            {
                await _bot.Client.SendTextMessageAsync(chatId, $"Email найден\\! Твой промокод: `{_bot.Config.BookPromo}`",
                    ParseMode.MarkdownV2);
            }
            else
            {
                await _bot.Client.SendTextMessageAsync(chatId, "Email не найден! Напиши @Netris");
            }
        }

        private async Task<bool> CheckEmailAsync(MailAddress email)
        {
            var productIds = new List<int>(_bot.Config.ProductId);
            DateTime finish = DateTime.Today.AddDays(1);
            IEnumerable<string> eMails = await FinanceHelper.Utils.GetDigisellerSellsEmailsAsync(_bot.Config.DigisellerId,
                productIds, _bot.Config.SellsStart, finish, _bot.Config.DigisellerApiGuid);
            return eMails.Contains(email.Address, StringComparer.InvariantCultureIgnoreCase);
        }

        private readonly Bot _bot;
    }
}
