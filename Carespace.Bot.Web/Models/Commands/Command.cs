using System;
using System.Linq;
using System.Threading.Tasks;
using Google;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Models.Commands
{
    public abstract class Command
    {
        internal enum AccessType
        {
            Admins,
            Users,
            All
        }

        internal abstract string Name { get; }
        internal abstract string Description { get; }

        internal bool Contains(Message message) => (message.Type == MessageType.Text) && message.Text.Contains(Name);

        internal virtual AccessType Type => AccessType.Admins;

        internal Task ExecuteAsyncWrapper(Message message, ITelegramBotClient client, bool fromAdmin)
        {
            CheckAccess(message.From, fromAdmin);
            return ExecuteAsync(message.From.Id, client, fromAdmin);
        }

        internal Task InvokeAsyncWrapper(Message message, ITelegramBotClient client, string data, bool fromAdmin)
        {
            CheckAccess(message.From, fromAdmin);
            return InvokeAsync(message, client, data);
        }

        internal virtual Task HandleExceptionAsync(Exception exception, long chatId, ITelegramBotClient client)
        {
            return IsUsageLimitExceed(exception)
                ? HandleUsageLimitExcessAsync(chatId, client)
                : throw exception;
        }

        internal bool ShouldProceed(bool isAdmin)
        {
            switch (Type)
            {
                case AccessType.Admins:
                    return isAdmin;
                case AccessType.Users:
                case AccessType.All:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract Task ExecuteAsync(ChatId chatId, ITelegramBotClient client, bool fromAdmin);

        protected virtual Task InvokeAsync(Message message, ITelegramBotClient client, string data)
        {
            return Task.CompletedTask;
        }

        private static bool IsUsageLimitExceed(Exception exception)
        {
            return exception is GoogleApiException googleException &&
                (googleException.Error.Code == UsageLimitsExceededCode) &&
                googleException.Error.Errors.Any(e => e.Domain == UsageLimitsExceededDomain);
        }

        private static Task<Message> HandleUsageLimitExcessAsync(long chatId, ITelegramBotClient client)
        {
            return client.SendTextMessageAsync(chatId,
                "Google хочет отдохнуть от меня какое-то время. Попробуй позже, пожалуйста!");
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckAccess(User user, bool isAdmin)
        {
            switch (Type)
            {
                case AccessType.Admins:
                    if (!isAdmin)
                    {
                        throw new Exception($"User @{user} is not in admin list!");
                    }
                    break;
                case AccessType.Users:
                case AccessType.All:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private const int UsageLimitsExceededCode = 403;
        private const string UsageLimitsExceededDomain = "usageLimits";
    }
}
