using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderTracking.Application.Orders;
using OrderTracking.Application.Orders.CreateOrder;
using OrderTracking.Application.Orders.AssignDriver;
using OrderTracking.Application.Orders.GetActiveOrders;
using OrderTracking.Application.Orders.GetOrder;
using OrderTracking.Application.Orders.UpdateOrderStatus;
using OrderTracking.Domain.Orders;

namespace OrderTracking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(
    CreateOrderHandler createOrder,
    AssignDriverHandler assignDriver,
    UpdateOrderStatusHandler updateStatus,
    GetActiveOrdersHandler getActiveOrders,
    GetOrderHandler getOrder) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await createOrder.Handle(
            new CreateOrderCommand(
                request.CustomerId,
                request.RestaurantId,
                request.EstimatedDelivery,
                request.Items.Select(item => new CreateOrderItem(item.MenuItemId, item.Quantity, item.Price)).ToArray()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getOrder.Handle(id, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpGet("active")]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await getActiveOrders.Handle(new GetActiveOrdersQuery(page, pageSize), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Dispatcher,Driver")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await updateStatus.Handle(
            new UpdateOrderStatusCommand(id, request.Status, request.Version), cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/assignments")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Assign(Guid id, AssignDriverRequest request, CancellationToken cancellationToken)
    {
        var assignmentId = await assignDriver.Handle(new AssignDriverCommand(id, request.DriverId), cancellationToken);
        return Ok(new { assignmentId });
    }
}

public sealed record CreateOrderItemRequest(Guid MenuItemId, int Quantity, decimal Price);
public sealed record CreateOrderRequest(
    Guid CustomerId,
    Guid RestaurantId,
    DateTimeOffset EstimatedDelivery,
    IReadOnlyCollection<CreateOrderItemRequest> Items);
public sealed record UpdateOrderStatusRequest(OrderStatus Status, string Version);
public sealed record AssignDriverRequest(Guid DriverId);
