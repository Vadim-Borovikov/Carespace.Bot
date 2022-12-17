using System;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Configs;
using GryphonUtilities.Extensions;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot;

[PublicAPI]
public class Config : ConfigGoogleSheets
{
    [Required]
    [MinLength(1)]
    public string GoogleSheetId { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleRange { get; init; } = null!;

    [Required]
    public Uri EventsFormUri { get; init; } = null!;

    [Required]
    public TimeOnly EventsUpdateAt { get; init; }

    [Required]
    [MinLength(1)]
    public string SavePath { get; init; } = null!;

    [Required]
    public long EventsChannelId { get; init; }

    internal long LogsChatId => SuperAdminId.GetValue(nameof(SuperAdminId));
}