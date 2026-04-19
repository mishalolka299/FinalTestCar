using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Validation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalTest.Api.Tests.Unit.Validators;

public class CreateCarValidatorTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CreateCarValidator _sut;

    public CreateCarValidatorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new CreateCarValidator(_db);
    }

    public void Dispose() => _db.Dispose();

    private static CreateCarRequest ValidRequest() =>
        new("Toyota", "Camry", 2022, "Black", 0, 25000m, "1HGCM82633A004352", "Petrol");

    [Fact]
    public async Task VIN_ExactlySeventeen_NoError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveValidationErrorFor(x => x.VIN);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData("TOOLONGVIN1234567890")]
    public async Task VIN_WrongLength_HasError(string vin)
    {
        var request = ValidRequest() with { VIN = vin };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.VIN);
    }

    [Fact]
    public async Task VIN_Duplicate_HasError()
    {
        var existingCar = new FinalTest.Api.Domain.Car
        {
            Make = "Honda", Model = "Civic", Year = 2020, Color = "White",
            Mileage = 0, Price = 20000m, VIN = "1HGCM82633A004352",
            FuelType = FinalTest.Api.Domain.FuelType.Petrol
        };
        _db.Cars.Add(existingCar);
        await _db.SaveChangesAsync();

        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldHaveValidationErrorFor(x => x.VIN);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2100)]
    public async Task Year_OutOfRange_HasError(int year)
    {
        var request = ValidRequest() with { Year = year };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(2023)]
    public async Task Year_InRange_NoError(int year)
    {
        var request = ValidRequest() with { Year = year };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Mileage_Negative_HasError(int mileage)
    {
        var request = ValidRequest() with { Mileage = mileage };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Mileage);
    }

    [Fact]
    public async Task Mileage_Zero_NoError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveValidationErrorFor(x => x.Mileage);
    }

    [Fact]
    public async Task Price_Zero_HasError()
    {
        var request = ValidRequest() with { Price = 0m };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task ValidRequest_PassesAllRules()
    {
        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
