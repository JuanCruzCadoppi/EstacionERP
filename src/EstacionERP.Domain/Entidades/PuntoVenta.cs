using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Punto de venta habilitado en ARCA para factura electrónica por web service.
/// </summary>
public class PuntoVenta : Entidad
{
    /// <summary>Número de punto de venta dado de alta en ARCA (1 a 99998).</summary>
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int UnidadNegocioId { get; set; }
    public UnidadNegocio? UnidadNegocio { get; set; }
    public bool Activo { get; set; } = true;

    // Impresión del comprobante.
    public FormatoImpresion FormatoImpresion { get; set; } = FormatoImpresion.A4;
    /// <summary>Nombre de la impresora de Windows. Vacío = impresora predeterminada.</summary>
    public string? Impresora { get; set; }
    /// <summary>Si es true, al autorizarse el comprobante se imprime solo.</summary>
    public bool ImprimirAlEmitir { get; set; } = true;
}
