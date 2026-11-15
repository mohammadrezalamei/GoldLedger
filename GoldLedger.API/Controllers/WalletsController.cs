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
        /// Ø¯Ø±ÛŒØ§ÙØª Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ùˆ Ø§Ø·Ù„Ø§Ø¹Ø§Øª Ú©ÛŒÙ Ù¾ÙˆÙ„ Ø·Ù„Ø§ Ø¨Ø± Ø§Ø³Ø§Ø³ Ø´Ù†Ø§Ø³Ù‡ Ú©Ø§Ø±Ø¨Ø±
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
        /// ÙˆØ§Ø±ÛŒØ² ÛŒØ§ Ø´Ø§Ø±Ú˜ Ø·Ù„Ø§ Ø¨Ù‡ Ú©ÛŒÙ Ù¾ÙˆÙ„ Ú©Ø§Ø±Ø¨Ø± (Ø¯Ø± ØµÙˆØ±Øª Ø¹Ø¯Ù… ÙˆØ¬ÙˆØ¯ Ú©ÛŒÙ Ù¾ÙˆÙ„ØŒ Ø¨Ù‡ ØµÙˆØ±Øª Ø®ÙˆØ¯Ú©Ø§Ø± Ø§ÛŒØ¬Ø§Ø¯ Ù…ÛŒâ€ŒØ´ÙˆØ¯)
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
        /// Ø¨Ø±Ø¯Ø§Ø´Øª Ø·Ù„Ø§ Ø§Ø² Ú©ÛŒÙ Ù¾ÙˆÙ„ Ú©Ø§Ø±Ø¨Ø± Ø¨Ø§ Ø±Ø¹Ø§ÛŒØª Ø§Ø¹ØªØ¨Ø§Ø±Ø³Ù†Ø¬ÛŒ Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ù‚Ø§Ø¨Ù„ Ø¨Ø±Ø¯Ø§Ø´Øª
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

