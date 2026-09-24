using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

public class Vehiculo : Entidad
{
    /// <summary>Patente en mayúsculas y sin espacios (ej. AB123CD).</summary>
    public string Patente { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public TipoVehiculo Tipo { get; set; } = TipoVehiculo.Auto;

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}
