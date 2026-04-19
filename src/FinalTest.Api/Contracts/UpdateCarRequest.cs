namespace FinalTest.Api.Contracts;

public record UpdateCarRequest(
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    decimal Price,
    string FuelType);
