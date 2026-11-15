using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.Repositories;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Application.Services;

public class WalletService : IWalletService
{
    private readonly IGoldWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock? _clock;

    public WalletService(
        IGoldWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IClock? clock = null)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<WalletResponseDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return Result<WalletResponseDto>.Failure("کیف پول مورد نظر یافت نشد.", ErrorType.NotFound);

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<WalletResponseDto>> DepositAsync(
        Guid userId,
        DepositRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWorkOperation.ExecuteAsync(_unitOfWork, async () =>
        {
            var weightResult = GoldWeight.Create(request.Amount);
            if (!weightResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(weightResult.Message!, weightResult.ErrorType);

            var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
            var isNewWallet = wallet is null;

            if (wallet is null)
            {
                var walletResult = GoldWallet.Create(userId, occurredAt: _clock?.Now.UtcDateTime);
                if (!walletResult.IsSuccess)
                    return Result<WalletResponseDto>.Failure(walletResult.Message!, walletResult.ErrorType);

                wallet = walletResult.Value!;
            }

            var depositResult = wallet.Deposit(weightResult.Value!, _clock?.Now.UtcDateTime);
            if (!depositResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(depositResult.Message!, depositResult.ErrorType);

            if (isNewWallet)
                await _walletRepository.AddAsync(wallet, cancellationToken);
            else
                _walletRepository.Update(wallet);

            var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(saveResult.Message!, saveResult.ErrorType);

            return Result<WalletResponseDto>.Success(MapToDto(wallet));
        }, cancellationToken);
    }

    public async Task<Result<WalletResponseDto>> WithdrawAsync(
        Guid userId,
        WithdrawRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWorkOperation.ExecuteAsync(_unitOfWork, async () =>
        {
            var weightResult = GoldWeight.Create(request.Amount);
            if (!weightResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(weightResult.Message!, weightResult.ErrorType);

            var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
            if (wallet is null)
                return Result<WalletResponseDto>.Failure("کیف پول مورد نظر یافت نشد.", ErrorType.NotFound);

            var withdrawResult = wallet.Withdraw(weightResult.Value!, _clock?.Now.UtcDateTime);
            if (!withdrawResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(withdrawResult.Message!, withdrawResult.ErrorType);

            _walletRepository.Update(wallet);

            var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(saveResult.Message!, saveResult.ErrorType);

            return Result<WalletResponseDto>.Success(MapToDto(wallet));
        }, cancellationToken);
    }

    private static WalletResponseDto MapToDto(GoldWallet wallet)
    {
        return new WalletResponseDto(
            wallet.Id,
            wallet.UserId,
            wallet.Balance.Value,
            wallet.LockedBalance.Value,
            wallet.AvailableBalance.Value);
    }
}
