using JetBrains.Annotations;

namespace Carespace.Bot.Web.Models;

[PublicAPI]
public sealed class Submission
{
    public string? Test { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Telegram { get; set; }
    public string? Items { get; set; }
    public string? FormId { get; set; }
    public string? TranId { get; set; }
}