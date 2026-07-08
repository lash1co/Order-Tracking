using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Drivers.GetDriverPerformance;
using OrderTracking.Application.Drivers.GetNearestDrivers;
using OrderTracking.Application.Drivers.CreateDriver;
using OrderTracking.Application.Drivers.UpdateDriverLocation;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/drivers")]
public sealed class DriversController(
    CreateDriverHandler createDriver,
    UpdateDriverLocationHandler updateLocation,
    GetNearestDriversHandler getNearestDrivers,
    GetDriverPerformanceHandler getDriverPerformance) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var id = await createDriver.Handle(
            new CreateDriverCommand(request.Name, request.VehicleType, request.Latitude, request.Longitude),
            cancellationToken);
        return Created($"/api/v1/drivers/{id}", new { id });
    }

    [HttpPatch("{id:guid}/location")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> UpdateLocation(Guid id, UpdateDriverLocationRequest request, CancellationToken cancellationToken)
    {
        await updateLocation.Handle(
            new UpdateDriverLocationCommand(id, request.Latitude, request.Longitude), cancellationToken);
        return NoContent();
    }

    [HttpGet("nearby")]
    [ProducesResponseType<IReadOnlyList<NearbyDriverDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusMeters = 5000,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getNearestDrivers.Handle(
            new GetNearestDriversQuery(latitude, longitude, radiusMeters, take), cancellationToken));

    [HttpGet("{id:guid}/performance")]
    [ProducesResponseType<DriverPerformanceDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformance(Guid id, CancellationToken cancellationToken) =>
        Ok(await getDriverPerformance.Handle(id, cancellationToken));
}

public sealed record CreateDriverRequest(
    string Name,
    VehicleType VehicleType,
    double Latitude,
    double Longitude);
public sealed record UpdateDriverLocationRequest(double Latitude, double Longitude);
