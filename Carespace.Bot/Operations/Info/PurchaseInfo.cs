using System.Collections.Generic;
using System.Net.Mail;
using GoogleSheetsManager.Extensions;

namespace Carespace.Bot.Operations.Info;

internal sealed class PurchaseInfo
{
    public readonly string Name;
    public readonly MailAddress Email;
    public readonly string Telegram;
    public readonly List<byte> ProductIds;

    private PurchaseInfo(string name, MailAddress email, string telegram, List<byte> productIds)
    {
        Name = name;
        Email = email;
        Telegram = telegram;
        ProductIds = productIds;
    }

    public static PurchaseInfo? TryParse(string input)
    {
        string[] parts = input.Split(FinanceManager.QuerySeparator);
        if (parts.Length != 4)
        {
            return null;
        }

        string name = parts[0];

        bool created = MailAddress.TryCreate(parts[1], out MailAddress? email);
        if (!created || email is null)
        {
            return null;
        }

        string telegram = parts[2];

        List<byte>? productIds = parts[3].ToList(FinanceManager.BytesSeparator, s => s.ToByte());
        if (productIds is null)
        {
            return null;
        }

        return new PurchaseInfo(name, email, telegram, productIds);
    }
}