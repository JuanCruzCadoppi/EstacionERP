using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EstacionERP.Infrastructure.Persistencia;

/// <summary>
/// Lo usa la herramienta "dotnet ef" para crear migraciones sin levantar la app.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EstacionDbContext>
{
    public EstacionDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ESTACION_DB")
                 ?? "Host=localhost;Port=5432;Database=estacion_erp;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<EstacionDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new EstacionDbContext(options);
    }
}
