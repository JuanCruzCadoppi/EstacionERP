using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Tests;

/// <summary>
/// Prueba contra PostgreSQL real. Solo corre si existe la variable ESTACION_PG_TEST con una cadena
/// de conexión (usa una base descartable: la borra y la vuelve a crear).
/// </summary>
public class PostgresIntegracionTests
{
    [Fact]
    public async Task Emite_y_lista_comprobantes_en_postgres()
    {
        var cadena = Environment.GetEnvironmentVariable("ESTACION_PG_TEST");
        if (string.IsNullOrWhiteSpace(cadena)) return;

        var opciones = new DbContextOptionsBuilder<EstacionDbContext>().UseNpgsql(cadena).Options;
        await using var db = new EstacionDbContext(opciones);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            db.ConfiguracionesFiscales.Add(new ConfiguracionFiscal
            {
                Id = 1, Cuit = "20123456786", RazonSocial = "PG", CondicionIva = CondicionIva.ResponsableInscripto,
                CertificadoPem = "x", ClavePrivadaPem = "x", InicioActividades = new DateTime(2020, 1, 1)
            });
            var pv = new PuntoVenta { Numero = 1, Descripcion = "Playa", UnidadNegocioId = UnidadNegocio.PlayaId };
            db.PuntosVenta.Add(pv);
            await db.SaveChangesAsync();

            var arca = new ArcaFalso();
            var s = new FacturacionService(db, Sesiones.Admin(), arca, () => DateTime.Now);
            var r = await s.EmitirAsync(new SolicitudEmision
            {
                UnidadNegocioId = UnidadNegocio.PlayaId, PuntoVentaId = pv.Id, ClienteId = Cliente.ConsumidorFinalId,
                Items = { new ItemEmision { Descripcion = "Nafta Super", Cantidad = 30.5m, PrecioUnitario = 1450.9m } }
            });
            Assert.True(r.Exito, string.Join(", ", r.Errores));
            Assert.Equal(EstadoComprobante.Autorizado, r.Valor!.Estado);

            arca.SinConexion = true;
            await s.EmitirAsync(new SolicitudEmision
            {
                UnidadNegocioId = UnidadNegocio.PlayaId, PuntoVentaId = pv.Id, ClienteId = Cliente.ConsumidorFinalId,
                Items = { new ItemEmision { Descripcion = "Gasoil", PrecioUnitario = 1000 } }
            });
            arca.SinConexion = false;
            Assert.Equal(1, (await s.ReintentarPendientesAsync()).Autorizados);

            var lista = await s.ListarAsync(DateTime.Today.AddDays(-1), DateTime.Today);
            Assert.Equal(2, lista.Count);
            Assert.All(lista, c => Assert.Equal(EstadoComprobante.Autorizado, c.Estado));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
