using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Ticket de acceso (TA) que devuelve el WSAA. Dura unas 12 horas y se reutiliza.
/// Se guarda en la base para que todas las PCs usen el mismo (ARCA no da otro mientras haya uno vigente).
/// </summary>
public class TicketAcceso : Entidad
{
    public string Servicio { get; set; } = string.Empty;
    public EntornoArca Entorno { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
}
