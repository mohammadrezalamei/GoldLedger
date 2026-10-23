namespace GoldLedger.Application.DTOs.Wallets;

public record WalletResponseDto(
    Guid Id,
    Guid UserId,
    decimal BalanceValue,
    decimal LockedBalanceValue,
    decimal AvailableBalanceValue
);