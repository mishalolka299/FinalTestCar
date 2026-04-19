using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Services;

public interface ISaleService
{
    Task<SaleResponse> CreateAsync(CreateSaleRequest request);
    Task<IReadOnlyList<SaleResponse>> GetAllAsync(DateTime? from, DateTime? to);
}

public class SaleService(AppDbContext db) : ISaleService
{
    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request)
    {
        var car = await db.Cars.FindAsync(request.CarId)
            ?? throw new KeyNotFoundException($"Car {request.CarId} not found.");

        car.Status = CarStatus.Sold;

        var sale = new Sale
        {
            CarId = request.CarId,
            CustomerId = request.CustomerId,
            SaleDate = request.SaleDate,
            SalePrice = request.SalePrice,
            PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod)
        };

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        await db.Entry(sale).Reference(s => s.Car).LoadAsync();
        await db.Entry(sale).Reference(s => s.Customer).LoadAsync();

        return ToResponse(sale);
    }

    public async Task<IReadOnlyList<SaleResponse>> GetAllAsync(DateTime? from, DateTime? to)
    {
        var query = db.Sales.Include(s => s.Car).Include(s => s.Customer).AsQueryable();

        if (from.HasValue) query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SaleDate <= to.Value);

        var sales = await query.ToListAsync();
        return sales.Select(ToResponse).ToList();
    }

    private static SaleResponse ToResponse(Sale s) =>
        new(s.Id, s.CarId, s.Car.Make, s.Car.Model, s.Car.VIN,
            s.CustomerId, s.Customer.FirstName, s.Customer.LastName,
            s.SaleDate, s.SalePrice, s.PaymentMethod.ToString());
}
