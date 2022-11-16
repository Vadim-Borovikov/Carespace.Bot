using System;
using GoogleSheetsManager;
using System.ComponentModel.DataAnnotations;
using GryphonUtilities;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Events;

internal sealed class Template
{
    [UsedImplicitly]
    [Required]
    [SheetField]
    public int Id;

    [UsedImplicitly]
    [Required]
    [SheetField("Как называется ваше мероприятие?")]
    public string Name = null!;

    [UsedImplicitly]
    [Required]
    [SheetField("Расскажите о нём побольше")]
    public string Description = null!;

    [UsedImplicitly]
    [SheetField("Кто будет вести?")]
    public string? Hosts;

    [UsedImplicitly]
    [Required]
    [SheetField("Цена")]
    public string Price = null!;

    [UsedImplicitly]
    [Required]
    [SheetField("Тип события")]
    public string Type = null!;

    [UsedImplicitly]
    [Required]
    [SheetField("Ссылка для регистрации или участия")]
    public Uri Uri = null!;

    [UsedImplicitly]
    [Required]
    [SheetField("Дата проведения")]
    public DateOnly StartDate;

    [UsedImplicitly]
    [Required]
    [SheetField("Время начала")]
    public TimeOnly StartTime;

    [UsedImplicitly]
    [Required]
    [SheetField("Продолжительность")]
    public TimeSpan Duration;

    [UsedImplicitly]
    [SheetField("Пропуск")]
    public DateOnly? Skip;

    public bool IsWeekly
    {
        get
        {
            return Type switch
            {
                "Еженедельное" => true,
                "Однократное"  => false,
                _              => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
            };
        }
    }

    public DateTimeOffset GetStart(TimeZoneInfo info) => DateTimeOffsetHelper.FromOnly(StartDate, StartTime, info);

    public DateTimeOffset GetEnd(TimeZoneInfo info) => GetStart(info) + Duration;

    public bool Active => !IsWeekly || (Skip != StartDate);

    public void MoveToWeek(DateOnly weekStart)
    {
        TimeSpan difference = DateTimeOffsetHelper.FromOnly(weekStart) - DateTimeOffsetHelper.FromOnly(StartDate);
        int weeks = (int) Math.Ceiling(difference.TotalDays / 7);
        StartDate = StartDate.AddDays(7 * weeks);
    }
}