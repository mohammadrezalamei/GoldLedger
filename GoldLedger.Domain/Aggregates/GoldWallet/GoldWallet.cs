using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.GoldWallet;

public class GoldWallet : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public GoldWeight Balance { get; private set; } = null!;
    public GoldWeight LockedBalance { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int ConcurrencyToken { get; private set; }

    public GoldWeight AvailableBalance => Balance - LockedBalance;

    private GoldWallet()
    {
    }

    private GoldWallet(Guid userId, GoldWeight balance, DateTime occurredAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Balance = balance;
        LockedBalance = GoldWeight.Zero;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    public static Result<GoldWallet> Create(Guid userId, GoldWeight? initialBalance = null, DateTime? occurredAt = null)
    {
        if (userId == Guid.Empty)
            return Result<GoldWallet>.Failure("شناسه کاربر نامعتبر است.", ErrorType.Validation);

        var balance = initialBalance ?? GoldWeight.Zero;

        var wallet = new GoldWallet(userId, balance, occurredAt ?? DateTime.UtcNow);
        return Result<GoldWallet>.Success(wallet);
    }

    public Result Deposit(GoldWeight weight, DateTime? occurredAt = null)
    {
        if (weight is null)
            return Result.Failure("مقدار واریز نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار واریز باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (weight.Value > decimal.MaxValue - Balance.Value)
            return Result.Failure("موجودی کیف پول از محدوده مجاز بیشتر می‌شود.", ErrorType.Validation);

        Balance += weight;
        UpdatedAt = occurredAt ?? DateTime.UtcNow;

        return Result.Success();
    }

    public Result Withdraw(GoldWeight weight, DateTime? occurredAt = null)
    {
        if (weight is null)
            return Result.Failure("مقدار برداشت نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار برداشت باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("موجودی آزاد کیف پول برای انجام این عملیات کافی نیست.", ErrorType.Validation);

        Balance -= weight;
        UpdatedAt = occurredAt ?? DateTime.UtcNow;

        return Result.Success();
    }

    public Result Lock(GoldWeight weight, DateTime? occurredAt = null)
    {
        if (weight is null)
            return Result.Failure("مقدار مسدودسازی نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار مسدودسازی باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("موجودی آزاد کافی برای مسدودسازی وجود ندارد.", ErrorType.Validation);

        LockedBalance += weight;
        UpdatedAt = occurredAt ?? DateTime.UtcNow;

        return Result.Success();
    }

    public Result ReleaseLocked(GoldWeight weight, bool isSettled, DateTime? occurredAt = null)
    {
        if (weight is null)
            return Result.Failure("مقدار قابل آزادسازی نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار قابل آزادسازی باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (LockedBalance < weight)
            return Result.Failure("مقدار درخواستی بیشتر از موجودی مسدودشده است.", ErrorType.Validation);

        LockedBalance -= weight;

        if (isSettled)
            Balance -= weight;

        UpdatedAt = occurredAt ?? DateTime.UtcNow;
        return Result.Success();
    }
}
