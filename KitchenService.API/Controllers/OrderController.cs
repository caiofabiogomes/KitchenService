using KitchenService.Application.Commands.AcceptedRejectedOrder;
using KitchenService.Application.Interfaces;
using KitchenService.Infrastructure.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Contracts.Enums;

namespace KitchenService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(ILogger<OrderController> logger,
                                 IHealthCheck healthCheck,
                                 IOrderRepository repository,
                                 AcceptOrderCommandHandler acceptHandler,
                                 RejectOrderCommandHandler rejectHandler) : ControllerBase
    {
        private readonly ILogger<OrderController> _logger = logger;
        private readonly IHealthCheck _healthCheck = healthCheck;
        private readonly IOrderRepository _repository = repository;
        private readonly AcceptOrderCommandHandler _acceptHandler = acceptHandler;
        private readonly RejectOrderCommandHandler _rejectHandler = rejectHandler;

        [HttpGet("health")]
        public async Task<IActionResult> GetHealthAsync(CancellationToken cancellationToken)
        {
            if (!await _healthCheck.IsMongoDbHealthyAsync(cancellationToken))
            {
                Console.WriteLine("Falha na verificação de Health check. Encerrando aplicação.");
                Environment.Exit(1);
            }

            _logger.LogInformation("Health check endpoint called");
            return Ok(new { Status = "Healthy" });
        }

        [HttpGet("Orders")]
        [Authorize(Roles = "Atendente")]

        public async Task<IActionResult> GetOrdersAsync()
        {
            _logger.LogInformation("GetOrders endpoint called");
            var orders = await _repository.GetPendingOrdersAsync();

            return Ok(orders);
        }

        [HttpPost("AcceptOrder")]
        [Authorize(Roles = "Atendente")]
        public async Task<IActionResult> AcceptOrderAsync(Guid orderId)
        {
            var result = await _acceptHandler.HandleAsync(new OrderCommand(orderId, AcceptOrRejectOrderEnum.Accepted));

            if (!result.IsSuccess)
            {
                _logger.LogError("Erro ao aceitar o pedido: {Error}", result.Message);
                return BadRequest(new { Message = result.Message });
            }

            return Ok(new { Message = "Pedido aceito.", OrderId = orderId });
        }

        [HttpPost("RejectOrder")]
        [Authorize(Roles = "Atendente")]
        public async Task<IActionResult> RejectedOrderAsync( Guid orderId)
        {
            var result = await _rejectHandler.HandleAsync(new OrderCommand(orderId, AcceptOrRejectOrderEnum.Rejected));


            if (!result.IsSuccess)
            {
                _logger.LogError("Erro ao rejeitar o pedido: {Error}", result.Message);
                return BadRequest(new { Message = result.Message });
            }

            return Ok(new { Message = "Pedido recusado.", OrderId = orderId });
        }
    }
}
