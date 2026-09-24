using EstacionERP.Domain.Common;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Cada negocio de la estación: Playa, Repuestos, Lavadero.
/// Todo movimiento (venta, stock, caja) queda asociado a una unidad de negocio.
/// </summary>
public class UnidadNegocio : Entidad
{
    public const int PlayaId = 1;
    public const int RepuestosId = 2;
    public const int LavaderoId = 3;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    public ICollection<PuntoVenta> PuntosVenta { get; set; } = new List<PuntoVenta>();
}
