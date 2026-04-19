namespace FinalTest.Api.Contracts;

public record CreateSaleRequest(
    int CarId,
    int CustomerId,
    DateTime SaleDate,
    decimal SalePrice,
    string PaymentMethod);
