using System;
using GoogleSheetsManager;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Events;

internal sealed class Template
{
    [UsedImplicitly]
    [Required]
    [SheetField("Id")]
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
    public DateTime StartDate;

    [UsedImplicitly]
    [Required]
    [SheetField("Время начала")]
    public DateTime StartTime;

    [UsedImplicitly]
    [Required]
    [SheetField("Продолжительность")]
    public TimeSpan Duration;

    [UsedImplicitly]
    [SheetField("Пропуск")]
    public DateTime? Skip;

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

    public DateTime Start => StartDate + StartTime.TimeOfDay;

    public DateTime End => Start + Duration;

    public bool Active => !IsWeekly || (Skip != Start.Date);

    public void MoveToWeek(DateTime weekStart)
    {
        int weeks = (int) Math.Ceiling((weekStart - Start).TotalDays / 7);
        StartDate = StartDate.AddDays(7 * weeks);
    }
}