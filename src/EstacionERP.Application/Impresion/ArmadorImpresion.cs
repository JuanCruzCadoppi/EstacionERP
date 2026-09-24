using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;

namespace EstacionERP.Application.Impresion;

/// <summary>Convierte un comprobante autorizado en los datos que se imprimen, con las leyendas que exige ARCA.</summary>
public static class ArmadorImpresion
{
    public const string LeyendaMonotributo =
        "El crédito fiscal discriminado en el presente comprobante, sólo podrá ser computado a efectos del " +
        "Régimen de Sostenimiento e Inclusión Fiscal para Pequeños Contribuyentes de la Ley Nº 27.618.";

    public static ComprobanteImprimible Armar(Comprobante c, ConfiguracionFiscal emisor)
    {
        if (c.Estado != EstadoComprobante.Autorizado || c.Numero is null || c.Cae is null || c.CaeVencimiento is null)
            throw new InvalidOperationException("Solo se imprimen comprobantes autorizados por ARCA.");

        var letra = ReglasComprobante.Letra(c.Tipo);
        var muestraNeto = letra == 'A';

        var renglones = c.Items.OrderBy(i => i.Id).Select(i => muestraNeto
            ? new RenglonImprimible(i.Codigo, i.Descripcion, i.Cantidad,
                i.Cantidad == 0 ? 0 : Math.Round(i.ImporteNeto / i.Cantidad, 2), i.AlicuotaIva.Texto(), i.ImporteNeto)
            : new RenglonImprimible(i.Codigo, i.Descripcion, i.Cantidad, i.PrecioUnitario, null, i.ImporteTotal)).ToList();

        var leyendas = new List<string>();
        if (letra == 'A' && c.ReceptorCondicionIva is CondicionIva.Monotributo or CondicionIva.MonotributistaSocial)
            leyendas.Add(LeyendaMonotributo);

        var tieneDoc = c.ReceptorTipoDocumento != TipoDocumento.SinIdentificar && c.ReceptorNumeroDocumento != "0";

        return new ComprobanteImprimible
        {
            EmisorRazonSocial = emisor.RazonSocial,
            EmisorNombreFantasia = emisor.NombreFantasia,
            EmisorCuit = DocumentoValidador.FormatearCuit(emisor.Cuit),
            EmisorCondicionIva = emisor.CondicionIva.Texto(),
            EmisorDomicilio = string.Join(" - ", new[] { emisor.Domicilio, emisor.Localidad }.Where(x => !string.IsNullOrWhiteSpace(x))),
            EmisorIngresosBrutos = emisor.IngresosBrutos,
            EmisorInicioActividades = emisor.InicioActividades,

            Tipo = c.Tipo,
            TipoTexto = TextoTipo(c.Tipo),
            Letra = letra,
            Codigo = (int)c.Tipo,
            PuntoVenta = c.PuntoVentaNumero,
            Numero = c.Numero.Value,
            Fecha = c.Fecha,
            EsServicio = c.Concepto != ConceptoComprobante.Productos,
            ServicioDesde = c.FechaServicioDesde,
            ServicioHasta = c.FechaServicioHasta,
            VencimientoPago = c.FechaVencimientoPago,

            ReceptorNombre = c.ReceptorRazonSocial,
            ReceptorDocumentoTipo = c.ReceptorTipoDocumento.TextoCorto(),
            ReceptorDocumento = !tieneDoc ? null
                : c.ReceptorTipoDocumento is TipoDocumento.Cuit or TipoDocumento.Cuil
                    ? DocumentoValidador.FormatearCuit(c.ReceptorNumeroDocumento)
                    : c.ReceptorNumeroDocumento,
            ReceptorCondicionIva = c.ReceptorCondicionIva.Texto(),
            ReceptorDomicilio = c.ReceptorDomicilio,

            MuestraNeto = muestraNeto,
            Renglones = renglones,
            Neto = c.ImporteNeto,
            Iva = c.Alicuotas.OrderBy(a => a.AlicuotaIva.Porcentaje()).Select(a => (a.AlicuotaIva.Texto(), a.Importe)).ToList(),
            OtrosTributos = c.ImporteTributos,
            Total = c.ImporteTotal,

            // Ley 27.743: en B se informa el IVA contenido en el precio (y otros impuestos nacionales indirectos).
            TransparenciaIvaContenido = letra == 'B' ? c.ImporteIva : null,
            TransparenciaOtrosImpuestos = 0m,

            Leyendas = leyendas,
            Cae = c.Cae,
            CaeVencimiento = c.CaeVencimiento.Value,
            QrUrl = QrComprobante.GenerarUrl(emisor.Cuit, c),
            EsDePrueba = c.Entorno == EntornoArca.Homologacion
        };
    }

    private static string TextoTipo(TipoComprobante t) => t switch
    {
        TipoComprobante.FacturaA or TipoComprobante.FacturaB or TipoComprobante.FacturaC => "FACTURA",
        TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC => "NOTA DE CRÉDITO",
        _ => "NOTA DE DÉBITO"
    };
}
