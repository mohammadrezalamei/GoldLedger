using Microsoft.AspNetCore.Mvc;
using GoldLedger.API.Extensions;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Common;

namespace GoldLedger.API.Controllers
{
    [ApiController]
    [Route("api/wallets")]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// دریافت موجودی و اطلاعات کیف پول طلا بر اساس شناسه کاربر
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(Result<WalletResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
        {
            var result = await _walletService.GetByUserIdAsync(userId, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// واریز یا شارژ طلا به کیف پول کاربر (در صورت عدم وجود کیف پول، به صورت خودکار ایجاد می‌شود)
        /// </summary>
        [HttpPost("user/{userId:guid}/deposit")]
        [ProducesResponseType(typeof(Result<WalletResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deposit(Guid userId, [FromBody] DepositRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _walletService.DepositAsync(userId, request, cancellationToken);

            return result.ToCreatedActionResult();
        }

        /// <summary>
        /// برداشت طلا از کیف پول کاربر با رعایت اعتبارسنجی موجودی قابل برداشت
        /// </summary>
        [HttpPost("user/{userId:guid}/withdraw")]
        [ProducesResponseType(typeof(Result<WalletResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Withdraw(Guid userId, [FromBody] WithdrawRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _walletService.WithdrawAsync(userId, request, cancellationToken);
            return result.ToActionResult();
        }
    }
}
