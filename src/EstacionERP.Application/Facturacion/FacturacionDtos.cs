using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Facturacion;

public class ItemEmision
{
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    /// <summary>Precio unitario final, con IVA.</summary>
    public decimal PrecioUnitario { get; set; }
    public AlicuotaIva AlicuotaIva { get; set; } = AlicuotaIva.Veintiuno;
    public bool EsServicio { get; set; }
}

public class SolicitudEmision
{
    public int UnidadNegocioId { get; set; }
    public int PuntoVentaId { get; set; }
    public int ClienteId { get; set; }
    public List<ItemEmision> Items { get; set; } = new();
}

public record ComprobanteEmitido(
    int Id,
    EstadoComprobante Estado,
    TipoComprobante Tipo,
    string NumeroCompleto,
    decimal Total,
    string? Cae,
    DateTime? CaeVencimiento,
    string? Mensajes);

public record ComprobanteResumen(
    int Id,
    DateTime Fecha,
    string Tipo,
    string Numero,
    string UnidadNegocio,
    string Cliente,
    decimal Total,
    EstadoComprobante Estado,
    string EstadoTexto,
    string? Cae,
    DateTime? CaeVencimiento,
    string? Mensajes,
    string Entorno);

public record ResultadoReintentos(int Autorizados, int Rechazados, int SiguenPendientes);
