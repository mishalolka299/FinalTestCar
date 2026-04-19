using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FinalTest.Api.Tests.Database;

[Collection("Database")]
public class RelationshipTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private AppDbContext? _db;

    public RelationshipTests(DatabaseFixture fixture) => _fixture = fixture;

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
    public async Task Sale_WithCarAndCustomer_NavigationPropertiesLoadCorrectly()
    {
        // Arrange
        var car = new Car
        {
            Make = "Audi", Model = "A4", Year = 2022, Color = "Gray",
            Mileage = 0, Price = 40000m, VIN = "RELTEST1234567890",
            Status = CarStatus.Available, FuelType = FuelType.Petrol
        };
        var customer = new Customer
        {
            FirstName = "Anna", LastName = "Smith",
            Email = "anna@test.com", Phone = "+380671234567",
            DriversLicense = "DL99999"
        };
        _db!.Cars.Add(car);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var sale = new Sale
        {
            CarId = car.Id,
            CustomerId = customer.Id,
            SaleDate = DateTime.UtcNow,
            SalePrice = 38000m,
            PaymentMethod = PaymentMethod.Finance
        };
        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        // Act
        var loaded = await _db.Sales
            .Include(s => s.Car)
            .Include(s => s.Customer)
            .FirstAsync(s => s.Id == sale.Id);

        // Assert
        loaded.Car.Make.ShouldBe("Audi");
        loaded.Car.VIN.ShouldBe("RELTEST1234567890");
        loaded.Customer.FirstName.ShouldBe("Anna");
        loaded.Customer.Email.ShouldBe("anna@test.com");
    }

    [Fact]
    public async Task Customer_WithMultipleSales_AllRelationsIntact()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "Bob", LastName = "Brown",
            Email = "bob@test.com", Phone = "+380631234567",
            DriversLicense = "DL88888"
        };
        _db!.Customers.Add(customer);
        await _db.SaveChangesAsync();

        for (var i = 1; i <= 3; i++)
        {
            var car = new Car
            {
                Make = "Ford", Model = "Focus", Year = 2020 + i, Color = "Red",
                Mileage = 0, Price = 15000m, VIN = $"MULTI{i:000000000000}",
                Status = CarStatus.Available, FuelType = FuelType.Petrol
            };
            _db.Cars.Add(car);
            await _db.SaveChangesAsync();

            _db.Sales.Add(new Sale
            {
                CarId = car.Id, CustomerId = customer.Id,
                SaleDate = DateTime.UtcNow, SalePrice = 14000m,
                PaymentMethod = PaymentMethod.Cash
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var salesCount = await _db.Sales.CountAsync(s => s.CustomerId == customer.Id);

        // Assert
        salesCount.ShouldBe(3);
    }
}
