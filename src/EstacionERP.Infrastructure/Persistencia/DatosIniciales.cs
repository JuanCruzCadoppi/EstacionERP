namespace EstacionERP.Infrastructure.Persistencia;

internal static class DatosIniciales
{
    /// <summary>Fecha fija para los datos semilla (EF exige valores constantes).</summary>
    public static readonly DateTime FechaAlta = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
