using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot
{
    public sealed class Bot : BotBaseGoogleSheets<Bot, Config.Config>
    {
        public Bot(Config.Config config) : base(config)
        {
            var saveManager = new SaveManager<SaveData>(Config.SavePath);

            Calendars = new Dictionary<int, Calendar>();
            EventManager = new Manager(this, saveManager);

            Commands.Add(new StartCommand(this));
            Commands.Add(new IntroCommand(this));
            Commands.Add(new ScheduleCommand(this));
            Commands.Add(new ExercisesCommand(this));
            Commands.Add(new LinksCommand(this));
            Commands.Add(new FeedbackCommand(this));
            Commands.Add(new ThanksCommand(this));
            Commands.Add(new WeekCommand(this));
            Commands.Add(new ConfirmCommand(this));

            _weeklyUpdateTimer = new Events.Timer(TimeManager);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _emeilChecker = new EmailChecker(this, Config.SellerId, Config.ProductId, Config.SellsStart, Config.SellerSecret, Config.BookPromo);

            await base.StartAsync(cancellationToken);
            await EventManager.PostOrUpdateWeekEventsAndScheduleAsync(true);
            Schedule(() => EventManager.PostOrUpdateWeekEventsAndScheduleAsync(false),
                nameof(EventManager.PostOrUpdateWeekEventsAndScheduleAsync));
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _weeklyUpdateTimer.Stop();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _weeklyUpdateTimer?.Dispose();
            EventManager?.Dispose();
            base.Dispose();
        }

        protected override async Task UpdateAsync(Message message, CommandBase<Bot, Config.Config> command,
            bool fromChat = false)
        {
            if (command == null)
            {
                if (!fromChat)
                {
                    MailAddress email = message.Text.AsEmail();
                    if (email == null)
                    {
                        await Client.SendStickerAsync(message.Chat, DontUnderstandSticker);
                    }
                    else
                    {
                        await _emeilChecker.CheckEmailAsync(message.Chat, email);
                    }
                }
                return;
            }

            if (fromChat)
            {
                try
                {
                    await Client.DeleteMessageAsync(message.Chat, message.MessageId);
                }
                catch (ApiRequestException e)
                    when ((e.ErrorCode == MessageToDeleteNotFoundCode)
                          && (e.Message == MessageToDeleteNotFoundText))
                {
                    return;
                }
            }

            if (command.AdminsOnly && !FromAdmin(message))
            {
                if (!fromChat)
                {
                    await Client.SendStickerAsync(message.Chat, ForbiddenSticker);
                }
                return;
            }

            try
            {
                await command.ExecuteAsync(message, fromChat);
            }
            catch (ApiRequestException e)
                when ((e.ErrorCode == CantInitiateConversationCode) && (e.Message == CantInitiateConversationText))
            {
            }
        }

        protected override async Task UpdateAsync(Message message)
        {
            bool fromChat = message.Chat.Id != message.From.Id;

            if (message.Type != MessageType.Text)
            {
                if (!fromChat)
                {
                    await Client.SendStickerAsync(message.Chat, DontUnderstandSticker);
                }
                return;
            }

            string botName = null;
            if (fromChat)
            {
                User user = await GetUserAsunc();
                botName = user.Username;
            }
            CommandBase<Bot, Config.Config> command =
                Commands.FirstOrDefault(c => c.IsInvokingBy(message.Text, fromChat, botName));
            await UpdateAsync(message, command, fromChat);
        }

        private void Schedule(Func<Task> func, string funcName)
        {
            DateTime nextUpdateAt = Utils.GetMonday(TimeManager).AddDays(7) + Config.EventsUpdateAt.TimeOfDay;
            _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeeklyAsync(func, funcName), funcName);
        }

        private async Task DoAndScheduleWeeklyAsync(Func<Task> func, string funcName)
        {
            await func();
            _weeklyUpdateTimer.DoWeekly(func, funcName);
        }

        public readonly IDictionary<int, Calendar> Calendars;

        internal readonly Manager EventManager;

        private readonly Events.Timer _weeklyUpdateTimer;

        private EmailChecker _emeilChecker;

        private const int MessageToDeleteNotFoundCode = 400;
        private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
        private const int CantInitiateConversationCode = 403;
        private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";
    }
}