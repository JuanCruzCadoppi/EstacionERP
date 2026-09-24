namespace EstacionERP.Domain.Common;

/// <summary>
/// Clase base de todas las entidades persistidas.
/// </summary>
public abstract class Entidad
{
    public int Id { get; set; }

    /// <summary>Fecha de alta (UTC).</summary>
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    /// <summary>Última modificación (UTC).</summary>
    public DateTime? ModificadoEn { get; set; }
}
