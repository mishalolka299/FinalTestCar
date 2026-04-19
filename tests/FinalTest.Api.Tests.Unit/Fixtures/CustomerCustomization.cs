using AutoFixture;
using FinalTest.Api.Domain;

namespace FinalTest.Api.Tests.Unit.Fixtures;

public class CustomerCustomization : ICustomization
{
    private int _counter;

    public void Customize(IFixture fixture)
    {
        fixture.Customize<Customer>(c => c
            .Without(x => x.Id)
            .Without(x => x.Sales)
            .With(x => x.FirstName, () => $"First_{Interlocked.Increment(ref _counter)}")
            .With(x => x.LastName, () => $"Last_{Interlocked.Increment(ref _counter)}")
            .With(x => x.Email, () => $"user{Interlocked.Increment(ref _counter)}@test.com")
            .With(x => x.Phone, () => $"+38012345{Interlocked.Increment(ref _counter):0000}")
            .With(x => x.DriversLicense, () => $"DL{Interlocked.Increment(ref _counter):00000}"));
    }
}
