using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AbstractBot.Bots;
using Carespace.Bot.Operations.Commands;
using Carespace.Bot.Config;
using Carespace.Bot.Save;
using Carespace.FinanceHelper;
using GryphonUtilities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Carespace.Bot.Operations;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace Carespace.Bot;

public sealed class Bot : BotWithSheets<Config.Config>
{
    internal readonly Dictionary<string, List<Share>> Shares = new();

    public Bot(Config.Config config) : base(config)
    {
        if (config.Shares is not null)
        {
            Shares = config.Shares;
        }
        else if (config.SharesJson is not null)
        {
            Dictionary<string, List<Share>>? deserialized =
                JsonSerializer.Deserialize<Dictionary<string, List<Share>>>(config.SharesJson,
                    JsonSerializerOptionsProvider.PascalCaseOptions);
            if (deserialized is not null)
            {
                Shares = deserialized;
            }
        }

        Dictionary<Type, Func<object?, object?>> additionalConverters = new()
        {
            { typeof(Uri), o => o.ToUri() }
        };
        additionalConverters[typeof(DateOnly)] = additionalConverters[typeof(DateOnly?)] =
            o => o.ToDateOnly(TimeManager);
        additionalConverters[typeof(TimeOnly)] = additionalConverters[typeof(TimeOnly?)] =
            o => o.ToTimeOnly(TimeManager);
        additionalConverters[typeof(TimeSpan)] = additionalConverters[typeof(TimeSpan?)] =
            o => o.ToTimeSpan(TimeManager);

        SaveManager<Data> saveManager = new(Config.SavePath, TimeManager);
        FinanceManager financeManager = new(this, DocumentsManager, additionalConverters);
        EmailChecker emailChecker = new(this, financeManager);

        RestrictionsManager antiSpam = new(this, saveManager);

        Operations.Add(new IntroCommand(this));
        Operations.Add(new ScheduleCommand(this));
        Operations.Add(new ExercisesCommand(this, config));
        Operations.Add(new LinksCommand(this));
        Operations.Add(new FeedbackCommand(this));

        Operations.Add(new FinanceCommand(this, financeManager));
        Operations.Add(new CheckEmailOperation(this, emailChecker));

        WarningCommand warningCommand = new(this, antiSpam);
        SpamCommand spamCommand = new(this, antiSpam);
        Operations.Add(warningCommand);
        Operations.Add(spamCommand);

        _restrictCommands = new List<BotCommand>
        {
            warningCommand.Command,
            spamCommand.Command
        };
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await Client.DeleteMyCommandsAsync(BotCommandScope.ChatAdministrators(Config.DiscussGroupId),
            cancellationToken: cancellationToken);

        await Client.SetMyCommandsAsync(_restrictCommands,
            BotCommandScope.ChatAdministrators(Config.DiscussGroupId), cancellationToken: cancellationToken);
    }

    internal Task SendMessageAsync(Link link, Chat chat)
    {
        if (string.IsNullOrWhiteSpace(link.PhotoPath))
        {
            string text = $"[{EscapeCharacters(link.Name)}]({link.Uri.AbsoluteUri})";
            return SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
        }

        InlineKeyboardMarkup keyboard = GetReplyMarkup(link);
        return PhotoRepository.SendPhotoAsync(this, chat, link.PhotoPath, replyMarkup: keyboard);
    }

    private static InlineKeyboardMarkup GetReplyMarkup(Link link)
    {
        InlineKeyboardButton button = new(link.Name) { Url = link.Uri.AbsoluteUri };
        return new InlineKeyboardMarkup(button);
    }

    private readonly List<BotCommand> _restrictCommands;
}