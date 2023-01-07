using System;
using System.Net.Mail;

namespace Carespace.Bot.Email;

internal readonly struct SellInfo
{
    public required DateOnly Date { get; init; }
    public required decimal Amount { get; init; }
    public required MailAddress Email { get; init; }
    public string? Promocode { get; init; }
}