namespace FinalTest.Api.Contracts;

public record CreateCarRequest(
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    decimal Price,
    string VIN,
    string FuelType);
