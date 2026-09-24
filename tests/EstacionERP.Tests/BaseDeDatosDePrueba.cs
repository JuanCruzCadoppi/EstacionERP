using EstacionERP.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Tests;

/// <summary>
/// Base SQLite en memoria: cada test arranca con una base limpia (con los datos semilla).
/// </summary>
public sealed class BaseDeDatosDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion;

    public BaseDeDatosDePrueba()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        using var db = CrearContexto();
        db.Database.EnsureCreated();
    }

    public EstacionDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EstacionDbContext>().UseSqlite(_conexion).Options);

    public void Dispose() => _conexion.Dispose();
}
