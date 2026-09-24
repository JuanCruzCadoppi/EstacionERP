using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Comprobante fiscal (factura, nota de crédito o débito).
/// Nunca se borra: si hay un error se emite una nota de crédito.
/// </summary>
public class Comprobante : Entidad
{
    public int UnidadNegocioId { get; set; }
    public UnidadNegocio? UnidadNegocio { get; set; }

    public int PuntoVentaId { get; set; }
    public PuntoVenta? PuntoVenta { get; set; }
    /// <summary>Copia del número de punto de venta al momento de emitir.</summary>
    public int PuntoVentaNumero { get; set; }

    public TipoComprobante Tipo { get; set; }

    /// <summary>Número definitivo; se asigna cuando ARCA lo autoriza.</summary>
    public long? Numero { get; set; }

    /// <summary>Número con el que se pidió el CAE (para recuperar si se cortó la conexión).</summary>
    public long? NumeroIntentado { get; set; }

    public DateTime Fecha { get; set; }
    public ConceptoComprobante Concepto { get; set; } = ConceptoComprobante.Productos;
    public DateTime? FechaServicioDesde { get; set; }
    public DateTime? FechaServicioHasta { get; set; }
    public DateTime? FechaVencimientoPago { get; set; }

    // Receptor (copia de los datos al momento de emitir).
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public TipoDocumento ReceptorTipoDocumento { get; set; }
    public string ReceptorNumeroDocumento { get; set; } = string.Empty;
    public string ReceptorRazonSocial { get; set; } = string.Empty;
    public CondicionIva ReceptorCondicionIva { get; set; }
    public string? ReceptorDomicilio { get; set; }

    // Importes.
    public decimal ImporteNeto { get; set; }
    public decimal ImporteIva { get; set; }
    public decimal ImporteNoGravado { get; set; }
    public decimal ImporteExento { get; set; }
    public decimal ImporteTributos { get; set; }
    public decimal ImporteTotal { get; set; }

    // Resultado ARCA.
    public EstadoComprobante Estado { get; set; } = EstadoComprobante.Pendiente;
    public string? Cae { get; set; }
    public DateTime? CaeVencimiento { get; set; }
    public EntornoArca Entorno { get; set; }
    public string? MensajesArca { get; set; }
    public int Intentos { get; set; }

    /// <summary>Para notas de crédito/débito: comprobante que ajustan.</summary>
    public int? ComprobanteAsociadoId { get; set; }
    public Comprobante? ComprobanteAsociado { get; set; }

    public int UsuarioId { get; set; }

    public ICollection<ComprobanteItem> Items { get; set; } = new List<ComprobanteItem>();
    public ICollection<ComprobanteIva> Alicuotas { get; set; } = new List<ComprobanteIva>();

    /// <summary>Ej.: 0001-00000023</summary>
    public string NumeroCompleto => Numero is null ? "(sin número)" : $"{PuntoVentaNumero:D4}-{Numero:D8}";
}

public class ComprobanteItem : Entidad
{
    public int ComprobanteId { get; set; }
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    /// <summary>Precio unitario final (con IVA).</summary>
    public decimal PrecioUnitario { get; set; }
    public AlicuotaIva AlicuotaIva { get; set; }
    public bool EsServicio { get; set; }
    public decimal ImporteNeto { get; set; }
    public decimal ImporteIva { get; set; }
    public decimal ImporteTotal { get; set; }
}

/// <summary>Subtotales por alícuota (lo que se informa a ARCA y va al libro IVA).</summary>
public class ComprobanteIva : Entidad
{
    public int ComprobanteId { get; set; }
    public AlicuotaIva AlicuotaIva { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Importe { get; set; }
}
