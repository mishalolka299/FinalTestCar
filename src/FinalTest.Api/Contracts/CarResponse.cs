namespace FinalTest.Api.Contracts;

public record CarResponse(
    int Id,
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    decimal Price,
    string VIN,
    string Status,
    string FuelType);
