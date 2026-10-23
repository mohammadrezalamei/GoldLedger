using GoldLedger.Domain.Common;
using System.Text.RegularExpressions;

namespace GoldLedger.Domain.ValueObjects;

public class Tag : ValueObject<Tag>
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Tag(string value)
    {
        Value = value;
    }

    private Tag()
    {
        Value = string.Empty;
    }

    public static Result<Tag> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Tag>.Failure("مقدار تگ نمی‌تواند خالی یا نامعتبر باشد.", ErrorType.Validation);

        var normalizedValue = Regex.Replace(value.Trim(), @"\s+", " ");

        if (normalizedValue.Length > MaxLength)
            return Result<Tag>.Failure($"طول تگ نمی‌تواند بیشتر از {MaxLength} کاراکتر باشد.", ErrorType.Validation);

        return Result<Tag>.Success(new Tag(normalizedValue));
    }

    public static implicit operator string(Tag tag) => tag?.Value ?? string.Empty;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}