using AutoFixture;
using FinalTest.Api.Data;
using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api;

public static class DbSeeder
{
    private static readonly string[] Makes = ["Toyota", "Honda", "BMW", "Ford", "Audi", "Mercedes", "Hyundai", "Kia", "Volkswagen", "Nissan"];
    private static readonly string[] Models = ["Sedan", "SUV", "Coupe", "Hatchback", "Pickup", "Van", "Wagon", "Convertible"];
    private static readonly string[] Colors = ["Black", "White", "Silver", "Red", "Blue", "Green", "Gray", "Yellow"];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Cars.AnyAsync(ct)) return;

        var fixture = new Fixture();

        var customers = Enumerable.Range(1, 500).Select(i => new Customer
        {
            FirstName = fixture.Create<string>()[..8],
            LastName = fixture.Create<string>()[..8],
            Email = $"customer{i}@autosalon.com",
            Phone = $"+380{Random.Shared.Next(100000000, 999999999)}",
            DriversLicense = $"DL{i:00000}"
        }).ToList();

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(ct);

        var fuelTypes = Enum.GetValues<FuelType>();
        var statuses = new[] { CarStatus.Available, CarStatus.Available, CarStatus.Available, CarStatus.Reserved, CarStatus.Sold };

        var cars = Enumerable.Range(1, 9000).Select(i =>
        {
            var vinSuffix = fixture.Create<string>().ToUpper()[..11];
            return new Car
            {
                Make = Makes[Random.Shared.Next(Makes.Length)],
                Model = Models[Random.Shared.Next(Models.Length)],
                Year = Random.Shared.Next(2000, DateTime.UtcNow.Year + 1),
                Color = Colors[Random.Shared.Next(Colors.Length)],
                Mileage = Random.Shared.Next(0, 200000),
                Price = Math.Round((decimal)(Random.Shared.NextDouble() * 90000) + 5000, 2),
                VIN = $"FT{i:00000}{vinSuffix}"[..17],
                Status = statuses[Random.Shared.Next(statuses.Length)],
                FuelType = fuelTypes[Random.Shared.Next(fuelTypes.Length)]
            };
        }).ToList();

        db.Cars.AddRange(cars);
        await db.SaveChangesAsync(ct);

        // seed ~500 sales from Sold/Reserved cars and existing customers
        var soldCars = cars.Where(c => c.Status == CarStatus.Sold).Take(500).ToList();
        var sales = soldCars.Select((car, i) => new Sale
        {
            CarId = car.Id,
            CustomerId = customers[i % customers.Count].Id,
            SaleDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 730)),
            SalePrice = Math.Round(car.Price * (decimal)(0.95 + Random.Shared.NextDouble() * 0.10), 2),
            PaymentMethod = Enum.GetValues<PaymentMethod>()[Random.Shared.Next(3)]
        }).ToList();

        db.Sales.AddRange(sales);
        await db.SaveChangesAsync(ct);
    }
}
