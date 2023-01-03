using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AbstractBot;
using Carespace.Bot.Operations.Commands;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Carespace.Bot.Email;

internal sealed class Manager
{
    public readonly Dictionary<string, MessageInfo> Mail = new();

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

            Mail.Clear();
            for (int i = 0; i < mail.Count; ++i)
            {
                string key = string.Format(EmailKeyTemplate, i);
                Mail[key] = mail[i];
            }

            await _bot.SendTextMessageAsync(chat, $"В почту: {_bot.Config.MailUri}", disableWebPagePreview: true);
            foreach (string key in Mail.Keys)
            {
                MessageInfo info = Mail[key];
                string text =
                    $"`{key}`\\. {AbstractBot.Bots.Bot.EscapeCharacters(info.Sender.DisplayName)}: {AbstractBot.Bots.Bot.EscapeCharacters(info.Subject)}";
                await _bot.SendTextMessageAsync(chat, text, ParseMode.MarkdownV2);
            }
        }
    }

    public Task PrepareEmailAsync(Chat chat, string key, string? name)
    {
        MessageInfo originalMessage = Mail[key];
        name ??= originalMessage.DefaultFirstName;

        string date = originalMessage.Sent.ToString(CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern);
        BodyBuilder builder = new()
        {
            HtmlBody = GetHtmlBody(name, date, originalMessage.Sender, originalMessage.GetHtmlBody()),
            Attachments = { _bot.Config.MailAttachmentName }
        };
        if (!string.IsNullOrWhiteSpace(originalMessage.TextBody))
        {
            builder.TextBody = GetTextBody(name, date, originalMessage.Sender, originalMessage.TextBody);
        }

        _toSend = new MimeMessage
        {
            From = { new MailboxAddress(_from.DisplayName, _from.Address) },
            // TODO
            To = { new MailboxAddress("Грифон тестирует", "9ryphon@gmail.com") },
            // To = { new MailboxAddress(originalMessage.Sender.DisplayName, originalMessage.Sender.Address) },
            Subject = AddRe(originalMessage.Subject),
            Body = builder.ToMessageBody()
        };

        _toSend.References.AddRange(originalMessage.References);
        if (!string.IsNullOrEmpty(originalMessage.Id))
        {
            _toSend.InReplyTo = originalMessage.Id;
            _toSend.References.Add(_toSend.MessageId);
        }

        // TODO
        return _bot.SendTextMessageAsync(chat,
            $"Я собираюсь послать книгу на 9ryphon@gmail.com. ОК? /{ConfirmEmailCommand.CommandName}");
        // return _bot.SendTextMessageAsync(chat,
        //     $"Я собираюсь послать книгу на {originalMessage.Sender.Address}. ОК? /{ConfirmEmailCommand.CommandName}");
    }

    public async Task SendEmailAsync(Chat chat)
    {
        if (_toSend is null)
        {
            await _bot.SendTextMessageAsync(chat, "Письмо не подготовлено.");
            return;
        }

        if (_toSend?.To.FirstOrDefault() is not MailboxAddress mailbox)
        {
            await _bot.SendTextMessageAsync(chat, "У письма некорректный адресат.");
            return;
        }

        await using (await StatusMessage.CreateAsync(_bot, chat, $"Посылаю книгу на {mailbox.Address}"))
        {
            using (MailKit.Net.Smtp.SmtpClient client = new())
            {
                await client.ConnectAsync(_bot.Config.MailSmtpHost, _bot.Config.MailSmtpPort);
                await client.AuthenticateAsync(_from.Address, _bot.Config.MailPassword);
                await client.SendAsync(_toSend);
                await client.DisconnectAsync(true);
            }
        }
    }

    private string GetTextBody(string name, string date, MailAddress to, string textBody)
    {
        return GetBody(Environment.NewLine, name, _bot.Config.MailTextBodyFormatLines, date, to.ToString(), textBody);
    }

    private string GetHtmlBody(string name, string date, MailAddress to, string textBody)
    {
        string address = string.Format(_bot.Config.MailHtmlAddressFormat, to.DisplayName, to.Address);
        return GetBody("<br />", name, _bot.Config.MailHtmlBodyFormatLines, date, address, textBody);
    }

    private string GetBody(string separator, string name, IEnumerable<string> bodyFormatLines, string date,
        string address, string textBody)
    {
        string bodyFormat = string.Join(separator, bodyFormatLines);
        string body = string.Format(string.Join(separator, _bot.Config.MailTextFormatLines), name);
        return string.Format(bodyFormat, body, date, address, textBody);
    }

    private string AddRe(string subject)
    {
        return subject.StartsWith(_bot.Config.MailReplyPrefix, StringComparison.Ordinal)
            ? subject
            : _bot.Config.MailReplyPrefix + subject;
    }

    private async Task<List<MessageInfo>?> ReadMessagesAsync(MailAddress from)
    {
        using (ImapClient client = new())
        {
            await client.ConnectAsync(_bot.Config.MailImapHost, _bot.Config.MailImapPort, true);
            await client.AuthenticateAsync(from.Address, _bot.Config.MailPassword);

            // TODO
            IMailFolder? folder = await client.GetFolderAsync(FolderName);
            // TODO
            if (folder is null)
            {
                return null;
            }

            // TODO
            await folder.OpenAsync(FolderAccess.ReadWrite);

            // TODO
            // IList<UniqueId>? recent = await client.Inbox.SearchAsync(SearchQuery.NotSeen);
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
                // TODO
                result.Insert(0, info.Value);
                // result.Add(info.Value);
            }

            return result;
        }
    }

    private const string EmailKeyTemplate = "email{0}";
    // TODO
    private const string FolderName = "Перенаправлено";

    private readonly Bot _bot;
    private readonly MailAddress _from;

    private MimeMessage? _toSend;
}