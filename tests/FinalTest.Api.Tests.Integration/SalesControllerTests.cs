using System.Net;
using System.Net.Http.Json;
using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Integration;

public class SalesControllerTests : IClassFixture<AutosalonApiFactory>, IAsyncLifetime
{
    private readonly AutosalonApiFactory _factory;
    private readonly HttpClient _client;

    public SalesControllerTests(AutosalonApiFactory factory)
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
        db.Customers.RemoveRange(db.Customers);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(int carId, int customerId)> SeedAsync(CarStatus status = CarStatus.Available)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var car = new Car
        {
            Make = "Toyota", Model = "Camry", Year = 2022, Color = "Black",
            Mileage = 0, Price = 30000m, VIN = "1HGCM82633A004352",
            Status = status, FuelType = FuelType.Petrol
        };
        var customer = new Customer
        {
            FirstName = "John", LastName = "Doe",
            Email = "john@test.com", Phone = "+380991234567",
            DriversLicense = "DL12345"
        };
        db.Cars.Add(car);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return (car.Id, customer.Id);
    }

    // --- Sale recording ---

    [Fact]
    public async Task CreateSale_AvailableCar_Returns201()
    {
        // Arrange
        var (carId, customerId) = await SeedAsync();
        var request = new CreateSaleRequest(carId, customerId, DateTime.UtcNow, 28000m, "Cash");

        // Act
        var response = await _client.PostAsJsonAsync("/api/sales", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        sale.ShouldNotBeNull();
        sale.SalePrice.ShouldBe(28000m);
        sale.PaymentMethod.ShouldBe("Cash");
    }

    [Fact]
    public async Task CreateSale_SoldCar_Returns400()
    {
        // Arrange
        var (carId, customerId) = await SeedAsync(CarStatus.Sold);
        var request = new CreateSaleRequest(carId, customerId, DateTime.UtcNow, 25000m, "Cash");

        // Act
        var response = await _client.PostAsJsonAsync("/api/sales", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSale_PriceExceeds5Percent_Returns400()
    {
        // Arrange — car price 30000, max allowed is 31500
        var (carId, customerId) = await SeedAsync();
        var request = new CreateSaleRequest(carId, customerId, DateTime.UtcNow, 32000m, "Cash");

        // Act
        var response = await _client.PostAsJsonAsync("/api/sales", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSales_NoFilter_ReturnsAllSales()
    {
        // Arrange
        var (carId, customerId) = await SeedAsync();
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(carId, customerId, DateTime.UtcNow, 25000m, "Finance"));

        // Act
        var response = await _client.GetAsync("/api/sales");
        var sales = await response.Content.ReadFromJsonAsync<List<SaleResponse>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sales.ShouldNotBeNull();
        sales.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetSales_WithDateFilter_ReturnsMatchingSales()
    {
        // Arrange
        var (carId, customerId) = await SeedAsync();
        var saleDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(carId, customerId, saleDate, 25000m, "Lease"));

        // Act
        var response = await _client.GetAsync("/api/sales?from=2024-01-01&to=2024-12-31");
        var sales = await response.Content.ReadFromJsonAsync<List<SaleResponse>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sales.ShouldNotBeNull();
        sales.Count.ShouldBe(1);
    }
}
