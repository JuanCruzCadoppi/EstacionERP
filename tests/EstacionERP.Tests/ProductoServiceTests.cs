using EstacionERP.Application.Productos;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Tests;

public class ProductoServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    private static ProductoDatos Filtro(string codigo = "FA-100", decimal precio = 12100m) => new()
    {
        Codigo = codigo, Descripcion = "filtro de aceite", Marca = "fram", Rubro = "filtros",
        UnidadNegocioId = UnidadNegocio.RepuestosId, Tipo = TipoProducto.Bien,
        AlicuotaIva = AlicuotaIva.Veintiuno, Costo = 5000m, PrecioVenta = precio
    };

    [Fact]
    public async Task Crea_producto_y_calcula_margen()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Admin());
        var r = await s.GuardarAsync(Filtro());
        Assert.True(r.Exito, string.Join(", ", r.Errores));

        var fila = Assert.Single(await s.BuscarAsync("fram", null));
        Assert.Equal("FILTRO DE ACEITE", fila.Descripcion);
        Assert.Equal(100m, fila.MargenPorcentaje); // 12100 / 1,21 = 10000 → 100% sobre 5000
    }

    [Fact]
    public async Task Codigo_unico_por_unidad_pero_se_puede_repetir_en_otra()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Admin());
        Assert.True((await s.GuardarAsync(Filtro())).Exito);
        Assert.False((await s.GuardarAsync(Filtro())).Exito);

        var lavado = Filtro();
        lavado.UnidadNegocioId = UnidadNegocio.LavaderoId;
        lavado.Tipo = TipoProducto.Servicio;
        Assert.True((await s.GuardarAsync(lavado)).Exito);
    }

    [Fact]
    public async Task Servicio_no_controla_stock_y_combustible_va_en_litros_en_playa()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Admin());

        var lavado = new ProductoDatos { Codigo = "LAV1", Descripcion = "Lavado completo", UnidadNegocioId = UnidadNegocio.LavaderoId,
                                         Tipo = TipoProducto.Servicio, ControlaStock = true, PrecioVenta = 15000 };
        var id = (await s.GuardarAsync(lavado)).Valor;
        Assert.False((await s.ObtenerAsync(id))!.ControlaStock);

        var nafta = new ProductoDatos { Codigo = "NS", Descripcion = "Nafta súper", UnidadNegocioId = UnidadNegocio.RepuestosId,
                                        Tipo = TipoProducto.Combustible, PrecioVenta = 1500 };
        Assert.False((await s.GuardarAsync(nafta)).Exito); // combustible fuera de Playa

        nafta.UnidadNegocioId = UnidadNegocio.PlayaId;
        var idNafta = (await s.GuardarAsync(nafta)).Valor;
        Assert.Equal(UnidadMedida.Litro, (await s.ObtenerAsync(idNafta))!.UnidadMedida);
    }

    [Fact]
    public async Task Operador_no_puede_editar_y_solo_ve_su_unidad()
    {
        using var db = _bd.CrearContexto();
        await new ProductoService(db, Sesiones.Admin()).GuardarAsync(Filtro());

        var playero = new ProductoService(db, Sesiones.Operador(UnidadNegocio.PlayaId));
        Assert.Empty(await playero.BuscarAsync(null, null));
        Assert.False((await playero.GuardarAsync(Filtro("OTRO"))).Exito);

        var vendedor = new ProductoService(db, Sesiones.Operador(UnidadNegocio.RepuestosId));
        Assert.Single(await vendedor.BuscarAsync(null, null));
    }

    [Fact]
    public async Task Encargado_no_puede_cargar_productos_en_unidad_ajena()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Encargado(UnidadNegocio.LavaderoId));
        Assert.False((await s.GuardarAsync(Filtro())).Exito);
    }

    [Fact]
    public async Task Aumento_masivo_por_rubro()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Admin());
        await s.GuardarAsync(Filtro("F1", 10000m));
        var otro = Filtro("B1", 20000m); otro.Rubro = "baterias";
        await s.GuardarAsync(otro);

        var filtro = new FiltroAumento { UnidadNegocioId = UnidadNegocio.RepuestosId, Rubro = "filtros" };
        Assert.Equal(1, await s.ContarParaAumentoAsync(filtro));

        var r = await s.AumentarPreciosAsync(filtro, 10m, tambienCosto: true);
        Assert.True(r.Exito);
        Assert.Equal(1, r.Valor);

        var lista = await s.BuscarAsync(null, null);
        Assert.Equal(11000m, lista.Single(p => p.Codigo == "F1").PrecioVenta);
        Assert.Equal(5500m, lista.Single(p => p.Codigo == "F1").Costo);
        Assert.Equal(20000m, lista.Single(p => p.Codigo == "B1").PrecioVenta);
    }

    [Fact]
    public async Task Operador_no_puede_hacer_aumento_masivo()
    {
        using var db = _bd.CrearContexto();
        var s = new ProductoService(db, Sesiones.Operador(UnidadNegocio.RepuestosId));
        Assert.False((await s.AumentarPreciosAsync(new FiltroAumento(), 10m, false)).Exito);
    }
}
