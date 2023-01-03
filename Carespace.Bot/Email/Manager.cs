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
            await ReadMessagesAsync(_from.Address);
            if (!Mail.Any())
            {
                await _bot.SendTextMessageAsync(chat, "Новых писем с вложениями нет.");
                return;
            }

            await _bot.SendTextMessageAsync(chat, $"В почту: {_bot.Config.MailUri}", disableWebPagePreview: true);
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (string key in Mail.Keys.Reverse())
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
        _currentMessage = string.IsNullOrWhiteSpace(name) ? Mail[key] : Mail[key] with { FirstName = name };

        string date =
            _currentMessage.Value.Sent.ToString(CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern);
        BodyBuilder builder = new()
        {
            HtmlBody = GetHtmlBody(_currentMessage.Value.FirstName, date, _currentMessage.Value.Sender,
                _currentMessage.Value.GetHtmlBody()),
            Attachments = { _bot.Config.MailAttachmentName }
        };
        if (!string.IsNullOrWhiteSpace(_currentMessage.Value.TextBody))
        {
            builder.TextBody = GetTextBody(_currentMessage.Value.FirstName, date, _currentMessage.Value.Sender,
                _currentMessage.Value.TextBody);
        }

        _toSend = new MimeMessage
        {
            From = { new MailboxAddress(_from.DisplayName, _from.Address) },
            To = { new MailboxAddress(_currentMessage.Value.Sender.DisplayName, _currentMessage.Value.Sender.Address) },
            Subject = AddRe(_currentMessage.Value.Subject),
            Body = builder.ToMessageBody()
        };

        _toSend.References.AddRange(_currentMessage.Value.References);
        if (!string.IsNullOrEmpty(_currentMessage.Value.Id))
        {
            _toSend.InReplyTo = _currentMessage.Value.Id;
            _toSend.References.Add(_toSend.MessageId);
        }

        return _bot.SendTextMessageAsync(chat,
            $"Я собираюсь послать книгу на {_currentMessage.Value.Sender.Address}. ОК? /{ConfirmEmailCommand.CommandName}");
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

    public async Task MarkMailAsReadAsync(Chat chat)
    {
        if (_currentMessage is null)
        {
            await _bot.SendTextMessageAsync(chat, "Письмо для отметки не выбрано.");
            return;
        }

        await using (await StatusMessage.CreateAsync(_bot, chat,
                         $"Отмечаю письмо \"{AbstractBot.Bots.Bot.EscapeCharacters(_currentMessage.Value.Subject)}\" от {_currentMessage.Value.Sender.Address} как прочитанное"))
        {
            using (ImapClient client = new())
            {
                await client.ConnectAsync(_bot.Config.MailImapHost, _bot.Config.MailImapPort, true);
                await client.AuthenticateAsync(_from.Address, _bot.Config.MailPassword);

                await client.Inbox.OpenAsync(FolderAccess.ReadWrite);

                await client.Inbox.AddFlagsAsync(_currentMessage.Value.UniqueId,
                    MessageFlags.Seen | MessageFlags.Answered, false);

                await client.Inbox.CloseAsync();
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

    private async Task ReadMessagesAsync(string userName)
    {
        Mail.Clear();
        using (ImapClient client = new())
        {
            await client.ConnectAsync(_bot.Config.MailImapHost, _bot.Config.MailImapPort, true);
            await client.AuthenticateAsync(userName, _bot.Config.MailPassword);

            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            IList<UniqueId>? recent = await client.Inbox.SearchAsync(SearchQuery.NotSeen);
            if (recent is null)
            {
                return;
            }
            foreach (UniqueId id in recent)
            {
                MimeMessage? message = await client.Inbox.GetMessageAsync(id);
                if (message is null || !message.Attachments.Any())
                {
                    continue;
                }
                MessageInfo? info = MessageInfo.From(message, id);
                if (info is null)
                {
                    continue;
                }

                string key = string.Format(EmailKeyTemplate, id);
                Mail[key] = info.Value;
            }

            await client.Inbox.CloseAsync();
            await client.DisconnectAsync(true);
        }
    }

    private const string EmailKeyTemplate = "email{0}";

    private readonly Bot _bot;
    private readonly MailAddress _from;

    private MessageInfo? _currentMessage;
    private MimeMessage? _toSend;
}