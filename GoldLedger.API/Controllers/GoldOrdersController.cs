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
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<GoldOrderResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetByIdAsync(id, cancellationToken);
            return result.ToActionResult();
        }
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(Result<IEnumerable<GoldOrderResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetByUserIdAsync(userId, cancellationToken);
            return result.ToActionResult();
        }
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _orderService.CreateOrderAsync(request, cancellationToken);

            return result.ToCreatedActionResult();
        }
        [HttpPatch("{id:guid}/cancel")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.CancelOrderAsync(id, cancellationToken);
            return result.ToActionResult();
        }
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

