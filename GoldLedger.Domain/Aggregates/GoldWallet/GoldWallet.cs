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
            return Result<GoldWallet>.Failure("Ø´Ù†Ø§Ø³Ù‡ Ú©Ø§Ø±Ø¨Ø± Ù†Ø§Ù…Ø¹ØªØ¨Ø± Ø§Ø³Øª.", ErrorType.Validation);

        var balance = initialBalance ?? GoldWeight.Zero;

        var wallet = new GoldWallet(userId, balance);
        return Result<GoldWallet>.Success(wallet);
    }

    public Result Deposit(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± ÙˆØ§Ø±ÛŒØ² Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ø®Ø§Ù„ÛŒ Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± ÙˆØ§Ø±ÛŒØ² Ø¨Ø§ÛŒØ¯ Ø¨ÛŒØ´ØªØ± Ø§Ø² ØµÙØ± Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        Balance += weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Withdraw(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ø¨Ø±Ø¯Ø§Ø´Øª Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ø®Ø§Ù„ÛŒ Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ø¨Ø±Ø¯Ø§Ø´Øª Ø¨Ø§ÛŒØ¯ Ø¨ÛŒØ´ØªØ± Ø§Ø² ØµÙØ± Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø¢Ø²Ø§Ø¯ Ú©ÛŒÙ Ù¾ÙˆÙ„ Ø¨Ø±Ø§ÛŒ Ø§Ù†Ø¬Ø§Ù… Ø§ÛŒÙ† Ø¹Ù…Ù„ÛŒØ§Øª Ú©Ø§ÙÛŒ Ù†ÛŒØ³Øª.", ErrorType.Validation);

        Balance -= weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Lock(GoldWeight weight)
    {
        if (weight is null)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ù…Ø³Ø¯ÙˆØ¯Ø³Ø§Ø²ÛŒ Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ø®Ø§Ù„ÛŒ Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (weight.Value <= 0)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ù…Ø³Ø¯ÙˆØ¯Ø³Ø§Ø²ÛŒ Ø¨Ø§ÛŒØ¯ Ø¨ÛŒØ´ØªØ± Ø§Ø² ØµÙØ± Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (AvailableBalance < weight)
            return Result.Failure("Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø¢Ø²Ø§Ø¯ Ú©Ø§ÙÛŒ Ø¨Ø±Ø§ÛŒ Ù…Ø³Ø¯ÙˆØ¯Ø³Ø§Ø²ÛŒ ÙˆØ¬ÙˆØ¯ Ù†Ø¯Ø§Ø±Ø¯.", ErrorType.Validation);

        LockedBalance += weight;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result ReleaseLocked(GoldWeight weight, bool isSettled)
    {
        if (weight is null)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ù‚Ø§Ø¨Ù„ Ø¢Ø²Ø§Ø¯Ø³Ø§Ø²ÛŒ Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ø®Ø§Ù„ÛŒ Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        if (LockedBalance < weight)
            return Result.Failure("Ù…Ù‚Ø¯Ø§Ø± Ø¯Ø±Ø®ÙˆØ§Ø³ØªÛŒ Ø¨ÛŒØ´ØªØ± Ø§Ø² Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ù…Ø³Ø¯ÙˆØ¯Ø´Ø¯Ù‡ Ø§Ø³Øª.", ErrorType.Validation);

        LockedBalance -= weight;

        if (isSettled)
            Balance -= weight;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}


