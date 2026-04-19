using FluentValidation;
using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Validation;

public class CreateSaleValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleValidator(AppDbContext db)
    {
        RuleFor(x => x.CarId)
            .MustAsync(async (carId, ct) =>
            {
                var car = await db.Cars.FindAsync([carId], ct);
                return car is not null && car.Status is CarStatus.Available or CarStatus.Reserved;
            })
            .WithMessage("Car must exist and be Available or Reserved to be sold.");

        RuleFor(x => x.CustomerId)
            .MustAsync(async (customerId, ct) => await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            .WithMessage("Customer not found.");

        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("SalePrice must be greater than zero.")
            .MustAsync(async (request, salePrice, ct) =>
            {
                var car = await db.Cars.FindAsync([request.CarId], ct);
                return car is null || salePrice <= car.Price * 1.05m;
            })
            .WithMessage("SalePrice cannot exceed the car's listed price by more than 5%.");

        RuleFor(x => x.PaymentMethod)
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _))
            .WithMessage("PaymentMethod must be Cash, Finance, or Lease.");
    }
}
