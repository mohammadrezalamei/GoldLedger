namespace GoldLedger.Application.DTOs.Orders;

public record CreateOrderRequestDto(
    Guid UserId,
    OrderType Type,
    decimal WeightValue,
    decimal UnitPriceAmount,
    decimal FeePercent,
    IEnumerable<string>? Tags,
    string? UserNote
) : System.ComponentModel.DataAnnotations.IValidatableObject
{
    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        if (UserId == Guid.Empty) yield return new System.ComponentModel.DataAnnotations.ValidationResult("شناسه کاربر معتبر نیست.", new[] { nameof(UserId) });
        if (WeightValue <= 0) yield return new System.ComponentModel.DataAnnotations.ValidationResult("وزن باید بیشتر از صفر باشد.", new[] { nameof(WeightValue) });
        if (UnitPriceAmount < 0) yield return new System.ComponentModel.DataAnnotations.ValidationResult("قیمت نمی‌تواند منفی باشد.", new[] { nameof(UnitPriceAmount) });
        if (FeePercent is < 0 or > 100) yield return new System.ComponentModel.DataAnnotations.ValidationResult("درصد کارمزد باید بین صفر و صد باشد.", new[] { nameof(FeePercent) });
        if (UserNote?.Length > 1000) yield return new System.ComponentModel.DataAnnotations.ValidationResult("یادداشت بیش از حد مجاز طولانی است.", new[] { nameof(UserNote) });
    }
}

