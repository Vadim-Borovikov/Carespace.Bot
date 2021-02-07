using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Carespace.Bot.Commands;
using Carespace.Bot.Events;
using GoogleSheetsManager;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Calendar = Carespace.Bot.Events.Calendar;

namespace Carespace.Bot
{
    public sealed class Bot : IDisposable
    {
        public Bot(Config.Config config)
        {
            _config = config;

            _client = new TelegramBotClient(_config.Token);

            string googleCredentialsJson = JsonConvert.SerializeObject(_config.GoogleCredentials);
            _googleSheetsProvider = new Provider(googleCredentialsJson, ApplicationName, _config.GoogleSheetId);

            Utils.SetupTimeZoneInfo(_config.SystemTimeZoneId);

            var saveManager = new Save.Manager(_config.SavePath);

            Calendars = new Dictionary<int, Calendar>();
            var eventsChatId = new ChatId($"@{_config.EventsChannelLogin}");
            var discussChatId = new ChatId($"@{_config.DiscussGroupLogin}");
            _eventManager = new Manager(_googleSheetsProvider, saveManager, _config.GoogleRange,
                _config.EventsFormUri, _client, eventsChatId, _config.LogsChatId, discussChatId,
                _config.Host, Calendars);

            _commands = new List<Command>();
            _commands.Add(new StartCommand(_commands));
            _commands.Add(new IntroCommand(_config.Introduction));
            _commands.Add(new ScheduleCommand(_config.Schedule));
            _commands.Add(new ExercisesCommand(_config.Template, _config.ExersisesLinks));
            _commands.Add(new LinksCommand(_config.Links));
            _commands.Add(new FeedbackCommand(_config.FeedbackLink));
            _commands.Add(new ThanksCommand(_config.Payees, _config.Banks));
            _commands.Add(new WeekCommand(_eventManager));

            _weeklyUpdateTimer = new Events.Timer();
            _dontUnderstandSticker = new InputOnlineFile(_config.DontUnderstandStickerFileId);
            _forbiddenSticker = new InputOnlineFile(_config.ForbiddenStickerFileId);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.SetWebhookAsync(_config.Url, cancellationToken: cancellationToken);
            await DoAndSchedule(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync,
                nameof(_eventManager.PostOrUpdateWeekEventsAndScheduleAsync));
        }

        public async Task UpdateAsync(Update update)
        {
            if (update?.Type != UpdateType.Message)
            {
                return;
            }

            Message message = update.Message;
            bool fromChat = message.Chat.Id != message.From.Id;
            string botName = fromChat ? await _client.GetNameAsync() : null;
            Command command = _commands.FirstOrDefault(c => c.IsInvokingBy(message, fromChat, botName));

            if (command == null)
            {
                if (!fromChat)
                {
                    await _client.SendStickerAsync(message, _dontUnderstandSticker);
                }
                return;
            }

            if (fromChat)
            {
                try
                {
                    await _client.DeleteMessageAsync(message.Chat, message.MessageId);
                }
                catch (ApiRequestException e)
                    when ((e.ErrorCode == MessageToDeleteNotFoundCode)
                          && (e.Message == MessageToDeleteNotFoundText))
                {
                    return;
                }
            }

            if (command.AdminsOnly)
            {
                bool isAdmin = _config.AdminIds.Contains(message.From.Id);
                if (!isAdmin)
                {
                    if (!fromChat)
                    {
                        await _client.SendStickerAsync(message, _forbiddenSticker);
                    }
                    return;
                }
            }

            try
            {
                await command.ExecuteAsync(message.From.Id, _client);
            }
            catch (ApiRequestException e)
                when ((e.ErrorCode == CantInitiateConversationCode) && (e.Message == CantInitiateConversationText))
            {
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _weeklyUpdateTimer.Stop();
            await _client.DeleteWebhookAsync(cancellationToken);
        }

        public void Dispose()
        {
            _weeklyUpdateTimer?.Dispose();
            _googleSheetsProvider?.Dispose();
            _eventManager?.Dispose();
        }

        public Task<User> GetUserAsunc() => _client.GetMeAsync();

        private async Task DoAndSchedule(Func<Task> func, string funcName)
        {
            await func();
            DateTime nextUpdateAt = Utils.GetMonday().AddDays(7) + _config.EventsUpdateAt.TimeOfDay;
            _weeklyUpdateTimer.DoOnce(nextUpdateAt, () => DoAndScheduleWeekly(func, funcName), funcName);
        }

        private async Task DoAndScheduleWeekly(Func<Task> func, string funcName)
        {
            await func();
            _weeklyUpdateTimer.DoWeekly(func, funcName);
        }

        public readonly IDictionary<int, Calendar> Calendars;

        private readonly List<Command> _commands;
        private readonly TelegramBotClient _client;
        private readonly Config.Config _config;
        private readonly InputOnlineFile _dontUnderstandSticker;
        private readonly InputOnlineFile _forbiddenSticker;

        private readonly Provider _googleSheetsProvider;
        private readonly Manager _eventManager;
        private readonly Events.Timer _weeklyUpdateTimer;

        private const int MessageToDeleteNotFoundCode = 400;
        private const string MessageToDeleteNotFoundText = "Bad Request: message to delete not found";
        private const int CantInitiateConversationCode = 403;
        private const string CantInitiateConversationText = "Forbidden: bot can't initiate conversation with a user";

        private const string ApplicationName = "Carespace.Bot";
    }
}