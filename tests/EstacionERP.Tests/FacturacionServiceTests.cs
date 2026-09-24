using EstacionERP.Application.Configuracion;
using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Infrastructure.Persistencia;

namespace EstacionERP.Tests;

public class FacturacionServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    private readonly ArcaFalso _arca = new();
    private int _pvRepuestos;
    private int _clienteRi;

    public FacturacionServiceTests()
    {
        using var db = _bd.CrearContexto();
        db.ConfiguracionesFiscales.Add(new ConfiguracionFiscal
        {
            Id = 1, Cuit = "20123456786", RazonSocial = "ESTACION TEST", CondicionIva = CondicionIva.ResponsableInscripto,
            CertificadoPem = "x", ClavePrivadaPem = "x", Entorno = EntornoArca.Homologacion
        });
        var pv = new PuntoVenta { Numero = 2, Descripcion = "Repuestos", UnidadNegocioId = UnidadNegocio.RepuestosId };
        db.PuntosVenta.Add(pv);
        var ri = new Cliente
        {
            TipoDocumento = TipoDocumento.Cuit, NumeroDocumento = "30500010912", RazonSocial = "TRANSPORTES SA",
            CondicionIva = CondicionIva.ResponsableInscripto
        };
        db.Clientes.Add(ri);
        db.SaveChanges();
        _pvRepuestos = pv.Id;
        _clienteRi = ri.Id;
    }

    public void Dispose() => _bd.Dispose();

    private FacturacionService Servicio(EstacionDbContext db, Application.Seguridad.ISesionActual? sesion = null) =>
        new(db, sesion ?? Sesiones.Admin(), _arca, () => new DateTime(2026, 9, 24));

    private SolicitudEmision Venta(int clienteId, decimal precio = 12100m) => new()
    {
        UnidadNegocioId = UnidadNegocio.RepuestosId,
        PuntoVentaId = _pvRepuestos,
        ClienteId = clienteId,
        Items = { new ItemEmision { Descripcion = "Filtro", Cantidad = 1, PrecioUnitario = precio, AlicuotaIva = AlicuotaIva.Veintiuno } }
    };

    [Fact]
    public async Task Consumidor_final_recibe_factura_B_autorizada_y_correlativa()
    {
        using var db = _bd.CrearContexto();
        var s = Servicio(db);

        var r1 = await s.EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        var r2 = await s.EmitirAsync(Venta(Cliente.ConsumidorFinalId));

        Assert.True(r1.Exito, string.Join(", ", r1.Errores));
        Assert.Equal(EstadoComprobante.Autorizado, r1.Valor!.Estado);
        Assert.Equal(TipoComprobante.FacturaB, r1.Valor.Tipo);
        Assert.Equal("0002-00000001", r1.Valor.NumeroCompleto);
        Assert.Equal("0002-00000002", r2.Valor!.NumeroCompleto);
        Assert.NotNull(r1.Valor.Cae);

        var enviado = _arca.Solicitudes[0];
        Assert.Equal(TipoDocumento.SinIdentificar, enviado.DocTipo);
        Assert.Equal(0, enviado.DocNro);
        Assert.Equal(CondicionIva.ConsumidorFinal, enviado.CondicionIvaReceptor);
        Assert.Equal(10000m, enviado.ImporteNeto);
        Assert.Equal(2100m, enviado.ImporteIva);
    }

    [Fact]
    public async Task Responsable_inscripto_recibe_factura_A()
    {
        using var db = _bd.CrearContexto();
        var r = await Servicio(db).EmitirAsync(Venta(_clienteRi));
        Assert.Equal(TipoComprobante.FacturaA, r.Valor!.Tipo);
        Assert.Equal(30500010912, _arca.Solicitudes[0].DocNro);
    }

    [Fact]
    public async Task Sin_conexion_queda_pendiente_y_despues_se_autoriza()
    {
        using var db = _bd.CrearContexto();
        var s = Servicio(db);
        _arca.SinConexion = true;

        var r = await s.EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.True(r.Exito);
        Assert.Equal(EstadoComprobante.Pendiente, r.Valor!.Estado);
        Assert.Contains("Sin conexión", r.Valor.Mensajes);

        _arca.SinConexion = false;
        var resumen = await s.ReintentarPendientesAsync();
        Assert.Equal(1, resumen.Autorizados);
        Assert.Equal(EstadoComprobante.Autorizado, db.Comprobantes.Single().Estado);
    }

    [Fact]
    public async Task Si_se_corta_despues_de_autorizar_no_se_duplica_al_reintentar()
    {
        using var db = _bd.CrearContexto();
        var s = Servicio(db);
        _arca.CortarDespuesDeAutorizar = true;

        var r = await s.EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.Equal(EstadoComprobante.Pendiente, r.Valor!.Estado);

        var reintento = await s.ReintentarAsync(r.Valor.Id);
        Assert.Equal(EstadoComprobante.Autorizado, reintento.Valor!.Estado);
        Assert.Equal("0002-00000001", reintento.Valor.NumeroCompleto);
        Assert.Single(_arca.Solicitudes);                    // no pidió un segundo CAE
        Assert.Equal(1, _arca.Ultimos[(2, TipoComprobante.FacturaB)]);
    }

    [Fact]
    public async Task Si_otra_pc_toma_el_numero_reintenta_con_el_siguiente()
    {
        using var db = _bd.CrearContexto();
        _arca.OtrasPcEmitenAntes = 1;
        var r = await Servicio(db).EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.Equal(EstadoComprobante.Autorizado, r.Valor!.Estado);
        Assert.Equal("0002-00000002", r.Valor.NumeroCompleto);
    }

    [Fact]
    public async Task Rechazo_de_arca_queda_registrado_sin_numero()
    {
        using var db = _bd.CrearContexto();
        _arca.ErroresProximos.Enqueue(new[] { new MensajeArca(10015, "DocNro invalido") });
        var r = await Servicio(db).EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.Equal(EstadoComprobante.Rechazado, r.Valor!.Estado);
        Assert.Equal("(sin número)", r.Valor.NumeroCompleto);
        Assert.Contains("10015", r.Valor.Mensajes);
    }

    [Fact]
    public async Task Consumidor_final_sin_identificar_desde_10_millones_es_rechazado()
    {
        using var db = _bd.CrearContexto();
        var r = await Servicio(db).EmitirAsync(Venta(Cliente.ConsumidorFinalId, 10_000_000m));
        Assert.False(r.Exito);
        Assert.Empty(_arca.Solicitudes);
    }

    [Fact]
    public async Task Playero_no_puede_facturar_en_repuestos()
    {
        using var db = _bd.CrearContexto();
        var r = await Servicio(db, Sesiones.Operador(UnidadNegocio.PlayaId)).EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Servicios_de_lavadero_mandan_fechas_de_servicio()
    {
        using var db = _bd.CrearContexto();
        var pv = new PuntoVenta { Numero = 3, Descripcion = "Lavadero", UnidadNegocioId = UnidadNegocio.LavaderoId };
        db.PuntosVenta.Add(pv);
        db.SaveChanges();

        var r = await Servicio(db).EmitirAsync(new SolicitudEmision
        {
            UnidadNegocioId = UnidadNegocio.LavaderoId, PuntoVentaId = pv.Id, ClienteId = Cliente.ConsumidorFinalId,
            Items = { new ItemEmision { Descripcion = "Lavado completo", PrecioUnitario = 15000, EsServicio = true } }
        });
        Assert.Equal(EstadoComprobante.Autorizado, r.Valor!.Estado);
        var s = _arca.Solicitudes.Single();
        Assert.Equal(ConceptoComprobante.Servicios, s.Concepto);
        Assert.Equal(new DateTime(2026, 9, 24), s.FechaServicioDesde);
    }

    [Fact]
    public async Task Sin_configuracion_fiscal_no_deja_facturar()
    {
        using var db = _bd.CrearContexto();
        db.ConfiguracionesFiscales.RemoveRange(db.ConfiguracionesFiscales);
        db.SaveChanges();
        var r = await Servicio(db).EmitirAsync(Venta(Cliente.ConsumidorFinalId));
        Assert.False(r.Exito);
        Assert.Contains("datos fiscales", r.Errores[0]);
    }
}
