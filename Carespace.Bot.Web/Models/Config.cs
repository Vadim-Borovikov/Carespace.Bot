using System.ComponentModel.DataAnnotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Web.Models;

public sealed class Config : Carespace.Bot.Config
{
    [Required]
    [MinLength(1)]
    public string CultureInfoName { get; init; } = null!;
}