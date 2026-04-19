using FinalTest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinalTest.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Make).HasMaxLength(100).IsRequired();
            e.Property(c => c.Model).HasMaxLength(100).IsRequired();
            e.Property(c => c.Color).HasMaxLength(50).IsRequired();
            e.Property(c => c.VIN).HasMaxLength(17).IsRequired();
            e.HasIndex(c => c.VIN).IsUnique();
            e.Property(c => c.Price).HasPrecision(18, 2);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.FuelType).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.Property(c => c.Phone).HasMaxLength(50).IsRequired();
            e.Property(c => c.DriversLicense).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.SalePrice).HasPrecision(18, 2);
            e.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.HasOne(s => s.Car).WithMany(c => c.Sales).HasForeignKey(s => s.CarId);
            e.HasOne(s => s.Customer).WithMany(c => c.Sales).HasForeignKey(s => s.CustomerId);
        });
    }
}
