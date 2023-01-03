using System.Collections.Generic;
using System.Net.Mail;
using GryphonUtilities;
using MimeKit;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Carespace.Bot.Email;

internal readonly struct MessageInfo
{
    public required MailAddress Sender { get; init; }
    public required string? HtmlBody { get; init; }
    public required string? TextBody { get; init; }
    public required DateTimeFull Sent { get; init; }
    public required string Subject { get; init; }
    public required IList<string> References { get; init; }
    public required string Id { get; init; }

    public static MessageInfo? From(MimeMessage message)
    {
        if ((message.From.Count == 0) || message.From[0] is not MailboxAddress from)
        {
            return null;
        }

        return new MessageInfo
        {
            Sender = new MailAddress(from.Address, from.Name),
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Sent = DateTimeFull.CreateUtc(message.Date),
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? NoSubject : message.Subject,
            References = message.References,
            Id = message.MessageId
        };
    }

    private const string NoSubject = "(Без темы)";
}