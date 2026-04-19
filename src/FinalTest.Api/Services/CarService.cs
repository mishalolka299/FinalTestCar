using FinalTest.Api.Contracts;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Services;

public interface ICarService
{
    Task<IReadOnlyList<CarResponse>> GetAllAsync(string? make, int? yearFrom, int? yearTo, decimal? priceFrom, decimal? priceTo, string? fuelType, string? status);
    Task<CarResponse> GetByIdAsync(int id);
    Task<CarResponse> CreateAsync(CreateCarRequest request);
    Task<CarResponse> UpdateAsync(int id, UpdateCarRequest request);
    Task<CarResponse> ReserveAsync(int id);
}

public class CarService(AppDbContext db) : ICarService
{
    public async Task<IReadOnlyList<CarResponse>> GetAllAsync(
        string? make, int? yearFrom, int? yearTo,
        decimal? priceFrom, decimal? priceTo,
        string? fuelType, string? status)
    {
        var query = db.Cars.AsQueryable();

        if (!string.IsNullOrEmpty(make))
            query = query.Where(c => c.Make.ToLower().Contains(make.ToLower()));

        if (yearFrom.HasValue) query = query.Where(c => c.Year >= yearFrom.Value);
        if (yearTo.HasValue) query = query.Where(c => c.Year <= yearTo.Value);
        if (priceFrom.HasValue) query = query.Where(c => c.Price >= priceFrom.Value);
        if (priceTo.HasValue) query = query.Where(c => c.Price <= priceTo.Value);

        if (!string.IsNullOrEmpty(fuelType) && Enum.TryParse<FuelType>(fuelType, out var ft))
            query = query.Where(c => c.FuelType == ft);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CarStatus>(status, out var st))
            query = query.Where(c => c.Status == st);

        var cars = await query.ToListAsync();
        return cars.Select(ToResponse).ToList();
    }

    public async Task<CarResponse> GetByIdAsync(int id)
    {
        var car = await db.Cars.FindAsync(id)
            ?? throw new KeyNotFoundException($"Car {id} not found.");
        return ToResponse(car);
    }

    public async Task<CarResponse> CreateAsync(CreateCarRequest request)
    {
        var car = new Car
        {
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Color = request.Color,
            Mileage = request.Mileage,
            Price = request.Price,
            VIN = request.VIN,
            Status = CarStatus.Available,
            FuelType = Enum.Parse<FuelType>(request.FuelType)
        };

        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return ToResponse(car);
    }

    public async Task<CarResponse> UpdateAsync(int id, UpdateCarRequest request)
    {
        var car = await db.Cars.FindAsync(id)
            ?? throw new KeyNotFoundException($"Car {id} not found.");

        car.Make = request.Make;
        car.Model = request.Model;
        car.Year = request.Year;
        car.Color = request.Color;
        car.Mileage = request.Mileage;
        car.Price = request.Price;
        car.FuelType = Enum.Parse<FuelType>(request.FuelType);

        await db.SaveChangesAsync();
        return ToResponse(car);
    }

    public async Task<CarResponse> ReserveAsync(int id)
    {
        var car = await db.Cars.FindAsync(id)
            ?? throw new KeyNotFoundException($"Car {id} not found.");

        if (car.Status != CarStatus.Available)
            throw new InvalidOperationException("Only Available cars can be reserved.");

        car.Status = CarStatus.Reserved;
        await db.SaveChangesAsync();
        return ToResponse(car);
    }

    private static CarResponse ToResponse(Car c) =>
        new(c.Id, c.Make, c.Model, c.Year, c.Color, c.Mileage, c.Price, c.VIN, c.Status.ToString(), c.FuelType.ToString());
}
