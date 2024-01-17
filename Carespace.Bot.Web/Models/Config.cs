using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Carespace.Bot.Web.Models;

[PublicAPI]
public sealed class Config : Configs.Config
{
    [Required]
    [MinLength(1)]
    public string CultureInfoName { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string TildaFormId { get; init; } = null!;
}