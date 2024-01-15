using System.Net.Mail;

namespace Carespace.FinanceHelper;

public static class ObjectExtensions
{
    public static MailAddress? ToEmail(this object? o)
    {
        if (o is MailAddress e)
        {
            return e;
        }

        try
        {
            string? s = o?.ToString();
            return s is null ? null : new MailAddress(s);
        }
        catch
        {
            return null;
        }
    }
}