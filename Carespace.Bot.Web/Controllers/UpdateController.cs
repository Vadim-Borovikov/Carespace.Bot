using System;
using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Controllers
{
    public sealed class UpdateController : Controller
    {
        private readonly IBot _bot;

        public UpdateController(IBot bot) { _bot = bot; }

        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            if (update != null)
            {
                Command command;
                bool isAdmin;
                switch (update.Type)
                {
                    case UpdateType.Message:
                        Message message = update.Message;

                        command = _bot.Commands.FirstOrDefault(c => c.Contains(message));
                        if (command != null)
                        {
                            isAdmin = IsAdmin(message.From);
                            if (command.ShouldProceed(isAdmin))
                            {
                                try
                                {
                                    await command.ExecuteAsyncWrapper(message, _bot.Client, isAdmin);
                                }
                                catch (Exception exception)
                                {
                                    await command.HandleExceptionAsync(exception, message.Chat.Id, _bot.Client);
                                }
                            }
                        }
                        break;
                    case UpdateType.CallbackQuery:
                        CallbackQuery query = update.CallbackQuery;

                        command = _bot.Commands.FirstOrDefault(c => query.Data.Contains(c.Name));
                        if (command != null)
                        {
                            isAdmin = IsAdmin(query.From);
                            if (command.ShouldProceed(isAdmin))
                            {
                                string queryData = query.Data.Replace(command.Name, "");
                                try
                                {
                                    await command.InvokeAsyncWrapper(query.Message, _bot.Client, queryData,
                                        isAdmin);
                                }
                                catch (Exception exception)
                                {
                                    await command.HandleExceptionAsync(exception, query.Message.Chat.Id, _bot.Client);
                                }
                            }
                        }
                        break;
                }
            }

            return Ok();
        }

        private bool IsAdmin(User user) => _bot.AdminIds.Contains(user.Id);
    }
}
