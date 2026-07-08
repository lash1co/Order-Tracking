using OrderTracking.Domain.Exceptions;
using OrderTracking.Domain.Orders;

namespace OrderTracking.UnitTests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_StartsPending()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.ActualDelivery);
    }

    [Fact]
    public void ChangeStatus_FollowsHappyPath_RecordsActualDelivery()
    {
        var order = CreateOrder();

        order.ChangeStatus(OrderStatus.Preparing, Now.AddMinutes(5));
        order.ChangeStatus(OrderStatus.OutForDelivery, Now.AddMinutes(15), hasActiveDriverAssignment: true);
        order.ChangeStatus(OrderStatus.Delivered, Now.AddMinutes(35));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(Now.AddMinutes(35), order.ActualDelivery);
    }

    [Fact]
    public void ChangeStatus_WithoutDriverAssignment_Throws()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Preparing, Now.AddMinutes(5));

        Assert.Throws<DomainException>(() =>
            order.ChangeStatus(OrderStatus.OutForDelivery, Now.AddMinutes(10)));
    }

    [Fact]
    public void ChangeStatus_WhenMovingBackwards_Throws()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Preparing, Now.AddMinutes(5));

        Assert.Throws<DomainException>(() =>
            order.ChangeStatus(OrderStatus.Pending, Now.AddMinutes(10)));
    }

    [Fact]
    public void AddItem_WithInvalidQuantity_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), 0, 10m));
    }

    [Fact]
    public void CreateWithoutCustomerThrows()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(Guid.Empty, Guid.NewGuid(), Now, Now.AddMinutes(45)));
    }

    [Fact]
    public void CreateWithoutRestaurantThrows()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(Guid.NewGuid(), Guid.Empty, Now, Now.AddMinutes(45)));
    }

    [Fact]
    public void CreateWithInvalidEstimateThrows()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(Guid.NewGuid(), Guid.NewGuid(), Now, Now));
    }

    [Fact]
    public void AddValidItemStoresIt()
    {
        var order = CreateOrder();
        var menuItemId = Guid.NewGuid();

        order.AddItem(menuItemId, 2, 12.50m);

        var item = Assert.Single(order.Items);
        Assert.Equal(menuItemId, item.MenuItemId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(12.50m, item.Price);
    }

    [Fact]
    public void AddItemWithoutMenuItemThrows()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.Empty, 1, 10m));
    }

    [Fact]
    public void AddItemWithNegativePriceThrows()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), 1, -0.01m));
    }

    [Fact]
    public void AddItemToCancelledOrderThrows()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Cancelled, Now.AddMinutes(1));

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), 1, 10m));
    }

    private static Order CreateOrder() =>
        Order.Create(Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddMinutes(45));
}
