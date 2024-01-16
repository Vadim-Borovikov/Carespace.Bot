using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.FinanceHelper;

[PublicAPI]
public sealed class Product
{
    [Required]
    [MinLength(1)]
    public string Name { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public List<Share> Shares { get; init; } = null!;
}