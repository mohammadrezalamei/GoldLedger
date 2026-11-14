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

    public WalletService(IGoldWalletRepository walletRepository, IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WalletResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return Result<WalletResponseDto>.Failure("کیف پول مورد نظر یافت نشد.", ErrorType.NotFound);

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<WalletResponseDto>> DepositAsync(Guid userId, DepositRequestDto request, CancellationToken cancellationToken = default)
    {
        var goldWeightResult = GoldWeight.Create(request.Amount);
        if (!goldWeightResult.IsSuccess)
            return Result<WalletResponseDto>.Failure(goldWeightResult.Message!, goldWeightResult.ErrorType);

        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);

        if (wallet is null)
        {
            var walletResult = GoldWallet.Create(userId);
            if (!walletResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(walletResult.Message!, walletResult.ErrorType);

            wallet = walletResult.Value!;

            var depositResult = wallet.Deposit(goldWeightResult.Value!);
            if (!depositResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(depositResult.Message!, depositResult.ErrorType);

            await _walletRepository.AddAsync(wallet, cancellationToken);
        }
        else
        {
            var depositResult = wallet.Deposit(goldWeightResult.Value!);
            if (!depositResult.IsSuccess)
                return Result<WalletResponseDto>.Failure(depositResult.Message!, depositResult.ErrorType);

            _walletRepository.Update(wallet);
        }

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saveResult.IsSuccess)
            return Result<WalletResponseDto>.Failure(saveResult.Message!, saveResult.ErrorType);

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<WalletResponseDto>> WithdrawAsync(Guid userId, WithdrawRequestDto request, CancellationToken cancellationToken = default)
    {
        var goldWeightResult = GoldWeight.Create(request.Amount);
        if (!goldWeightResult.IsSuccess)
            return Result<WalletResponseDto>.Failure(goldWeightResult.Message!, goldWeightResult.ErrorType);

        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is null)
            return Result<WalletResponseDto>.Failure("کیف پول مورد نظر یافت نشد.", ErrorType.NotFound);

        var withdrawResult = wallet.Withdraw(goldWeightResult.Value!);
        if (!withdrawResult.IsSuccess)
            return Result<WalletResponseDto>.Failure(withdrawResult.Message!, withdrawResult.ErrorType);

        _walletRepository.Update(wallet);

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saveResult.IsSuccess)
            return Result<WalletResponseDto>.Failure(saveResult.Message!, saveResult.ErrorType);

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    private static WalletResponseDto MapToDto(GoldWallet wallet)
    {
        return new WalletResponseDto(
            wallet.Id,
            wallet.UserId,
            wallet.Balance.Value,
            wallet.LockedBalance.Value,
            wallet.AvailableBalance.Value
        );
    }
}
