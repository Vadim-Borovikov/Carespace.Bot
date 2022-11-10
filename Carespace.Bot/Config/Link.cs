using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Config;

[PublicAPI]
public sealed class Link
{
    [Required]
    [MinLength(1)]
    public string Name { get; init; } = null!;

    [Required]
    public Uri Uri { get; init; } = null!;

    public string? PhotoPath { get; init; }
}