using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Impresion;

/// <summary>Todo lo que se imprime en la representación gráfica del comprobante (ya formateado).</summary>
public record ComprobanteImprimible
{
    // Emisor
    public required string EmisorRazonSocial { get; init; }
    public string? EmisorNombreFantasia { get; init; }
    public required string EmisorCuit { get; init; }
    public required string EmisorCondicionIva { get; init; }
    public string? EmisorDomicilio { get; init; }
    public string? EmisorIngresosBrutos { get; init; }
    public DateTime? EmisorInicioActividades { get; init; }

    // Comprobante
    public required TipoComprobante Tipo { get; init; }
    public required string TipoTexto { get; init; }        // "FACTURA"
    public required char Letra { get; init; }
    public required int Codigo { get; init; }              // 1, 6, 11...
    public required int PuntoVenta { get; init; }
    public required long Numero { get; init; }
    public required DateTime Fecha { get; init; }
    public bool EsServicio { get; init; }
    public DateTime? ServicioDesde { get; init; }
    public DateTime? ServicioHasta { get; init; }
    public DateTime? VencimientoPago { get; init; }

    // Receptor
    public required string ReceptorNombre { get; init; }
    public required string ReceptorDocumentoTipo { get; init; }
    public string? ReceptorDocumento { get; init; }
    public required string ReceptorCondicionIva { get; init; }
    public string? ReceptorDomicilio { get; init; }

    // Detalle
    /// <summary>true en A: precios sin IVA, se muestra el IVA por alícuota.</summary>
    public required bool MuestraNeto { get; init; }
    public required IReadOnlyList<RenglonImprimible> Renglones { get; init; }
    public decimal Neto { get; init; }
    public IReadOnlyList<(string Alicuota, decimal Importe)> Iva { get; init; } = Array.Empty<(string, decimal)>();
    public decimal OtrosTributos { get; init; }
    public required decimal Total { get; init; }

    /// <summary>Régimen de Transparencia Fiscal al Consumidor (Ley 27.743). null = no corresponde.</summary>
    public decimal? TransparenciaIvaContenido { get; init; }
    public decimal TransparenciaOtrosImpuestos { get; init; }

    public IReadOnlyList<string> Leyendas { get; init; } = Array.Empty<string>();

    // Autorización
    public required string Cae { get; init; }
    public required DateTime CaeVencimiento { get; init; }
    public required string QrUrl { get; init; }
    public bool EsDePrueba { get; init; }

    public string NumeroCompleto => $"{PuntoVenta:D5}-{Numero:D8}";
    public string NombreArchivo => $"{TipoTexto} {Letra} {PuntoVenta:D5}-{Numero:D8}.pdf";
}

public record RenglonImprimible(
    string? Codigo,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    string? Alicuota,
    decimal Subtotal);

public interface IGeneradorComprobantePdf
{
    byte[] Generar(ComprobanteImprimible c, FormatoImpresion formato);
}
