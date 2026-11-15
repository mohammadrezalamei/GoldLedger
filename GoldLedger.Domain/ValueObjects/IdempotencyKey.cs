using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public sealed class IdempotencyKey
{
    public const int MaxLength = 128;
    public string Value { get; }

    private IdempotencyKey(string value)
    {
        Value = value;
    }

    public static Result<IdempotencyKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
            return Result<IdempotencyKey>.Failure("کلید درخواست باید بین ۱ تا ۱۲۸ کاراکتر و شامل حروف انگلیسی، عدد، خط تیره، زیرخط یا نقطه باشد.", ErrorType.Validation);

        return Result<IdempotencyKey>.Success(new IdempotencyKey(value));
    }
}
