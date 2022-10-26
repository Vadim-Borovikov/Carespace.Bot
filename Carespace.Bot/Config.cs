using System;
using System.ComponentModel.DataAnnotations;
using AbstractBot;
using GryphonUtilities;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot;

[PublicAPI]
public class Config : ConfigGoogleSheets
{
    [Required]
    [MinLength(1)]
    public string GoogleRange { get; init; } = null!;

    [Required]
    public Uri EventsFormUri { get; init; } = null!;

    [Required]
    public DateTime EventsUpdateAt { get; init; }

    [Required]
    [MinLength(1)]
    public string SavePath { get; init; } = null!;

    [Required]
    public long EventsChannelId { get; init; }

    internal long LogsChatId => SuperAdminId.GetValue(nameof(SuperAdminId));
}