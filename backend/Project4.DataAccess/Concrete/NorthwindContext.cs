using Microsoft.EntityFrameworkCore;
using Project4.Entities;

namespace Project4.DataAccess
{
    public class NorthwindContext:DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
               
                    var connectionString = Environment.GetEnvironmentVariable("AVMAPP_NORTHWIND_CONNECTION");
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException("Set AVMAPP_NORTHWIND_CONNECTION before using NorthwindContext directly.");
                    }

                    optionsBuilder.UseSqlServer(connectionString);
                
                
            }

            public DbSet<Product> Products { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<Personel> Personels { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<Customer> Customers { get; set; }
            public DbSet<OrderStatus> OrderStatuses { get; set; }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                //modelBuilder.HasDefaultSchema("admin");
                modelBuilder.Entity<Personel>().ToTable("Employees");
                modelBuilder.Entity<Personel>().Property(p => p.Id).HasColumnName("EmployeeID");
                modelBuilder.Entity<Personel>().Property(p => p.Name).HasColumnName("FirstName");
                modelBuilder.Entity<Personel>().Property(p => p.Surname).HasColumnName("LastName");
            }
    }
}
