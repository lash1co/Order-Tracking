using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Common.Exceptions;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Drivers.CreateDriver;
using OrderTracking.Application.Drivers.GetDriverPerformance;
using OrderTracking.Application.Drivers.GetNearestDrivers;
using OrderTracking.Application.Drivers.UpdateDriverLocation;
using OrderTracking.Application.Orders.AssignDriver;
using OrderTracking.Application.Orders.CreateOrder;
using OrderTracking.Application.Orders.GetActiveOrders;
using OrderTracking.Application.Orders.GetOrder;
using OrderTracking.Application.Orders;
using OrderTracking.Application.Orders.UpdateOrderStatus;
using OrderTracking.Domain.Assignments;
using OrderTracking.Domain.Drivers;
using OrderTracking.Domain.Orders;

namespace OrderTracking.UnitTests.Application;

public sealed class HandlerTests
{
    [Fact]
    public async Task CreateOrderPersistsOrderAndItems()
    {
        var store = new FakeStore();
        var handler = new CreateOrderHandler(store, store, store, store, store);
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            [new CreateOrderItem(Guid.NewGuid(), 2, 15m)]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(store.Orders);
        Assert.Single(result.Items);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task CreateOrderWithoutItemsThrows()
    {
        var store = new FakeStore();
        var handler = new CreateOrderHandler(store, store, store, store, store);
        var command = new CreateOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), []);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrderReturnsExistingOrder()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        store.Orders.Add(order);

        var result = await new GetOrderHandler(store).Handle(order.Id, CancellationToken.None);

        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task GetMissingOrderThrows()
    {
        var store = new FakeStore();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetOrderHandler(store).Handle(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveOrdersValidatesPagination()
    {
        var store = new FakeStore();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new GetActiveOrdersHandler(store, store).Handle(new GetActiveOrdersQuery(0, 50), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusRejectsStaleVersion()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        store.Orders.Add(order);
        var handler = new UpdateOrderStatusHandler(store, store, store, store, store, store, store);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Preparing, "AQ=="), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusRejectsInvalidVersionEncoding()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        store.Orders.Add(order);
        var handler = new UpdateOrderStatusHandler(store, store, store, store, store, store, store);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Preparing, "not-base64"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusPersistsValidTransition()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        store.Orders.Add(order);
        var handler = new UpdateOrderStatusHandler(store, store, store, store, store, store, store);

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Preparing, string.Empty), CancellationToken.None);

        Assert.Equal(OrderStatus.Preparing, result.Status);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task CreateDriverPersistsAvailableDriver()
    {
        var store = new FakeStore();

        var id = await new CreateDriverHandler(store, store).Handle(
            new CreateDriverCommand("Ada", VehicleType.Bicycle, 4.7, -74.1), CancellationToken.None);

        Assert.Equal(id, Assert.Single(store.Drivers).Id);
        Assert.Equal(DriverStatus.Available, store.Drivers[0].Status);
    }

    [Fact]
    public async Task UpdateDriverLocationChangesPosition()
    {
        var store = new FakeStore();
        var driver = CreateDriver();
        store.Drivers.Add(driver);

        await new UpdateDriverLocationHandler(store, store, store, store).Handle(
            new UpdateDriverLocationCommand(driver.Id, 4.7, -74.1), CancellationToken.None);

        Assert.Equal(4.7, driver.CurrentLocation.Latitude);
    }

    [Fact]
    public async Task AssignDriverCreatesAssignment()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        var driver = CreateDriver();
        driver.ChangeStatus(DriverStatus.Available);
        store.Orders.Add(order);
        store.Drivers.Add(driver);

        var id = await new AssignDriverHandler(store, store, store, store, store, store, store).Handle(
            new AssignDriverCommand(order.Id, driver.Id), CancellationToken.None);

        Assert.Equal(id, Assert.Single(store.Assignments).Id);
        Assert.Equal(DriverStatus.Assigned, driver.Status);
    }

    [Fact]
    public async Task AssignUnavailableDriverThrowsConflict()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        var driver = CreateDriver();
        store.Orders.Add(order);
        store.Drivers.Add(driver);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new AssignDriverHandler(store, store, store, store, store, store, store).Handle(
                new AssignDriverCommand(order.Id, driver.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeliveringAssignedOrderCompletesAssignmentAndReleasesDriver()
    {
        var store = new FakeStore();
        var order = CreateOrder();
        var driver = CreateDriver();
        driver.ChangeStatus(DriverStatus.Available);
        store.Orders.Add(order);
        store.Drivers.Add(driver);
        var handler = new UpdateOrderStatusHandler(store, store, store, store, store, store, store);

        await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Preparing, string.Empty), CancellationToken.None);
        await new AssignDriverHandler(store, store, store, store, store, store, store).Handle(
            new AssignDriverCommand(order.Id, driver.Id), CancellationToken.None);
        await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.OutForDelivery, string.Empty), CancellationToken.None);

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Delivered, string.Empty), CancellationToken.None);

        Assert.Equal(OrderStatus.Delivered, result.Status);
        Assert.NotNull(Assert.Single(store.Assignments).CompletedAt);
        Assert.Equal(DriverStatus.Available, driver.Status);
    }

    [Fact]
    public async Task NearestDriversMapsDistance()
    {
        var store = new FakeStore();
        var driver = CreateDriver();
        driver.ChangeStatus(DriverStatus.Available);
        store.NearbyDrivers.Add(new DriverDistance(driver, 125));

        var result = await new GetNearestDriversHandler(store).Handle(
            new GetNearestDriversQuery(4.7, -74.1), CancellationToken.None);

        Assert.Equal(125, Assert.Single(result).DistanceMeters);
    }

    [Fact]
    public async Task DriverPerformanceMapsResult()
    {
        var store = new FakeStore { Performance = new DriverPerformance(Guid.NewGuid(), 4, 18.5) };

        var result = await new GetDriverPerformanceHandler(store).Handle(
            store.Performance.DriverId, CancellationToken.None);

        Assert.Equal(4, result.CompletedDeliveries);
    }

    private static Order CreateOrder() => Order.Create(
        Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

    private static Driver CreateDriver() =>
        Driver.Create("Ada", VehicleType.Bicycle, GeoLocation.Create(0, 0));

    private sealed class FakeStore :
        IOrderRepository,
        IDriverRepository,
        IDriverAssignmentRepository,
        IUnitOfWork,
        IActiveOrdersCache,
        ITrackingNotifier,
        IOrderTrackingEventPublisher
    {
        public List<Order> Orders { get; } = [];
        public List<Driver> Drivers { get; } = [];
        public List<DriverAssignment> Assignments { get; } = [];
        public List<DriverDistance> NearbyDrivers { get; } = [];
        public DriverPerformance? Performance { get; init; }
        public int SaveCount { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetActiveAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(Orders.Skip(skip).Take(take).ToArray());

        public Task AddAsync(Driver driver, CancellationToken cancellationToken)
        {
            Drivers.Add(driver);
            return Task.CompletedTask;
        }

        Task<Driver?> IDriverRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Drivers.SingleOrDefault(driver => driver.Id == id));

        public Task<IReadOnlyList<Driver>> GetActiveAsync(int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Driver>>(Drivers.Where(driver => driver.Status != DriverStatus.Offline).Take(take).ToArray());

        public Task<IReadOnlyList<DriverDistance>> GetNearestAvailableAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DriverDistance>>(NearbyDrivers.Take(take).ToArray());

        public Task<IReadOnlyList<Driver>> GetForLocationSimulationAsync(int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Driver>>(Drivers.Take(take).ToArray());

        public Task AddAsync(DriverAssignment assignment, CancellationToken cancellationToken)
        {
            Assignments.Add(assignment);
            return Task.CompletedTask;
        }

        public Task<DriverAssignment?> GetActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(Assignments.SingleOrDefault(item => item.OrderId == orderId && item.CompletedAt is null));

        public Task<bool> HasActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(Assignments.Any(item => item.OrderId == orderId && item.CompletedAt is null));

        public Task<bool> HasActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
            Task.FromResult(Assignments.Any(item => item.DriverId == driverId && item.CompletedAt is null));

        public Task<DriverPerformance?> GetPerformanceAsync(Guid driverId, CancellationToken cancellationToken) =>
            Task.FromResult(Performance?.DriverId == driverId ? Performance : null);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<OrderDto>?> GetAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderDto>?>(null);

        public Task SetAsync(int skip, int take, IReadOnlyList<OrderDto> orders, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task InvalidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task OrderChangedAsync(OrderDto order, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DriverLocationChangedAsync(DriverLocationDto driverLocation, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
            where TEvent : notnull => Task.CompletedTask;
    }
}
