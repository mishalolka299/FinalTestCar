namespace FinalTest.Api.Contracts;

public record SaleResponse(
    int Id,
    int CarId,
    string CarMake,
    string CarModel,
    string CarVIN,
    int CustomerId,
    string CustomerFirstName,
    string CustomerLastName,
    DateTime SaleDate,
    decimal SalePrice,
    string PaymentMethod);

public record CustomerPurchaseResponse(
    int SaleId,
    int CarId,
    string CarMake,
    string CarModel,
    string CarVIN,
    DateTime SaleDate,
    decimal SalePrice,
    string PaymentMethod);
