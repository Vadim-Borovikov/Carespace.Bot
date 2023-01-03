using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Email;

internal sealed class Manager
{
    public Manager(Bot bot)
    {
        _bot = bot;
        _from = new MailAddress(bot.Config.MailFromAddress, bot.Config.MailFromName);
    }

    public async Task Check(Chat chat)
    {
        await using (await StatusMessage.CreateAsync(_bot, chat, "Проверяю почту"))
        {
            List<MessageInfo>? mail = await ReadMessagesAsync(_from);
            if (mail is null)
            {
                return;
            }

            _mail.Clear();
            for (int i = 0; i < mail.Count; ++i)
            {
                string key = string.Format(EmailKeyTemplate, i);
                _mail[key] = mail[i];
            }

            await _bot.SendTextMessageAsync(chat, $"В почту: {_bot.Config.MailUri}", disableWebPagePreview: true);
            foreach (string key in _mail.Keys)
            {
                MessageInfo info = _mail[key];
                string text =
                    $"`{key}`\\. {AbstractBot.Bots.Bot.EscapeCharacters(info.Sender.DisplayName)}: {AbstractBot.Bots.Bot.EscapeCharacters(info.Subject)}";
                await _bot.SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
            }
        }
    }

    private async Task<List<MessageInfo>?> ReadMessagesAsync(MailAddress from)
    {
        using (ImapClient client = new())
        {
            await client.ConnectAsync(_bot.Config.MailHost, _bot.Config.MailPort, true);
            await client.AuthenticateAsync(from.Address, _bot.Config.MailPassword);

            IMailFolder? folder = await client.GetFolderAsync(FolderName);
            if (folder is null)
            {
                return null;
            }

            await folder.OpenAsync(FolderAccess.ReadWrite);

            IList<UniqueId>? recent = await folder.SearchAsync(SearchQuery.All);
            if (recent is null)
            {
                return null;
            }
            List<MessageInfo> result = new();
            foreach (UniqueId id in recent)
            {
                MimeMessage? message = await folder.GetMessageAsync(id);
                if (message is null || !message.Attachments.Any())
                {
                    continue;
                }
                MessageInfo? info = MessageInfo.From(message);
                if (info is null)
                {
                    continue;
                }
                result.Insert(0, info.Value);
            }

            return result;
        }
    }

    private const string EmailKeyTemplate = "email{0}";
    private const string FolderName = "Перенаправлено";

    private readonly Bot _bot;
    private readonly MailAddress _from;

    private readonly Dictionary<string, MessageInfo> _mail = new();
}