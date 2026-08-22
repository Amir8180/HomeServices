using HomeServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeServices.Infrastructure;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` can instantiate AppDbContext
/// without running the host. The connection string is only a placeholder — model
/// building never contacts the database.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=.;Database=HomeServicesDb;User Id=sa;Password=1;TrustServerCertificate=True;Encrypt=False;")
            .Options;
        return new AppDbContext(options);
    }
}
