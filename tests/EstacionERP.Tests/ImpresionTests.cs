using EstacionERP.Application.Impresion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Infrastructure.Impresion;

namespace EstacionERP.Tests;

public class ImpresionTests
{
    private static ConfiguracionFiscal Emisor() => new()
    {
        Id = 1, Cuit = "20454832400", RazonSocial = "JUAN CRUZ CADOPPI", NombreFantasia = "ESTACIÓN LAGUNA LARGA",
        CondicionIva = CondicionIva.ResponsableInscripto, Domicilio = "Ruta 9 Km 640", Localidad = "Laguna Larga",
        IngresosBrutos = "280-123456-7", InicioActividades = new DateTime(2020, 3, 1)
    };

    public static Comprobante Comprobante(TipoComprobante tipo, CondicionIva receptor, int items = 3, bool servicio = false)
    {
        var c = new Comprobante
        {
            Id = 1, Tipo = tipo, PuntoVentaNumero = 1, Numero = 25, Fecha = new DateTime(2026, 9, 24),
            Concepto = servicio ? ConceptoComprobante.Servicios : ConceptoComprobante.Productos,
            FechaServicioDesde = servicio ? new DateTime(2026, 9, 24) : null,
            FechaServicioHasta = servicio ? new DateTime(2026, 9, 24) : null,
            FechaVencimientoPago = servicio ? new DateTime(2026, 9, 24) : null,
            ReceptorTipoDocumento = receptor == CondicionIva.ConsumidorFinal ? TipoDocumento.SinIdentificar : TipoDocumento.Cuit,
            ReceptorNumeroDocumento = receptor == CondicionIva.ConsumidorFinal ? "0" : "20111111112",
            ReceptorRazonSocial = receptor == CondicionIva.ConsumidorFinal ? "CONSUMIDOR FINAL" : "TRANSPORTES DEL CENTRO S.R.L.",
            ReceptorCondicionIva = receptor,
            ReceptorDomicilio = receptor == CondicionIva.ConsumidorFinal ? null : "Av. San Martín 1234, Laguna Larga, Córdoba",
            Estado = EstadoComprobante.Autorizado, Cae = "86390924716382", CaeVencimiento = new DateTime(2026, 10, 4),
            Entorno = EntornoArca.Homologacion
        };
        var nombres = new[] { "Lavado completo con aspirado y siliconado de interiores", "Filtro de aceite Fram PH5949", "Aceite Elaion F50 5W-40 4 litros", "Escobilla limpiaparabrisas 22\"" };
        long id = 1;
        for (var i = 0; i < items; i++)
        {
            var precio = new[] { 15000m, 12100m, 48399.99m, 8500m }[i % 4];
            var cant = i % 3 == 2 ? 2m : 1m;
            var total = precio * cant;
            var neto = Math.Round(total / 1.21m, 2);
            c.Items.Add(new ComprobanteItem { Id = (int)id++, Codigo = $"P{i + 1:000}", Descripcion = nombres[i % 4], Cantidad = cant, PrecioUnitario = precio,
                AlicuotaIva = AlicuotaIva.Veintiuno, ImporteNeto = neto, ImporteIva = total - neto, ImporteTotal = total });
        }
        c.ImporteTotal = c.Items.Sum(i => i.ImporteTotal);
        if (tipo == TipoComprobante.FacturaC)
        {
            c.ImporteNeto = c.ImporteTotal;
        }
        else
        {
            c.ImporteNeto = Math.Round(c.ImporteTotal / 1.21m, 2);
            c.ImporteIva = c.ImporteTotal - c.ImporteNeto;
            c.Alicuotas.Add(new ComprobanteIva { AlicuotaIva = AlicuotaIva.Veintiuno, BaseImponible = c.ImporteNeto, Importe = c.ImporteIva });
        }
        return c;
    }

    [Fact]
    public void Leyendas_segun_tipo_y_receptor()
    {
        var a = ArmadorImpresion.Armar(Comprobante(TipoComprobante.FacturaA, CondicionIva.Monotributo), Emisor());
        Assert.Contains(ArmadorImpresion.LeyendaMonotributo, a.Leyendas);
        Assert.Null(a.TransparenciaIvaContenido);
        Assert.True(a.MuestraNeto);

        var b = ArmadorImpresion.Armar(Comprobante(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal), Emisor());
        Assert.Empty(b.Leyendas);
        Assert.Equal(b.Total - b.Neto, b.TransparenciaIvaContenido);
        Assert.Null(b.ReceptorDocumento);
        Assert.False(b.MuestraNeto);

        var ri = ArmadorImpresion.Armar(Comprobante(TipoComprobante.FacturaA, CondicionIva.ResponsableInscripto), Emisor());
        Assert.Empty(ri.Leyendas);
        Assert.Equal("20-11111111-2", ri.ReceptorDocumento);
    }

    [Fact]
    public void No_imprime_comprobantes_sin_cae()
    {
        var c = Comprobante(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal);
        c.Estado = EstadoComprobante.Pendiente;
        Assert.Throws<InvalidOperationException>(() => ArmadorImpresion.Armar(c, Emisor()));
    }

    [Theory]
    [InlineData(TipoComprobante.FacturaA, CondicionIva.Monotributo, 4, false, FormatoImpresion.A4, "a4-factura-a")]
    [InlineData(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal, 1, true, FormatoImpresion.A4, "a4-factura-b")]
    [InlineData(TipoComprobante.FacturaA, CondicionIva.ResponsableInscripto, 4, false, FormatoImpresion.Ticket80, "ticket-factura-a")]
    [InlineData(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal, 1, true, FormatoImpresion.Ticket80, "ticket-factura-b")]
    [InlineData(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal, 60, false, FormatoImpresion.A4, "a4-muchos-items")]
    public void Genera_pdf(TipoComprobante tipo, CondicionIva receptor, int items, bool servicio, FormatoImpresion formato, string nombre)
    {
        var datos = ArmadorImpresion.Armar(Comprobante(tipo, receptor, items, servicio), Emisor());
        var pdf = new GeneradorComprobantePdf().Generar(datos, formato);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

        var carpeta = Environment.GetEnvironmentVariable("ESTACION_MUESTRAS_PDF");
        if (!string.IsNullOrWhiteSpace(carpeta))
            File.WriteAllBytes(Path.Combine(carpeta, nombre + ".pdf"), pdf);
    }
}

public class ImpresionServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    [Fact]
    public async Task Usa_el_formato_del_punto_de_venta_y_solo_imprime_autorizados()
    {
        using var db = _bd.CrearContexto();
        db.ConfiguracionesFiscales.Add(new ConfiguracionFiscal
        {
            Id = 1, Cuit = "20454832400", RazonSocial = "PRUEBA", CondicionIva = CondicionIva.ResponsableInscripto,
            CertificadoPem = "x", ClavePrivadaPem = "x"
        });
        var pv = new PuntoVenta { Numero = 1, Descripcion = "Lavadero", UnidadNegocioId = UnidadNegocio.LavaderoId,
            FormatoImpresion = FormatoImpresion.Ticket80, Impresora = "TERMICA", ImprimirAlEmitir = true };
        db.PuntosVenta.Add(pv);
        db.SaveChanges();

        var arca = new ArcaFalso();
        var fact = new Application.Facturacion.FacturacionService(db, Sesiones.Admin(), arca, () => new DateTime(2026, 9, 24));
        var emitido = await fact.EmitirAsync(new Application.Facturacion.SolicitudEmision
        {
            UnidadNegocioId = UnidadNegocio.LavaderoId, PuntoVentaId = pv.Id, ClienteId = Cliente.ConsumidorFinalId,
            Items = { new Application.Facturacion.ItemEmision { Descripcion = "Lavado", PrecioUnitario = 15000, EsServicio = true } }
        });
        arca.SinConexion = true;
        var pendiente = await fact.EmitirAsync(new Application.Facturacion.SolicitudEmision
        {
            UnidadNegocioId = UnidadNegocio.LavaderoId, PuntoVentaId = pv.Id, ClienteId = Cliente.ConsumidorFinalId,
            Items = { new Application.Facturacion.ItemEmision { Descripcion = "Lavado", PrecioUnitario = 15000, EsServicio = true } }
        });

        var s = new ImpresionService(db, Sesiones.Admin(), new GeneradorComprobantePdf());
        var r = await s.PrepararAsync(emitido.Valor!.Id);
        Assert.True(r.Exito, string.Join(", ", r.Errores));
        Assert.Equal(FormatoImpresion.Ticket80, r.Valor!.Formato);
        Assert.Equal("TERMICA", r.Valor.Impresora);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(r.Valor.Pdf, 0, 4));

        var a4 = await s.PrepararAsync(emitido.Valor.Id, FormatoImpresion.A4);
        Assert.Equal(FormatoImpresion.A4, a4.Valor!.Formato);

        Assert.False((await s.PrepararAsync(pendiente.Valor!.Id)).Exito);

        // Un operador de otra unidad no puede imprimir comprobantes del lavadero.
        var ajeno = new ImpresionService(db, Sesiones.Operador(UnidadNegocio.PlayaId), new GeneradorComprobantePdf());
        Assert.False((await ajeno.PrepararAsync(emitido.Valor.Id)).Exito);
    }
}
