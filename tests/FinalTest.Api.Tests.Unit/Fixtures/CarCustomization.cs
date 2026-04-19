using AutoFixture;
using FinalTest.Api.Domain;

namespace FinalTest.Api.Tests.Unit.Fixtures;

public class CarCustomization : ICustomization
{
    private int _counter;

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Car>(c => c
            .Without(x => x.Id)
            .Without(x => x.Sales)
            .With(x => x.Make, () => $"Make_{Interlocked.Increment(ref _counter)}")
            .With(x => x.Model, () => $"Model_{Interlocked.Increment(ref _counter)}")
            .With(x => x.Color, () => $"Color_{Interlocked.Increment(ref _counter)}")
            .With(x => x.Year, () => Random.Shared.Next(2000, 2026))
            .With(x => x.Mileage, () => Random.Shared.Next(0, 150000))
            .With(x => x.Price, () => Math.Round((decimal)(Random.Shared.NextDouble() * 50000) + 5000, 2))
            .With(x => x.VIN, () => $"VIN{Interlocked.Increment(ref _counter):00000000000000}")
            .With(x => x.Status, CarStatus.Available)
            .With(x => x.FuelType, FuelType.Petrol)
            .With(x => x.FuelType, () => Random.Shared.Next(0, 4) switch
            {
                0 => FuelType.Petrol,
                1 => FuelType.Diesel,
                2 => FuelType.Electric,
                _ => FuelType.Hybrid
            }));
    }
}
