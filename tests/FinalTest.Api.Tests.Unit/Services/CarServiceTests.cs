using AutoFixture;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using FinalTest.Api.Services;
using FinalTest.Api.Tests.Unit.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Unit.Services;

public class CarServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CarService _sut;
    private readonly Fixture _fixture;

    public CarServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new CarService(_db);

        _fixture = new Fixture();
        _fixture.Customize(new CarCustomization());
    }

    public void Dispose() => _db.Dispose();

    // --- Status transitions ---

    [Fact]
    public async Task Reserve_AvailableCar_StatusBecomesReserved()
    {
        // Arrange
        var car = _fixture.Create<Car>();
        car.Status = CarStatus.Available;
        _db.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.ReserveAsync(car.Id);

        // Assert
        result.Status.ShouldBe("Reserved");
    }

    [Fact]
    public async Task Reserve_ReservedCar_ThrowsInvalidOperationException()
    {
        // Arrange
        var car = _fixture.Create<Car>();
        car.Status = CarStatus.Reserved;
        _db.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.ReserveAsync(car.Id));
    }

    [Fact]
    public async Task Reserve_SoldCar_ThrowsInvalidOperationException()
    {
        // Arrange
        var car = _fixture.Create<Car>();
        car.Status = CarStatus.Sold;
        _db.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.ReserveAsync(car.Id));
    }

    [Fact]
    public async Task Reserve_NonExistentCar_ThrowsKeyNotFoundException()
    {
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.ReserveAsync(9999));
    }

    [Fact]
    public async Task GetAll_WithMakeFilter_ReturnsMatchingCars()
    {
        // Arrange
        var toyota = new Car
        {
            Make = "Toyota",
            Model = "Camry",
            Color = "White",
            Year = 2024,
            Mileage = 1000,
            Price = 25000,
            VIN = "VIN111111111",
            Status = CarStatus.Available,
            FuelType = FuelType.Petrol
        };
        var honda = new Car
        {
            Make = "Honda",
            Model = "Accord",
            Color = "Black",
            Year = 2023,
            Mileage = 15000,
            Price = 22000,
            VIN = "VIN222222222",
            Status = CarStatus.Available,
            FuelType = FuelType.Petrol
        };
        _db.Cars.AddRange(toyota, honda);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("Toyota", null, null, null, null, null, null);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Make.ShouldBe("Toyota");
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingCars()
    {
        // Arrange
        var available = new Car
        {
            Make = "Toyota",
            Model = "Camry",
            Color = "White",
            Year = 2024,
            Mileage = 1000,
            Price = 25000,
            VIN = "VIN123456789",
            Status = CarStatus.Available,
            FuelType = FuelType.Petrol
        };
        var sold = new Car
        {
            Make = "Honda",
            Model = "Accord",
            Color = "Black",
            Year = 2023,
            Mileage = 15000,
            Price = 22000,
            VIN = "VIN987654321",
            Status = CarStatus.Sold,
            FuelType = FuelType.Petrol
        };
        _db.Cars.AddRange(available, sold);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(null, null, null, null, null, null, "Available");

        // Assert
        result.Count.ShouldBe(1);
        result[0].Status.ShouldBe("Available");
    }
}
