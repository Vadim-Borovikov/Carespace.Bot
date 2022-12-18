using System;
using System.Net.Mail;

namespace Carespace.FinanceHelper;

public static class ObjectExtensions
{
    public static Transaction.PayMethod? ToPayMathod(this object? o)
    {
        if (o is Transaction.PayMethod p)
        {
            return p;
        }
        return Enum.TryParse(o?.ToString(), out p) ? p : null;
    }

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