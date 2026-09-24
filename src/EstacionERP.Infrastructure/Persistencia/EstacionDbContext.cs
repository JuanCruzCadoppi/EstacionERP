using EstacionERP.Application.Common;
using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Infrastructure.Persistencia;

public class EstacionDbContext : DbContext, IEstacionDbContext
{
    public EstacionDbContext(DbContextOptions<EstacionDbContext> options) : base(options) { }

    public DbSet<UnidadNegocio> UnidadesNegocio => Set<UnidadNegocio>();
    public DbSet<PuntoVenta> PuntosVenta => Set<PuntoVenta>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases IEntityTypeConfiguration de la carpeta Configuraciones.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EstacionDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todos los importes con 2 decimales, salvo que se indique otra cosa.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
