using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.GoldOrder;

public sealed class GoldOrder : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public OrderType Type { get; private set; }

    public GoldWeight Weight { get; private set; } = null!;

    public Money UnitPrice { get; private set; } = null!;

    public Percentage FeePercent { get; private set; } = null!;

    public Money TotalAmount { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public string? UserNote { get; private set; }

    private readonly HashSet<Tag> _tags = new();

    public IReadOnlyCollection<Tag> Tags => _tags.ToArray();

    public DateTime CreatedAt { get; private set; }

    public int ConcurrencyToken { get; private set; }

    private GoldOrder()
    {
    }

    private GoldOrder(
        Guid userId,
        OrderType type,
        GoldWeight weight,
        Money unitPrice,
        Percentage feePercent,
        IEnumerable<Tag> tags,
        string? userNote,
        Money totalAmount,
        DateTime occurredAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Weight = weight;
        UnitPrice = unitPrice;
        FeePercent = feePercent;
        Status = OrderStatus.Pending;
        UserNote = userNote;
        CreatedAt = occurredAt;

        foreach (var tag in tags)
            _tags.Add(tag);

        TotalAmount = totalAmount;
    }

    public static Result<GoldOrder> Create(
        Guid userId,
        OrderType type,
        GoldWeight weight,
        Money unitPrice,
        Percentage feePercent,
        IEnumerable<Tag> tags,
        string? userNote = null,
        DateTime? occurredAt = null)
    {
        if (userId == Guid.Empty)
        {
            return Result<GoldOrder>.Failure(
                "شناسه کاربر نامعتبر است.",
                ErrorType.Validation);
        }

        if (type != OrderType.Buy && type != OrderType.Sell)
        {
            return Result<GoldOrder>.Failure(
                "نوع سفارش نامعتبر است.",
                ErrorType.Validation);
        }

        if (weight is null)
        {
            return Result<GoldOrder>.Failure(
                "وزن طلا نمی‌تواند خالی باشد.",
                ErrorType.Validation);
        }

        if (weight.Value <= 0)
        {
            return Result<GoldOrder>.Failure(
                "وزن سفارش باید بیشتر از صفر باشد.",
                ErrorType.Validation);
        }

        if (unitPrice is null)
        {
            return Result<GoldOrder>.Failure(
                "مظنه قیمت نمی‌تواند خالی باشد.",
                ErrorType.Validation);
        }

        if (unitPrice.Amount <= 0)
        {
            return Result<GoldOrder>.Failure(
                "قیمت واحد سفارش باید بیشتر از صفر باشد.",
                ErrorType.Validation);
        }

        if (feePercent is null)
        {
            return Result<GoldOrder>.Failure(
                "درصد کارمزد نمی‌تواند خالی باشد.",
                ErrorType.Validation);
        }

        if (userNote?.Length > 1000)
        {
            return Result<GoldOrder>.Failure(
                "یادداشت سفارش نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.",
                ErrorType.Validation);
        }

        var normalizedTags = (tags ?? Enumerable.Empty<Tag>()).ToArray();
        if (normalizedTags.Any(tag => tag is null))
            return Result<GoldOrder>.Failure("برچسب سفارش نمی‌تواند خالی باشد.", ErrorType.Validation);

        Money totalAmount;
        try
        {
            var baseAmount = weight.Value * unitPrice.Amount;
            var feeAmount = baseAmount * (feePercent.Value / 100m);
            var total = type == OrderType.Buy ? baseAmount + feeAmount : baseAmount - feeAmount;
            totalAmount = Money.Create(total).Value!;
        }
        catch (OverflowException)
        {
            return Result<GoldOrder>.Failure("مبلغ سفارش از محدوده مجاز بیشتر است.", ErrorType.Validation);
        }

        var order = new GoldOrder(
            userId,
            type,
            weight,
            unitPrice,
            feePercent,
            normalizedTags,
            userNote,
            totalAmount,
            occurredAt ?? DateTime.UtcNow);

        if (type == OrderType.Buy)
        {
            order.RaiseDomainEvent(
                new GoldPurchasedDomainEvent(
                    order.Id,
                    order.UserId,
                    order.Weight,
                    order.TotalAmount));
        }

        if (type == OrderType.Sell)
        {
            order.RaiseDomainEvent(
                new GoldSoldDomainEvent(
                    order.Id,
                    order.UserId,
                    order.Weight,
                    order.TotalAmount));
        }

        return Result<GoldOrder>.Success(order);
    }

    public Result UpdateMetadata(
        IEnumerable<Tag> newTags,
        string? newUserNote)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(
                "امکان ویرایش سفارش لغوشده وجود ندارد.",
                ErrorType.Validation);
        }

        if (newUserNote?.Length > 1000)
        {
            return Result.Failure(
                "یادداشت سفارش نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.",
                ErrorType.Validation);
        }

        var normalizedTags = newTags?.ToArray();
        if (normalizedTags is null || normalizedTags.Any(tag => tag is null))
            return Result.Failure("برچسب‌های سفارش نامعتبر است.", ErrorType.Validation);

        _tags.Clear();

        foreach (var tag in normalizedTags)
            _tags.Add(tag);

        UserNote = newUserNote;

        return Result.Success();
    }

    public Result Complete(DateTime? occurredAt = null)
    {
        if (Status != OrderStatus.Pending)
        {
            return Result.Failure(
                "فقط سفارش‌های معلق قابل تکمیل هستند.",
                ErrorType.Validation);
        }

        Status = OrderStatus.Settled;
        RaiseDomainEvent(new GoldOrderSettledDomainEvent(
            Id, UserId, Type, TotalAmount, occurredAt ?? DateTime.UtcNow));

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            return Result.Failure(
                "فقط سفارش‌های معلق قابل لغو هستند.",
                ErrorType.Validation);
        }

        Status = OrderStatus.Cancelled;

        return Result.Success();
    }
}
