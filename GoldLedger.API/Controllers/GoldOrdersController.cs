using Microsoft.AspNetCore.Mvc;
using GoldLedger.API.Extensions;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Domain.Common;

namespace GoldLedger.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class GoldOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public GoldOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Ø¯Ø±ÛŒØ§ÙØª Ù…Ø´Ø®ØµØ§Øª ÛŒÚ© Ø³ÙØ§Ø±Ø´ Ø®Ø±ÛŒØ¯ ÛŒØ§ ÙØ±ÙˆØ´ Ø·Ù„Ø§ Ø¨Ø§ Ø§Ø³ØªÙØ§Ø¯Ù‡ Ø§Ø² Ø´Ù†Ø§Ø³Ù‡
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<GoldOrderResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetByIdAsync(id, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Ø¯Ø±ÛŒØ§ÙØª Ù„ÛŒØ³Øª ØªÙ…Ø§Ù… Ø³ÙØ§Ø±Ø´â€ŒÙ‡Ø§ÛŒ Ø«Ø¨Øªâ€ŒØ´Ø¯Ù‡ ÛŒÚ© Ú©Ø§Ø±Ø¨Ø± Ø®Ø§Øµ Ø¨Ø± Ø§Ø³Ø§Ø³ Ø´Ù†Ø§Ø³Ù‡ Ú©Ø§Ø±Ø¨Ø±
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(Result<IEnumerable<GoldOrderResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetByUserIdAsync(userId, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Ø«Ø¨Øª Ù…Ø¹Ø§Ù…Ù„Ù‡ Ø¬Ø¯ÛŒØ¯ (Ø®Ø±ÛŒØ¯ ÛŒØ§ ÙØ±ÙˆØ´ Ø·Ù„Ø§) Ù‡Ù…Ø±Ø§Ù‡ Ø¨Ø§ Ø§Ø¹ØªØ¨Ø§Ø±Ø³Ù†Ø¬ÛŒâ€ŒÙ‡Ø§ØŒ Ú©ÛŒÙ Ù¾ÙˆÙ„ Ùˆ Ù…Ø­Ø§Ø³Ø¨Ù‡ Ù…Ø¨Ù„Øº Ú©Ù„
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _orderService.CreateOrderAsync(request, cancellationToken);

            return result.ToCreatedActionResult();
        }

        /// <summary>
        /// Ù„ØºÙˆ Ú©Ø±Ø¯Ù† ÛŒÚ© Ø³ÙØ§Ø±Ø´ Ù…Ø¹Ù„Ù‚ Ùˆ Ø¨Ø§Ø²Ú¯Ø±Ø¯Ø§Ù†Ø¯Ù† Ù…Ù†Ø§Ø¨Ø¹ Ù‚ÙÙ„â€ŒØ´Ø¯Ù‡ Ø¯Ø± Ú©ÛŒÙ Ù¾ÙˆÙ„
        /// </summary>
        [HttpPatch("{id:guid}/cancel")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.CancelOrderAsync(id, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// ØªÚ©Ù…ÛŒÙ„ Ùˆ Ù†Ù‡Ø§ÛŒÛŒ Ú©Ø±Ø¯Ù† ÛŒÚ© Ø³ÙØ§Ø±Ø´ (ØªØ³ÙˆÛŒÙ‡ Ùˆ Ø§Ø¹Ù…Ø§Ù„ ØªØºÛŒÛŒØ±Ø§Øª Ù†Ù‡Ø§ÛŒÛŒ Ø¯Ø± Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ú©ÛŒÙ Ù¾ÙˆÙ„)
        /// </summary>
        [HttpPatch("{id:guid}/complete")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteOrder(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.CompleteOrderAsync(id, cancellationToken);
            return result.ToActionResult();
        }
    }
}

