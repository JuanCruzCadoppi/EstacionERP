using EstacionERP.Domain.Common;

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
}
