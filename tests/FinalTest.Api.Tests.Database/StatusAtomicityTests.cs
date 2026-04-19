using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Database;

[Collection("Database")]
public class StatusAtomicityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private AppDbContext? _db;

    public StatusAtomicityTests(DatabaseFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task UpdateStatus_PersistedToDatabase()
    {
        // Arrange
        var car = new Car
        {
            Make = "Honda", Model = "Civic", Year = 2021, Color = "White",
            Mileage = 5000, Price = 20000m, VIN = "ATOMICTEST1234567",
            Status = CarStatus.Available, FuelType = FuelType.Diesel
        };
        _db!.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Act
        car.Status = CarStatus.Reserved;
        await _db.SaveChangesAsync();

        // Assert — fresh context to verify DB commit
        using var verifyDb = new AppDbContext(_fixture.CreateDbContextOptions());
        var saved = await verifyDb.Cars.FindAsync(car.Id);
        saved!.Status.ShouldBe(CarStatus.Reserved);
    }

    [Fact]
    public async Task UpdateStatus_RollbackOnFailure_StatusUnchanged()
    {
        // Arrange
        var car = new Car
        {
            Make = "BMW", Model = "3 Series", Year = 2023, Color = "Blue",
            Mileage = 0, Price = 45000m, VIN = "ROLLBACKTEST12345",
            Status = CarStatus.Available, FuelType = FuelType.Electric
        };
        _db!.Cars.Add(car);
        await _db.SaveChangesAsync();

        // Act — simulate rollback by using a transaction
        using var transaction = await _db.Database.BeginTransactionAsync();
        car.Status = CarStatus.Sold;
        await _db.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert
        await _db.Entry(car).ReloadAsync();
        car.Status.ShouldBe(CarStatus.Available);
    }
}
