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
        public EmailChecker(Bot bot, int sellerId, int productId, DateTime dateStat, string sellerSecret, string bookPromo)
        {
            _bot = bot;
            _sellerId = sellerId;
            _productId = productId;
            _dateStat = dateStat;
            _sellerSecret = sellerSecret;
            _bookPromo = bookPromo;
        }

        public async Task CheckEmailAsync(ChatId chatId, MailAddress email)
        {
            Message statusMessage = await _bot.Client.SendTextMessageAsync(chatId, "_Проверяю…_", ParseMode.MarkdownV2);
            bool found = CheckEmail(email);
            await _bot.Client.FinalizeStatusMessageAsync(statusMessage);
            if (found)
            {
                await _bot.Client.SendTextMessageAsync(chatId, $"Email найден\\! Твой промокод: `{_bookPromo}`",
                    ParseMode.MarkdownV2);
            }
            else
            {
                await _bot.Client.SendTextMessageAsync(chatId, "Email не найден! Напиши @Netris");
            }
        }

        private bool CheckEmail(MailAddress email)
        {
            DateTime finish = DateTime.Today.AddDays(1);
            IEnumerable<string> eMails =
                Utils.GetDigisellerSellsEmails(_sellerId, _productId, _dateStat, finish, _sellerSecret);
            return eMails.Contains(email.Address.ToLowerInvariant());
        }

        private readonly Bot _bot;
        private readonly int _sellerId;
        private readonly int _productId;
        private readonly DateTime _dateStat;
        private readonly string _sellerSecret;
        private readonly string _bookPromo;
    }
}
