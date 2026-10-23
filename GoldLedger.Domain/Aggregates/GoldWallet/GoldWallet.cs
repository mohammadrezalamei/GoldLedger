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

    public GoldWeight AvailableBalance => Balance - LockedBalance;

    private GoldWallet() { }

    private GoldWallet(Guid userId, GoldWeight balance)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Balance = balance;
        LockedBalance = GoldWeight.Zero;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<GoldWallet> Create(Guid userId, GoldWeight? initialBalance = null)
    {
        if (userId == Guid.Empty)
            return Result<GoldWallet>.Failure("شناسه کاربر نامعتبر است.", ErrorType.Validation);

        var balance = initialBalance ?? GoldWeight.Zero;

        var wallet = new GoldWallet(userId, balance);
        return Result<GoldWallet>.Success(wallet);
    }

    public Result Deposit(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("مقدار واریز نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار واریز باید بیشتر از صفر باشد.", ErrorType.Validation);

        Balance += weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Withdraw(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("مقدار برداشت نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار برداشت باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("موجودی آزاد کیف پول برای انجام این عملیات کافی نیست.", ErrorType.Validation);

        Balance -= weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Lock(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("مقدار مسدودسازی نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("مقدار مسدودسازی باید بیشتر از صفر باشد.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("موجودی آزاد کافی برای مسدودسازی وجود ندارد.", ErrorType.Validation);

        LockedBalance += weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result ReleaseLocked(GoldWeight weight, bool isSettled)
    {
        if (weight is null)
            return Result.Failure("مقدار قابل آزادسازی نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (LockedBalance < weight)
            return Result.Failure("مقدار درخواستی بیشتر از موجودی مسدودشده است.", ErrorType.Validation);

        LockedBalance -= weight;

        if (isSettled)
            Balance -= weight;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}