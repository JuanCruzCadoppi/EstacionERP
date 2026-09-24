using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Common;

/// <summary>
/// Abstracción de la base de datos. La capa Application no sabe si abajo hay PostgreSQL,
/// SQL Server o SQLite (en los tests).
/// </summary>
public interface IEstacionDbContext
{
    DbSet<UnidadNegocio> UnidadesNegocio { get; }
    DbSet<PuntoVenta> PuntosVenta { get; }
    DbSet<Cliente> Clientes { get; }
    DbSet<Vehiculo> Vehiculos { get; }
    DbSet<Producto> Productos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
