using EstacionERP.Application.Clientes;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Tests;

public class ClienteServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();

    public void Dispose() => _bd.Dispose();

    private static ClienteDatos Transportista() => new()
    {
        TipoDocumento = TipoDocumento.Cuit,
        NumeroDocumento = "20-12345678-6",
        RazonSocial = "  transportes pérez  ",
        CondicionIva = CondicionIva.ResponsableInscripto,
        Localidad = "Laguna Larga",
        TieneCuentaCorriente = true,
        LimiteCredito = 500_000m
    };

    [Fact]
    public async Task Existen_las_tres_unidades_de_negocio_y_el_consumidor_final()
    {
        using var db = _bd.CrearContexto();
        Assert.Equal(3, db.UnidadesNegocio.Count());
        var cf = db.Clientes.Single(c => c.Id == Cliente.ConsumidorFinalId);
        Assert.Equal("CONSUMIDOR FINAL", cf.RazonSocial);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Crea_cliente_y_normaliza_datos()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);

        var r = await servicio.GuardarAsync(Transportista());

        Assert.True(r.Exito, string.Join(", ", r.Errores));
        var guardado = await servicio.ObtenerAsync(r.Valor);
        Assert.NotNull(guardado);
        Assert.Equal("TRANSPORTES PÉREZ", guardado!.RazonSocial);
        Assert.Equal("20123456786", guardado.NumeroDocumento);
    }

    [Fact]
    public async Task No_permite_documento_duplicado()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);

        await servicio.GuardarAsync(Transportista());
        var r = await servicio.GuardarAsync(Transportista());

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("Ya existe"));
    }

    [Fact]
    public async Task Responsable_inscripto_requiere_cuit()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);
        var datos = Transportista();
        datos.TipoDocumento = TipoDocumento.Dni;
        datos.NumeroDocumento = "12345678";

        var r = await servicio.GuardarAsync(datos);

        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("debe tener CUIT"));
    }

    [Fact]
    public async Task Cuit_con_digito_verificador_incorrecto_es_rechazado()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);
        var datos = Transportista();
        datos.NumeroDocumento = "20-12345678-0";

        var r = await servicio.GuardarAsync(datos);

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Busca_por_nombre_y_por_documento()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);
        await servicio.GuardarAsync(Transportista());

        Assert.Single(await servicio.BuscarAsync("pérez"));
        Assert.Single(await servicio.BuscarAsync("12345678"));
        Assert.Empty(await servicio.BuscarAsync("gomez"));
    }

    [Fact]
    public async Task Desactivar_oculta_de_la_busqueda_pero_no_borra()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);
        var id = (await servicio.GuardarAsync(Transportista())).Valor;

        var r = await servicio.CambiarEstadoAsync(id, activo: false);

        Assert.True(r.Exito);
        Assert.Empty(await servicio.BuscarAsync("pérez"));
        Assert.Single(await servicio.BuscarAsync("pérez", incluirInactivos: true));
    }

    [Fact]
    public async Task Consumidor_final_no_se_puede_modificar_ni_desactivar()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);

        var datos = await servicio.ObtenerAsync(Cliente.ConsumidorFinalId);
        datos!.RazonSocial = "OTRO";

        Assert.False((await servicio.GuardarAsync(datos)).Exito);
        Assert.False((await servicio.CambiarEstadoAsync(Cliente.ConsumidorFinalId, false)).Exito);
    }

    [Fact]
    public async Task Sin_cuenta_corriente_el_limite_queda_en_cero()
    {
        using var db = _bd.CrearContexto();
        var servicio = new ClienteService(db);
        var datos = Transportista();
        datos.TieneCuentaCorriente = false;

        var id = (await servicio.GuardarAsync(datos)).Valor;

        Assert.Equal(0m, (await servicio.ObtenerAsync(id))!.LimiteCredito);
    }
}
