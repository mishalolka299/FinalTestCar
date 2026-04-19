using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerPurchaseResponse>> GetPurchasesAsync(int customerId);
}

public class CustomerService(AppDbContext db) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerPurchaseResponse>> GetPurchasesAsync(int customerId)
    {
        var exists = await db.Customers.AnyAsync(c => c.Id == customerId);
        if (!exists)
            throw new KeyNotFoundException($"Customer {customerId} not found.");

        var sales = await db.Sales
            .Include(s => s.Car)
            .Where(s => s.CustomerId == customerId)
            .ToListAsync();

        return sales.Select(s => new CustomerPurchaseResponse(
            s.Id, s.CarId, s.Car.Make, s.Car.Model, s.Car.VIN,
            s.SaleDate, s.SalePrice, s.PaymentMethod.ToString())).ToList();
    }
}
