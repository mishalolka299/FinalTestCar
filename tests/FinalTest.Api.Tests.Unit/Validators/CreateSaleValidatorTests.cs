using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using FinalTest.Api.Validation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalTest.Api.Tests.Unit.Validators;

public class CreateSaleValidatorTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CreateSaleValidator _sut;

    public CreateSaleValidatorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new CreateSaleValidator(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Car car, Customer customer)> SeedAsync(CarStatus status = CarStatus.Available)
    {
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
        _db.Cars.Add(car);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return (car, customer);
    }

    [Fact]
    public async Task SalePrice_WithinFivePercent_NoError()
    {
        // Arrange — 30000 * 1.05 = 31500
        var (car, customer) = await SeedAsync();
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 31500m, "Cash");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SalePrice);
    }

    [Fact]
    public async Task SalePrice_ExceedsFivePercent_HasError()
    {
        // Arrange — 30000 * 1.05 = 31500, so 31501 is over 5%
        var (car, customer) = await SeedAsync();
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 31501m, "Cash");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SalePrice);
    }

    [Fact]
    public async Task Car_WithAvailableStatus_NoError()
    {
        var (car, customer) = await SeedAsync(CarStatus.Available);
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 25000m, "Cash");
        var result = await _sut.TestValidateAsync(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CarId);
    }

    [Fact]
    public async Task Car_WithReservedStatus_NoError()
    {
        var (car, customer) = await SeedAsync(CarStatus.Reserved);
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 25000m, "Cash");
        var result = await _sut.TestValidateAsync(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CarId);
    }

    [Fact]
    public async Task Car_WithSoldStatus_HasError()
    {
        var (car, customer) = await SeedAsync(CarStatus.Sold);
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 25000m, "Cash");
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.CarId);
    }

    [Fact]
    public async Task PaymentMethod_Invalid_HasError()
    {
        var (car, customer) = await SeedAsync();
        var request = new CreateSaleRequest(car.Id, customer.Id, DateTime.UtcNow, 25000m, "Bitcoin");
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }
}
