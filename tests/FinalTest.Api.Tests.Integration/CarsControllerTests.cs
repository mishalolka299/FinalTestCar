using System.Net;
using System.Net.Http.Json;
using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Integration;

public class CarsControllerTests : IClassFixture<AutosalonApiFactory>, IAsyncLifetime
{
    private readonly AutosalonApiFactory _factory;
    private readonly HttpClient _client;

    public CarsControllerTests(AutosalonApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Sales.RemoveRange(db.Sales);
        db.Cars.RemoveRange(db.Cars);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CreateCarRequest ValidCar(string vin = "1HGCM82633A004352") =>
        new("Toyota", "Camry", 2022, "Black", 0, 25000m, vin, "Petrol");

    // --- Inventory management ---

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/cars");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cars = await response.Content.ReadFromJsonAsync<List<CarResponse>>();
        cars.ShouldNotBeNull();
        cars.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_ValidCar_Returns201()
    {
        // Arrange
        var request = ValidCar();

        // Act
        var response = await _client.PostAsJsonAsync("/api/cars", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var car = await response.Content.ReadFromJsonAsync<CarResponse>();
        car.ShouldNotBeNull();
        car.Make.ShouldBe("Toyota");
        car.VIN.ShouldBe("1HGCM82633A004352");
        car.Status.ShouldBe("Available");
    }

    [Fact]
    public async Task Create_DuplicateVIN_Returns400()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/cars", ValidCar("AAAAAAAAAAAAAAAAA"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/cars", ValidCar("AAAAAAAAAAAAAAAAA"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ExistingCar_ReturnsCar()
    {
        // Arrange
        var created = await (await _client.PostAsJsonAsync("/api/cars", ValidCar()))
            .Content.ReadFromJsonAsync<CarResponse>();

        // Act
        var response = await _client.GetAsync($"/api/cars/{created!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var car = await response.Content.ReadFromJsonAsync<CarResponse>();
        car!.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/cars/99999");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ExistingCar_Returns200()
    {
        // Arrange
        var created = await (await _client.PostAsJsonAsync("/api/cars", ValidCar()))
            .Content.ReadFromJsonAsync<CarResponse>();
        var update = new UpdateCarRequest("Honda", "Civic", 2023, "White", 100, 22000m, "Diesel");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/cars/{created!.Id}", update);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var car = await response.Content.ReadFromJsonAsync<CarResponse>();
        car!.Make.ShouldBe("Honda");
        car.Color.ShouldBe("White");
    }

    // --- Reservation process ---

    [Fact]
    public async Task Reserve_AvailableCar_Returns200WithReservedStatus()
    {
        // Arrange
        var created = await (await _client.PostAsJsonAsync("/api/cars", ValidCar()))
            .Content.ReadFromJsonAsync<CarResponse>();

        // Act
        var response = await _client.PatchAsync($"/api/cars/{created!.Id}/reserve", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var car = await response.Content.ReadFromJsonAsync<CarResponse>();
        car!.Status.ShouldBe("Reserved");
    }

    [Fact]
    public async Task Reserve_AlreadyReservedCar_Returns409()
    {
        // Arrange
        var created = await (await _client.PostAsJsonAsync("/api/cars", ValidCar()))
            .Content.ReadFromJsonAsync<CarResponse>();
        await _client.PatchAsync($"/api/cars/{created!.Id}/reserve", null);

        // Act
        var response = await _client.PatchAsync($"/api/cars/{created.Id}/reserve", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAll_WithMakeFilter_ReturnsFilteredCars()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/cars", ValidCar("AAAAAAAAAAAAAAABB") with { Make = "Ford" });
        await _client.PostAsJsonAsync("/api/cars", ValidCar("AAAAAAAAAAAAAAAC1") with { Make = "BMW" });

        // Act
        var response = await _client.GetAsync("/api/cars?make=Ford");
        var cars = await response.Content.ReadFromJsonAsync<List<CarResponse>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        cars.ShouldNotBeNull();
        cars.All(c => c.Make == "Ford").ShouldBeTrue();
    }
}
