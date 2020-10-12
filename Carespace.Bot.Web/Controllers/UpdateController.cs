using System;
using System.Linq;
using System.Threading.Tasks;
using Carespace.Bot.Web.Models.Commands;
using Carespace.Bot.Web.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Web.Controllers
{
    public class UpdateController : Controller
    {
        private readonly IBotService _botService;

        public UpdateController(IBotService botService) { _botService = botService; }

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

                        command = _botService.Commands.FirstOrDefault(c => c.Contains(message));
                        if (command != null)
                        {
                            isAdmin = IsAdmin(message.From);
                            if (command.ShouldProceed(isAdmin))
                            {
                                try
                                {
                                    await command.ExecuteAsyncWrapper(message, _botService.Client, isAdmin);
                                }
                                catch (Exception exception)
                                {
                                    await command.HandleExceptionAsync(exception, message.Chat.Id, _botService.Client);
                                }
                            }
                        }
                        break;
                    case UpdateType.CallbackQuery:
                        CallbackQuery query = update.CallbackQuery;

                        command = _botService.Commands.FirstOrDefault(c => query.Data.Contains(c.Name));
                        if (command != null)
                        {
                            isAdmin = IsAdmin(query.From);
                            if (command.ShouldProceed(isAdmin))
                            {
                                string queryData = query.Data.Replace(command.Name, "");
                                try
                                {
                                    await command.InvokeAsyncWrapper(query.Message, _botService.Client, queryData,
                                        isAdmin);
                                }
                                catch (Exception exception)
                                {
                                    await command.HandleExceptionAsync(exception, query.Message.Chat.Id,
                                        _botService.Client);
                                }
                            }
                        }
                        break;
                }
            }

            return Ok();
        }

        private bool IsAdmin(User user) => _botService.AdminIds.Contains(user.Id);
    }
}
