namespace OrderTracking.Application.Orders.GetActiveOrders;

public sealed record GetActiveOrdersQuery(int Page = 1, int PageSize = 50);
