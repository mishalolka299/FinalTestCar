using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Database;

[Collection("Database")]
public class VinUniquenessTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private AppDbContext? _db;

    public VinUniquenessTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(_fixture.CreateDbContextOptions());
        await _db.Database.EnsureCreatedAsync();
        _db.Sales.RemoveRange(_db.Sales);
        _db.Cars.RemoveRange(_db.Cars);
        _db.Customers.RemoveRange(_db.Customers);
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_db != null) await _db.DisposeAsync().AsTask();
    }

    private static Car CreateCar(string vin) => new()
    {
        Make = "Toyota", Model = "Camry", Year = 2022, Color = "Black",
        Mileage = 0, Price = 25000m, VIN = vin,
        Status = CarStatus.Available, FuelType = FuelType.Petrol
    };

    [Fact]
    public async Task InsertCar_UniqueVIN_Succeeds()
    {
        // Arrange
        var car = CreateCar("1HGCM82633A004352");

        // Act
        _db!.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Assert
        var count = await _db.Cars.CountAsync();
        count.ShouldBe(1);
    }

    [Fact]
    public async Task InsertCar_DuplicateVIN_ThrowsException()
    {
        // Arrange
        _db!.Cars.Add(CreateCar("DUPVINTEST1234567"));
        await _db.SaveChangesAsync();

        // Act & Assert
        _db.Cars.Add(CreateCar("DUPVINTEST1234567"));
        await Should.ThrowAsync<Exception>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task InsertMultipleCars_AllUniqueVINs_AllPersisted()
    {
        // Arrange
        var cars = Enumerable.Range(1, 5).Select(i => CreateCar($"UNIQUEVIN{i:00000000}")).ToList();

        // Act
        _db!.Cars.AddRange(cars);
        await _db.SaveChangesAsync();

        // Assert
        var count = await _db.Cars.CountAsync();
        count.ShouldBe(5);
    }
}
