using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Web.Models;

[PublicAPI]
public sealed class Config : Carespace.Bot.Config.Config
{
    [Required]
    [MinLength(1)]
    public string CultureInfoName { get; init; } = null!;
}