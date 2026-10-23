using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Contracts.Services;

public interface IWalletService
{
    Task<Result<WalletResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<WalletResponseDto>> DepositAsync(Guid userId, DepositRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<WalletResponseDto>> WithdrawAsync(Guid userId, WithdrawRequestDto request, CancellationToken cancellationToken = default);
}