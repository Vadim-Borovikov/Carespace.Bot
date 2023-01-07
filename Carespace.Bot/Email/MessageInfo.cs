using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using GryphonUtilities;
using MailKit;
using MimeKit;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Carespace.Bot.Email;

internal readonly struct MessageInfo
{
    public required MailAddress Sender { get; init; }
    public required DateOnly Date { get; init; }
    public required string? HtmlBody { get; init; }
    public required string? TextBody { get; init; }
    public required DateTimeFull Sent { get; init; }
    public required string Subject { get; init; }
    public required IList<string> References { get; init; }
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required UniqueId UniqueId { get; init; }
    public required IList<string> Attachments { get; init; }
    public required decimal Amount { get; init; }

    public string? Promocode { get; init; }

    public static async Task<MessageInfo?> FromAsync(MimeMessage message, DirectoryInfo folder,
        TimeManager timeManager, UniqueId uniqueId, decimal defaultAmount)
    {
        if ((message.From.Count == 0) || message.From[0] is not MailboxAddress from)
        {
            return null;
        }

        folder = Directory.CreateDirectory(Path.Combine(folder.FullName, uniqueId.ToString()));

        List<string> attachments = new();
        foreach (MimePart attachment in message.Attachments.OfType<MimePart>())
        {
            string path = await DownloadAsync(attachment, folder);
            attachments.Add(path);
        }

        return new MessageInfo
        {
            Sender = new MailAddress(from.Address, from.Name),
            Date = timeManager.GetDateTimeFull(message.Date).DateOnly,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Sent = DateTimeFull.CreateUtc(message.Date),
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? NoSubject : message.Subject,
            References = message.References,
            Id = message.MessageId,
            Attachments = attachments,
            FirstName = from.Name.Split().First(),
            UniqueId = uniqueId,
            Amount = defaultAmount
        };
    }

    public string GetHtmlBody()
    {
        if (!string.IsNullOrWhiteSpace(HtmlBody))
        {
            return HtmlBody;
        }

        return string.IsNullOrWhiteSpace(TextBody) ? "" : $"<div>{TextBody}</div>";
    }

    private static async Task<string> DownloadAsync(MimePart attachment, FileSystemInfo folder)
    {
        string path = Path.Combine(folder.FullName, attachment.FileName);
        await using (FileStream stream = File.Create(path))
        {
            await attachment.Content.DecodeToAsync(stream);
            return path;
        }
    }

    private const string NoSubject = "(Без темы)";
}